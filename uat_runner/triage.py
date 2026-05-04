"""UAT failure triage — classifies runtime failures into one of four classes.

Failure taxonomy (compile-time failures are already eliminated by W0):

  APP_FUNCTIONAL_BUG  — A VERIFY step's user-visible expectation did not match
                        the observed state. The system under test has a bug.
                        Never auto-fixed. Default when uncertain.
  CATALOG_STALE       — A locator that compiled successfully now finds nothing;
                        the app changed since the catalog was generated.
                        Auto-applies a targeted re-crawl.
  SEED_DATA_GAP       — A step requires test data that wasn't seeded (e.g.,
                        "click row where customer = X" but X doesn't exist).
                        Files a human-review issue.
  AMBIGUOUS_LOCATOR   — A locator matched multiple elements; needs a
                        data-testid discriminator.
                        Files a disambiguation issue for human review.

Design invariants:
  - APP_FUNCTIONAL_BUG is never auto-fixed by modifying the checklist or
    the system under test. It is always escalated to human review.
  - If triage is uncertain between APP_FUNCTIONAL_BUG and CATALOG_STALE,
    it defaults to APP_FUNCTIONAL_BUG (preserves the bug-detection signal).
  - Cost cap: at most MAX_TRIAGE_CALLS calls per runner execution, and at
    most MAX_TRIAGE_COST_USD total spend. Beyond the cap, remaining failures
    are added to the report with classification="BUDGET_EXCEEDED" and
    outcome="human-review-required".
"""

from __future__ import annotations

import json
import re
from dataclasses import dataclass, field
from typing import Literal


# Matches Playwright's strict-mode-violation message when a locator
# resolved to N>1 elements. Captures N. Tolerant of trailing content
# (test runners sometimes append the page state or selector).
_AMBIGUOUS_LOCATOR_RE = re.compile(
    r"strict mode violation:.*?resolved to (\d+) elements?",
    re.IGNORECASE | re.DOTALL,
)


def _detect_ambiguous_locator(playwright_error: str) -> int | None:
    """Return the matched element count if the error is a strict-mode
    violation with N>1 elements, otherwise None.

    The strict-mode-violation pattern with N>1 is a deterministic
    AMBIGUOUS_LOCATOR signal — the live app has multiple elements with
    the same visible label and a data-testid discriminator is needed.
    No LLM judgement required.
    """
    if not playwright_error:
        return None
    match = _AMBIGUOUS_LOCATOR_RE.search(playwright_error)
    if not match:
        return None
    try:
        count = int(match.group(1))
    except (TypeError, ValueError):
        return None
    return count if count > 1 else None

SESSION_SET = "docs/session-sets/uat-runner-and-triage-feedback"

MAX_TRIAGE_CALLS = 3
MAX_TRIAGE_COST_USD = 0.20

Classification = Literal[
    "APP_FUNCTIONAL_BUG",
    "CATALOG_STALE",
    "SEED_DATA_GAP",
    "AMBIGUOUS_LOCATOR",
    "BUDGET_EXCEEDED",
    "PARSE_ERROR",
]


@dataclass
class FailureRecord:
    """Structured representation of a single test-step failure.

    ``step_number`` and ``step_text`` come from the UAT checklist.
    The remaining fields come from the .trx / Playwright trace artifacts
    (extracted by ``runner.py``).
    """

    step_number: int
    step_text: str
    playwright_error: str
    current_url: str = ""
    console_errors: list[str] = field(default_factory=list)
    screenshot_path: str = ""
    dom_snapshot_path: str = ""
    trx_file: str = ""


@dataclass
class TriageResult:
    failure: FailureRecord
    classification: Classification
    confidence: float  # 0.0–1.0
    evidence: str
    proposed_fix: str = ""
    triage_cost_usd: float = 0.0
    raw_response: str = ""
    skipped_reason: str = ""  # non-empty if classification is BUDGET_EXCEEDED / PARSE_ERROR


class TriageSession:
    """Stateful triage coordinator that enforces cost caps across one runner run.

    Usage::

        ts = TriageSession(session_set="docs/session-sets/my-set", session_number=2)
        for failure in run_report.failed:
            result = ts.classify(failure)
            ...
        print(ts.total_cost_usd)
    """

    def __init__(
        self,
        session_set: str = SESSION_SET,
        session_number: int | None = None,
        catalog_path: str = "",
    ) -> None:
        self._session_set = session_set
        self._session_number = session_number
        self._catalog_path = catalog_path
        self._calls = 0
        self._total_cost = 0.0

    @property
    def total_cost_usd(self) -> float:
        return self._total_cost

    @property
    def calls_made(self) -> int:
        return self._calls

    def _budget_ok(self) -> bool:
        return (
            self._calls < MAX_TRIAGE_CALLS
            and self._total_cost < MAX_TRIAGE_COST_USD
        )

    def classify(
        self,
        failure: FailureRecord,
        catalog_entry: str = "",
    ) -> TriageResult:
        """Classify a single failure. Returns BUDGET_EXCEEDED if over cap.

        ``catalog_entry`` is the relevant section of ``index.json`` for the
        route × role in which the failure occurred. Pass it when available;
        the model uses it to detect CATALOG_STALE drift.
        """
        # Deterministic short-circuit — Playwright's "strict mode violation
        # ... resolved to N elements. Expected: 1." with N > 1 is a literal
        # AMBIGUOUS_LOCATOR signal. The LLM classifies this correctly most
        # of the time but is non-deterministic at low confidence; a regex
        # short-circuit removes the variance for free and saves an API call.
        ambiguous = _detect_ambiguous_locator(failure.playwright_error)
        if ambiguous is not None:
            return TriageResult(
                failure=failure,
                classification="AMBIGUOUS_LOCATOR",
                confidence=1.0,
                evidence=(
                    f"Playwright strict-mode-violation pattern: locator "
                    f"resolved to {ambiguous} elements (>1). "
                    "A data-testid discriminator is needed."
                ),
                proposed_fix="Add a data-testid attribute to disambiguate the locator.",
                triage_cost_usd=0.0,
                skipped_reason="deterministic_pre_filter",
            )

        if not self._budget_ok():
            return TriageResult(
                failure=failure,
                classification="BUDGET_EXCEEDED",
                confidence=1.0,
                evidence=(
                    f"Triage budget exceeded: {self._calls} calls / "
                    f"${self._total_cost:.4f} spent."
                ),
                skipped_reason="budget_exceeded",
            )

        # Late import to avoid circular dependency with the router module.
        import ai_router as ar

        failure_json = json.dumps(
            {
                "stepNumber": failure.step_number,
                "stepText": failure.step_text,
                "playwrightError": failure.playwright_error,
                "currentUrl": failure.current_url,
                "consoleErrors": failure.console_errors,
                "screenshotPath": failure.screenshot_path,
                "domSnapshotPath": failure.dom_snapshot_path,
                "trxFile": failure.trx_file,
            },
            indent=2,
        )

        context_parts = ["## Failure record\n\n```json\n", failure_json, "\n```"]
        if catalog_entry:
            context_parts += [
                "\n\n## Catalog entry for the affected route × role\n\n```json\n",
                catalog_entry,
                "\n```",
            ]

        # Increment call counter BEFORE the API call so that failures
        # (network error, rate limit, timeout) still consume budget.
        # A failed call costs the latency; letting it bypass the cap would
        # allow unlimited retries on a flaky API.
        self._calls += 1
        try:
            result = ar.route(
                content=failure_json,
                task_type="uat-failure-triage",
                context="".join(context_parts),
                session_set=self._session_set,
                session_number=self._session_number,
            )
        except Exception as exc:
            return TriageResult(
                failure=failure,
                classification="APP_FUNCTIONAL_BUG",
                confidence=0.0,
                evidence=f"Triage call failed: {exc}. Defaulting to APP_FUNCTIONAL_BUG.",
                skipped_reason=f"api_error: {exc}",
            )

        self._total_cost += result.cost_usd

        raw = result.content.strip()
        return _parse_triage_response(failure, raw, result.cost_usd)


def _parse_triage_response(
    failure: FailureRecord,
    raw: str,
    cost_usd: float,
) -> TriageResult:
    """Parse the model's JSON triage response into a TriageResult.

    Expected shape::

        {
          "classification": "APP_FUNCTIONAL_BUG",
          "confidence": 0.95,
          "evidence": "The VERIFY TEXT step expected 'XXX-XX-...' but the page shows '123-45-6789'.",
          "proposed_fix": ""
        }

    On parse failure, defaults to APP_FUNCTIONAL_BUG.
    """
    # Strip markdown fences if the model wrapped the JSON.
    text = raw
    if "```" in text:
        lines = text.splitlines()
        in_block = False
        kept = []
        for line in lines:
            stripped = line.strip()
            if stripped.startswith("```"):
                in_block = not in_block
                continue
            if in_block:
                kept.append(line)
        text = "\n".join(kept).strip()

    valid_classes: set[Classification] = {
        "APP_FUNCTIONAL_BUG",
        "CATALOG_STALE",
        "SEED_DATA_GAP",
        "AMBIGUOUS_LOCATOR",
    }

    try:
        obj = json.loads(text)
        classification = obj.get("classification", "APP_FUNCTIONAL_BUG")
        if classification not in valid_classes:
            classification = "APP_FUNCTIONAL_BUG"
        return TriageResult(
            failure=failure,
            classification=classification,
            confidence=float(obj.get("confidence", 0.5)),
            evidence=str(obj.get("evidence", "")),
            proposed_fix=str(obj.get("proposed_fix", "")),
            triage_cost_usd=cost_usd,
            raw_response=raw,
        )
    except (json.JSONDecodeError, ValueError):
        return TriageResult(
            failure=failure,
            classification="APP_FUNCTIONAL_BUG",
            confidence=0.0,
            evidence=(
                "Could not parse triage response as JSON. "
                "Defaulting to APP_FUNCTIONAL_BUG to preserve bug-detection signal."
            ),
            raw_response=raw,
            triage_cost_usd=cost_usd,
            skipped_reason="parse_error",
        )


def classify_failure(
    failure: FailureRecord,
    catalog_entry: str = "",
    session_set: str = SESSION_SET,
    session_number: int | None = None,
) -> TriageResult:
    """Convenience wrapper that creates a single-use TriageSession and classifies one failure.

    For multi-failure runs, prefer constructing a ``TriageSession`` directly so
    the cost cap is enforced across all failures.
    """
    ts = TriageSession(
        session_set=session_set,
        session_number=session_number,
        catalog_path="",
    )
    return ts.classify(failure, catalog_entry=catalog_entry)


def apply_remediation(
    result: TriageResult,
    session_set_slug: str = "uat-runner-and-triage-feedback",
    skip_real_crawl: bool = False,
    base_url: str = "http://localhost:5010",
) -> dict:
    """Apply the appropriate remediation based on classification.

    Args:
        result: A TriageResult with a classification (one of the four classes).
        session_set_slug: The session-set slug (used for issue-file paths).
        skip_real_crawl: When True, CATALOG_STALE remediations return a
            "deferred" outcome instead of invoking the multi-minute
            full-catalog re-crawl. Used by unit tests and synthetic
            failure suites.
        base_url: Web app base URL used by CATALOG_STALE precondition
            check. Ignored unless skip_real_crawl is False.

    Returns:
        A dict with keys {type, stepNumber, classification, outcome, issueFile?, ...}
        describing the remediation applied.

    Remediations:
      - APP_FUNCTIONAL_BUG: Files a human-review issue.
      - CATALOG_STALE: Auto-applies a full catalog re-crawl + locator
        sniff (deferred when skip_real_crawl=True or preconditions
        unmet).
      - SEED_DATA_GAP: Files a human-review issue.
      - AMBIGUOUS_LOCATOR: Files a human-review issue.
      - BUDGET_EXCEEDED / PARSE_ERROR: Files a human-review issue.
    """
    # Late import to avoid circular dependencies
    from . import remediations

    classification = result.classification

    if classification == "APP_FUNCTIONAL_BUG":
        return remediations.app_functional_bug.file_issue(result, session_set_slug)
    elif classification == "CATALOG_STALE":
        return remediations.catalog_stale.apply(
            result, base_url=base_url, skip_real_crawl=skip_real_crawl
        )
    elif classification == "SEED_DATA_GAP":
        return remediations.seed_data_gap.file_issue(result, session_set_slug)
    elif classification == "AMBIGUOUS_LOCATOR":
        return remediations.ambiguous_locator.file_issue(result, session_set_slug)
    elif classification == "BUDGET_EXCEEDED":
        return remediations.app_functional_bug.file_issue(result, session_set_slug)
    elif classification == "PARSE_ERROR":
        return remediations.app_functional_bug.file_issue(result, session_set_slug)
    else:
        # Catch-all for unrecognized classifications
        return {
            "type": "uat-runner-failure",
            "stepNumber": result.failure.step_number,
            "classification": classification,
            "autoApplied": False,
            "outcome": "human-review-required",
            "reason": f"Unhandled or unexpected classification: {classification}",
        }
