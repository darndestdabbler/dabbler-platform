"""CLI entry point for the UAT runner.

Usage (script form, preferred — works because the package directory has a
hyphen which prevents `python -m ai_router.uat_runner.cli`):

    python ai-router/uat_runner/cli.py \\
      --checklist docs/session-sets/{slug}/{slug}-uat-checklist.json \\
      --catalog docs/component-catalog/index.json \\
      --session-set {slug} \\
      [--base-url http://localhost:5010] \\
      [--auth-email admin@acmecoffee.com]

Exits 0 if all steps pass or only CATALOG_STALE failures auto-recovered.
Exits 1 if any human-review issues filed.
"""

from __future__ import annotations

import argparse
import importlib.util
import sys
from pathlib import Path

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
if hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")


def _bootstrap_imports() -> None:
    """Make ``ai_router.uat_runner.runner`` importable when this file is
    invoked as a script (``python ai-router/uat_runner/cli.py``).

    The on-disk package directory uses a hyphen (``ai-router``), which is not
    a valid Python identifier — so ``python -m ai_router.uat_runner.cli``
    never works on a fresh shell. The test scripts under ``test_cases/``
    use the same bootstrap. A repo-wide rename to ``ai_router/`` is the
    long-term fix; this bootstrap removes the practical CLI breakage.
    """
    ai_router_root = Path(__file__).resolve().parents[1]
    if "ai_router" in sys.modules:
        return
    spec = importlib.util.spec_from_file_location(
        "ai_router",
        str(ai_router_root / "__init__.py"),
        submodule_search_locations=[str(ai_router_root)],
    )
    mod = importlib.util.module_from_spec(spec)  # type: ignore[arg-type]
    sys.modules["ai_router"] = mod
    spec.loader.exec_module(mod)  # type: ignore[union-attr]


# Module is invoked either as a script (`python cli.py`) or imported as
# `ai_router.uat_runner.cli` after a caller has already bootstrapped
# `ai_router`. In the script case, we have no parent package, so the
# relative `from . import runner` would fail. Detect and bootstrap first.
if __package__ in (None, ""):
    _bootstrap_imports()
    from ai_router.uat_runner import runner  # type: ignore[import-not-found]
else:
    from . import runner


def main() -> int:
    """Parse CLI arguments and run the UAT checklist."""
    parser = argparse.ArgumentParser(
        description="Run a UAT checklist against the live app.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )
    parser.add_argument(
        "--checklist",
        type=Path,
        required=True,
        help="Path to the UAT checklist JSON file.",
    )
    parser.add_argument(
        "--catalog",
        type=Path,
        required=True,
        help="Path to the component catalog index.json.",
    )
    parser.add_argument(
        "--session-set",
        type=str,
        required=True,
        help="Session set slug (e.g., 'sensitive-field-masking-audit').",
    )
    parser.add_argument(
        "--base-url",
        type=str,
        default="http://localhost:5010",
        help="Base URL of the app under test (default: %(default)s).",
    )
    parser.add_argument(
        "--auth-email",
        type=str,
        default="admin@acmecoffee.com",
        help="Email for seeded sign-in (default: %(default)s).",
    )
    parser.add_argument(
        "--active-role",
        type=str,
        default=None,
        help=(
            "Override the active role passed to the W0 CLI. When omitted, "
            "the runner infers the role from the checklist's first sign-in "
            "step via personas.json. Pass an explicit role here to bypass "
            "inference (e.g., for ad-hoc smoke tests under a non-default "
            "persona)."
        ),
    )
    parser.add_argument(
        "--skip-triage",
        action="store_true",
        help="Skip triage even when failures exist (useful for testing).",
    )
    parser.add_argument(
        "--skip-dotnet-test",
        action="store_true",
        help="Stop after compile; do not run dotnet test.",
    )

    args = parser.parse_args()

    try:
        report = runner.run_checklist(
            checklist_path=args.checklist,
            catalog_path=args.catalog,
            session_set_slug=args.session_set,
            base_url=args.base_url,
            auth_email=args.auth_email,
            skip_triage=args.skip_triage,
            skip_dotnet_test=args.skip_dotnet_test,
            active_role=args.active_role,
        )
    except (FileNotFoundError, RuntimeError) as e:
        print(f"Error: {e}", file=sys.stderr)
        return 1

    if not report.compile_ok:
        print(f"Compilation failed:\n{report.compile_output}", file=sys.stderr)
        if report.runner_message:
            print(f"\nRunner: {report.runner_message}", file=sys.stderr)
        return 1

    if not report.test_invoked:
        if report.runner_message:
            print(f"Test not invoked. {report.runner_message}")
        else:
            print("Test not invoked (compile OK, but no test run).")
        return 0

    passed_count = len(report.passed)
    failed_count = len(report.failed)
    triage_count = len(report.triage_results)
    # Count how many triage results are human-review (not auto-applied CATALOG_STALE)
    human_review_count = sum(
        1 for tr in report.triage_results
        if tr.classification != "CATALOG_STALE"
    )

    print(f"\n{'='*60}")
    print(f"UAT Run Report: {args.session_set}")
    print(f"{'='*60}")
    print(f"Passed:           {passed_count}")
    print(f"Failed:           {failed_count}")
    if triage_count > 0:
        print(f"Triaged:          {triage_count}")
        auto_recovered = sum(
            1 for tr in report.triage_results
            if tr.classification == "CATALOG_STALE"
        )
        print(f"  Auto-recovered: {auto_recovered}")
        print(f"  Human review:   {human_review_count}")
    print(f"Triage cost:      ${report.triage_total_cost_usd:.4f}")
    print(f"{'='*60}\n")

    if report.runner_message:
        print(f"Runner message: {report.runner_message}")

    if human_review_count > 0:
        print(
            f"\n⚠️  {human_review_count} issue(s) filed for human review. "
            f"Check the session-set issues/ directory."
        )
        return 1

    if report.has_failures and triage_count == 0:
        print("\n⚠️  Test failures detected but triage was skipped.")
        return 1

    print("\n✓ All checks passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
