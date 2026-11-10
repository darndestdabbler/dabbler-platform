# Refresh response for `CLAUDE.md`

- Model: gemini-pro
- Input tokens: 2832
- Output tokens: 1861
- Cost: $0.0222

---

```markdown
# CLAUDE.md — Dabbler Platform

## Project Overview

Integrated repo for the AcmeCoffeeOnline rebuild, required Dabbler libraries,
and the AI-led workflow assets used to develop from one canonical solution.

Development uses the integrated libraries under `src/Libs/`. New
cross-repository dependencies should be consumed as versioned packages
(e.g., NuGet), not direct source inclusions, unless a session spec explicitly
requires it for a specific tooling purpose.

## Required reading before every session

- `README.md` — repo layout and build entry points
- `docs/planning/project-guidance.md` — durable principles + conventions
- `docs/planning/lessons-learned.md` — repo-specific failure patterns and
  tactics learned from prior sessions
- `docs/planning/session-set-authoring-guide.md` — how to author or update a
  session-set spec, including the Session Set Configuration block
  (`requiresUAT` / `requiresE2E` / `uatScope`), When-UAT-Is-Required and
  When-E2E-Is-Required heuristics, slug naming, sizing, deliverables, and
  anti-patterns
- `docs/ai-led-session-workflow.md` — the authoritative session-set workflow.
  This is the single source of truth for the step-by-step procedure, the AI
  router usage, cross-provider verification, UAT checklist handling (now
  conditional on the spec's `requiresUAT` flag), **the Step 7
  verifier-disagreement resolution procedure (four human options, disagreement
  logging)**, the Step 9 reorganization review (run after the Step 8 notify
  so the human is not blocking the completion signal), **the Delegation
  Discipline rules (when to route vs. handle directly)**, and the mandatory
  rules list. Do not duplicate that content here.

The human starts a session with one of the trigger phrases listed in
`docs/ai-led-session-workflow.md` → **Trigger Phrases** (sequential,
named-set, parallel-worktree, optional `— maxout <engine>` suffix).
That section is the canonical list; do not re-list trigger phrases
here.

What follows below is only the Claude-specific bootstrap the workflow needs.

## Claude-specific bootstrap

### API key export (Windows)

```bash
export GEMINI_API_KEY=$(powershell -Command "[System.Environment]::GetEnvironmentVariable('GEMINI_API_KEY', 'User')" | tr -d '\r')
export ANTHROPIC_API_KEY=$(powershell -Command "[System.Environment]::GetEnvironmentVariable('ANTHROPIC_API_KEY', 'User')" | tr -d '\r')
export OPENAI_API_KEY=$(powershell -Command "[System.Environment]::GetEnvironmentVariable('OPENAI_API_KEY', 'User')" | tr -d '\r')
.venv/Scripts/python.exe -c "
import os
missing = [k for k in ('ANTHROPIC_API_KEY', 'GEMINI_API_KEY', 'OPENAI_API_KEY') if not os.environ.get(k)]
if missing:
    raise SystemExit(f'Missing environment variables: {missing}')
print('API keys OK')
"
```

If keys are missing, stop and tell the human.

### AI Router and UAT Runner

The **AI router** is the `dabbler-ai-router` PyPI package, declared in
`requirements.txt` and installed into the workspace `.venv`. Bare imports
work for orchestration tasks:

```python
from ai_router import route, send_session_complete_notification
from ai_router.session_log import SessionLog
```

The operator-tuned router config lives at `ai_router/router-config.yaml`.
Set the environment variables so `route()` can find its configuration and
metrics files:

```bash
export AI_ROUTER_CONFIG=ai_router/router-config.yaml
export AI_ROUTER_METRICS_PATH=ai_router/router-metrics.jsonl
```

The **UAT runner** is the top-level `uat_runner/` package. It is invoked
from the command line, not imported directly in session code:

```bash
# Example invocation
.venv/Scripts/python.exe -m uat_runner.cli --checklist-path docs/session-sets/core-api/001-initial-setup/uat.md
```

`send_session_complete_notification()` reads `PUSHOVER_API_KEY` and
`PUSHOVER_USER_KEY` from the environment.

If `pip install` needs to be re-run, the canonical VS Code extension's
**Dabbler: Install ai-router** command handles it.

### Delegation Discipline (pointer)

Your role is orchestrator, not solo coder. Reasoning tasks — code review,
security review, architecture, analysis, documentation, test generation,
session verification — **always** go through `route()`. Only handle work
directly when it is mechanical, single-file, and under ~50 lines.

This is not cost paranoia; it is the design of the workflow. The router
already picks the cheapest capable model and applies the correct reasoning
depth from `router-config.yaml`. Your job is to pass the right `task_type`
and let the router handle the rest.

See `docs/ai-led-session-workflow.md` → **Delegation Discipline** for the
full criteria, the human-tunable thresholds (in `ai_router/router-config.yaml`
under `delegation:`), and the rationale.

## Building & Testing

```bash
dotnet build Dabbler.Platform.sln
dotnet test Dabbler.Platform.sln

# E2E (Playwright runs from the host against the app containers):
docker compose -f docker/docker-compose.infra.yml up -d        # sqlserver + mailhog (start once, leave up)
docker compose -f docker/docker-compose.app.yml   up --build -d # api + web (rebuild after code changes)
dotnet test tests/AcmeCoffeeOnline.Playwright/
```

The compose stack is split: `docker-compose.infra.yml` holds the persistent
dev dependencies (sqlserver, mailhog, the `dabbler-dev` network) and stays
up across sessions. `docker-compose.app.yml` holds the rebuildable app
services (api, web). Playwright is intentionally **not** a compose service — run
it from the host via `dotnet test` against `http://localhost:5010`.

## Solution Structure

- `src/Apps/AcmeCoffeeOnline.Web/` — Blazor Server web app
- `src/Apps/AcmeCoffeeOnline.Api/` — ASP.NET Core Web API
- `src/Libs/` — integrated Dabbler libraries
- `tests/` — xUnit, bUnit, and Playwright tests
- `docker/` — local and CI-like compose stack
- `ai_router/` — operator-tuned `router-config.yaml` and the
  append-only `router-metrics.jsonl` cost telemetry; the router
  itself is `pip install dabbler-ai-router` (declared in
  `requirements.txt`)
- `uat_runner/` — platform-specific UAT runner package (CLI +
  triage + remediations + test_cases). Calls into the canonical
  router via `import ai_router`.
- `docs/` — workflow docs and session sets

Code style conventions (namespaces, casing, nullable, `Async` suffix) live in
`docs/planning/project-guidance.md` → Conventions → Code Style.

If a file-system operation fails with a Windows file-lock error, run
`.venv/Scripts/python.exe -m ai_router.utils cleanup-dev-orphans`
first — it shuts down dotnet build servers and kills other orphan processes.
Pass `--match-path dabbler-platform` to scope the process kill to this
repo only. Each category is also runnable on its own
(`kill-dotnet-build-servers`, `kill-stale-claude-polls`,
`kill-conhost`). The source is part of the pip-installed `dabbler-ai-router`
package.
```

---
**Verification (gpt-5-4-mini, openai):** ISSUES_FOUND

**Issue 1** [unknown / unknown]: NO CHANGES NEEDED

The revision fixes the stale `ai-router/uat_runner/` coupling, keeps the router and UAT paths aligned with current platform truth, and preserves the consumer-relevant bootstrap/build guidance without reintroducing retired patterns. Minor wording simplifications do not materially change correctness.
