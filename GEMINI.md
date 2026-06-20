# GEMINI.md -- Dabbler Platform (Gemini)

## Project Overview

Integrated repo for the AcmeCoffeeOnline rebuild, required Dabbler libraries,
and the AI-led workflow assets used to develop from one canonical solution.

Current intended development mode: use integrated source references under
`src/Libs/`. Do not add new sibling-repo dependencies unless a session spec
explicitly requires an import from a sibling repo.

## Required reading before every session

- `README.md` -- repo layout and build entry points
- `docs/planning/project-guidance.md` -- durable principles + conventions
- `docs/planning/lessons-learned.md` -- repo-specific failure patterns and
  tactics learned from prior sessions
- [`session-set-authoring-guide.md`](https://github.com/darndestdabbler/dabbler-ai-orchestration/blob/master/docs/planning/session-set-authoring-guide.md)
  (canonical, lives in `dabbler-ai-orchestration`) -- how to author or update a
  session-set spec, including the Session Set Configuration block
  (`requiresUAT` / `requiresE2E` / `uatScope` / `tier` / `prerequisites`),
  When-UAT-Is-Required and When-E2E-Is-Required heuristics, slug naming,
  sizing, deliverables, and anti-patterns. Pair with the local
  `docs/planning/session-set-authoring-guide.platform-addendum.md` for the
  dabbler-platform-specific extensions (Blazor / MudBlazor / Playwright /
  AcmeCoffeeOnline conventions).
- [`ai-led-session-workflow.md`](https://github.com/darndestdabbler/dabbler-ai-orchestration/blob/master/docs/ai-led-session-workflow.md)
  (canonical, lives in `dabbler-ai-orchestration`) -- the authoritative
  session-set workflow. This is the single source of truth for the
  step-by-step procedure, the AI router usage, cross-provider verification,
  UAT checklist handling (now conditional on the spec's `requiresUAT` flag),
  **the Step 7 verifier-disagreement resolution procedure (four human
  options, disagreement logging)**, the Step 9 reorganization review (run
  after the Step 8 notify so the human is not blocking the completion
  signal), **the Delegation Discipline rules (when to route vs. handle
  directly)**, and the mandatory rules list. Do not duplicate that content
  here.
- [`greenfield-matrix-protocol.md`](https://github.com/darndestdabbler/dabbler-ai-orchestration/blob/master/docs/greenfield-matrix-protocol.md)
  (canonical, lives in `dabbler-ai-orchestration`) -- **this repo is the LEAD
  signal in the Set 075 greenfield finding-power pilot.** On
  **source-code-bearing build sessions** (skip doc / planning / mechanical
  ones), run the provider×surface verification matrix at **Step 6, BEFORE
  remediation**, following the standard instruction addendum
  [`greenfield-matrix-addendum.md`](https://github.com/darndestdabbler/dabbler-ai-orchestration/blob/master/ai_router/prompt-templates/greenfield-matrix-addendum.md);
  adjudicate every finding against the fixed
  [`greenfield-adjudication-rubric.md`](https://github.com/darndestdabbler/dabbler-ai-orchestration/blob/master/docs/greenfield-adjudication-rubric.md);
  freeze the run and ship the `matrix-run/` telemetry back to the canonical
  set dir (`telemetry/dabbler-platform/<session>/`). Requires
  `dabbler-ai-router >= 0.26.0` (see `requirements.txt`). The matrix varies
  *provider*, not framing (both arms stay strong adversarial -- L-069-2).

The human starts a session with one of the trigger phrases listed in
`docs/ai-led-session-workflow.md` -> **Trigger Phrases** (sequential,
named-set, parallel-worktree, optional ` -- maxout <engine>` suffix).
That section is the canonical list; do not re-list trigger phrases
here.

What follows below is only the Gemini-specific bootstrap the workflow needs.

## Gemini-specific bootstrap

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

### AI router import

The router is the `dabbler-ai-router` PyPI package (canonical
dabbler-ai-orchestration repo, Set 010 onwards). It is declared in
`requirements.txt` and lives in the workspace `.venv`. Bare imports
work:

```python
from ai_router import route, send_session_complete_notification
from ai_router.session_log import SessionLog
```

The platform-specific UAT runner that used to live at
`ai-router/uat_runner/` is now a top-level `uat_runner/` package
alongside the pip-installed canonical router:

```python
from uat_runner.runner import run_checklist
from uat_runner.triage import FailureRecord, TriageSession
```

The operator-tuned config lives at `ai_router/router-config.yaml`
(workspace-relative, not inside the venv). Set the env var so
`route()` reads it:

```bash
export AI_ROUTER_CONFIG=ai_router/router-config.yaml
export AI_ROUTER_METRICS_PATH=ai_router/router-metrics.jsonl
```

Use `.venv/Scripts/python.exe` to run Python scripts on Windows.
`send_session_complete_notification()` reads `PUSHOVER_API_KEY` and
`PUSHOVER_USER_KEY` from the environment or Windows User environment.

If `pip install` of the router needs to be re-run (fresh clone, dropped
venv, etc.), the canonical extension's **Dabbler: Install ai-router**
command does it from VS Code: PyPI install (default) or GitHub
sparse-checkout fallback for offline / pre-release / fork scenarios.

### Delegation Discipline (pointer)

Your role is orchestrator, not solo coder. Reasoning tasks -- code review,
security review, architecture, analysis, documentation, test generation,
session verification -- **always** go through `route()`. Only handle work
directly when it is mechanical, single-file, and under ~50 lines.

This is not cost paranoia; it is the design of the workflow. The router
already picks the cheapest capable model and applies the correct reasoning
depth from `router-config.yaml`. Your job is to pass the right `task_type`
and let the router handle the rest.

See `docs/ai-led-session-workflow.md` -> **Delegation Discipline** for the
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
services (api, web) with explicit `image:` tags so rebuilds replace tagged
images instead of orphaning dangling layers. Playwright is intentionally
**not** a compose service -- run it from the host via `dotnet test` against
`http://localhost:5010`.

## Solution Structure

- `src/Apps/AcmeCoffeeOnline.Web/` -- Blazor Server web app
- `src/Apps/AcmeCoffeeOnline.Api/` -- ASP.NET Core Web API
- `src/Libs/` -- integrated Dabbler libraries
- `tests/` -- xUnit, bUnit, and Playwright tests
- `docker/` -- local and CI-like compose stack
- `templates/Dabbler.Templates/` -- `dotnet new` template package (`dabbler-app`,
  `dabbler-min`). The `dabbler-app` template ships a `docker/` subfolder with a
  split compose stack (SQL Server + Mailpit infra + Web + Api app services),
  per-project Dockerfiles, `appsettings.Docker.json`, and a `packages/` local
  NuGet feed for Dabbler.* packages. Consumers copy `pack-output/*.nupkg` into
  `packages/` and run two `docker compose` commands to bring the full stack up.
- `ai_router/` -- operator-tuned `router-config.yaml` and the
  append-only `router-metrics.jsonl` cost telemetry; the router
  itself is `pip install dabbler-ai-router` (declared in
  `requirements.txt`)
- `uat_runner/` -- platform-specific UAT runner package (CLI +
  triage + remediations + test_cases). Calls into the canonical
  router via `import ai_router`.
- `docs/` -- workflow docs and session sets

Code style conventions (namespaces, casing, nullable, `Async` suffix) live in
`docs/planning/project-guidance.md` -> Conventions -> Code Style.

If a file-system operation (typically `mv`, `rm -rf`, or `git worktree
remove`) fails with a Windows file-lock error, run
`.venv/Scripts/python.exe -m ai_router.utils cleanup-dev-orphans`
first -- it shuts down dotnet build servers, kills stale Claude Code
background polling loops, and clears orphan `conhost.exe`. Pass
`--match-path dabbler-platform` to scope the polling-loop kill to this
repo only. Each category is also runnable on its own
(`kill-dotnet-build-servers`, `kill-stale-claude-polls`,
`kill-conhost`). Source lives in the canonical
`dabbler-ai-orchestration` repo at `ai_router/utils.py` Section 5;
the same code is what gets pip-installed into `.venv`.
