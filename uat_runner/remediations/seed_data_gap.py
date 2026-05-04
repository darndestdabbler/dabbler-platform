"""SEED_DATA_GAP remediation — file a data-gap issue for human review.

When a step requires test data that wasn't seeded (e.g., a checklist step
references a customer that doesn't exist in the test database), this
remediation creates a structured issue file documenting the gap and
suggesting an Entity Framework seed addition.

The issue includes:
  - Which route/role the gap affects
  - What data is needed (entity + attribute values)
  - Which EF seeding class should be updated
  - A suggested seed entry (as pseudocode / comment template)

The issue is filed as:
  docs/session-sets/{slug}/issues/{step-number}-seed-gap.md
"""

from __future__ import annotations

from pathlib import Path


def file_issue(
    triage_result: object,
    session_set_slug: str = "",
) -> dict:
    """File a SEED_DATA_GAP issue and return outcome record.

    Args:
        triage_result: A TriageResult object with classification="SEED_DATA_GAP"
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

    issue_file = issues_dir / f"{step_number:03d}-seed-gap.md"

    # Generate the issue content
    content = _generate_issue_content(triage_result, failure)
    issue_file.write_text(content, encoding="utf-8")

    return {
        "type": "uat-runner-failure",
        "stepNumber": step_number,
        "classification": "SEED_DATA_GAP",
        "autoApplied": False,
        "issueFile": str(issue_file.relative_to(repo_root)),
        "outcome": "human-review-required",
    }


def _generate_issue_content(triage_result: object, failure: object) -> str:
    """Generate markdown issue content from triage result and failure record."""
    f = failure  # type: ignore
    tr = triage_result  # type: ignore

    lines = [
        f"## UAT Step {f.step_number} — Seed Data Gap",
        "",
        f"**Step text:**",
        f"> {f.step_text}",
        "",
        f"**Triage classification:** `SEED_DATA_GAP` (confidence: {tr.confidence:.2%})",
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

    lines.extend([
        f"**Status:** Required test data is not present in the seeded database.",
        f"",
        f"**Next steps:**",
        f"  1. Review the Playwright error above to identify which entity/field is missing.",
        f"  2. Determine which EF seed class should provide the data (e.g., `CustomerSeeder`).",
        f"  3. Add or update a seed entry in that class with the required data.",
        f"  4. Rebuild the test database and re-run the UAT checklist.",
    ])

    return "\n".join(lines)
