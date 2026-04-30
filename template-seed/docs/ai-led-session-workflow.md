<!-- Generated from live docs — see docs/ai-led-session-workflow.md for the
     canonical version. This seed copy is intentionally trimmed: it omits
     extended code samples and repo-specific conventions while keeping the
     full step numbering, rules list, and structural outline aligned with
     the live doc. When the live doc adds a section, apply the same trimming
     here rather than a full replacement. -->

# AI-Led Session-Set Workflow

This document describes the orchestration pattern used to develop features in
this repository. An AI coding agent (Claude Code, Codex, or a Gemini-based
tool) acts as the **orchestrator**, executing a predefined session plan one
session at a time. A separate **AI Router** Python module routes reasoning
tasks to cheaper external models and enforces cross-provider verification.

This workflow document is the single source of truth for the session-set
workflow — procedure, rules, router usage, verification, UAT/E2E handling
(conditional on the spec's configuration block), and session-set close.
`CLAUDE.md`, `AGENTS.md`, and `GEMINI.md` provide only agent-specific
bootstrap and point here for everything else.

Read `docs/planning/project-guidance.md`, `docs/planning/lessons-learned.md`,
and `docs/planning/session-set-authoring-guide.md` before every session.

## Overview

```
Human
  |
  | trigger phrase (see Trigger Phrases section below)
  v
Orchestrator (Claude / Codex / Gemini)
  |
  |-- reads instruction file (CLAUDE.md / AGENTS.md / GEMINI.md)
  |-- reads project-guidance.md, lessons-learned.md,
  |   session-set-authoring-guide.md
  |-- reads spec.md (incl. Session Set Configuration block)
  |-- reads activity-log.json for prior progress
  |-- writes session-state.json (in-progress) for external tooling
  |
  |  For each step in the session plan:
  |    |-- file work: creates/edits files directly
  |    |-- reasoning: calls route() via ai-router Python module
  |    |     |-- router selects cheapest capable model
  |    |     +-- auto-verification for code-review/security-review
  |    |-- logs every step to activity-log.json
  |
  |-- runs build and test
  |
  |-- MANDATORY: end-of-session verification (different AI provider)
  |     |-- sends all work to a DIFFERENT AI provider
  |     |-- requires structured JSON response (verdict + issues array)
  |     |-- saves raw verifier output (never edited)
  |     +-- fixes issues if found; max 2 retries
  |
  |-- prints cost report; generates change-log.md (last session only)
  |-- authors next-orchestrator recommendation (every session)
  |-- authors next-session-set recommendation (last session only)
  |-- marks session-state.json complete
  |-- commits and pushes; sends session-complete notification
  |-- on last session ONLY (after notify): proposes reorganization of
  |   project-guidance.md / lessons-learned.md, then commits separately
  +-- STOPS (one session per conversation)
```

---

## Trigger Phrases

The human starts a session with one of:

- `Start the next session.` — sequential, current working tree.
- `Start the next session of <slug>.` — sequential, named set.
- `Start the next parallel session of <slug>.` — isolated git worktree.

Any phrase may be suffixed with `— maxout <engine>` (e.g.,
`— maxout Claude`) to override `ai-assignment.md` routing
recommendations when a token-window allotment needs burning.
`session-verification` is always cross-provider regardless of maxout.

---

## Key Concepts

### Session Set

A session set is a planned body of work broken into sequential sessions.
Each session set lives in `docs/session-sets/<name>/` and contains:

| File | Purpose |
|---|---|
| `spec.md` | Full plan: goals, Session Set Configuration block, per-session steps |
| `session-state.json` | Live status written at Step 1; flipped to complete at Step 8 |
| `ai-assignment.md` | Per-session routing ledger + next-session recommendation |
| `activity-log.json` | Machine-readable log of every step across all sessions |
| `session-reviews/session-NNN.md` | Raw verifier output (never edited) |
| `session-reviews/issues-NNN.json` | Issue log if verification found problems |
| `change-log.md` | Generated on the final session; marks the set as complete |
| `<slug>-uat-checklist.json` | Human-UAT checklist (only when `requiresUAT: true`) |

### Session Set Configuration block

Every spec declares:

```yaml
## Session Set Configuration

```yaml
requiresUAT: false      # human UAT checklist required
requiresE2E: false      # E2E coverage required before notifying
uatScope: none          # per-session | per-set | none
```

Omitting the block is identical to all-flags-false. The orchestrator
obeys the spec's flags at runtime — do not re-litigate them during a
session. See `docs/planning/session-set-authoring-guide.md` for the
When-UAT and When-E2E authoring heuristics.

### Session

One unit of work within a session set. Each session:
- Has a defined list of steps in `spec.md`
- Is executed by exactly one orchestrator in one conversation
- Ends with mandatory cross-provider verification
- Produces a commit on completion

### AI Router

A Python module in `ai-router/` that routes reasoning tasks to external
AI models. The orchestrator calls `route()` for any non-mechanical work.

---

## Setting Up a New Session Set

### 1. Create the Directory Structure

```
docs/session-sets/<name>/
  spec.md
```

`activity-log.json` and `session-reviews/` are created automatically
on first use.

### 2. Write spec.md

See `docs/planning/session-set-authoring-guide.md` for the full
authoring guide (slug naming, sizing, the configuration block schema,
deliverables, anti-patterns, and a copy-paste template). Minimum
structure:

```markdown
# <Feature Name> Spec

> **Purpose:** One-sentence description
> **Created:** YYYY-MM-DD
> **Session Set:** `docs/session-sets/<name>/`
> **Prerequisite:** What must be done before starting
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification

---

## Session Set Configuration

```yaml
requiresUAT: false
requiresE2E: false
uatScope: none
```

> Rationale: <why these flags>

---

## Project Overview

Describe the goals and deliverables.

## Session Plan

### Session 1 of N: <Title>

**Steps:**
1. Step description
2. Step description

**Creates:** List of files created
**Touches:** List of files modified
**Ends with:** Success criteria
**Progress keys:** `session-1/step-name`
```

### 3. Configure the AI Router

The router is configured via `ai-router/router-config.yaml`. Key sections:

```yaml
models:
  gemini-flash: { provider: google, tier: 1 }
  gemini-pro:   { provider: google, tier: 2 }
  sonnet:       { provider: anthropic, tier: 2 }
  opus:         { provider: anthropic, tier: 3 }

routing:
  tier_assignments: { 1: gemini-flash, 2: gemini-pro, 3: opus }
  task_type_overrides: { code-review: sonnet }

verification:
  preferred_pairings:
    sonnet: gemini-pro
    gemini-pro: sonnet
    opus: gpt-5-4
  auto_verify_task_types:
    - code-review
    - security-review
```

Refer to the live `ai-router/router-config.yaml` for the full
configuration, including `delegation:`, `task_type_params:`,
`metrics:`, and tunable thresholds.

### 4. Set Environment Variables

```
ANTHROPIC_API_KEY
GEMINI_API_KEY
OPENAI_API_KEY
PUSHOVER_API_KEY    (optional, for notifications)
PUSHOVER_USER_KEY   (optional, for notifications)
```

### 5. Create the Python Virtual Environment

```bash
python -m venv .venv
.venv/Scripts/pip install pyyaml google-genai anthropic httpx
```

---

## Executing a Session

### Step 0: Verify API Keys And Read Guidance

1. Read `docs/planning/project-guidance.md`
2. Read `docs/planning/lessons-learned.md`
3. Read `docs/planning/session-set-authoring-guide.md`
4. Load API keys; stop and tell the human if any are missing.

### Step 1: Identify the Active Session Set and Register Session Start

Find the active session set via `find_active_session_set()` (or use
the slug named in the trigger phrase). Then immediately write
`session-state.json` via `register_session_start()` — before any
`log_step()` call — so external tooling (VS Code Session Set Explorer)
sees the session as in-progress without waiting for the first commit.

```python
from ai_router.session_log import find_active_session_set, SessionLog
from ai_router import register_session_start

SESSION_SET = find_active_session_set("docs/session-sets")
log = SessionLog(SESSION_SET)
next_session = log.get_next_session_number()
register_session_start(
    session_set=SESSION_SET,
    session_number=next_session,
    total_sessions=log.total_sessions,
    orchestrator_engine="claude-code",   # or "codex", "gemini-cli"
    orchestrator_model="claude-opus-4-7",
    orchestrator_effort="high",          # or "unknown" if not introspectable
)
```

### Step 2: Read the Spec and the Configuration Block

Open `spec.md` and extract:
1. The `## Session Set Configuration` block (`requiresUAT`, `requiresE2E`,
   `uatScope`). Cache these — they govern which gates apply for the rest
   of the session.
2. The plan for the current session number.

If the block is absent, treat all flags as false.

### Step 3: Verify Prerequisites

Confirm that files listed in prior sessions' "Creates" and "Touches" exist.

### Step 3.5: Author or Update `ai-assignment.md`

On Session 1, produce a per-session routing ledger via
`route(task_type="analysis")` — never self-opine on which model is
cheapest. On Sessions 2..N, append the prior session's actuals and
route a fresh recommendation for the next session. See the live
workflow doc for the full schema.

### Step 4: Execute Each Step

For each step: do file work directly; route reasoning tasks.

```python
result = route(
    content="...",
    task_type="code-review",
    session_set=str(SESSION_SET),
    session_number=next_session,
)
log.log_step(
    session_number=next_session,
    step_number=step_num,
    step_key="session-N/step-name",
    description="What was done",
    status="complete",
    api_calls=[{"model": result.model_name, "costUsd": result.cost_usd, ...}]
)
```

### Step 5: Build and Test

Run build and test sequentially (not in parallel — file-lock contention
on Windows obscures real results). Log the result.

### Step 6: End-of-Session Verification (MANDATORY)

Send all session work to a **different AI provider** for independent
review. The verification prompt must include the **structured JSON
response schema** (from `ai-router/prompt-templates/verification.md`)
so the verifier returns `{"verdict": ..., "issues": [...]}` rather
than a bare paragraph.

```python
result = route(
    content=verification_prompt,
    task_type="session-verification",
    complexity_hint=70,
    session_set=str(SESSION_SET),
    session_number=next_session,
)
log.save_session_review(session_number=next_session,
                        review_text=result.content, round_number=1)
```

Never edit the saved review file. If the first-choice verifier fails at
the HTTPS level, the router excludes that provider and re-picks once
(two-attempt fallback).

### Step 7: Handle Verification Result

**VERIFIED:** Proceed to commit.

**ISSUES_FOUND:**
1. Parse issues. Fix each. Save: `log.save_issue_log(...)`.
2. Re-run verification (max 2 retries; use `complexity_hint=85` for
   Critical/Major issues).

**If the orchestrator disagrees with a verifier finding:** present the
finding, the dismissal reason, and the context sent to the verifier to
the human. The human then picks one of four paths: (a) accept finding,
(b) accept dismissal, (c) re-verify with reshaped context, or (d)
second opinion from a different provider. Log the outcome via
`record_adjudication()`. See the live workflow doc for the full
disagreement procedure.

### Step 8: Cost Report, Change Log (Last Session), Recommendations, Commit, Push, Mark Complete, And Notify

```python
from ai_router import print_cost_report, mark_session_complete
print_cost_report(SESSION_SET)
```

On the **last session**, generate `change-log.md` before committing:
```python
result = route(content=change_log_prompt, task_type="summarization", max_tier=1)
```

**Every session:** author next-orchestrator recommendation via
`route(task_type="analysis")` — never self-opine. Append to
`ai-assignment.md`.

**Last session only:** author next-session-set recommendation via
`route(task_type="analysis")`. If the repo has a master plan file,
validate against it (confirm the planned next set or propose a
deviation with justification); otherwise recommend from backlog.

Commit, push, then mark the session complete:
```python
mark_session_complete(SESSION_SET, verification_verdict="VERIFIED")
```

Send a session-complete notification if configured:
```python
from ai_router import send_session_complete_notification
send_session_complete_notification(
    session_set=SESSION_SET,
    session_number=next_session,
    total_sessions=log.total_sessions,
    verification_verdict="VERIFIED",
    summary="<short description>",
)
```

### Step 9: Last Session Only — Reorganization Proposals (Post-Notify)

On the last session only, after notifying, review
`docs/planning/project-guidance.md` and `docs/planning/lessons-learned.md`
and propose reorganization candidates (lesson → convention promotions ≥2
contexts, convention → principle, relocations). Present each to the human;
apply accepted changes in a **separate follow-up commit**. Never delete —
only move.

### Step 10: Stop

Report: session number, verification verdict, deferred issues, cost
summary, and sessions remaining. If the session produced or refreshed a
UAT checklist (`requiresUAT: true`), the stop message must identify the
checklist path and state that work is waiting on human review.

**Do not start the next session. Wait for the human.**

---

## Orchestrator Instruction Files

| Agent | Instruction File |
|---|---|
| Claude Code | `CLAUDE.md` |
| Codex (OpenAI) and GitHub Copilot | `AGENTS.md` |
| Gemini Code Assistant | `GEMINI.md` |

Each file keeps only agent-specific bootstrap (API key export, router
import) and points to this document for the full workflow and rules.

---

## AI Router Details

### Importing the Router

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

### Task Types

| Task Type | Typical Tier | Notes |
|---|---|---|
| `formatting`, `summarization` | 1 | Cheap, high-volume |
| `documentation`, `test-generation`, `code-review`, `analysis`, `refactoring` | 2 | Medium complexity |
| `uat-plan-generation`, `uat-script-author` | 3 / 2 | Only when `requiresUAT: true`; prefer `uat-script-author` (catalog-grounded DSL) for new sets |
| `session-verification`, `security-review`, `architecture`, `planning` | 3 | High stakes |

### Delegation Discipline

The orchestrator does **directly** (no `route()` call): reading specs and
logs, creating/editing files, running shell commands, mechanical single-file
edits under ~50 lines.

The orchestrator **always routes**: code review, security review,
architecture decisions, analysis, documentation, test generation, session
verification, and `ai-assignment.md` / next-orc / next-set recommendations.

`session-verification` MUST route regardless of whether the orchestrator's
model matches the routing target — the cross-provider opinion is the point.

---

## Rules (Apply to All Orchestrators)

1. **One session only.** Never execute more than the assigned session.
2. **Never skip verification.** `session-verification` always routes
   cross-provider; this survives any `— maxout` suffix.
3. **Never edit session review files.** They are the verifier's raw output.
4. **Log every step** via `log.log_step()`.
5. **Delegate reasoning** to `route()`. Do file work directly; route
   everything else.
6. **Do not commit with unresolved Critical/Major issues.**
7. **The human controls orchestrator choice.** Any session can use any agent.
8. **Before every session, read all required-reading files:**
   `project-guidance.md`, `lessons-learned.md`, `session-set-authoring-guide.md`.
9. **Treat pending UAT as blocking** *(when `requiresUAT: true`).* Do not
   start downstream sessions over an unreviewed checklist without human override.
10. **One UAT checklist per session set** *(when `requiresUAT: true`).* Name
    it `<slug>-uat-checklist.json`; keep human results inline.
11. **E2E coverage before UAT handoff** *(when both flags true).* Under
    the DSL pipeline, every functional checklist step compiles via W0
    into the Playwright `.cs` test and the W2 runner pass is the
    coverage proof — there is no separate `uat-coverage-review` AI call.
12. **Share screenshots during UI/E2E work when practical.**
13. **Escalate durable guidance.** Ask whether new decisions should update
    `docs/planning/project-guidance.md`.
14. **Recommend lessons learned after failures.**
15. **Run the Step 9 reorganization review on the last session, after notify.**
16. **Register session start before the first activity-log entry.** Call
    `register_session_start()` at Step 1; call `mark_session_complete()` at
    Step 8.
17. **Author `ai-assignment.md` and recommendations via routed analysis —
    never self-opine.**
18. **Obey the spec's Session Set Configuration block at runtime.** Specs
    that omit the block are treated as all-flags-false. If a flag is wrong,
    correct it in the spec and revisit at Step 9.
