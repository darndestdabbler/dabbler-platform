"""Session 3 cross-provider verification for security-dashboard-uat-remediation.

Uses max_tier=2 to route to Gemini Pro (non-Anthropic) because Sessions 1 and
2 both used Anthropic opus. Writes the verifier's review to
session-reviews/session-003.md and returns the route result for logging.
"""

import importlib.util
import json
import subprocess
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
SPEC = REPO / "docs/session-sets/security-dashboard-uat-remediation/spec.md"
LOG = REPO / "docs/session-sets/security-dashboard-uat-remediation/activity-log.json"
OUT = REPO / "docs/session-sets/security-dashboard-uat-remediation/session-reviews/session-003.md"


def load_ai_router():
    spec = importlib.util.spec_from_file_location(
        "ai_router",
        str(REPO / "ai-router/__init__.py"),
        submodule_search_locations=[str(REPO / "ai-router")],
    )
    mod = importlib.util.module_from_spec(spec)
    sys.modules["ai_router"] = mod
    spec.loader.exec_module(mod)
    return mod


def extract_session3_log():
    data = json.loads(LOG.read_text(encoding="utf-8"))
    entries = [e for e in data["entries"] if e["sessionNumber"] == 3]
    return json.dumps(entries, indent=2)


def extract_finding2_and_session3_scope():
    text = SPEC.read_text(encoding="utf-8")
    # Grab Finding 2 block
    start = text.index("### 2. Per-role tab gating")
    end = text.index("### 3. Organization scope filter")
    finding2 = text[start:end].strip()
    # Grab Session 3 block
    s3_start = text.index("### Session 3 of 8")
    s3_end = text.index("### Session 4 of 8")
    session3 = text[s3_start:s3_end].strip()
    # Grab Finding 10 block
    f10_start = text.index("### 10. Docker test infrastructure")
    f10_end = text.index("## Goals")
    finding10 = text[f10_start:f10_end].strip()
    return finding2, session3, finding10


def run_git(*args) -> str:
    r = subprocess.run(
        ["git", "-C", str(REPO), *args],
        capture_output=True, text=True, check=True, encoding="utf-8"
    )
    return r.stdout


def main():
    ar = load_ai_router()

    finding2, session3_scope, finding10 = extract_finding2_and_session3_scope()
    activity = extract_session3_log()
    diff = run_git("diff", "--unified=5", "HEAD", "--",
                   "src/", "docker/", "tests/AcmeCoffeeOnline.Playwright/")
    status = run_git("status", "--porcelain")

    content = f"""
You are reviewing Session 3 of the `security-dashboard-uat-remediation` session
set. Two prior sessions (1 and 2) were already verified by Anthropic Opus. This
Session 3 verification intentionally routes to you (non-Anthropic) for
cross-provider review.

## Session 3 — assigned scope (Finding 2)

{finding2}

## Session 3 plan from spec.md

{session3_scope}

## Activity log entries for Session 3

```json
{activity}
```

## Newly filed Finding 10 tracking four residual failures

{finding10}

## Full working-tree diff (src/, docker/, tests/AcmeCoffeeOnline.Playwright/)

```diff
{diff}
```

## Git status

```
{status}
```

## What I need you to verify

1. **Finding 2 closure**: Does the source change correctly make
   `/admin/security` reachable and the nav link visible for all five
   baked-in roles while preserving per-tab compiled-privilege gating?
2. **New tests cover the contract**: Do the four new
   `SecurityDashboardUatCoverageTests` in the diff actually exercise
   the three role variants (SystemAdmin → 5 tabs, CrossOrgUserAdmin →
   3 tabs, baked-in User → 1 tab) plus the Session 2 Users-tab
   regression on the newly-permitted persona?
3. **Docker infrastructure fixes**: Are the four Docker fixes
   (Dockerfile --no-restore drop, MapStaticAssets placement + @Assets
   helper, AllowNonLoopback opt-in, web-service DB connection string)
   each applied correctly and safely? Flag any that weaken security
   in production (e.g., if AllowNonLoopback could leak into prod).
4. **Finding 10 framing**: Is Finding 10 scoped correctly as
   pre-existing pre-Session-3, and is its Session-4-Step-0 triage
   assignment clear enough to prevent indefinite deferral?
5. **Any regressions or missed scope** versus the session plan in
   spec.md?

Respond as a structured verification report: verdict first (pass /
minor issues / major issues), then per-item findings with file paths
and line numbers where applicable. Be precise; avoid generic praise.
""".strip()

    result = ar.route(
        content=content,
        task_type="session-verification",
        max_tier=2,
        session_set="security-dashboard-uat-remediation",
        session_number=3,
    )

    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(
        f"# Verification Round 1\n\n"
        f"Model: `{result.model_name}` | Tokens in: {result.input_tokens} | "
        f"Tokens out: {result.output_tokens} | Cost: ${result.cost_usd:.4f}\n\n"
        f"---\n\n"
        f"{result.content}\n",
        encoding="utf-8",
    )

    summary = {
        "model": result.model_name,
        "taskType": "session-verification",
        "inputTokens": result.input_tokens,
        "outputTokens": result.output_tokens,
        "costUsd": result.cost_usd,
        "reviewFile": str(OUT.relative_to(REPO)).replace("\\", "/"),
    }
    print(json.dumps(summary, indent=2))


if __name__ == "__main__":
    main()
