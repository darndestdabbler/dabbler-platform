# Upstream feedback for `dabbler-ai-orchestration` — `disposition.json` closeout gate

> **Target repo:** `darndestdabbler/dabbler-ai-orchestration`
> **Suggested issue title:** "Closeout `disposition_present` gate is undiscoverable — no workflow-doc reference, no schema link in error message"
> **Reporter:** dabbler-platform, `admin-users-cross-links` Session 1 (2026-05-11)

## What happened (consumer side)

`dabbler-platform` is a `dabbler-ai-router` consumer (pip-installed via `requirements.txt`). Routine implementation-session closeout via `mark_session_complete()` was blocked by:

```
CloseoutGateFailure: close-out gate rejected 1 check(s):
  - disposition_present: disposition.json is required for close-out — write it
    before calling mark_session_complete (or pass force=True to bypass the gate;
    incident-recovery use only).
```

The orchestrator (Claude Sonnet 4.6) had no in-context signal that `disposition.json` was required because:

1. `dabbler-platform/docs/ai-led-session-workflow.md` Step 8 was authored before the gate landed and does not mention `disposition.json`.
2. The error message describes *what* file is missing but not *what fields it must contain* or *where the schema is documented*.
3. Searching the pip-installed package for "disposition" surfaces the `Disposition` dataclass but not a copyable JSON template.

## Why this matters for other consumers

`dabbler-ai-router>=<version-that-added-the-gate>` is shared across every project using the orchestration workflow. Each downstream consumer:

- Has its own `docs/ai-led-session-workflow.md` (or equivalent agent-bootstrap docs) that may pre-date the gate.
- Will hit the same blockage on its next routine closeout.
- Will likely reach for `force=True` (the only path the error message names), which triggers `forceClosed=true` audit markers across the fleet — diluting the signal that `force=True` is supposed to carry ("incident recovery only").

## Concrete suggestions

Three changes, ordered by leverage:

### 1. Documentation: surface `disposition.json` in the canonical workflow procedure

Update `dabbler-ai-orchestration/docs/<canonical-workflow-doc>.md` (or whichever doc is the source-of-truth for the 10-step procedure) so Step 8 reads something like:

> **Step 8:** Author `disposition.json` (schema: `Disposition` dataclass in `ai_router/close_session.py`), then call `mark_session_complete()`, then cost report, commit, push, notify.

If `dabbler-ai-orchestration` already maintains a per-consumer "adopt this workflow" template, update the template too.

### 2. Error message: link to schema or provide template inline

Right now:

```
disposition.json is required for close-out — write it before calling
mark_session_complete (or pass force=True to bypass the gate;
incident-recovery use only).
```

Suggested:

```
disposition.json is required for close-out at docs/session-sets/<slug>/disposition.json.
Schema: status, summary, verification_method, files_changed, next_orchestrator, blockers.
See https://github.com/darndestdabbler/dabbler-ai-orchestration/blob/main/docs/disposition-schema.md
(or `ai_router/close_session.py` Disposition dataclass). Pass force=True to bypass
(incident-recovery only — emits closeout_force_used event).
```

The schema link is the highest-leverage addition. Three of the field names (`verification_method`, `next_orchestrator`, `blockers`) have non-obvious semantics that an orchestrator without docs can't guess correctly.

### 3. Optional: auto-generate `disposition.json` from existing artifacts when missing

`session-state.json`, `activity-log.json`, and `change-log.md` already contain most of the information needed for a `Disposition`:

- `status` → `session-state.json.verificationVerdict` (`VERIFIED` → `completed`, `ISSUES_FOUND` with deferred Major → `requires_review`)
- `summary` → `change-log.md` first paragraph
- `verification_method` → `api` (from `outsourceMode` in spec)
- `files_changed` → `activity-log.json` step records (or git diff)
- `next_orchestrator` → routed `ai-assignment.md` recommendation

A "missing disposition.json — generate from artifacts? [y/N]" CLI path (or a `disposition.json.template` written next to the failure) would close the discoverability gap without compromising the gate's audit value. Optional because docs + error-message fixes get most of the way.

## What the consumer side is doing in the meantime

For this session: authored `disposition.json` retroactively (the session was already force-closed) and added a lessons-learned entry pointing future Sonnet/Opus orchestrators at the requirement. Pre-authoring `disposition.json` before `mark_session_complete` is now part of the platform's Step 8 procedure.

## How to verify the fix on the consumer side

After landing the doc + error-message updates, a smoke test:

1. New consumer clones `dabbler-platform` (or any router consumer) on a clean session set.
2. Orchestrator follows its own `docs/ai-led-session-workflow.md` through Step 8.
3. `mark_session_complete()` either succeeds (because the workflow doc told the orchestrator to author the file) or fails with a message that contains a schema link the orchestrator can act on.

`force=True` should remain available but rarely needed — its audit-trail value should be preserved for true incident-recovery scenarios.
