"""Smoke tests for the catalog grounding gate.

Run with: python -m pytest uat_runner/test_cases/catalog_grounding_gate_tests.py -v
"""
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2]))

from uat_runner.runner import (
    CatalogGroundingViolation,
    _check_catalog_grounding,
    _load_catalog_index,
)


def _write_catalog(tmp_path: Path, entries: list[dict]) -> Path:
    p = tmp_path / "catalog.json"
    p.write_text(json.dumps({"appName": "Test", "generatedAt": "2026-01-01T00:00:00Z",
                              "schemaVersion": "1.1", "entries": entries}), encoding="utf-8")
    return p


def _write_checklist(tmp_path: Path, steps: list[dict], item_label: str = "Test item") -> Path:
    p = tmp_path / "checklist.json"
    p.write_text(json.dumps({
        "ProjectName": "Test",
        "ReleaseLabel": "Test",
        "DocumentDate": "2026-01-01",
        "RequiresAppStateReset": False,
        "Review": [{"FunctionalArea": "Test", "Subarea": "Test",
                     "ItemLabel": item_label, "Steps": steps,
                     "IsJudgmentItem": False, "Passes": False, "Feedback": ""}],
    }), encoding="utf-8")
    return p


# ---------------------------------------------------------------------------
# _load_catalog_index
# ---------------------------------------------------------------------------

def test_load_catalog_index_extracts_labels(tmp_path):
    catalog = _write_catalog(tmp_path, [
        {"routePath": "/admin/security", "role": "SystemAdmin",
         "elementRole": "Tab", "userVisibleLabel": "ROLE MASTER DETAILS",
         "htmlTag": "div", "isWatermarked": False},
        {"routePath": "/admin/security", "role": "SystemAdmin",
         "elementRole": "Button", "userVisibleLabel": "ADD USER-TYPE ROLE",
         "htmlTag": "button", "isWatermarked": False},
    ])
    index = _load_catalog_index(catalog)
    assert "/admin/security" in index
    assert "ROLE MASTER DETAILS" in index["/admin/security"]
    assert "ADD USER-TYPE ROLE" in index["/admin/security"]


def test_load_catalog_index_normalises_to_upper(tmp_path):
    catalog = _write_catalog(tmp_path, [
        {"routePath": "/orders", "role": "SystemAdmin",
         "elementRole": "Button", "userVisibleLabel": "Save and Use",
         "htmlTag": "button", "isWatermarked": False},
    ])
    index = _load_catalog_index(catalog)
    assert "SAVE AND USE" in index["/orders"]


# ---------------------------------------------------------------------------
# _check_catalog_grounding — no violations
# ---------------------------------------------------------------------------

def test_no_violation_when_label_exists(tmp_path):
    catalog = _write_catalog(tmp_path, [
        {"routePath": "/admin/security", "role": "SystemAdmin",
         "elementRole": "Tab", "userVisibleLabel": "ROLE MASTER DETAILS",
         "htmlTag": "div", "isWatermarked": False},
    ])
    checklist = _write_checklist(tmp_path, [
        {"StepNumber": 1, "Action": 'NAVIGATE TO "http://localhost:5010/admin/security"', "Verifications": []},
        {"StepNumber": 2, "Action": 'CLICK TAB with name "ROLE MASTER DETAILS"', "Verifications": []},
    ])
    violations = _check_catalog_grounding(checklist, catalog)
    assert violations == []


def test_no_violation_for_uncrawled_route(tmp_path):
    """Labels on routes not in the catalog are silently skipped (CATALOG_STALE path)."""
    catalog = _write_catalog(tmp_path, [
        {"routePath": "/known-route", "role": "SystemAdmin",
         "elementRole": "Button", "userVisibleLabel": "SAVE",
         "htmlTag": "button", "isWatermarked": False},
    ])
    checklist = _write_checklist(tmp_path, [
        {"StepNumber": 1, "Action": 'NAVIGATE TO "http://localhost:5010/new-uncrawled-page"', "Verifications": []},
        {"StepNumber": 2, "Action": 'CLICK BUTTON with caption "Some Button"', "Verifications": []},
    ])
    violations = _check_catalog_grounding(checklist, catalog)
    assert violations == [], "Should not flag labels on uncrawled routes"


def test_no_violation_for_navigate_step_itself(tmp_path):
    catalog = _write_catalog(tmp_path, [
        {"routePath": "/admin/security", "role": "SystemAdmin",
         "elementRole": "Tab", "userVisibleLabel": "ROLE MASTER DETAILS",
         "htmlTag": "div", "isWatermarked": False},
    ])
    checklist = _write_checklist(tmp_path, [
        {"StepNumber": 1, "Action": 'NAVIGATE TO "http://localhost:5010/admin/security"', "Verifications": []},
    ])
    violations = _check_catalog_grounding(checklist, catalog)
    assert violations == []


# ---------------------------------------------------------------------------
# _check_catalog_grounding — violations detected
# ---------------------------------------------------------------------------

def test_violation_when_tab_name_wrong(tmp_path):
    """CLICK TAB with name "ROLES" on a route that has "ROLE MASTER DETAILS" → violation."""
    catalog = _write_catalog(tmp_path, [
        {"routePath": "/admin/security", "role": "SystemAdmin",
         "elementRole": "Tab", "userVisibleLabel": "ROLE MASTER DETAILS",
         "htmlTag": "div", "isWatermarked": False},
    ])
    checklist = _write_checklist(tmp_path, [
        {"StepNumber": 1, "Action": 'NAVIGATE TO "http://localhost:5010/admin/security"', "Verifications": []},
        {"StepNumber": 2, "Action": 'CLICK TAB with name "ROLES"', "Verifications": []},
    ])
    violations = _check_catalog_grounding(checklist, catalog)
    assert len(violations) == 1
    assert violations[0].route == "/admin/security"
    assert violations[0].label == "ROLES"


def test_violation_when_link_text_is_placeholder(tmp_path):
    """CLICK LINK with text "first role row in the grid" → violation (fake placeholder)."""
    catalog = _write_catalog(tmp_path, [
        {"routePath": "/admin/security", "role": "SystemAdmin",
         "elementRole": "Link", "userVisibleLabel": "BackofficeUser",
         "htmlTag": "a", "isWatermarked": False},
    ])
    checklist = _write_checklist(tmp_path, [
        {"StepNumber": 1, "Action": 'NAVIGATE TO "http://localhost:5010/admin/security"', "Verifications": []},
        {"StepNumber": 2, "Action": 'CLICK LINK with text "first role row in the grid"', "Verifications": []},
    ])
    violations = _check_catalog_grounding(checklist, catalog)
    assert len(violations) == 1
    assert violations[0].label == "first role row in the grid"


def test_violation_only_on_catalog_known_route_not_new_route(tmp_path):
    """Only routes in the catalog produce violations; new routes are silently skipped."""
    catalog = _write_catalog(tmp_path, [
        {"routePath": "/known-route", "role": "SystemAdmin",
         "elementRole": "Button", "userVisibleLabel": "SAVE",
         "htmlTag": "button", "isWatermarked": False},
    ])
    checklist = _write_checklist(tmp_path, [
        {"StepNumber": 1, "Action": 'NAVIGATE TO "http://localhost:5010/known-route"', "Verifications": []},
        {"StepNumber": 2, "Action": 'CLICK BUTTON with caption "Fake Label"', "Verifications": []},
        {"StepNumber": 3, "Action": 'NAVIGATE TO "http://localhost:5010/new-page"', "Verifications": []},
        {"StepNumber": 4, "Action": 'CLICK BUTTON with caption "Another Fake"', "Verifications": []},
    ])
    violations = _check_catalog_grounding(checklist, catalog)
    assert len(violations) == 1  # only /known-route violation, not /new-page
    assert violations[0].route == "/known-route"
    assert violations[0].label == "Fake Label"


def test_case_insensitive_match(tmp_path):
    """Labels are matched case-insensitively: catalog has "ROLE MASTER DETAILS",
    checklist uses "Role Master Details" → no violation."""
    catalog = _write_catalog(tmp_path, [
        {"routePath": "/admin/security", "role": "SystemAdmin",
         "elementRole": "Tab", "userVisibleLabel": "ROLE MASTER DETAILS",
         "htmlTag": "div", "isWatermarked": False},
    ])
    checklist = _write_checklist(tmp_path, [
        {"StepNumber": 1, "Action": 'NAVIGATE TO "http://localhost:5010/admin/security"', "Verifications": []},
        {"StepNumber": 2, "Action": 'CLICK TAB with name "Role Master Details"', "Verifications": []},
    ])
    violations = _check_catalog_grounding(checklist, catalog)
    assert violations == []


def test_multiple_violations_across_items(tmp_path):
    """Multiple items each with a fake label all produce violations."""
    catalog = _write_catalog(tmp_path, [
        {"routePath": "/admin/security", "role": "SystemAdmin",
         "elementRole": "Tab", "userVisibleLabel": "ROLE MASTER DETAILS",
         "htmlTag": "div", "isWatermarked": False},
    ])
    steps_a = [
        {"StepNumber": 1, "Action": 'NAVIGATE TO "http://localhost:5010/admin/security"', "Verifications": []},
        {"StepNumber": 2, "Action": 'CLICK TAB with name "ROLES"', "Verifications": []},
    ]
    steps_b = [
        {"StepNumber": 1, "Action": 'NAVIGATE TO "http://localhost:5010/admin/security"', "Verifications": []},
        {"StepNumber": 2, "Action": 'CLICK BUTTON with caption "Nonexistent"', "Verifications": []},
    ]
    checklist_path = tmp_path / "checklist.json"
    checklist_path.write_text(json.dumps({
        "ProjectName": "Test", "ReleaseLabel": "Test", "DocumentDate": "2026-01-01",
        "RequiresAppStateReset": False,
        "Review": [
            {"FunctionalArea": "A", "Subarea": "A", "ItemLabel": "Item A",
             "Steps": steps_a, "IsJudgmentItem": False, "Passes": False, "Feedback": ""},
            {"FunctionalArea": "B", "Subarea": "B", "ItemLabel": "Item B",
             "Steps": steps_b, "IsJudgmentItem": False, "Passes": False, "Feedback": ""},
        ],
    }), encoding="utf-8")
    catalog = _write_catalog(tmp_path, [
        {"routePath": "/admin/security", "role": "SystemAdmin",
         "elementRole": "Tab", "userVisibleLabel": "ROLE MASTER DETAILS",
         "htmlTag": "div", "isWatermarked": False},
    ])
    violations = _check_catalog_grounding(checklist_path, catalog)
    assert len(violations) == 2
    labels = {v.label for v in violations}
    assert "ROLES" in labels
    assert "Nonexistent" in labels


# ---------------------------------------------------------------------------
# Integration: verify the real broken checklist triggers violations
# ---------------------------------------------------------------------------

def test_real_broken_checklist_triggers_violation():
    """The unified-master-details-composite checklist must produce violations."""
    repo_root = Path(__file__).resolve().parents[2]
    checklist = repo_root / "docs/session-sets/unified-master-details-composite/unified-master-details-composite-uat-checklist.json"
    catalog = repo_root / "docs/component-catalog/index.json"
    if not checklist.exists() or not catalog.exists():
        import pytest
        pytest.skip("Checklist or catalog not found")

    violations = _check_catalog_grounding(checklist, catalog)
    labels = {v.label for v in violations}

    assert "ROLES" in labels, "Expected 'ROLES' violation on /admin/security"
    assert "first role row in the grid" in labels, "Expected fake-link violation"


if __name__ == "__main__":
    import pytest
    pytest.main([__file__, "-v"])
