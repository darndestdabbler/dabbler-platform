# UAT DSL — Watermark Integration Spec

> **Purpose:** Wire the `Dabbler.Testing.Blazor` watermark (delivered by
> `playwright-sync-watermark-and-playbook`) into the UAT DSL pipeline so that
> DSL-compiled E2E tests automatically wait for `data-actions-in-flight = "[]"`
> after interactions on watermarked components. Closes the integration gap
> between the W3 (watermark) and W1 (catalog-grounded authoring) deliverables —
> without it, every DSL-compiled UAT test for a watermarked page replays the
> click-during-rerender race that this set's predecessor just fixed in
> `E2E_WF_04`.
> **Created:** 2026-04-29
> **Session Set:** `docs/session-sets/uat-dsl-watermark-integration/`
> **Prerequisite:** `playwright-sync-watermark-and-playbook` complete (W3
> watermark is in place); `component-catalog-grounded-authorship` complete
> (catalog crawler exists and emits `docs/component-catalog/index.json`);
> `uat-dsl-compiler` complete (grammar + compiler exist).
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Session Set Configuration

```yaml
requiresUAT: false
requiresE2E: false
uatScope: none
```

> Rationale: This set changes test infrastructure only — the DSL grammar +
> compiler in `dabbler-uat-dsl`, the catalog crawler in `dabbler-platform`,
> and the `uat-script-author` prompt template. No user-visible behavior
> changes. Per the platform addendum, test-only changes and library refactors
> with no observable behavior change are `requiresUAT: false` /
> `requiresE2E: false`. Session 3's end-to-end smoke proof is the set's
> primary quality bar: a fresh UAT checklist authored on a watermarked
> page must compile to E2E code that calls `WaitForQuietAsync` after each
> interaction, and the resulting test must pass deterministically.

---

## Project Overview

### What this set delivers

1. **Catalog flag for watermarked routes.** The component catalog at
   `docs/component-catalog/index.json` gains an `IsWatermarked: bool` field
   (or equivalent) on each route entry, true iff the page component for that
   route inherits from `Dabbler.Testing.Blazor.Components.TrackedComponentBase`.
   The catalog crawler (`tools/ComponentCatalogCrawler/`) computes this by
   inspecting the served HTML or — better — by reflecting over the loaded
   `AcmeCoffeeOnline.Web` assemblies at crawl time.

2. **New DSL verb that compiles to `WaitForQuietAsync`.** A new verb in the
   `dabbler-uat-dsl` grammar — recommended form: `WAIT FOR ACTIONS QUIET` —
   that the compiler emits as `await _fixture.WaitForQuietAsync(page);` in
   the generated Playwright C# code. This is the explicit, opt-in form
   spec authors can use when they know the target component is watermarked.

3. **`uat-script-author` prompt template knows about the watermark.** The
   prompt (in `ai-router/prompt-templates/task-prompts.md`) is updated to:
   - Read `IsWatermarked` from the catalog when generating steps.
   - For interaction steps targeting a watermarked route, emit
     `WAIT FOR ACTIONS QUIET` immediately after the interaction verb.
   - For non-watermarked routes, fall back to the existing wait-for-state
     verbs (no behavior change).

4. **End-to-end smoke proof.** Session 3 picks one watermarked page
   (`/admin/products` is the natural choice — it's the page Session 5 of
   `playwright-sync-watermark-and-playbook` validated), authors a fresh
   UAT checklist via the updated `uat-script-author`, compiles it through
   the updated DSL, runs the resulting test, and confirms the watermark
   call appears in the emitted code AND the test passes deterministically
   (run 5x).

### What this set does not do

- Does not retroactively re-author existing UAT checklists. Existing
  checklists keep their current form and are grandfathered. New checklists
  authored after this set lands automatically pick up the watermark
  integration via the updated prompt + DSL.
- Does not change the watermark mechanism itself. Iteration 4 from
  `playwright-sync-watermark-and-playbook` is the load-bearing primitive;
  this set only routes UAT-DSL output through it.
- Does not migrate any new components to `TrackedComponentBase`. Migration
  scope is governed by the existing project-guidance Convention; this set
  takes the catalog as-is and flags whatever the catalog already sees.
- Does not change the per-checklist UAT runner (`ai-router/uat_runner/`).
  The runner already runs `dotnet test` against the compiled DSL output;
  no integration changes there are needed.

---

## Design Commitments (Decided Up Front)

### Catalog flag is computed, not authored

`IsWatermarked` is determined at catalog-crawl time by checking whether
the page component's `@inherits` directive points at
`Dabbler.Testing.Blazor.Components.TrackedComponentBase`. Authors do not
manually maintain the flag. If the crawler cannot reliably determine the
inheritance (e.g., because the component is dynamically loaded), the flag
defaults to `false` (the watermark is opt-in; missing is the safe default).

### New verb, not implicit emission

The DSL grammar gains a new explicit verb (`WAIT FOR ACTIONS QUIET` or
similar). The compiler does **not** emit `WaitForQuietAsync` implicitly
after every interaction. Reasons:

- Explicit verbs preserve the closed-grammar contract: every emitted line
  in the test corresponds to an authored DSL step.
- Spec authors retain the option to skip the wait when they know the
  interaction does not produce an async re-render (e.g., a no-op
  acknowledgement).
- Implicit emission would require the compiler to know which interactions
  produce async re-renders — a deeper integration than this set should
  attempt.

The `uat-script-author` prompt is the layer that decides when to emit the
verb: it sees the catalog flag and emits the verb after every interaction
on a watermarked route. Manual hand-authoring keeps the same option.

### Verb name: `WAIT FOR ACTIONS QUIET`

Mirrors the C# helper name `WaitForQuietAsync`. Reads naturally in DSL
prose ("CLICK BUTTON ... WAIT FOR ACTIONS QUIET"). Uses already-reserved
DSL keywords (`WAIT FOR`). The Session 2 implementer may pick a better
form during grammar-fit review; the only firm requirement is that the
verb compiles to `await _fixture.WaitForQuietAsync(page);`.

### Smoke target: `/admin/products`

The Session 3 smoke uses `/admin/products` because it's the page Session 5
of `playwright-sync-watermark-and-playbook` validated 59/60 isolated. Any
other watermarked page would also work; the choice is operational
(reproducibility of the prior batch results).

---

## Cross-repo orchestration

This set spans `dabbler-platform` and `dabbler-uat-dsl`. Per the
established cross-repo convention (and the `dabbler-uat-dsl/CLAUDE.md`
instructions): the spec, activity log, ai-assignment, session-reviews,
and change-log all live in `dabbler-platform` here. Code commits land in
whichever repo the session's work touches.

| Session | Authoritative repo for code | Touches sibling? |
|---|---|---|
| 1 | `dabbler-platform` | no |
| 2 | `dabbler-uat-dsl` | no |
| 3 | `dabbler-platform` | no — uses Session 2's already-published DSL |

Session 2's commit message must reference the dabbler-platform spec by
slug; Session 3's spec excerpt for the prompt-template change must
reference the Session 2 DSL commit SHA.

---

## Session Plan

### Session 1 of 3: Catalog crawler — flag watermarked routes

**Steps:**

1. Read planning docs (`project-guidance.md`, `lessons-learned.md`,
   `session-set-authoring-guide.md`) and the `cumulative-flake-investigation.md`
   from `playwright-sync-watermark-and-playbook` for context.
2. Audit `tools/ComponentCatalogCrawler/` to find the catalog-emit code
   path. Locate where each route entry is built.
3. Add an `IsWatermarked: bool` field to the catalog index schema.
   Update the schema documentation in the crawler's README (or wherever
   the schema is described).
4. Implement the detection. Two acceptable approaches:
   - **(a) Reflection on the loaded Web assemblies.** Cheaper to reason
     about: load `AcmeCoffeeOnline.Web.dll`, find the component type for
     each route, walk the inheritance chain, return true iff
     `Dabbler.Testing.Blazor.Components.TrackedComponentBase` is in the
     chain.
   - **(b) HTML probe at crawl time.** Visit the route, read
     `data-actions-in-flight` from `<html>` after a known interaction;
     if it transitions, the page is watermarked.
   Prefer (a) — it's deterministic and doesn't require a triggering
   interaction. Fall back to (b) only if reflection cannot resolve the
   component type for some routes.
5. Re-crawl against the running compose stack. Verify each watermarked
   page (`/admin/products`, plus the routes touched by Session 4 of
   `playwright-sync-watermark-and-playbook` — `/admin/customers/*`,
   `/admin/orders/*`) gets `IsWatermarked: true`. Verify a non-watermarked
   page (e.g., `/dashboard`) gets `IsWatermarked: false`.
6. Add unit tests for the detection logic.
7. `dotnet build && dotnet test`.
8. `route(task_type="session-verification", max_tier=2)` — cross-provider.
9. Commit. Commit message: `Catalog crawler: flag IsWatermarked per route`.

**Creates:** Possibly a new helper class in
`tools/ComponentCatalogCrawler/`. Updated `docs/component-catalog/index.json`
(committed diff so future PRs see the change).
**Touches:** Catalog crawler source, catalog index, crawler unit tests.
**Ends with:** Catalog has accurate `IsWatermarked` flags. Detection
unit-tested. Build clean.
**Progress keys:** `session-1/read-planning`, `session-1/crawler-audit`,
`session-1/schema-update`, `session-1/detection-impl`, `session-1/recrawl`,
`session-1/unit-tests`, `session-1/build-and-test`, `session-1/verification`,
`session-1/commit`.

---

### Session 2 of 3: DSL grammar + compiler — `WAIT FOR ACTIONS QUIET` verb

> **Code work happens in `dabbler-uat-dsl`.** Per
> `dabbler-uat-dsl/CLAUDE.md`, read this spec and the
> dabbler-platform planning docs first; do code work in the sibling repo;
> log step entries in this set's `activity-log.json` in `dabbler-platform`.

**Steps:**

1. Read planning docs (in `dabbler-platform`) + this spec + the existing
   `dabbler-uat-dsl/grammar/DabblerUatDsl.g4` and the compiler emitter for
   wait-for verbs.
2. Extend the grammar with a new lifecycle verb. Recommended:
   `WAIT FOR ACTIONS QUIET`. Implementer may choose a better form if
   grammar fit dictates — must compile to
   `await _fixture.WaitForQuietAsync(page);` regardless of name.
3. Update the parser, AST, and semantic validator to accept the verb.
4. Update the C# emitter to produce
   `await _fixture.WaitForQuietAsync(page);` for the new verb.
5. Add grammar-level tests (parse, validate, emit) for the verb. Update
   the `sample-checklist.json` and `sample-output.cs` in the repo root if
   they're used as canonical examples.
6. Update the published DSL grammar reference in
   `https://github.com/darndestdabbler/dabbler-uat-dsl/blob/main/grammar/DabblerUatDsl.g4`
   commit so it's discoverable.
7. Pack a new local NuGet package via the repo's existing build pipeline
   (`pack-output/`) so `dabbler-platform` consumers can pick up the
   change without a manual coordination step.
8. `dotnet build && dotnet test` in `dabbler-uat-dsl`.
9. `route(task_type="session-verification", max_tier=2)` — cross-provider.
10. Commit (in `dabbler-uat-dsl`). Commit message:
    `Add WAIT FOR ACTIONS QUIET verb; emit WaitForQuietAsync`.
    Reference the `dabbler-platform` spec slug in the message.

**Creates:** New grammar rule, new AST node, new emitter case, new tests.
**Touches:** `grammar/DabblerUatDsl.g4`, parser, AST, semantic validator,
emitter, tests, `pack-output/`, `sample-output.cs` (if used).
**Ends with:** New verb is parseable, validates, emits the correct C#,
and the local NuGet pack is published. All grammar tests pass.
**Progress keys:** `session-2/read-planning`, `session-2/grammar-extend`,
`session-2/parser-ast`, `session-2/emitter`, `session-2/grammar-tests`,
`session-2/pack`, `session-2/build-and-test`, `session-2/verification`,
`session-2/commit`.

---

### Session 3 of 3: Prompt update + end-to-end smoke + close

**Steps:**

1. Read planning docs + this spec + Sessions 1-2 outcomes from
   `activity-log.json`.
2. Update `dabbler-platform`'s NuGet reference (or
   `nuget.config` source paths) so the new `dabbler-uat-dsl` package
   from Session 2's pack-output is consumed by the build.
3. Update `ai-router/prompt-templates/task-prompts.md` for the
   `uat-script-author` task type:
   - Add a section describing the catalog's `IsWatermarked` field.
   - Add a rule: "When generating steps for a route whose catalog entry
     has `IsWatermarked: true`, emit `WAIT FOR ACTIONS QUIET` immediately
     after each interaction verb (CLICK BUTTON, CLICK SWITCH,
     CLICK INPUT...) for that route. Do not emit the verb after pure
     navigation or verification verbs."
   - Include one positive example (a click on a watermarked route +
     `WAIT FOR ACTIONS QUIET`) and one negative example (a click on a
     non-watermarked route — no verb emitted).
4. End-to-end smoke proof:
   - Pick `/admin/products` as the smoke target.
   - Author a fresh UAT checklist via
     `route(task_type="uat-script-author")` with the updated prompt.
   - Validate via `dabbler-uat-dsl validate`.
   - Compile via `dabbler-uat-dsl compile`.
   - Inspect the emitted `*.cs` and confirm `WaitForQuietAsync(page)` is
     present after each interaction step.
   - Run via `python ai-router/uat_runner/cli.py` against the running
     compose stack, 5 iterations.
   - Record pass rate. **Quality bar: 5/5 PASS.**
5. If the smoke fails (any iteration), root-cause via the cumulative-flake
   investigation discipline (diagnostic instrumentation FIRST, max two
   iterations, then route to analysis). The cumulative-flake-investigation.md
   pattern from `playwright-sync-watermark-and-playbook` is the model.
6. Generate `change-log.md` via
   `route(task_type="summarization", max_tier=1)`.
7. `dotnet build && dotnet test` in `dabbler-platform` (full solution
   excluding Playwright suite).
8. `route(task_type="session-verification", max_tier=2)` — cross-provider.
9. Commit. Commit message: `UAT DSL watermark integration complete; smoke proves end-to-end`.
10. Send `send_session_complete_notification()`.
11. Step 9 reorganization review:
    - Candidate: extend the `project-guidance.md` testing convention again
      to mention the DSL `WAIT FOR ACTIONS QUIET` verb as the canonical
      way UAT checklists synchronize with the watermark.
    - Candidate: lessons-learned entry on the value of catalog-driven
      flags vs. manual annotation (if Session 1 surfaced a clean pattern).
    - Apply accepted proposals in a separate follow-up commit.

**Creates:** `change-log.md`, possibly a new sample/fixture UAT checklist
under the session-set folder for future reference.
**Touches:** `ai-router/prompt-templates/task-prompts.md`, `nuget.config`
or NuGet package reference, `docs/planning/project-guidance.md` (if Step 9
accepts).
**Ends with:** Set complete. UAT checklist authored on a watermarked page
compiles to E2E code that uses `WaitForQuietAsync`, and the resulting test
passes 5/5 against the live stack.
**Progress keys:** `session-3/read-planning`, `session-3/nuget-update`,
`session-3/prompt-update`, `session-3/smoke-author`, `session-3/smoke-compile`,
`session-3/smoke-run`, `session-3/change-log`, `session-3/build-and-test`,
`session-3/verification`, `session-3/commit-push-notify`,
`session-3/step-9-reorg`.

---

## What This Set Preserves

| Existing Asset | Status |
|---|---|
| Existing UAT checklists | Unchanged; grandfathered in their current form |
| Existing E2E tests | Unchanged |
| `Dabbler.Testing.Blazor` watermark mechanism | Unchanged; Session 5 of `playwright-sync-watermark-and-playbook` is load-bearing |
| `dabbler-uat-dsl` grammar (existing verbs) | Unchanged; only one new verb added |
| Component catalog schema (existing fields) | Unchanged; only `IsWatermarked` added |
| `uat-script-author` prompt template (existing rules) | Unchanged; new rule added for watermarked routes |
| UAT runner (`ai-router/uat_runner/`) | Unchanged |

## Risk Notes

- **Session 2 takes work in a sibling repo.** The orchestrator must
  follow the `dabbler-uat-dsl/CLAUDE.md` cross-repo workflow: spec lives
  here, code commits land in the sibling, and the activity log is updated
  here. Confirm both commits land cleanly before declaring Session 2
  complete.

- **Catalog reflection vs. HTML probe.** Session 1's preferred detection
  is reflection on loaded Web assemblies. If the crawler runs in a process
  that doesn't load `AcmeCoffeeOnline.Web.dll`, this is harder than it
  sounds. The HTML-probe fallback (perform a known interaction, observe
  the watermark transition) is more robust but requires triggering an
  interaction during catalog crawl, which today's crawler doesn't do.
  If reflection is unworkable, Session 1 may need to either (a) extend
  the crawler to load assemblies for inspection, or (b) accept the
  HTML-probe approach with a stable seed interaction. Decide during
  Session 1's audit step.

- **Verb-naming bikeshed.** `WAIT FOR ACTIONS QUIET` is the recommended
  name; the Session 2 implementer is empowered to pick a different form
  if grammar fit suggests it. The only firm contract is: the verb
  compiles to `await _fixture.WaitForQuietAsync(page);`. Name churn is
  cheap before the verb has consumers.

- **`uat-script-author` prompt regression risk.** The prompt change in
  Session 3 affects EVERY future UAT checklist generated for a watermarked
  route. The Session 3 smoke is the primary regression check. If the
  smoke generates a checklist whose emitted C# is malformed or fails to
  pass, Step 9 must NOT close the set — file an issue and revert the
  prompt change to the prior baseline.

- **Smoke against `/admin/products` is contingent on the page being healthy.**
  Session 5 of `playwright-sync-watermark-and-playbook` left the page in a
  passing state (135 / 1 / 2 in full suite, with the 1 residual unrelated
  to watermark). If between then and Session 3's start a regression has
  landed on the page, the smoke target may need to switch to another
  watermarked page (`/admin/customers/*`, `/admin/orders/*`).
