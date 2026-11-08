"""AMBIGUOUS_LOCATOR remediation — file a disambiguation issue.

When a locator matches multiple elements on the page (e.g., multiple
buttons with the same label), the UAT cannot proceed deterministically.
This remediation creates a structured issue file proposing a data-testid
addition to the affected component to make the locator unambiguous.

The issue includes:
  - Step text and current locator expression
  - How many matches were found (from Playwright error)
  - The inferred component .razor file and element
  - A suggested data-testid name and placement
  - References to the DOM snapshot showing all matches

The issue is filed as:
  docs/session-sets/{slug}/issues/{step-number}-ambiguous-locator.md
"""

from __future__ import annotations

from pathlib import Path


def file_issue(
    triage_result: object,
    session_set_slug: str = "",
) -> dict:
    """File an AMBIGUOUS_LOCATOR issue and return outcome record.

    Args:
        triage_result: A TriageResult object with classification="AMBIGUOUS_LOCATOR"
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

    issue_file = issues_dir / f"{step_number:03d}-ambiguous-locator.md"

    # Generate the issue content
    content = _generate_issue_content(triage_result, failure)
    issue_file.write_text(content, encoding="utf-8")

    return {
        "type": "uat-runner-failure",
        "stepNumber": step_number,
        "classification": "AMBIGUOUS_LOCATOR",
        "autoApplied": False,
        "issueFile": str(issue_file.relative_to(repo_root)),
        "outcome": "human-review-required",
    }


def _generate_issue_content(triage_result: object, failure: object) -> str:
    """Generate markdown issue content from triage result and failure record."""
    f = failure  # type: ignore
    tr = triage_result  # type: ignore

    lines = [
        f"## UAT Step {f.step_number} — Ambiguous Locator",
        "",
        f"**Step text:**",
        f"> {f.step_text}",
        "",
        f"**Triage classification:** `AMBIGUOUS_LOCATOR` (confidence: {tr.confidence:.2%})",
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

    if f.dom_snapshot_path:
        lines.extend([
            f"**DOM snapshot:** {f.dom_snapshot_path}",
            f"(Shows all matching elements; review the snapshot to identify the correct element)",
            "",
        ])

    if f.screenshot_path:
        lines.extend([
            f"**Screenshot:** {f.screenshot_path}",
            "",
        ])

    lines.extend([
        f"**Status:** Locator is ambiguous; requires a `data-testid` discriminator.",
        f"",
        f"**Remediation approach:**",
        f"  1. Review the DOM snapshot to see which element should be targeted.",
        f"  2. Add a unique `data-testid` attribute to that element in the .razor component.",
        f"  3. Update the catalog entry to use the new `data-testid` in the locator.",
        f"  4. Re-run the UAT checklist to confirm the locator now matches unambiguously.",
        f"",
        f"**Example data-testid:**",
        f"```html",
        f"<!-- Before (ambiguous) -->",
        f"<button>Approve</button>",
        f"<button>Approve</button>",
        f"",
        f"<!-- After (unambiguous) -->",
        f"<button data-testid=\"approve-pending-request\">Approve</button>",
        f"<button data-testid=\"approve-completed-request\">Approve</button>",
        f"```",
    ])

    return "\n".join(lines)
