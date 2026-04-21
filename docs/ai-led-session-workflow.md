# AI-Led Session-Set Workflow

This document describes the orchestration pattern used to develop features in
this repository. An AI coding agent (Claude Code, Codex, or a Gemini-based
tool) acts as the **orchestrator**, executing a predefined session plan one
session at a time. A separate **AI Router** Python module routes reasoning
tasks to cheaper external models and enforces cross-provider verification.

This document is the single source of truth for the session-set workflow
itself — procedure, rules, router usage, verification, UAT handling, and
session-set close. `CLAUDE.md`, `AGENTS.md`, and `GEMINI.md` provide only
agent-specific bootstrap (API key export, router import snippet) and point
here for everything else.

The orchestrator can change from session to session at the human's discretion.
All three orchestrators follow the same workflow — only the instruction file
they read differs.

Read `docs/planning/project-guidance.md` and
`docs/planning/lessons-learned.md` before every session and before changing
architecture, testing strategy, workflow assets, or human-UAT conventions.

## Overview

```
Human
  |
  | "Start the next session"
  v
Orchestrator (Claude / Codex / Gemini)
  |
  |-- reads instruction file (CLAUDE.md / AGENTS.md / GEMINI.md)
  |-- reads project-guidance.md and lessons-learned.md
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
  |-- on last session: generates change-log.md (part of the same commit)
  |-- prints cost report
  |-- commits and pushes
  |-- sends session-complete notification (if configured)
  |-- on last session ONLY (after notify, so the human is not blocking
  |                    the notification on answering proposals):
  |                    proposes reorganization candidates for
  |                    project-guidance.md / lessons-learned.md, then
  |                    commits and pushes any accepted changes separately
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
| `<name>-uat-checklist.json` | Per-set human-UAT checklist (if the set requires one) |

Human-UAT sets use one checklist per session set, named after the set, rather
than re-running an earlier set's checklist. See
`docs/planning/project-guidance.md` → Conventions → Human UAT And Checklist
Editor.

### Session

One unit of work within a session set. Sessions are numbered (Session 1 of 5,
Session 2 of 5, etc.). Each session:

- Has a defined list of steps in `spec.md`
- Is executed by exactly one orchestrator in one conversation
- Ends with a mandatory cross-provider verification
- Produces a commit on completion

If a session creates or refreshes a checklist for later human UAT execution,
that pending human review becomes a blocker for downstream sessions unless the
human explicitly overrides it.

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
Each model may also declare `generation_params` controlling the API-level
reasoning knobs: `effort` and `thinking` for Anthropic (Sonnet/Opus), and
`thinking_budget` (Gemini 2.5) or `thinking_level` (Gemini 3.x) for Google.
These are sent on every call, so leaving `effort` unset on Sonnet 4.6
means the API defaults to `high` — which burns tokens unnecessarily on
simple tasks. Explicitly setting `effort: medium` (or lower per task
type) is the single biggest cost lever.

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

**Verification:** Verifier selection is rule-based. For each routed
call, `verification.py` picks a verifier that is from a different
provider than the generator, is enabled, is enabled as a verifier,
matches the generator's tier (or one tier higher), and — among the
survivors — has the cheapest output price. Every model entry carries
`is_enabled` (generator pool) and `is_enabled_as_verifier` (verifier
pool) flags; new models join with the verifier flag off and are
promoted only after calibration data supports it.

`preferred_pairings` is an optional advisory layer: if a listed
pairing survives the rules it is used, otherwise the rules decide. The
legacy key name `cross_provider_map` is still accepted for backward
compatibility with older branches.

```yaml
verification:
  preferred_pairings:
    sonnet:       gemini-pro
    gemini-flash: gpt-5-4-mini
    gemini-pro:   gpt-5-4-mini
    opus:         gpt-5-4
    gpt-5-4-mini: sonnet
    gpt-5-4:      opus
  auto_verify_task_types:
    - code-review
    - security-review
```

**Per-task-type parameter overrides:** `task_type_params` maps each task
type to per-model parameter overrides, layered on top of each model's
`generation_params`. This is where cheap tasks like `formatting` and
`summarization` get `effort: low` and `thinking_budget: 0`, while
`architecture` and `session-verification` get deeper reasoning.

```yaml
task_type_params:
  formatting:
    sonnet:       { effort: low, thinking: { enabled: false } }
    gemini-flash: { thinking_budget: 0 }
  session-verification:
    sonnet:       { effort: high, thinking: { enabled: true, type: adaptive } }
    gemini-pro:   { thinking_budget: -1 }
```

All tuning now lives in `router-config.yaml` as of the Session 2
consolidation. The prior runtime overlay file (`router-tuning.json`)
was removed; `metrics.enabled` and `delegation.always_route_task_types`
moved into the YAML under `metrics:` and `delegation:` respectively.
To adjust depth for a run, edit the YAML directly — there is no
separate overlay file.

Precedence (low → high): model-level `generation_params` →
`task_type_params` override.

### 4. Set Environment Variables

The router requires API keys as environment variables:

```
ANTHROPIC_API_KEY   (for Claude Sonnet/Opus)
GEMINI_API_KEY      (for Gemini Flash/Pro)
OPENAI_API_KEY      (for GPT-5.4 and GPT-5.4 Mini)
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
.venv/Scripts/pip install pyyaml google-genai anthropic httpx
```

The router uses `httpx` directly — it does NOT require the `openai`,
`anthropic`, or `google-genai` SDKs at runtime (those are listed for
compatibility with other tools in the repo). OpenAI calls go through
the Responses API via plain HTTP.

---

## Executing a Session

When the human says **"Start the next session"**, the orchestrator follows
this exact procedure.

### UAT Checklist Rule

When a session set includes a human-executed UAT checklist:

- Each session set gets its own checklist named
  `<session-set-slug>-uat-checklist.json`, placed inside the session set's
  folder. Do not re-use or re-run a checklist from an earlier session set.
- The checklist JSON must match the schema from
  `https://github.com/darndestdabbler/uat-checklist-editor/blob/main/checklist-schema.json`
- The human runs it through
  `https://darndestdabbler.github.io/uat-checklist-editor/`
- Review results are saved inline in the checklist JSON
- Do not create a separate empty findings scaffold just to hold future human
  review results
- After a checklist is prepared and waiting on the human, do not start a new
  session that depends on or bypasses that review unless the human explicitly
  says to do so

### When UAT Is Required

A session set must produce a UAT checklist whenever its work changes the
behavior of a UI surface or a service that the UI talks to directly. In
practice, any of these triggers UAT:

- Any change to a Blazor page, component, nav menu, form, grid, or dialog.
- Any change to a shell element (app bar, drawer, theme, layout) or a
  cross-page interaction pattern (role switcher, org switcher, sign-in flow).
- Any change to an API endpoint the UI consumes — request shape, response
  shape, error-status contract, or authorization rule.
- Any change to authorization rules (role assignments, restriction types,
  mask exemptions, loopback gates) that the UI can surface.
- Any change to a browser-visible workflow: search, filter, sort, paging,
  export, document generation, multi-step forms.

A session set does **not** require UAT when it only touches internal-only
surfaces — pure library refactors, build or infrastructure changes with no
UI effect, router or prompt-template edits, test-only changes, and
documentation. If a session is ambiguous (e.g., refactoring an API the UI
may depend on), default to requiring UAT.

When UAT is required, the checklist is built during the session set that
makes the change — not deferred to a later "UAT session set." Deferring
UAT across session sets breaks the traceability between a change and its
human sign-off.

### E2E Coverage Before UAT

Before a UAT checklist is committed and the human is notified, every
functional item in the checklist must have matching E2E test coverage.
This is the procedural form of the "human UAT is not the first line of
defense" principle in `docs/planning/project-guidance.md`.

Specifically:

- Every checklist item with a functional expectation (route reached,
  control visible/enabled, data persisted, grid refreshed, error shown,
  etc.) must have a Playwright test that drives the same steps with the
  same parameters and asserts the same outcome.
- Items whose expectation is purely a judgment call (aesthetic, layout
  feel, copy quality) are flagged with `IsJudgmentItem: true` in the
  checklist JSON and must include a one-sentence justification. These
  are exempt from the matching-test requirement but should still have
  a sequence-reachability test so the human is rendering judgment on a
  verified-live UI rather than debugging exceptions.
- The `uat-coverage-review` task type (see Task Types table) is the
  mechanical check: given the checklist and the Playwright file, it
  returns `VERIFIED` only when every non-judgment item has a matching
  test. Any mismatch blocks the checklist handoff.

A checklist shipped without this coverage is a session-closeout defect
and must be rebuilt before the human is notified.

### Step 0: Verify API Keys And Read Guidance

Before doing anything else:

1. Read `docs/planning/project-guidance.md`
2. Read `docs/planning/lessons-learned.md`
3. Then load keys from the environment and confirm both are present

If keys are missing, stop and tell the human.

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

**File work** — create or edit files directly:
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

**Reasoning tasks** — delegate to the router:
```python
from ai_router import route

result = route(
    content="<the text to analyze>",
    task_type="code-review",       # or documentation, analysis, etc.
    context="<optional background>",
    complexity_hint=None,          # optional 1-100 override
    session_set=str(SESSION_SET),  # enables per-set metrics grouping
    session_number=N,              # enables per-session metrics grouping
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

During execution:

- if the human gives a decision or instruction that looks durable enough to
  guide future sessions or future projects, ask whether it should be added to
  `docs/planning/project-guidance.md`
- if a failure or avoidable friction suggests a reusable tactic, recommend an
  update to `docs/planning/lessons-learned.md`

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
       complexity_hint=70,
       session_set=str(SESSION_SET),
       session_number=N,
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

#### Disagreement With A Verifier Finding

If the orchestrator disagrees with a specific finding rather than
accepting it, the orchestrator does **not** dismiss the finding on its
own authority and does **not** appeal to another AI provider for a
consensus vote. The authority model is: verifiers flag, humans
adjudicate.

Most orchestrator-vs-verifier disagreements turn out to be context
problems rather than who-is-right problems. The common failure modes
are:

- **Context gap** — the verifier flagged something that's actually
  handled elsewhere in code the verifier wasn't shown. Most common case.
- **Genuine split** — the verifier and orchestrator both have the same
  context and disagree on the call.
- **Orchestrator error** — the orchestrator is wrong and should accept
  the finding.

When the orchestrator wants to challenge a finding it must present to
the human:

1. **The exact finding** (verbatim from the saved review file).
2. **The dismissal reason** — why the orchestrator believes the finding
   does not apply.
3. **The context that went to the verifier** — the list of files and
   excerpts included in the verification prompt.
4. **Self-assessment of context** — whether relevant context may have
   been missing or whether the verifier may have been distracted by
   irrelevant material.

The human then chooses one of four options:

- **(a) Accept verifier finding** — treat as an issue and fix.
- **(b) Accept orchestrator's dismissal** — close without changes.
- **(c) Re-verify with reshaped context** — same verifier, adjusted
  input (add missing files, trim irrelevant ones). Resolves the
  context-gap case cleanly.
- **(d) Second opinion from a different provider** — route the same
  content to a tiebreaker model from a different provider than the
  original verifier. This reuses the existing
  `verification.settings.on_disagreement` / `tiebreaker_model`
  configuration in `router-config.yaml`; no new code path is required.

Whichever option the human picks, log the disagreement so the ratio
is visible in the manager report over time. The router exposes
`record_adjudication` for this — it writes one JSON line to
`router-metrics.jsonl` alongside the route/verify records so the same
report pass aggregates all of it:

```python
from ai_router import record_adjudication

record_adjudication(
    config,                         # already loaded router config
    task_type="architecture",       # same task_type as the verify call
    cause="context-gap",            # context-gap | genuine-split | orchestrator-error
    resolution="reverify-reshaped", # accept-finding | accept-dismissal
                                    # | reverify-reshaped | second-opinion
    session_set=str(SESSION_SET),
    session_number=N,
    generator_model=route_result.model_name,
    verifier_model=route_result.verification.verifier_model,
    finding_summary="<one-line summary of the finding>",
    dismissal_reason="<why the orchestrator challenged it>",
)
```

The `resolution` values map to the four human options above
((a)=accept-finding, (b)=accept-dismissal, (c)=reverify-reshaped,
(d)=second-opinion). Unrecognized values are accepted but prefixed
with `unknown:` in the log so ad-hoc strings do not silently fail
to aggregate.

The distribution matters for tuning. If most disagreements resolve via
(c), the signal is that the orchestrator needs better context-selection
guidance rather than more verifiers. If (d) is common, the generator
and verifier pairing may be mismatched for this kind of work. The
manager report (`report.py`) aggregates these by cause and resolution
in its "Verifier findings & adjudication" section.

### Step 8: Cost Report, Change Log (Last Session), Commit, Push, And Notify

Print the cost report:

```python
from ai_router import print_cost_report
print_cost_report(SESSION_SET)
```

**Last session only — generate the change log before committing.** This
is the former Step 10a. It must land in the same commit as the final
session's code so the session set is marked complete in a single push.

```python
result = route(content=change_log_prompt, task_type="documentation")
# write docs/session-sets/<name>/change-log.md
```

Commit and push:
```bash
git add -A
git commit -m "Session N of M: <description>"
git push
```

Then send a session-complete notification through the reusable helper in
`ai-router/`. **Critical ordering:** notify here — before the Step 9
reorganization review — so the human is not holding up the "session
complete" signal while they think about proposals.

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

### Step 9: Last Session Only — Reorganization Proposals (Post-Notify)

**Non-last sessions skip this step and proceed to Step 10.**

On the last session, AFTER notifying in Step 8, review
`docs/planning/project-guidance.md` and `docs/planning/lessons-learned.md`
and propose any reorganization that this session set's experience
justifies. This step is run after notify because it blocks on a human
response; the human should already have received the completion
notification and can answer on their own schedule.

Candidate moves include:

- **Lesson → Convention.** A lesson in `lessons-learned.md` that has been
  applied as the right call in **at least two different contexts** (different
  session sets, features, or problem areas) is a promotion candidate. A
  single repeat is not enough — wait for the second context.
- **Lesson → Principle.** A lesson that has proven itself strategic in ≥2
  contexts and is broader than a specific pattern may be promoted directly
  to Principles. Rare — most promotions go to Conventions first.
- **Convention → Principle.** A convention whose rationale has become
  clearly strategic (not just a rule, but a reason the rule exists).
- **Principle → Convention.** A principle that turns out to be a specific
  rule rather than durable strategy.
- **Relocation within a file.** Moving an item to a more fitting section.
- **Staleness flag.** An item whose driving context is gone may be flagged
  for the human — but do not delete. Only move, with a note about why.

Procedure:

1. Scan both files and identify candidates with reasoning.
2. Present each candidate to the human, one at a time:
   - What is moving (text or pointer)
   - From where, to where
   - Why (which two-or-more contexts justify the move; or why the current
     classification is wrong)
3. Human accepts or rejects each proposal.
4. Apply accepted proposals. **Never delete content — only move.** If the
   destination already contains equivalent guidance, collapse by reference
   rather than duplicate.
5. If no candidates qualify, output exactly:
   > No reorganization changes recommended for `project-guidance.md` or
   > `lessons-learned.md`.
6. If any proposal was accepted, commit and push those changes in a
   **separate follow-up commit**:
   ```bash
   git add docs/planning/project-guidance.md docs/planning/lessons-learned.md
   git commit -m "Session set <name>: Step 9 reorganization (<summary>)"
   git push
   ```
   If no proposal was accepted, no additional commit is needed.

This step is mandatory even when the output is "no changes recommended" —
the review itself is the checkpoint.

### Step 10: Stop

Report: session number, verification verdict, deferred issues (if any),
cost summary, and sessions remaining.

If the session produced or refreshed a human-UAT checklist that still needs
the human to run it, the stop message must also:

- clearly identify the checklist path
- point the human to the checklist editor URL
- state that work is waiting on the human review
- keep any optional parallel suggestions low-risk and clearly optional

**Do not start the next session. Wait for the human.**

---

## Orchestrator Instruction Files

Each AI coding agent reads a different instruction file. All three instruct
the orchestrator to follow this same workflow.

| Agent | Instruction File | Global Config |
|---|---|---|
| Claude Code | `CLAUDE.md` (repo root) | `~/.claude/CLAUDE.md` |
| Codex (OpenAI) | `AGENTS.md` (repo root) | `~/.codex/instructions.md` |
| Gemini | `GEMINI.md` (repo root) | Varies by tool |

The human chooses which agent to use for each session. The agent reads its
own instruction file. Each instruction file keeps only agent-specific
bootstrap (API key export syntax, router import snippet) and points here
for the full workflow and rules.

### What Goes in the Instruction File

Each agent-specific file should contain:

1. **Project overview** — what the repo is, package structure
2. **Pointer to `docs/planning/project-guidance.md`**
3. **Pointer to `docs/planning/lessons-learned.md`**
4. **Pointer to this workflow doc** for the full procedure and rules
5. **AI router import snippet** — how to load the `ai-router` module
6. **API key export commands** — platform-specific commands to load keys
7. **Build and test commands**
8. **Solution structure**

The instruction file **should not** duplicate the per-step procedure, the
rules list, the UAT checklist rule, or the reorganization-proposal rule —
those live here, and duplication creates drift.

### Switching Orchestrators Between Sessions

The workflow is designed so that any orchestrator can pick up where another
left off. The `activity-log.json` and `spec.md` provide all the state needed.
The orchestrator:

1. Reads the activity log to find the next session number
2. Reads the spec to find that session's plan
3. Checks prerequisites from prior sessions
4. Executes — regardless of which agent ran previous sessions

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
| `uat-plan-generation` | 70 | Opus |
| `uat-coverage-review` | 70 | Opus |
| `session-verification` | 70 | Opus |
| `security-review` | 75 | Opus |
| `architecture` | 80 | Opus |
| `planning` | 70 | Opus |

`uat-plan-generation` produces the structured UAT checklist (numbered
steps, verifications, test data) for a session set. `uat-coverage-review`
verifies that every functional checklist item has a matching E2E step
with the same action, parameters, and verifications. Both are
auto-verified cross-provider — they are the checks that prevent the
"UAT as first line of defense" failure mode, so they get high-effort
settings in `router-config.yaml`.

### Model Tiers and Pricing

| Tier | Model | Provider | Input $/1M | Output $/1M | Use Case |
|---|---|---|---|---|---|
| 1 | Gemini Flash | Google | $0.15 | $0.60 | Simple formatting, boilerplate |
| 2 | Gemini Pro | Google | $1.25 | $10.00 | Documentation, medium analysis |
| 2 | Sonnet | Anthropic | $3.00 | $15.00 | Code review, when Anthropic quality matters |
| 2 | GPT-5.4 Mini | OpenAI | $0.75 | $4.50 | Cross-provider verification of Gemini output |
| 3 | Opus | Anthropic | $15.00 | $75.00 | Architecture, security, verification |
| 3 | GPT-5.4 | OpenAI | $2.50 | $15.00 | Frontier verification of Opus output |

### Escalation

If a model produces an empty, truncated, or suspiciously short response, the
router automatically escalates to the next tier (up to 2 escalations).

### Cross-Provider Verification

For auto-verified task types (code-review, security-review, and any
others listed in `verification.auto_verify_task_types`), the router
sends the initial response to a verifier from a different provider.
Verifier selection is rule-based: a candidate must be from a different
provider than the generator, be in the enabled model pool
(`is_enabled: true`), be trusted for verification
(`is_enabled_as_verifier: true`), and match the generator's tier or be
one tier higher. Among survivors, the cheapest output price wins.

With three providers (Anthropic, Google, OpenAI), this rotates so every
provider acts as both generator and verifier. The advisory
`verification.preferred_pairings` map in `router-config.yaml` is
consulted as a tiebreaker against the rule-qualified candidate set: if
the listed pairing survives, it is used; otherwise the rules decide.
The current preferences:

- Sonnet (Anthropic) output → verified by Gemini Pro (Google)
- Gemini Flash/Pro (Google) output → verified by GPT-5.4 Mini (OpenAI)
- Opus (Anthropic) output → verified by GPT-5.4 (OpenAI)
- GPT-5.4 Mini (OpenAI) output → verified by Sonnet (Anthropic)
- GPT-5.4 (OpenAI) output → verified by Opus (Anthropic)

If the first-choice verifier fails at the HTTPS layer (provider
outage, timeout), the router excludes that provider and re-picks once.
The fallback call is recorded in the metrics log with
`verifier_fallback: true` and the failed provider name, so the audit
trail reflects the verifier that actually ran.

Swapping or retiring a model requires editing only its entry under
`models:` in the YAML. The rules recompute the verifier choice from
whatever pool remains, so there is no pairing table to maintain.

### When To Use The Router

- Code review → `route(code, task_type="code-review")` ← auto-verified
- Security review → `route(code, task_type="security-review")` ← auto-verified
- Documentation → `route(code, task_type="documentation")`
- Analysis → `route(question, task_type="analysis")`
- Test generation → `route(code, task_type="test-generation")`

Do NOT route file creation, shell commands, or anything needing the
filesystem.

---

## Metrics and Observability

Every routed call, verifier call, and tiebreaker call is appended to a
global log at `ai-router/router-metrics.jsonl`. The log is append-only
JSON lines (one record per line). It spans all session sets — it is
NOT per-session-set — so cross-project trends can be analyzed.

### What gets logged

For each call: timestamp, session set, session number, call type
(route/verify/tiebreaker), task type, model, provider, tier,
complexity score, effort / thinking setting, input and output tokens,
cost, elapsed time, escalation flag, stop reason. Verifier calls
additionally log the verdict (VERIFIED / ISSUES_FOUND) and issue count.

### Reading the log

Call `print_metrics_report()` for a terminal summary:

```python
from ai_router import print_metrics_report
print_metrics_report()
```

The report groups by model, task type, verifier, and session set,
showing call counts, costs, escalation rates, and verifier pass rates.

For deeper analysis, read the JSONL file directly. The human or an
orchestrator can hand the file to a reasoning model and ask
questions like *"which task types are escalating most?"* or *"is GPT-5.4
a stricter or looser verifier than Opus?"* — the records carry enough
context to answer those without additional instrumentation.

### Manager Report

For a governance-oriented markdown summary rather than the developer
text dump, run `report.py`:

```bash
# Print to stdout
python -m ai_router.report

# Write to a file
python -m ai_router.report --output docs/reports/router-2026-Q2.md

# Filter by date range or session set
python -m ai_router.report --since 2026-04-01 --until 2026-04-30
python -m ai_router.report --session-set docs/session-sets/reports-pdf-layout
```

The report contains:

- **Headline** — total calls, total spend, and the ratio of actual spend
  to an Opus-only baseline (what the same token volume would have cost
  if every call had gone to Opus). The savings percentage is the
  governance-slide headline.
- **Per-task-type summary** — primary model, average cost per call,
  escalation rate, verifier rejection rate, retry rate, and a composite
  unreliability rate. Rows with fewer than 5 calls are flagged as
  too-few-to-rate rather than shown with false precision.
- **Outliers** — top 3 most expensive individual calls and top 3 task
  types by unreliability.
- **Auto-generated action items** — one bullet per task type whose
  composite unreliability exceeds 20%, naming the specific component(s)
  driving the signal.

Unreliability is the mean of three independent rates: escalation rate,
verifier rejection rate (`ISSUES_FOUND` verdicts), and retry rate
(tiebreaker calls divided by verify calls). Components with a zero
denominator are omitted rather than counted as zero.

The developer-oriented `print_metrics_report()` above is unchanged —
both views coexist. Use `report.py` for reviewers and managers; use
`print_metrics_report()` for in-session debugging.

### Threading session info

When the orchestrator calls `route()`, it should pass `session_set`
and `session_number` so the metrics can be grouped by session:

```python
result = route(
    content=prompt,
    task_type="code-review",
    session_set=str(SESSION_SET),
    session_number=next_session,
)
```

These kwargs are optional — if omitted, the metric still records but
without session-level grouping.

### Disabling metrics

Set `metrics.enabled: false` in `router-config.yaml` to stop writing.
The log is append-only; rotate or archive manually when it gets large
(expect ~100 bytes per call, so thousands of calls per megabyte).

---

## Delegation Discipline

The orchestrator's job is to plan, sequence, and dispatch — not to do
every piece of reasoning itself. Orchestrators tend to hoard work
because calling themselves "feels faster." In practice that means paying
the orchestrator's premium model rate for tasks a Gemini Flash or
Sonnet call at low effort would handle just as well.

The following discipline applies to every session.

### Default: Route Reasoning, Own Mechanics

The orchestrator does these **directly**, without calling `route()`:

- Reading the spec and the activity log
- Creating, editing, renaming, or deleting files
- Running shell commands (build, test, git, Docker)
- Dispatching work to `route()` and logging the result
- Single-file edits under ~50 lines that are mechanical (renames,
  imports, formatting, trivial boilerplate the spec dictates verbatim)
- Interpreting errors enough to decide which task to route next

The orchestrator **always** routes these through `route()`, never
performs them itself:

- Code review
- Security review
- Architecture decisions, pattern selection, design proposals
- Analysis of existing code or test results beyond a surface read
- Documentation writing (change logs, READMEs, doc comments on
  non-trivial APIs)
- Test generation beyond one-off smoke tests
- Session verification (the cross-provider check at end of session)
- Any task that requires producing more than ~50 lines of reasoned
  output

### The "I'll just do this directly" trap

When the orchestrator catches itself thinking *"this is easy, I'll
handle it myself and save the API call,"* that is a signal to route,
not a reason to proceed. The orchestrator is not cheap — its own
token rate for reasoning work is the highest in the system. Routing
a task to Gemini Flash or low-effort Sonnet is almost always the
cheaper and more auditable choice, because the routed call's cost
and output are logged.

The one valid reason to skip `route()` and do the work directly is
when the task genuinely meets all three criteria from the "directly"
list above: mechanical, single-file, under ~50 lines. If any of
those is in doubt, route it.

### How the router already keeps itself cheap

The orchestrator does not pick the model or the effort level. Those
come from `router-config.yaml`:

- The router estimates complexity and picks the cheapest capable tier
- `task_type_params` sets per-task effort/thinking defaults (low for
  formatting, high for verification)
- For code-review/security-review/architecture, a second provider
  auto-verifies — independently, without the orchestrator asking
- Escalation kicks in automatically if a tier-1 response looks
  truncated or refuses

The orchestrator's only job is to pick the right `task_type` when
calling `route()`. Everything else — model selection, effort, thinking
depth, verification — the router handles. Passing the wrong
`task_type` (e.g., tagging an architecture decision as `documentation`)
undercuts the tuning, so this matters.

### Thresholds (human-tunable)

The thresholds above (~50 lines, single file) live in
`router-config.yaml` under `delegation.direct_work_max_lines` and
`delegation.direct_work_max_files`. The human can adjust these per
project. The `delegation.always_route_task_types` list in the same
file is the authoritative list of task types the orchestrator must
never handle directly.

---

## Rules (Apply to All Orchestrators)

This is the authoritative rules list. Instruction files (`CLAUDE.md`,
`AGENTS.md`, `GEMINI.md`) reference this section rather than duplicating it.

1. **One session only.** Never execute more than the assigned session.
2. **Never skip verification.** Every session must be independently verified
   by a different AI provider via `route(task_type="session-verification")`.
3. **Never edit session review files.** They are the verifier's raw output.
4. **Log every step** via `log.log_step()` — including build, test, and
   verification.
5. **Delegate reasoning** to `route()`. See the Delegation Discipline
   section above for the full criteria. In short: code review, security
   review, analysis, architecture, documentation, test generation, and
   session verification always go through `route()`. Do the work yourself
   only for mechanical, single-file edits under ~50 lines.
6. **Do not commit with unresolved Critical/Major issues.** Inform the human.
7. **The human controls orchestrator choice.** Any session can use any agent.
8. **Before every session, read both guidance files.**
   `docs/planning/project-guidance.md` and
   `docs/planning/lessons-learned.md` are mandatory pre-session context.
9. **Treat pending human UAT as blocking.** Do not start downstream sessions
   on top of a checklist the human has not yet reviewed unless the human
   explicitly overrides the pause.
10. **One UAT checklist per session set.** Name it
    `<session-set-slug>-uat-checklist.json` and keep human results inline.
11. **E2E coverage before UAT handoff.** Every functional checklist item
    must have matching Playwright coverage and pass `uat-coverage-review`
    before the checklist is committed and the human is notified. Judgment
    items (`IsJudgmentItem: true`) are exempt from matching-test parity
    but still require sequence-reachability coverage. See the `E2E
    Coverage Before UAT` section above.
12. **Share screenshots during UI and E2E work when practical.**
13. **Escalate durable new guidance.** If the human gives an instruction that
    looks like a future principle or convention, ask whether it should be
    added to `docs/planning/project-guidance.md`.
14. **Recommend lessons learned after failures.** When a failure suggests a
    reusable tactic, propose an update to `docs/planning/lessons-learned.md`.
15. **Run the Step 9 reorganization review on the last session of every
    set, after the notify.** Output "no changes recommended" if nothing
    qualifies — but do the review. Apply any accepted proposals in a
    separate follow-up commit so the session-complete notification is
    never held up by the human reviewing proposals.
