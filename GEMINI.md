# GEMINI.md -- Dabbler Platform (Gemini)

## Project Overview

Integrated repo for the AcmeCoffeeOnline rebuild, required Dabbler libraries,
and the AI-led workflow assets used to develop from one canonical solution.

Read `README.md` for the current repo layout and build entry points.
Read `docs/ai-led-session-workflow.md` for the complete session-set workflow.
Read `docs/platform-guardrails.md` and `docs/dev-test-auth-modes.md` before
changing auth helpers, browser coverage, or workflow assets.

Current intended development mode: use integrated source references under
`src/Libs/`. Do not add new sibling-repo dependencies unless a session spec
explicitly requires an import from a sibling repo.

## AI Router

This project includes an AI Router module (`ai-router/`) for routing reasoning
tasks to cheaper models and verifying results cross-provider.

### When to use the router

- Code review: `route(code, task_type="code-review")` -- auto-verified
- Security review: `route(code, task_type="security-review")` -- auto-verified
- Documentation: `route(code, task_type="documentation")`
- Analysis: `route(question, task_type="analysis")`
- Test generation: `route(code, task_type="test-generation")`

Do NOT route file creation, shell commands, or anything needing the filesystem.

### How to use it

The module folder is `ai-router/` (hyphen). Import via `importlib` since Python
cannot import hyphenated names:

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

result = route(
    content="<the text to analyze>",
    task_type="code-review",
    context="<optional background>"
)
print(result.content)
print(result.total_cost_usd)
```

Use `.venv/Scripts/python.exe` to run Python scripts on Windows.
The same module also exposes reusable notification helpers, including
`send_session_complete_notification()`, which reads `PUSHOVER_API_KEY` and
`PUSHOVER_USER_KEY` from the environment or Windows User environment.

## Session Set Workflow

When the human says **"Start the next session"**, follow the procedure in
`docs/ai-led-session-workflow.md` exactly. Summary:

### Step 0: Verify API Keys

```bash
export GEMINI_API_KEY=$(powershell -Command "[System.Environment]::GetEnvironmentVariable('GEMINI_API_KEY', 'User')" | tr -d '\r')
export ANTHROPIC_API_KEY=$(powershell -Command "[System.Environment]::GetEnvironmentVariable('ANTHROPIC_API_KEY', 'User')" | tr -d '\r')
.venv/Scripts/python.exe -c "
import os
missing = [k for k in ('ANTHROPIC_API_KEY', 'GEMINI_API_KEY') if not os.environ.get(k)]
if missing:
    raise SystemExit(f'Missing environment variables: {missing}')
print('API keys OK')
"
```

### Step 1: Identify the Active Session Set

```python
from ai_router.session_log import find_active_session_set, SessionLog

SESSION_SET = find_active_session_set("docs/session-sets")
log = SessionLog(SESSION_SET)
next_session = log.get_next_session_number()
```

### Steps 2-4: Read Spec, Check Prerequisites, Execute Steps

See `docs/ai-led-session-workflow.md` Steps 2-4.

- File creation/modification: do directly, then `log.log_step()`
- Reasoning tasks: `route()` first, then `log.log_step()` with cost data

### Step 5: Build and Test

```bash
dotnet build
dotnet test
```

### Step 6: End-of-Session Verification (MANDATORY)

You MUST NOT verify your own work. Call `route(task_type="session-verification")`
to dispatch to an external AI model for independent review.

### Step 7: Cost Report, Commit, Notify, Stop

```python
from ai_router import print_cost_report
print_cost_report(SESSION_SET)
```

```bash
git add -A
git commit -m "Session N of M: <description>"
git push
```

```python
from ai_router import send_session_complete_notification

try:
    send_session_complete_notification(
        session_set=SESSION_SET,
        session_number=next_session,
        total_sessions=log.total_sessions,
        verification_verdict="VERIFIED",
        summary="<short description>"
    )
except Exception as exc:
    print(f"Session completed, but Pushover notification failed: {exc}")
```

If Pushover keys are configured, send the notification after a successful push.
If the notification fails, report it, but do not treat the session itself as
failed or roll back the successful work.

Do NOT start the next session. Wait for the human.

## Rules

- **One session only.** Never execute more than your assigned session.
- **Never skip verification.** Every session must be independently verified.
- **Never edit session review files.** They are the verifier's raw output.
- **Log every step** via `log.log_step()`.
- **Delegate reasoning** to `route()` -- do not perform reviews or analysis
  yourself when the task should go through the router.

## Code Style

- Namespaces: `AcmeCoffeeOnline.Web`, `AcmeCoffeeOnline.Api`,
  `AcmeCoffeeOnline.Shared`, and `Dabbler.*` for integrated libraries
- PascalCase public, `_camelCase` private
- Nullable reference types enabled
- All event handlers return `Task`, suffix `Async`

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
