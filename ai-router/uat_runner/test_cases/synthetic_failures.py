"""Synthetic failure cases for triage classification validation.

Run from the repo root::

    .venv/Scripts/python.exe -m ai_router.uat_runner.test_cases.synthetic_failures

Each case constructs a FailureRecord that is characteristic of one of the
four runtime failure classes, calls TriageSession.classify(), and asserts
the correct classification is returned. These are scripted manual tests
(not xUnit); they are run during the session to validate the triage taxonomy
and prompt template.

Expected: each case prints PASS or FAIL with the actual classification.
"""

from __future__ import annotations

import sys
import os

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


def _assert_classification(
    case_name: str,
    expected: str,
    failure_record_kwargs: dict,
    catalog_entry: str = "",
) -> bool:
    from ai_router.uat_runner.triage import FailureRecord, TriageSession

    fr = FailureRecord(**failure_record_kwargs)
    ts = TriageSession(
        session_set="docs/session-sets/uat-runner-and-triage-feedback",
        session_number=2,
    )
    result = ts.classify(fr, catalog_entry=catalog_entry)
    ok = result.classification == expected
    status = "PASS" if ok else "FAIL"
    print(
        f"[{status}] {case_name}: expected={expected}, "
        f"got={result.classification} (confidence={result.confidence:.2f})\n"
        f"       evidence: {result.evidence[:120]}"
    )
    if not ok:
        print(f"       raw_response: {result.raw_response[:300]}")
    return ok


APP_FUNCTIONAL_BUG_CASE = dict(
    step_number=15,
    step_text='VERIFY TEXT in "Social Security Number" field equals "XXX-XX-####"',
    playwright_error=(
        "Expected text to equal 'XXX-XX-####' but found '123-45-6789'. "
        "Selector: input[aria-label='Social Security Number']"
    ),
    current_url="/customers/42/edit",
)

CATALOG_STALE_CASE = dict(
    step_number=3,
    step_text='CLICK BUTTON with caption "Add Mask Exemption"',
    playwright_error=(
        "strict mode violation: getByRole('button', {name: 'Add Mask Exemption'}) "
        "resolved to 0 elements. Expected: 1."
    ),
    current_url="/admin/security/masks",
)

SEED_DATA_GAP_CASE = dict(
    step_number=7,
    step_text='CLICK ROW in "Customers" where "Name" = "Acme Corp"',
    playwright_error=(
        "strict mode violation: locator resolved to 0 elements. "
        "Filter: text='Acme Corp' in table row."
    ),
    current_url="/customers",
)

AMBIGUOUS_LOCATOR_CASE = dict(
    step_number=11,
    step_text='CLICK BUTTON with caption "Save"',
    playwright_error=(
        "strict mode violation: getByRole('button', {name: 'Save'}) "
        "resolved to 3 elements. Expected: 1."
    ),
    current_url="/admin/security/roles/5",
)


def main() -> None:
    _load_router()

    results = []
    results.append(
        _assert_classification(
            "APP_FUNCTIONAL_BUG — SSN masking not applied",
            "APP_FUNCTIONAL_BUG",
            APP_FUNCTIONAL_BUG_CASE,
        )
    )
    results.append(
        _assert_classification(
            "CATALOG_STALE — Add Mask Exemption button missing",
            "CATALOG_STALE",
            CATALOG_STALE_CASE,
            catalog_entry='[{"routePath":"/admin/security/masks","role":"SystemAdmin","elementRole":"button","userVisibleLabel":"Add Mask Exemption","ariaLabel":"Add Mask Exemption"}]',
        )
    )
    results.append(
        _assert_classification(
            "SEED_DATA_GAP — Acme Corp not in DB",
            "SEED_DATA_GAP",
            SEED_DATA_GAP_CASE,
        )
    )
    results.append(
        _assert_classification(
            "AMBIGUOUS_LOCATOR — Save button matched 3 elements",
            "AMBIGUOUS_LOCATOR",
            AMBIGUOUS_LOCATOR_CASE,
        )
    )

    passed = sum(1 for r in results if r)
    total = len(results)
    print(f"\n{passed}/{total} cases passed.")
    sys.exit(0 if passed == total else 1)


if __name__ == "__main__":
    main()
