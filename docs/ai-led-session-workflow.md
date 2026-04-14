# AI-Led Session-Set Workflow

This document describes the orchestration pattern used to develop features in
this repository. An AI coding agent (Claude Code, Codex, or a Gemini-based
tool) acts as the **orchestrator**, executing a predefined session plan one
session at a time. A separate **AI Router** Python module routes reasoning
tasks to cheaper external models and enforces cross-provider verification.

The orchestrator can change from session to session at the human's discretion.
All three orchestrators follow the same workflow -- only the instruction file
they read differs.

## Overview

```
Human
  |
  | "Start the next session"
  v
Orchestrator (Claude / Codex / Gemini)
  |
  |-- reads instruction file (CLAUDE.md / AGENTS.md / GEMINI.md)
  |-- reads spec.md in the active session set
  |-- reads activity-log.json for prior progress
  |
  |  For each step in the session plan:
  |    |-- file work: creates/edits files directly
  |    |-- reasoning: calls route() via ai-router Python module
  |    |     |-- router selects cheapest capable model
  |    |     |-- Gemini Flash (tier 1), Gemini Pro (tier 2),
  |    |     |   Sonnet (tier 2), or Opus (tier 3)
  |    |     +-- auto-verification for code-review/security-review
  |    |-- logs every step to activity-log.json
  |
  |-- runs dotnet build && dotnet test
  |
  |-- MANDATORY: end-of-session verification
  |     |-- sends all work to a DIFFERENT AI provider
  |     |-- saves raw verifier output (never edited)
  |     +-- fixes issues if found (max 2 retries)
  |
  |-- prints cost report
  |-- commits and pushes
  |-- sends session-complete notification (if configured)
  +-- STOPS (one session per conversation)
```

## Key Concepts

### Session Set

A session set is a planned body of work broken into sequential sessions. Each
session set lives in `docs/session-sets/<name>/` and contains:

| File | Purpose |
|---|---|
| `spec.md` | The full plan: goals, features, and per-session step lists |
| `activity-log.json` | Machine-readable log of every step across all sessions |
| `session-reviews/session-NNN.md` | Raw verifier output for each session (never edited) |
| `session-reviews/issues-NNN.json` | Issue log if verification found problems |
| `change-log.md` | Generated after the final session; marks the set as complete |

### Session

One unit of work within a session set. Sessions are numbered (Session 1 of 5,
Session 2 of 5, etc.). Each session:

- Has a defined list of steps in `spec.md`
- Is executed by exactly one orchestrator in one conversation
- Ends with a mandatory cross-provider verification
- Produces a commit on completion

### AI Router

A Python module in `ai-router/` that routes reasoning tasks to external AI
models. The orchestrator calls `route()` instead of performing analysis,
review, or documentation itself. The router:

- Estimates task complexity (1-100 score)
- Selects the cheapest model capable of handling the complexity
- Escalates to a higher-tier model if the response is poor
- Auto-verifies certain task types using a different AI provider
- Tracks costs per session set

### Cross-Provider Verification

Every session ends with an independent verification step. The orchestrator
sends its work to a model from a **different AI provider** than the one that
did the work. This catches provider-specific biases and blind spots:

- If the orchestrator is Claude and used Gemini for routing, verification
  goes to Opus or Sonnet (Anthropic)
- If the orchestrator is Codex/Gemini, verification goes to an Anthropic model
- The verifier's raw output is saved and never edited

---

## Setting Up a New Session Set

### 1. Create the Directory Structure

```
docs/session-sets/<name>/
  spec.md
```

The `activity-log.json` and `session-reviews/` directory are created
automatically by the SessionLog class on first use.

### 2. Write spec.md

The spec defines the entire body of work. Structure:

```markdown
# <Feature Name> Spec

> **Purpose:** One-sentence description
> **Created:** YYYY-MM-DD
> **Session Set:** `docs/session-sets/<name>/`
> **Prerequisite:** What must be done before starting
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification

---

## Project Overview

Describe the goals and deliverables.

## Feature 1: <Name>

### Scope
What's included.

### Standards
Rules and conventions.

---

## Session Plan

### Session 1 of N: <Title>

**Steps:**
1. Step description
2. Step description (route documentation task)
3. Step description

**Creates:** List of files created
**Touches:** List of files modified
**Ends with:** Success criteria
**Progress keys:** `session-1/step-name`, `session-1/other-step`

---

### Session 2 of N: <Title>
...
```

Key rules for the spec:
- Each session must be completable in one conversation
- Steps should be specific and actionable
- "Creates" and "Touches" sections let subsequent sessions verify prerequisites
- Progress keys are used in `log.log_step()` for tracking

### 3. Configure the AI Router

The router is configured via `ai-router/router-config.yaml`. Prompt templates
and workflow utilities live under `ai-router/prompt-templates/` and
`ai-router/utils/`. Key sections:

**Models:** Define available models with tier, pricing, and context limits.

```yaml
models:
  gemini-flash:
    provider: google
    model_id: gemini-2.5-flash
    tier: 1
  gemini-pro:
    provider: google
    model_id: gemini-2.5-pro
    tier: 2
  sonnet:
    provider: anthropic
    model_id: claude-sonnet-4-6
    tier: 2
  opus:
    provider: anthropic
    model_id: claude-opus-4-6
    tier: 3
```

**Tier assignments:** Map complexity tiers to models.

```yaml
routing:
  tier1_max_complexity: 30    # score <= 30 -> tier 1
  tier2_max_complexity: 65    # score 31-65 -> tier 2
  tier_assignments:
    1: gemini-flash
    2: gemini-pro      # or sonnet -- see task_type_overrides
    3: opus
  task_type_overrides:
    code-review: sonnet   # force Sonnet for code review tasks
```

**Verification:** Cross-provider model pairings.

```yaml
verification:
  cross_provider_map:
    gemini-flash: sonnet
    gemini-pro: sonnet
    sonnet: gemini-pro
    opus: gemini-pro
  auto_verify_task_types:
    - code-review
    - security-review
```

### 4. Set Environment Variables

The router requires API keys as environment variables:

```
ANTHROPIC_API_KEY   (for Claude Sonnet/Opus)
GEMINI_API_KEY      (for Gemini Flash/Pro)
PUSHOVER_API_KEY    (optional, for end-of-session phone notifications)
PUSHOVER_USER_KEY   (optional, for end-of-session phone notifications)
```

On Windows, set these as User environment variables. The orchestrator
instruction files include the commands to export them into the shell. The
notification helper also falls back to the Windows User/Machine environment if
the current process environment does not already contain the Pushover keys.

### 5. Create the Python Virtual Environment

```bash
python -m venv .venv
.venv/Scripts/pip install pyyaml google-genai anthropic
```

The router uses `.venv/Scripts/python.exe` on Windows.

---

## Executing a Session

When the human says **"Start the next session"**, the orchestrator follows
this exact procedure:

### Step 0: Verify API Keys

Load keys from the environment and confirm both are present. If missing, stop
and tell the human.

### Step 1: Identify the Active Session Set

The `find_active_session_set()` function auto-detects:
- Folder with `spec.md` + `activity-log.json` but no `change-log.md` = in-progress
- Folder with `spec.md` only = not-started (use if no in-progress exists)
- Folder with `change-log.md` = complete (skip)

```python
from ai_router.session_log import find_active_session_set, SessionLog

SESSION_SET = find_active_session_set("docs/session-sets")
log = SessionLog(SESSION_SET)
next_session = log.get_next_session_number()
```

### Step 2: Read the Spec

Open `spec.md` and find the plan for the current session number.

### Step 3: Verify Prerequisites

Confirm that files listed in prior sessions' "Creates" and "Touches" exist.

### Step 4: Execute Each Step

For each step in the session plan:

**File work** -- create or edit files directly:
```python
log.log_step(
    session_number=N,
    step_number=step_num,
    step_key="session-N/step-name",
    description="What was done",
    status="complete",
    api_calls=[]
)
```

**Reasoning tasks** -- delegate to the router:
```python
from ai_router import route

result = route(
    content="<the text to analyze>",
    task_type="code-review",       # or documentation, analysis, etc.
    context="<optional background>",
    complexity_hint=None           # optional 1-100 override
)

# Use result.content, then log:
log.log_step(N, step_num, "session-N/step-name", "description", "complete", [{
    "model": result.model_name,
    "taskType": "code-review",
    "inputTokens": result.input_tokens,
    "outputTokens": result.output_tokens,
    "costUsd": result.cost_usd
}])
```

### Step 5: Build and Test

```bash
dotnet build
dotnet test
```

Log the result with `log.log_step()`.

### Step 6: End-of-Session Verification (MANDATORY)

**The orchestrator must not verify its own work.** The `route()` function
dispatches to a different AI provider for independent review.

1. Collect all files created or modified during the session.
2. Build a verification prompt with: spec excerpt + file contents + build results.
3. Execute:
   ```python
   result = route(
       content=verification_prompt,
       task_type="session-verification",
       complexity_hint=70
   )
   log.save_session_review(
       session_number=N,
       review_text=result.content,
       round_number=1
   )
   ```
4. **Never edit the saved review file.**
5. Log the verification step.

### Step 7: Handle Verification Result

**VERIFIED:** Proceed to commit.

**ISSUES_FOUND:**
1. Parse issues from the verifier's response.
2. Fix each issue. Update status to "fixed" or "deferred".
3. Save: `log.save_issue_log(session_number=N, issues=issues)`
4. Re-run verification (max 2 retries). Use `complexity_hint=85` if any
   issue is Major or Critical.

### Step 8: Cost Report, Commit, and Notify

```python
from ai_router import print_cost_report
print_cost_report(SESSION_SET)
```

Commit and push:
```bash
git add -A
git commit -m "Session N of M: <description>"
git push
```

Then send a session-complete notification through the reusable helper in
`ai-router/`:

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

If notification delivery fails, report it, but do not undo or fail an
otherwise successful session.

### Step 9: Stop

Report: session number, verification verdict, deferred issues (if any),
cost summary, and sessions remaining.

**Do not start the next session. Wait for the human.**

### Step 10: Last Session Only

Generate `change-log.md` using `route(task_type="documentation")`. This marks
the session set as complete.

---

## Orchestrator Instruction Files

Each AI coding agent reads a different instruction file. All three instruct
the orchestrator to follow this same workflow.

| Agent | Instruction File | Global Config |
|---|---|---|
| Claude Code | `CLAUDE.md` (repo root) | `~/.claude/CLAUDE.md` |
| Codex (OpenAI) | `AGENTS.md` (repo root) | `~/.codex/instructions.md` |
| Gemini | `GEMINI.md` (repo root) | Varies by tool |

The human chooses which agent to use for each session. The agent reads its own
instruction file, which tells it how to import the AI router, follow the
session-set workflow, and where to find the spec.

### What Goes in the Instruction File

Each agent-specific file should contain:

1. **Project overview** -- what the repo is, package structure
2. **Session-set workflow reference** -- pointer to this document
3. **AI router import snippet** -- how to load the `ai-router` module
4. **API key export commands** -- platform-specific commands to load keys
5. **Build and test commands** -- `dotnet build`, `dotnet test`
6. **Notification helper usage** -- reusable end-of-session alert via `ai-router`
7. **Code style rules** -- naming conventions, patterns to follow
8. **Mandatory rules** -- never skip verification, one session only, log every step, delegate reasoning

### Switching Orchestrators Between Sessions

The workflow is designed so that any orchestrator can pick up where another
left off. The `activity-log.json` and `spec.md` provide all the state needed.
The orchestrator:

1. Reads the activity log to find the next session number
2. Reads the spec to find that session's plan
3. Checks prerequisites from prior sessions
4. Executes -- regardless of which agent ran previous sessions

---

## AI Router Details

### Importing the Router

The module directory is `ai-router/` (hyphenated). Python cannot import
hyphenated package names directly, so use `importlib`:

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

On Windows, use `.venv/Scripts/python.exe` to run Python.

### Task Types

| Task Type | Base Complexity | Typical Model |
|---|---|---|
| `formatting` | 10 | Gemini Flash |
| `summarization` | 20 | Gemini Flash |
| `documentation` | 25 | Gemini Flash/Pro |
| `test-generation` | 35 | Gemini Pro |
| `code-review` | 40 | Sonnet (if overridden) or Gemini Pro |
| `analysis` | 55 | Gemini Pro |
| `refactoring` | 65 | Gemini Pro |
| `session-verification` | 70 | Opus |
| `security-review` | 75 | Opus |
| `architecture` | 80 | Opus |
| `planning` | 70 | Opus |

### Model Tiers and Pricing

| Tier | Model | Provider | Input $/1M | Output $/1M | Use Case |
|---|---|---|---|---|---|
| 1 | Gemini Flash | Google | $0.15 | $0.60 | Simple formatting, boilerplate |
| 2 | Gemini Pro | Google | $1.25 | $10.00 | Documentation, medium analysis |
| 2 | Sonnet | Anthropic | $3.00 | $15.00 | Code review, when Anthropic quality matters |
| 3 | Opus | Anthropic | $15.00 | $75.00 | Architecture, security, verification |

### Escalation

If a model produces an empty, truncated, or suspiciously short response, the
router automatically escalates to the next tier (up to 2 escalations).

### Cross-Provider Verification

For auto-verified task types (code-review, security-review), the router
automatically sends the response to a model from a different provider:

- Gemini output -> verified by Sonnet (Anthropic)
- Sonnet output -> verified by Gemini Pro (Google)
- Opus output -> verified by Gemini Pro (Google)

---

## Rules (Apply to All Orchestrators)

1. **One session only.** Never execute more than the assigned session.
2. **Never skip verification.** Every session must be independently verified.
3. **Never edit session review files.** They are the verifier's raw output.
4. **Log every step** via `log.log_step()`.
5. **Delegate reasoning** to `route()` -- do not perform reviews or analysis
   yourself when the task should go through the router.
6. **Do not commit with unresolved Critical/Major issues.** Inform the human.
7. **The human controls orchestrator choice.** Any session can use any agent.
