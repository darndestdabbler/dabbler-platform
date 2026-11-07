"""CLI entry point for the UAT runner.

Usage::

    python -m uat_runner.cli \\
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
import subprocess
import sys
import time
from pathlib import Path

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
if hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")

# Module is invoked either as a script (``python uat_runner/cli.py``) or
# imported as ``uat_runner.cli`` after a caller has already imported
# ``uat_runner``. The script invocation has no parent package, so a relative
# ``from . import runner`` would fail. The historical bootstrap that loaded
# the dashed ``ai-router`` package via importlib is no longer needed — the
# router is on PyPI as ``dabbler-ai-router``, ``ai_router`` imports natively,
# and ``uat_runner`` lives at the workspace root as its own package.
if __package__ in (None, ""):
    sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
    from uat_runner import runner
else:
    from . import runner


_APP_COMPOSE_FILE = Path("docker/docker-compose.app.yml")


def _checklist_requires_reset(checklist_path: Path) -> bool:
    """Return True when the checklist JSON declares
    `RequiresAppStateReset: true` at the top level. The flag is the
    canonical signal a checklist mutates persisted application state
    (MaskExemption grants, RoleSwitching writes, OrganizationSwitching
    overrides, etc.) and therefore needs the container's writable
    layer reset before the next run. Missing or malformed file falls
    back to False — the CLI's `--reset-app-state` flag is still
    available for ad-hoc forcing."""
    try:
        import json as _json
        with checklist_path.open(encoding="utf-8") as f:
            data = _json.load(f)
        return bool(data.get("RequiresAppStateReset"))
    except (OSError, ValueError):
        return False


def _reset_app_state_or_warn() -> None:
    """Force-recreate web + api containers so the next run starts from a
    clean writable layer. Skips if compose isn't available (developer is
    running services from `dotnet run` or similar) — prints a warning so
    the user knows persisted state from a prior run may bleed in."""
    if not _APP_COMPOSE_FILE.exists():
        print(
            f"[reset-state] {_APP_COMPOSE_FILE} not found; skipping reset. "
            "If the app is running outside compose, restart it manually before re-running."
        )
        return

    print("[reset-state] Force-recreating web + api containers (~10s) so "
          "persisted state from the prior run does not bleed in...")
    cmd = [
        "docker", "compose",
        "-f", str(_APP_COMPOSE_FILE),
        "up", "--force-recreate", "-d",
        "web", "api",
    ]
    proc = subprocess.run(cmd, capture_output=True, text=True, timeout=240)
    if proc.returncode != 0:
        print(
            f"[reset-state] WARNING: docker compose returned {proc.returncode}.\n"
            f"stdout: {proc.stdout}\nstderr: {proc.stderr}\n"
            "Continuing — but persisted state may bleed in."
        )
        return
    # Give the web container a moment to finish startup before the test
    # connects. The compose `Healthy` gate covers the api; the web has no
    # healthcheck wired today so a short wait is the simplest guard.
    time.sleep(6)
    print("[reset-state] Containers recreated.")


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
    parser.add_argument(
        "--reset-app-state",
        action="store_true",
        help=(
            "Force-recreate the web (and api, when present) docker compose "
            "containers before running the checklist. Required when a UAT "
            "step mutates state that the SecurityConfigurationManager "
            "persists to the container's writable layer (e.g., MaskExemption "
            "grants in UAT-2 of sensitive-field-masking-audit-uat-remediation). "
            "Without this flag, a re-run will start with the prior run's "
            "writes still in place, causing UAT-1 expectations to fail "
            "non-deterministically. The flag adds ~10 seconds per run; use "
            "it whenever the checklist contains an exemption-grant or any "
            "other persisted state mutation."
        ),
    )

    args = parser.parse_args()

    # Honor the checklist's `RequiresAppStateReset` flag — the canonical
    # signal a checklist mutates persisted state and therefore needs the
    # container's writable layer reset before each run. The CLI flag is
    # also accepted, so an operator can force a reset for ad-hoc runs.
    needs_reset = args.reset_app_state or _checklist_requires_reset(args.checklist)
    if needs_reset:
        _reset_app_state_or_warn()

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
