"""CATALOG_STALE remediation — re-crawl the affected component and re-run.

When a locator that compiled successfully finds nothing at runtime, the
component catalog is stale relative to the live app. This remediation:

  1. Invokes ``tools/ComponentCatalogCrawler/`` to re-crawl. The crawler
     today rebuilds the full catalog (it does not yet support a
     ``--route X --role Y`` targeted scope); see the
     ``targeted-recrawl`` follow-up note below for the optimization.
  2. The full re-crawl rewrites ``docs/component-catalog/index.json`` in
     place. The original catalog is preserved at ``index.json.bak`` so
     the runner can roll back on crawl failure.
  3. Confirms the affected step's locator now resolves by sniffing
     ``index.json`` for the failure's ``step_text`` literal label.
  4. Returns ``outcome="auto-recovered"`` if the locator now exists,
     ``outcome="auto-recovery-failed"`` otherwise. The runner is
     responsible for actually re-running the failing test (the runner
     owns the dotnet-test invocation; this module only updates the
     catalog and reports whether the locator is back).

Preconditions checked at apply time. If any are absent the remediation
returns ``outcome="auto-recovery-deferred"`` so unit tests and CI smoke
runs do not hang on missing infrastructure:

  - The crawler project exists at ``tools/ComponentCatalogCrawler``.
  - The Web app is reachable on ``base_url`` (default
    ``http://localhost:5010``).
  - ``DOTNET_EXECUTABLE`` is on PATH.

Follow-up (out-of-scope for this set):
  - Targeted re-crawl: extend ``ComponentCatalogCrawler`` to accept a
    ``--route`` + ``--role`` filter so a single failure does not trigger
    a multi-minute full crawl.
"""

from __future__ import annotations

import json
import shutil
import subprocess
import urllib.error
import urllib.request
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
CATALOG_PATH = REPO_ROOT / "docs/component-catalog/index.json"
CATALOG_BACKUP_PATH = REPO_ROOT / "docs/component-catalog/index.json.bak"
COMPONENT_CRAWLER = REPO_ROOT / "tools/ComponentCatalogCrawler"
DEFAULT_BASE_URL = "http://localhost:5010"
CRAWLER_TIMEOUT_SECONDS = 600  # 10 minutes — full crawl runs ~3-5 min in dev


def apply(
    triage_result: object,
    runner_module: object = None,
    base_url: str = DEFAULT_BASE_URL,
    skip_real_crawl: bool = False,
) -> dict:
    """Apply CATALOG_STALE remediation and return outcome record.

    Args:
        triage_result: A TriageResult with classification="CATALOG_STALE".
        runner_module: Reserved (unused; kept for back-compat with stub).
        base_url: Web app URL to probe before invoking the crawler.
        skip_real_crawl: When True, returns
            ``outcome="auto-recovery-deferred"`` without invoking the
            crawler. Used by unit tests and CI smoke runs.

    Returns:
        A dict with keys: type, stepNumber, classification, autoApplied,
        outcome, reason, plus optional crawlerExitCode / crawlerStdoutTail
        when a real crawl ran.
    """
    failure = triage_result.failure  # type: ignore[attr-defined]
    step_number = failure.step_number

    base_outcome: dict = {
        "type": "uat-runner-failure",
        "stepNumber": step_number,
        "classification": "CATALOG_STALE",
    }

    # Test-mode short-circuit. Honoured even if preconditions are met,
    # so the synthetic-failures suite never invokes a multi-minute crawl.
    if skip_real_crawl:
        return {
            **base_outcome,
            "autoApplied": True,
            "outcome": "auto-recovery-deferred",
            "reason": (
                "skip_real_crawl=True; CATALOG_STALE auto-recovery would "
                "invoke a full ComponentCatalogCrawler run. The runner "
                "treats this outcome as deferred so the failure is "
                "still surfaced for human follow-up."
            ),
        }

    # Precondition checks. If any fails we do not raise — we return a
    # deferred outcome so the upstream runner can surface this to the
    # human alongside the original failure.
    if not COMPONENT_CRAWLER.exists():
        return {
            **base_outcome,
            "autoApplied": False,
            "outcome": "auto-recovery-deferred",
            "reason": (
                f"ComponentCatalogCrawler not found at {COMPONENT_CRAWLER}. "
                "Cannot perform catalog re-crawl."
            ),
        }

    if not _web_app_reachable(base_url):
        return {
            **base_outcome,
            "autoApplied": False,
            "outcome": "auto-recovery-deferred",
            "reason": (
                f"Web app at {base_url} is not reachable. Start the "
                "compose stack (docker compose -f "
                "docker/docker-compose.app.yml up -d) and re-run."
            ),
        }

    if shutil.which("dotnet") is None:
        return {
            **base_outcome,
            "autoApplied": False,
            "outcome": "auto-recovery-deferred",
            "reason": "dotnet executable not on PATH; cannot run crawler.",
        }

    # Back up the existing catalog so a crashed crawler doesn't leave us
    # with a half-rewritten file.
    if CATALOG_PATH.exists():
        shutil.copy2(CATALOG_PATH, CATALOG_BACKUP_PATH)

    crawler_proc = subprocess.run(
        [
            "dotnet",
            "run",
            "--project",
            str(COMPONENT_CRAWLER),
            "--",
            "--base-url",
            base_url,
        ],
        cwd=REPO_ROOT,
        capture_output=True,
        timeout=CRAWLER_TIMEOUT_SECONDS,
    )

    stdout = crawler_proc.stdout.decode("utf-8", errors="replace")
    stderr = crawler_proc.stderr.decode("utf-8", errors="replace")
    stdout_tail = stdout[-500:] if stdout else ""
    stderr_tail = stderr[-500:] if stderr else ""

    if crawler_proc.returncode != 0:
        # Roll back to the backed-up catalog so a partial overwrite
        # doesn't leave the project in a worse state than before.
        if CATALOG_BACKUP_PATH.exists():
            shutil.copy2(CATALOG_BACKUP_PATH, CATALOG_PATH)
        return {
            **base_outcome,
            "autoApplied": False,
            "outcome": "auto-recovery-failed",
            "reason": (
                f"Crawler exited with code {crawler_proc.returncode}. "
                "Catalog rolled back."
            ),
            "crawlerExitCode": crawler_proc.returncode,
            "crawlerStdoutTail": stdout_tail,
            "crawlerStderrTail": stderr_tail,
        }

    # Sniff the new catalog for the affected step's literal label.
    locator_now_present = _locator_present_in_catalog(failure)
    if locator_now_present:
        return {
            **base_outcome,
            "autoApplied": True,
            "outcome": "auto-recovered",
            "reason": (
                "Catalog re-crawl completed; the affected locator is now "
                "present. Re-run the failing step to confirm."
            ),
            "crawlerExitCode": 0,
            "crawlerStdoutTail": stdout_tail,
        }

    return {
        **base_outcome,
        "autoApplied": True,
        "outcome": "auto-recovery-failed",
        "reason": (
            "Catalog re-crawl completed but the affected locator is "
            "still missing. The component may have been removed, renamed, "
            "or moved behind a permission gate not exercised by the "
            "crawler. Falls through to human review."
        ),
        "crawlerExitCode": 0,
        "crawlerStdoutTail": stdout_tail,
    }


def _web_app_reachable(base_url: str, timeout: float = 2.0) -> bool:
    """Probe ``{base_url}/health`` and return True on a 2xx response."""
    try:
        with urllib.request.urlopen(f"{base_url}/health", timeout=timeout) as resp:
            return 200 <= resp.status < 300
    except (urllib.error.URLError, OSError, TimeoutError):
        return False


def _locator_present_in_catalog(failure: object) -> bool:
    """Best-effort check that the affected locator label now appears in
    the catalog index. The matching is intentionally conservative — if
    the catalog format changes or the failure record is malformed, this
    returns False so the runner falls through to human review.
    """
    if not CATALOG_PATH.exists():
        return False
    try:
        index = json.loads(CATALOG_PATH.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return False

    step_text: str = getattr(failure, "step_text", "") or ""  # type: ignore[assignment]
    if not step_text:
        return False

    # Pull out the visible-label heuristic: anything in double quotes
    # in the step text is the user-visible string the locator targets.
    import re

    quoted = re.findall(r'"([^"]+)"', step_text)
    if not quoted:
        return False

    # Accept any catalog entry whose visible label OR aria label OR
    # caption matches one of the quoted strings.
    needles = {q.strip() for q in quoted if q.strip()}
    routes = index.get("routes", []) if isinstance(index, dict) else []
    for route in routes:
        for element in route.get("elements", []):
            for field in ("userVisibleLabel", "ariaLabel", "caption", "text"):
                value = element.get(field)
                if isinstance(value, str) and value.strip() in needles:
                    return True
    return False
