"""5-iteration deterministic smoke driver for w0-cli-suppressions-and-role-pinning.

Per the session-set spec (Step 6, "Quality bar: 5/5 PASS"), invoke the W2
runner 5 times against the live compose stack at http://localhost:5010 and
emit a pass/fail summary. Between iterations, clear the dev email inbox via
the test-auth backdoor — magic-link sign-in is test setup (not the SUT), and
without the wipe MailHog/DevEmailInbox accumulates rows that trip Playwright's
strict-mode row matcher.

The script writes per-iteration .trx outcomes plus a JSON summary into
``smoke/run-5x-results.json`` for the change log.
"""

from __future__ import annotations

import json
import subprocess
import sys
import time
import urllib.request
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[4]
SMOKE_DIR = Path(__file__).resolve().parent
CHECKLIST = SMOKE_DIR / "w0-cli-suppressions-and-role-pinning-uat-checklist.json"
CATALOG = REPO_ROOT / "docs/component-catalog/index.json"
RESULTS = SMOKE_DIR / "run-5x-results.json"

WEB_BASE = "http://localhost:5010"
TEST_AUTH_SECRET = "acme-local-seeded-session"
ITERATIONS = 5


def clear_dev_email_inbox() -> int:
    req = urllib.request.Request(
        f"{WEB_BASE}/test-auth/magic-links?clear=true&email=admin@acmecoffee.com",
        method="GET",
        headers={"X-Test-Auth-Secret": TEST_AUTH_SECRET},
    )
    with urllib.request.urlopen(req, timeout=10) as resp:
        return resp.status


def run_one_iteration(iteration: int) -> dict:
    clear_status = clear_dev_email_inbox()
    started = time.time()
    proc = subprocess.run(
        [
            sys.executable,
            str(REPO_ROOT / "ai-router/uat_runner/cli.py"),
            "--checklist", str(CHECKLIST),
            "--catalog", str(CATALOG),
            "--session-set", "w0-cli-suppressions-and-role-pinning",
            "--active-role", "SystemAdmin",
            "--skip-triage",
        ],
        cwd=REPO_ROOT,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
        timeout=900,
    )
    duration = round(time.time() - started, 1)
    passed = "Failed:           0" in proc.stdout
    return {
        "iteration": iteration,
        "passed": passed,
        "duration_sec": duration,
        "exit_code": proc.returncode,
        "inbox_clear_http": clear_status,
        "tail": "\n".join(proc.stdout.splitlines()[-12:]),
    }


def main() -> int:
    results = []
    for i in range(1, ITERATIONS + 1):
        r = run_one_iteration(i)
        marker = "PASS" if r["passed"] else "FAIL"
        print(f"  iteration {i}/{ITERATIONS}: {marker} ({r['duration_sec']}s)")
        results.append(r)
    pass_count = sum(1 for r in results if r["passed"])
    summary = {
        "iterations": ITERATIONS,
        "passed": pass_count,
        "failed": ITERATIONS - pass_count,
        "results": results,
    }
    RESULTS.write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(f"\n{pass_count}/{ITERATIONS} PASS — wrote {RESULTS}")
    return 0 if pass_count == ITERATIONS else 1


if __name__ == "__main__":
    sys.exit(main())
