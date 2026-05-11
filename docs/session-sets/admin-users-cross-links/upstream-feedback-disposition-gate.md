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

---

# Second upstream issue (suggest filing separately) — W0 runner emits `SeededSignInAsync` that conflicts with DSL-driven login

> **Target repo:** `darndestdabbler/dabbler-ai-orchestration` (uat_runner) or `darndestdabbler/dabbler-uat-dsl` (W0 CLI) — whichever owns the runner-emitted test wrapper.
> **Suggested issue title:** "Runner-emitted Playwright wrapper auto-signs-in via SeededSignInAsync, breaking checklists whose `Steps[]` start with `NAVIGATE TO \"/login\"`"
> **Surfaced 2026-05-11** during `dabbler-platform` `admin-users-cross-links` Session 1 W2 attempt.

## What happens

The W0 runner emits a thin xUnit wrapper around the compiled DSL:

```csharp
[Fact]
public async Task RunChecklistAsync()
{
    var page = await _fixture.CreatePageAsync();
    await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");  // ← auto-inserted
    var adapter = new PlaywrightFixtureUatAdapter(_fixture);
    await <Name>Uat.RunAsync(adapter, page);
}
```

`SeededSignInAsync` is correct for checklists like `w0-cli-suppressions-and-role-pinning` (whose first DSL step is `NAVIGATE TO "/admin/products"` — post-login). But for checklists authored under the `dabbler-platform/docs/planning/project-guidance.md` "self-contained user journey starting from `/login`" Convention, the auto-inserted pre-sign-in collides:

1. Wrapper signs in via `SeededSignInAsync` → the persona's session cookie is set.
2. DSL runs `NAVIGATE TO "http://localhost:5010/login"` → server redirects to `/dashboard` (already signed in).
3. DSL runs `CLICK INPUT with label "Email address" and enter "admin@acmecoffee.com"` → fails with `TimeoutException : Locator … GetByLabel("Email address")` because `/dashboard` does not render an email input.

## Why a consumer can't easily work around this on their end

- Manually editing the wrapper is non-durable — the runner overwrites it on every compile.
- Removing the wrapper from the runner's emission isn't a consumer-side choice; it lives in the upstream `uat_runner`.
- Working around with suppressions only quiets the validator — the Playwright runtime still hits the timeout.
- The masking-audit-uat-remediation set side-stepped this by hand-authoring its Playwright test (no W0 emission). New sets following the project-guidance Convention can't take that path because it deprecates the W0 pipeline.

## Concrete suggestions (ordered by leverage)

### 1. Auto-detect DSL-driven login

Inspect the compiled DSL output (or the checklist JSON pre-compile). If the first navigation step is to `/login` (or matches `personas.json[persona].login_route` if that becomes a thing), skip the `SeededSignInAsync` line in the emitted wrapper. The runner already inspects the checklist's first sign-in step to infer `--active-role` per `personas.json` — extending that inspection to "first step is `/login` → suppress pre-sign-in" is the cheapest fix and requires no consumer-side opt-in.

### 2. Explicit CLI flag: `--no-pre-sign-in`

Add a flag to `python -m uat_runner.cli` that, when passed, omits `SeededSignInAsync` from the emitted wrapper. Consumer-side sets that author DSL-driven login pass the flag. Less elegant than auto-detect but easier to land and easier to document.

### 3. Make pre-sign-in opt-in via checklist top-level field

Add a top-level field `PreSignIn: false` (default `true` for back-compat) on the checklist JSON. Sets that author DSL-driven login set this to `false`. The runner reads the field and decides whether to emit `SeededSignInAsync`.

## Cross-reference

Related to the `disposition.json` discoverability issue (above) in that both flow from the runner-emitted wrapper / closeout gates introducing constraints that consumer-side workflow docs haven't been updated to reflect. A single round of orchestration-repo documentation + runner enhancements would address both.

## What the consumer side is doing in the meantime

`dabbler-platform/admin-users-cross-links` Session 1: shipped the human-readable UAT checklist following the project-guidance "self-contained user journey" Convention, accepted W2-compiled-test failure as a documented blocker in `disposition.json`, relied on bUnit tests for programmatic correctness of dropdown filtering + banner visibility + cross-link rendering. Once the W0 runner enhancement lands, re-run W2 to convert the blocker to a closed item.
