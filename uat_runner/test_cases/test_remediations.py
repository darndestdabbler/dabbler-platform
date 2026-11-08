"""Test remediation dispatch for all four failure classes.

Run from the repo root::

    .venv/Scripts/python.exe uat_runner/test_cases/test_remediations.py

Validates that:
  1. Each failure class is correctly triaged via TriageSession.
  2. The apply_remediation() dispatch routes each class correctly.
  3. APP_FUNCTIONAL_BUG/SEED_DATA_GAP/AMBIGUOUS_LOCATOR issue files are created.
  4. CATALOG_STALE returns an auto-recovery outcome.
"""

from __future__ import annotations

import sys
import os
import tempfile
import shutil
from pathlib import Path

# Allow running as a script as well as a module: put the repo root on
# sys.path so ``from uat_runner.triage import ...`` resolves regardless
# of cwd.
_REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
if _REPO_ROOT not in sys.path:
    sys.path.insert(0, _REPO_ROOT)


def main() -> None:
    from uat_runner.triage import FailureRecord, TriageSession, apply_remediation

    # Create a temporary issues directory for testing
    test_session_slug = "test-remediation-session"
    test_dir = Path(_REPO_ROOT) / "docs/session-sets" / test_session_slug
    if test_dir.exists():
        shutil.rmtree(test_dir)
    test_dir.mkdir(parents=True, exist_ok=True)

    # Run triage on the four synthetic cases
    ts = TriageSession(
        session_set=str(test_dir.parent / "uat-runner-and-triage-feedback"),
        session_number=3,
    )

    test_cases = [
        ("APP_FUNCTIONAL_BUG", dict(
            step_number=15,
            step_text='VERIFY TEXT in "SSN" field equals "XXX-XX-####"',
            playwright_error="Expected 'XXX-XX-####' but found '123-45-6789'",
            current_url="/customers/42/edit",
        )),
        ("CATALOG_STALE", dict(
            step_number=3,
            step_text='CLICK BUTTON with caption "Add Mask Exemption"',
            playwright_error="strict mode violation: getByRole('button', {name: 'Add Mask Exemption'}) resolved to 0 elements",
            current_url="/admin/security/masks",
        )),
        ("SEED_DATA_GAP", dict(
            step_number=7,
            step_text='CLICK ROW in "Customers" where "Name" = "Acme Corp"',
            playwright_error="strict mode violation: locator resolved to 0 elements. Filter: text='Acme Corp'",
            current_url="/customers",
        )),
        ("AMBIGUOUS_LOCATOR", dict(
            step_number=11,
            step_text='CLICK BUTTON with caption "Save"',
            playwright_error=(
                "strict mode violation: getByRole('button', {name: 'Save'}) "
                "resolved to 3 elements. Expected: 1."
            ),
            current_url="/admin/security/roles/5",
        )),
    ]

    print("Testing remediation dispatch...\n")
    results = []

    for expected_class, kwargs in test_cases:
        fr = FailureRecord(**kwargs)
        result = ts.classify(fr)
        # skip_real_crawl=True so CATALOG_STALE doesn't invoke the
        # multi-minute full ComponentCatalogCrawler subprocess just to
        # smoke-test dispatch. The auto-recovery path is exercised by
        # the Session 4 backfill against a real checklist.
        outcome = apply_remediation(result, test_session_slug, skip_real_crawl=True)

        print(f"[{expected_class}] Step {outcome['stepNumber']}")
        print(f"  Classification: {outcome['classification']}")
        print(f"  Outcome: {outcome['outcome']}")

        # Pass criteria — classification MUST match expected. Earlier versions
        # of this test only checked "an issue file was created", which masked
        # a real misclassification bug (AMBIGUOUS_LOCATOR being classified as
        # APP_FUNCTIONAL_BUG and routed to the wrong issue file). The redo
        # asserts classification first, then the dispatch outcome.
        classification_ok = outcome["classification"] == expected_class

        if expected_class == "CATALOG_STALE":
            dispatch_ok = (
                outcome.get("autoApplied") is True
                and outcome.get("outcome") == "auto-recovery-deferred"
            )
            label = "auto-recovery (deferred — skip_real_crawl=True)"
        else:
            # APP_FUNCTIONAL_BUG, SEED_DATA_GAP, AMBIGUOUS_LOCATOR should file
            # issues at predictable per-class filenames.
            expected_filename_suffix = {
                "APP_FUNCTIONAL_BUG": "-functional-bug.md",
                "SEED_DATA_GAP": "-seed-gap.md",
                "AMBIGUOUS_LOCATOR": "-ambiguous-locator.md",
            }[expected_class]
            issue_relative = outcome.get("issueFile", "")
            issue_path = Path(_REPO_ROOT) / issue_relative if issue_relative else None
            dispatch_ok = (
                bool(issue_relative)
                and issue_path is not None
                and issue_path.exists()
                and issue_path.name.endswith(expected_filename_suffix)
            )
            label = (
                f"issue file {issue_path.name if issue_path else '<missing>'} "
                f"(suffix={expected_filename_suffix})"
            )

        ok = classification_ok and dispatch_ok
        if classification_ok:
            cls_msg = "classification OK"
        else:
            cls_msg = (
                f"classification MISMATCH: expected={expected_class}, "
                f"got={outcome['classification']}"
            )
        print(f"  Status: {'PASS' if ok else 'FAIL'} ({label}; {cls_msg})")
        results.append(ok)
        print()

    # Clean up test directory
    if test_dir.exists():
        shutil.rmtree(test_dir)

    passed = sum(1 for r in results if r)
    total = len(results)
    print(f"{passed}/{total} cases passed.")
    sys.exit(0 if passed == total else 1)


if __name__ == "__main__":
    main()
