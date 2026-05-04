"""Tests for the persona-inference module.

Run from the repo root::

    .venv/Scripts/python.exe uat_runner/test_cases/test_personas.py

Validates:

1. Happy path: a checklist whose first sign-in step matches a persona
   in personas.json yields ``role=<expected>`` and no error.
2. No sign-in step in the lookahead window: returns
   ``email=None, role=None, error=None`` so the caller falls back to
   AnyRole rather than aborting.
3. Email present but missing from personas.json: returns
   ``role=None, error=<message naming the email>`` so the runner
   surfaces the gap rather than silently degrading to AnyRole.
4. The shipped personas.json maps the canonical seeded admin email to
   ``SystemAdmin`` (smoke check on the file the runner ships with).
"""

from __future__ import annotations

import json
import os
import sys
import tempfile
from pathlib import Path

# Allow running as a script as well as a module: put the repo root on
# sys.path so ``from uat_runner import personas`` resolves regardless
# of cwd.
_REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
if _REPO_ROOT not in sys.path:
    sys.path.insert(0, _REPO_ROOT)


def _write_checklist(tmpdir: Path, steps: list[str]) -> Path:
    checklist = {
        "ProjectName": "Test",
        "Review": [
            {
                "FunctionalArea": "Test",
                "Subarea": "Test",
                "ItemLabel": "Test",
                "Steps": [
                    {"StepNumber": i + 1, "Action": action}
                    for i, action in enumerate(steps)
                ],
            }
        ],
    }
    path = tmpdir / "checklist.json"
    path.write_text(json.dumps(checklist), encoding="utf-8")
    return path


def test_happy_path_admin_signin_resolves_to_systemadmin() -> None:
    from uat_runner import personas

    with tempfile.TemporaryDirectory() as tmp:
        path = _write_checklist(
            Path(tmp),
            [
                'NAVIGATE TO "http://localhost:5010/login"',
                'CLICK INPUT with label "Email address" and enter "admin@acmecoffee.com"',
                'CLICK BUTTON with caption "Send sign-in link"',
            ],
        )
        result = personas.infer_active_role(path)

    assert result.email == "admin@acmecoffee.com", result
    assert result.role == "SystemAdmin", result
    assert result.error is None, result
    print("OK happy_path_admin_signin_resolves_to_systemadmin")


def test_no_signin_step_returns_no_role_and_no_error() -> None:
    from uat_runner import personas

    with tempfile.TemporaryDirectory() as tmp:
        path = _write_checklist(
            Path(tmp),
            [
                'NAVIGATE TO "http://localhost:5010/admin/products"',
                'CLICK SWITCH with label "Available"',
                'WAIT FOR ACTIONS QUIET',
            ],
        )
        result = personas.infer_active_role(path)

    assert result.email is None, result
    assert result.role is None, result
    assert result.error is None, result
    print("OK no_signin_step_returns_no_role_and_no_error")


def test_signin_lookahead_window_skips_late_email_step() -> None:
    """A `CLICK INPUT ... Email ...` step that lands beyond the first
    five actions does not count as the persona signal — the runner
    should treat that as no-signin (fall back to AnyRole) rather than
    misattribute a mid-flow form input as the active persona.
    """
    from uat_runner import personas

    with tempfile.TemporaryDirectory() as tmp:
        path = _write_checklist(
            Path(tmp),
            [
                'NAVIGATE TO "/admin/dashboard"',
                'CLICK BUTTON with caption "Open"',
                'WAIT FOR ACTIONS QUIET',
                'CLICK BUTTON with caption "Next"',
                'WAIT FOR ACTIONS QUIET',
                'CLICK INPUT with label "Email address" and enter "admin@acmecoffee.com"',
            ],
        )
        result = personas.infer_active_role(path)

    assert result.email is None, result
    assert result.role is None, result
    assert result.error is None, result
    print("OK signin_lookahead_window_skips_late_email_step")


def test_email_not_in_personas_returns_explicit_error() -> None:
    from uat_runner import personas

    with tempfile.TemporaryDirectory() as tmp:
        path = _write_checklist(
            Path(tmp),
            [
                'NAVIGATE TO "http://localhost:5010/login"',
                'CLICK INPUT with label "Email address" and enter "ghost@nowhere.com"',
                'CLICK BUTTON with caption "Send sign-in link"',
            ],
        )
        result = personas.infer_active_role(path)

    assert result.email == "ghost@nowhere.com", result
    assert result.role is None, result
    assert result.error is not None, result
    assert "ghost@nowhere.com" in result.error, result.error
    assert "personas.json" in result.error, result.error
    print("OK email_not_in_personas_returns_explicit_error")


def test_persona_map_includes_canonical_seeded_personas() -> None:
    from uat_runner import personas

    pmap = personas.load_persona_map()
    assert pmap.get("admin@acmecoffee.com") == "SystemAdmin", pmap
    assert pmap.get("juser@acmecoffee.com") == "User", pmap
    assert pmap.get("warehouse@acmecoffee.com") == "WarehouseUser", pmap
    print("OK persona_map_includes_canonical_seeded_personas")


def main() -> int:
    tests = [
        test_happy_path_admin_signin_resolves_to_systemadmin,
        test_no_signin_step_returns_no_role_and_no_error,
        test_signin_lookahead_window_skips_late_email_step,
        test_email_not_in_personas_returns_explicit_error,
        test_persona_map_includes_canonical_seeded_personas,
    ]
    failures = 0
    for t in tests:
        try:
            t()
        except AssertionError as e:
            failures += 1
            print(f"FAIL {t.__name__}: {e}")
        except Exception as e:
            failures += 1
            print(f"ERROR {t.__name__}: {type(e).__name__}: {e}")
    if failures:
        print(f"\n{failures} test(s) failed.")
        return 1
    print("\nAll persona-inference tests passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
