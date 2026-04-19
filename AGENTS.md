# AGENTS.md -- Dabbler Platform (Codex)

## Project Overview

Integrated repo for the AcmeCoffeeOnline rebuild, required Dabbler libraries,
and the AI-led workflow assets used to keep app, library, and test work in
one canonical place.

Current intended development mode: use integrated source references under
`src/Libs/`. Do not add new sibling-repo dependencies unless a session spec
explicitly requires an import from a sibling repo.

## Required reading before every session

- `README.md` -- repo layout and build entry points
- `docs/planning/project-guidance.md` -- durable principles + conventions
- `docs/planning/lessons-learned.md` -- repo-specific failure patterns and
  tactics learned from prior sessions
- `docs/ai-led-session-workflow.md` -- the authoritative session-set
  workflow. This is the single source of truth for the step-by-step
  procedure, the AI router usage, cross-provider verification, UAT checklist
  handling, the Step 9 reorganization review (run after the Step 8 notify
  so the human is not blocking the completion signal), **the Delegation
  Discipline rules (when to route vs. handle directly)**, and the
  mandatory rules list. Do not duplicate that content here.

When the human says **"Start the next session"**, follow the workflow in
`docs/ai-led-session-workflow.md` exactly. What follows below is only the
Codex-specific bootstrap the workflow needs.

## Codex-specific bootstrap

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

```python
import importlib.util, sys

def load_ai_router():
    spec = importlib.util.spec_from_file_location(
        'ai_router', 'ai-router/__init__.py',
        submodule_search_locations=['ai-router'])
    mod = importlib.util.module_from_spec(spec)
    sys.modules['ai_router'] = mod
    spec.loader.exec_module(mod)
    return mod

ar = load_ai_router()
route = ar.route
```

Use `.venv/Scripts/python.exe` to run Python scripts on Windows. The same
module exposes `send_session_complete_notification()`, which reads
`PUSHOVER_API_KEY` and `PUSHOVER_USER_KEY` from the environment or Windows
User environment.

### Delegation Discipline (pointer)

Your role is orchestrator, not solo coder. Reasoning tasks -- code review,
security review, architecture, analysis, documentation, test generation,
session verification -- **always** go through `route()`. Only handle work
directly when it is mechanical, single-file, and under ~50 lines.

This is not cost paranoia; it is the design of the workflow. The router
already picks the cheapest capable model and applies the correct reasoning
depth from `router-config.yaml` / `router-tuning.json`. Your job is to
pass the right `task_type` and let the router handle the rest.

See `docs/ai-led-session-workflow.md` -> **Delegation Discipline** for the
full criteria, the human-tunable thresholds (in `ai-router/router-tuning.json`),
and the rationale.

## Building & Testing

```bash
dotnet build Dabbler.Platform.sln
dotnet test Dabbler.Platform.sln
docker compose -f docker/docker-compose.yml up --build --abort-on-container-exit playwright
```

## Solution Structure

- `src/Apps/AcmeCoffeeOnline.Web/` -- Blazor Server web app
- `src/Apps/AcmeCoffeeOnline.Api/` -- ASP.NET Core Web API
- `src/Libs/` -- integrated Dabbler libraries
- `tests/` -- xUnit, bUnit, and Playwright tests
- `docker/` -- local and CI-like compose stack
- `ai-router/` -- routing code, prompt templates, and workflow utilities
- `docs/` -- workflow docs and session sets

Code style conventions (namespaces, casing, nullable, `Async` suffix) live in
`docs/planning/project-guidance.md` -> Conventions -> Code Style.
