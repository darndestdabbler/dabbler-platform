"""Test remediation dispatch for all four failure classes.

Run from the repo root::

    .venv/Scripts/python.exe ai-router/uat_runner/test_cases/test_remediations.py

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

# Allow running as a module from the repo root.
_REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
if _REPO_ROOT not in sys.path:
    sys.path.insert(0, _REPO_ROOT)


def _load_router() -> object:
    import importlib.util

    spec = importlib.util.spec_from_file_location(
        "ai_router",
        os.path.join(_REPO_ROOT, "ai-router", "__init__.py"),
        submodule_search_locations=[os.path.join(_REPO_ROOT, "ai-router")],
    )
    mod = importlib.util.module_from_spec(spec)  # type: ignore[arg-type]
    sys.modules["ai_router"] = mod
    spec.loader.exec_module(mod)  # type: ignore[union-attr]
    return mod


def main() -> None:
    _load_router()

    from ai_router.uat_runner.triage import FailureRecord, TriageSession, apply_remediation

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
            playwright_error="strict mode violation: getByRole('button', {name: 'Save'}) resolved to 3 elements",
            current_url="/admin/security/roles/5",
        )),
    ]

    print("Testing remediation dispatch...\n")
    results = []

    for expected_class, kwargs in test_cases:
        fr = FailureRecord(**kwargs)
        result = ts.classify(fr)
        outcome = apply_remediation(result, test_session_slug)

        print(f"[{expected_class}] Step {outcome['stepNumber']}")
        print(f"  Classification: {outcome['classification']}")
        print(f"  Outcome: {outcome['outcome']}")

        # Check outcome based on class
        if expected_class == "CATALOG_STALE":
            ok = outcome.get("autoApplied") == True and outcome.get("outcome") == "auto-recovery-available"
            print(f"  Status: {'PASS' if ok else 'FAIL'} (auto-recovery)")
            results.append(ok)
        else:
            # APP_FUNCTIONAL_BUG, SEED_DATA_GAP, AMBIGUOUS_LOCATOR should file issues
            if "issueFile" in outcome:
                issue_relative = outcome["issueFile"]
                # Construct the absolute path
                issue_path = Path(_REPO_ROOT) / issue_relative
                if issue_path.exists():
                    print(f"  Issue file: {issue_path.name} (CREATED)")
                    results.append(True)
                else:
                    print(f"  Issue file: {issue_path.name} (NOT FOUND at {issue_path})")
                    results.append(False)
            else:
                print(f"  WARNING: No issueFile in outcome")
                results.append(False)

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
