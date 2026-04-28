"""APP_FUNCTIONAL_BUG remediation — file a human-review issue.

When a VERIFY step's user-visible expectation does not match the
observed state, it indicates a functional bug in the application.
This remediation creates a structured issue file for human review
and code fix.

The issue includes:
  - Step number and text
  - Expected vs. observed values
  - Screenshot and DOM snapshot references (from Playwright trace)
  - Triage confidence and evidence
  - URL and any console errors captured

The issue is filed as:
  docs/session-sets/{slug}/issues/{step-number}-bug.md

No code changes are proposed; the fix is a development task.
"""

from __future__ import annotations

from pathlib import Path


def file_issue(
    triage_result: object,
    session_set_slug: str = "",
) -> dict:
    """File an APP_FUNCTIONAL_BUG issue and return outcome record.

    Args:
        triage_result: A TriageResult object with classification="APP_FUNCTIONAL_BUG"
        session_set_slug: The session-set slug (used to construct the issue path)

    Returns:
        A dict with keys: {type, stepNumber, classification, outcome, issueFile}
    """
    failure = triage_result.failure  # type: ignore
    step_number = failure.step_number

    # Construct the issues directory path
    repo_root = Path(__file__).resolve().parents[3]
    session_set_path = repo_root / "docs/session-sets" / (session_set_slug or "uat-runner-and-triage-feedback")
    issues_dir = session_set_path / "issues"
    issues_dir.mkdir(parents=True, exist_ok=True)

    issue_file = issues_dir / f"{step_number:03d}-functional-bug.md"

    # Generate the issue content
    content = _generate_issue_content(triage_result, failure)
    issue_file.write_text(content, encoding="utf-8")

    return {
        "type": "uat-runner-failure",
        "stepNumber": step_number,
        "classification": "APP_FUNCTIONAL_BUG",
        "autoApplied": False,
        "issueFile": str(issue_file.relative_to(repo_root)),
        "outcome": "human-review-required",
    }


def _generate_issue_content(triage_result: object, failure: object) -> str:
    """Generate markdown issue content from triage result and failure record."""
    f = failure  # type: ignore
    tr = triage_result  # type: ignore

    # Handle special cases (BUDGET_EXCEEDED, PARSE_ERROR)
    if tr.classification == "BUDGET_EXCEEDED":
        lines = [
            f"## UAT Step {f.step_number} — Triage Budget Exceeded",
            "",
            f"**Step text:**",
            f"> {f.step_text}",
            "",
            f"**Issue:** Triage budget limit reached before this step could be classified.",
            "",
            f"**Playwright error:**",
            f"```",
            f.playwright_error,
            f"```",
            "",
            f"**Status:** Requires human review. The triage process hit its cost or call limit.",
            f"",
            f"**Next steps:**",
            f"  1. Review the Playwright error and context above.",
            f"  2. Classify manually as one of: APP_FUNCTIONAL_BUG, CATALOG_STALE, SEED_DATA_GAP, AMBIGUOUS_LOCATOR.",
            f"  3. Apply the appropriate remediation for that class.",
        ]
        return "\n".join(lines)

    if tr.classification == "PARSE_ERROR":
        lines = [
            f"## UAT Step {f.step_number} — Triage Parse Error",
            "",
            f"**Step text:**",
            f"> {f.step_text}",
            "",
            f"**Issue:** Triage response could not be parsed. Defaulting to human review.",
            "",
            f"**Playwright error:**",
            f"```",
            f.playwright_error,
            f"```",
            "",
            f"**Status:** Requires human review. The triage model returned an unparseable response.",
            f"",
            f"**Next steps:**",
            f"  1. Review the Playwright error and context above.",
            f"  2. Classify manually as one of: APP_FUNCTIONAL_BUG, CATALOG_STALE, SEED_DATA_GAP, AMBIGUOUS_LOCATOR.",
            f"  3. Apply the appropriate remediation for that class.",
        ]
        return "\n".join(lines)

    # Normal APP_FUNCTIONAL_BUG case
    lines = [
        f"## UAT Step {f.step_number} — Functional Bug",
        "",
        f"**Step text:**",
        f"> {f.step_text}",
        "",
        f"**Triage classification:** `APP_FUNCTIONAL_BUG` (confidence: {tr.confidence:.2%})",
        "",
        f"**Triage evidence:**",
        f"> {tr.evidence}",
        "",
        f"**Playwright error:**",
        f"```",
        f.playwright_error,
        f"```",
        "",
    ]

    if f.current_url:
        lines.extend([
            f"**Current URL:** `{f.current_url}`",
            "",
        ])

    if f.screenshot_path:
        lines.extend([
            f"**Screenshot:** {f.screenshot_path}",
            "",
        ])

    if f.dom_snapshot_path:
        lines.extend([
            f"**DOM Snapshot:** {f.dom_snapshot_path}",
            "",
        ])

    if f.console_errors:
        lines.extend([
            f"**Console errors:**",
            "",
        ])
        for err in f.console_errors:
            lines.append(f"  - {err}")
        lines.append("")

    lines.extend([
        f"**Status:** Requires human investigation and code fix.",
        f"",
        f"**Next steps:**",
        f"  1. Review the evidence and Playwright artifacts above.",
        f"  2. Identify the root cause in the application code.",
        f"  3. Implement a code fix.",
        f"  4. Re-run the UAT checklist to confirm the fix.",
    ])

    return "\n".join(lines)
