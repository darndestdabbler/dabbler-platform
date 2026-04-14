# CLAUDE.md — Dabbler Platform

## Project Overview

Integrated repo for the AcmeCoffeeOnline rebuild, required Dabbler libraries,
and the AI-led workflow assets used to develop from one canonical solution.

Read `README.md` for the current repo layout and build entry points.
Read `docs/ai-led-session-workflow.md` for the complete session-set workflow.

Current intended development mode: use integrated source references under
`src/Libs/`. Do not add new sibling-repo dependencies unless a session spec
explicitly requires an import from a sibling repo.

## AI Router

This project includes an AI Router module (`ai-router/`) for routing reasoning
tasks to cheaper models and verifying results cross-provider.

### When to use the router

- Code review → `route(code, task_type="code-review")` ← auto-verified
- Security review → `route(code, task_type="security-review")` ← auto-verified
- Documentation → `route(code, task_type="documentation")`
- Analysis → `route(question, task_type="analysis")`
- Test generation → `route(code, task_type="test-generation")`

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

## Session Set Workflow

When the human says **"Start the next session"**, follow this procedure exactly:

### Step 0: Verify API Keys

Load API keys from the Windows OS user environment and confirm both are present:

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

If keys are missing, stop and tell the human.

### Step 1: Identify the Active Session Set

Auto-detect the active session set:

```python
from ai_router.session_log import find_active_session_set

SESSION_SET = find_active_session_set("docs/session-sets")
print(f"Active session set: {SESSION_SET}")
```

Detection rules:
1. Folder with `spec.md` + `activity-log.json` but no `change-log.md` → **in-progress**
2. Folder with `spec.md` only → **not-started** (use if no in-progress exists)
3. Folder with `change-log.md` → **complete** (skip)

If multiple candidates are found, present them to the user and ask which to use.

### Step 2: Read the Activity Log

```python
from ai_router.session_log import SessionLog

log = SessionLog(SESSION_SET)
next_session = log.get_next_session_number()
```

If this is Session 1, read the total sessions from the spec and set
`log.total_sessions`.

### Step 3: Read the Spec

Open `spec.md` in the session set folder. Find the plan for the current
session number (e.g., "Session 3 of 5: ...").

### Step 4: Verify Prerequisites

Confirm that files listed in prior sessions' "Creates" and "Touches" sections
exist. If something is missing, report it and stop.

### Step 5: Execute the Session

For each step in the session plan:
1. **File work:** Do it directly.
2. **Reasoning tasks:** Use `route()` to route to the cheapest capable model.
3. **Log the step** via `log.log_step()`:
   ```python
   log.log_step(
       session_number=N,
       step_number=step_num,
       step_key="session-N/step-name",
       description="What was done",
       status="complete",
       api_calls=[{
           "model": result.model_name,
           "taskType": "code-review",
           "inputTokens": result.input_tokens,
           "outputTokens": result.output_tokens,
           "costUsd": result.cost_usd
       }]  # or [] if no router calls
   )
   ```

### Step 6: Build and Test

```bash
dotnet build
dotnet test
```

Log the result with `log.log_step()` before moving on.

### Step 7: End-of-Session Verification (MANDATORY)

**CRITICAL: You are the system under review. You MUST NOT verify your own work.**
The call to `route()` dispatches to a Gemini model — a separate AI provider.

1. Collect all files created or modified during this session.
2. Build a verification prompt with the spec excerpt + file contents + build results.
3. Execute:
   ```python
   result = route(
       content=verification_prompt,
       task_type="session-verification",
       complexity_hint=70
   )
   ```
4. Save the verifier's raw output:
   ```python
   log.save_session_review(session_number=N, review_text=result.content, round_number=1)
   ```
5. Do NOT edit the session review file.
6. Log the verification step with `log.log_step()`.

### Step 8: Handle Verification Result

**VERIFIED:** Proceed to Step 9.

**ISSUES_FOUND:**
1. Parse issues from the verifier's response.
2. Attempt fixes. Update status to "fixed" or "deferred".
3. Save issue log: `log.save_issue_log(session_number=N, issues=issues)`
4. Re-run verification (max 2 retries). Use `complexity_hint=85` if any
   issue is Major or Critical.

### Step 9: Cost Report and Completion

```python
from ai_router import print_cost_report
print_cost_report(SESSION_SET)
```

After a successful session, commit and push:

```bash
git add -A
git commit -m "Session N of M: <short description>

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
git push
```

### Step 10: Last Session Only

If this is the last session, generate `change-log.md` using
`route(task_type="documentation")`.

### Rules

- **One session only.** Never execute more than your assigned session.
- **Never skip verification.** Every session must be independently verified.
- **Never edit session review files.** They are the verifier's raw output.
- **Log every step** via `log.log_step()` — including build and verification.
- **Delegate reasoning** to `route()` — do not perform reviews or analysis
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

- `src/Apps/AcmeCoffeeOnline.Web/` — Blazor Server web app
- `src/Apps/AcmeCoffeeOnline.Api/` — ASP.NET Core Web API
- `src/Libs/` — integrated Dabbler libraries
- `tests/` — xUnit, bUnit, and Playwright tests
- `docker/` — local and CI-like compose stack
- `ai-router/` — routing code, prompt templates, and workflow utilities
- `docs/` — workflow docs and session sets
