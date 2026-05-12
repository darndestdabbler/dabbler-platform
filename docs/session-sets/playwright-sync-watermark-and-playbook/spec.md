# Playwright Sync Watermark and Test Playbook Spec

> **Purpose:** Eliminate the class of Playwright timing flakes caused by
> Blazor Server's SignalR-driven render model. Introduces a
> `TrackedComponentBase` that exposes a DOM watermark — `data-actions-in-flight`
> and `data-render-version` on `<html>` — so tests can wait for a deterministic
> "user action fully settled" signal instead of using `Task.Delay()` or hoping
> `NetworkIdle` is sufficient. Ships alongside a supporting playbook:
> `data-testid` gap-fill, web-first assertion cleanup, a first-navigation
> readiness sentinel, reconnect-modal suppression in test, and draining the
> `FlakyPlaywrightCollection` using the new primitive.
> **Created:** 2026-04-28
> **Session Set:** `docs/session-sets/playwright-sync-watermark-and-playbook/`
> **Prerequisite:** `naive-uat-runner-with-feedback` complete. (No technical
> dependency — this set is independent of W1/W2 structurally. Priority
> sequencing places it third because timing-flake reliability is less urgent
> than authorship correctness and functional-bug detection.)
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Session Set Configuration

```yaml
requiresUAT: false
requiresE2E: false
uatScope: none
```

> Rationale: This set changes test infrastructure only — a new Dabbler lib,
> Playwright fixture helpers, and component opt-in base class. No UI behavior
> visible to a human user changes. Per the platform addendum, test-only changes
> and library refactors confined to `src/Libs/` with no observable behavior
> change are `requiresUAT: false` / `requiresE2E: false`. The set's own quality
> bar is: the full Playwright suite runs five consecutive times under full
> parallelism with zero flakes from converted tests.

---

## Project Overview

### What this set delivers

1. **`src/Libs/Dabbler.Testing.Blazor/`** — new Dabbler lib:
   - `Components/TrackedComponentBase.cs` — opt-in `ComponentBase` derivative.
   - `Components/TestReadinessProbe.razor` — writes `data-blazor-ready="true"`
     on `<html>` from `OnAfterRenderAsync(firstRender: true)`.
   - `Configuration/TrackedActionsOptions.cs` — bound to
     `Testing:ActionTracking` config section.
   - `Hosting/TrackedActionsServiceCollectionExtensions.cs` — `AddTrackedActions(IConfiguration)`.
   - `wwwroot/dabbler-tracked-actions.js` — `beginAction` / `endAction` JS
     shim that maintains the watermark attributes.

2. **`appsettings.Development.json` addition** — `"Testing": { "ActionTracking": { "Enabled": true } }`.
   Production has no `Testing` section → defaults to disabled.

3. **Reconnect-modal suppression in test** — `ReconnectModal.razor.css`
   gains a `[data-test-mode="true"] .reconnect-overlay { display: none; }`
   rule; the JS shim sets `data-test-mode="true"` on `<html>` when
   tracking is enabled.

4. **`PlaywrightFixture` updates** — `WaitForQuietAsync(IPage)` helper,
   augmented first-navigation gate with `data-blazor-ready` assertion.

5. **`Task.Delay` replacement** — the ~9 test files identified as using
   `Task.Delay(250..500)` are updated to use `WaitForQuietAsync` (where
   the underlying component has been migrated to `TrackedComponentBase`).

6. **`FlakyPlaywrightCollection` drain** — the session-5 close either
   moves flaky tests to the main collection (timing issue resolved by
   watermark) or [Skip]s them with a filed root-cause per the project
   convention.

### What this set does not do

- Does not force every component to inherit from `TrackedComponentBase`.
  Opt-in. Components are migrated when they drive a test that uses
  `WaitForQuietAsync`.
- Does not change any user-visible UI behavior. The watermark attributes
  are not rendered to users (only set on `<html>`, not visible in the
  viewport). The reconnect-modal suppression only applies when
  `data-test-mode="true"` is present (i.e., in development with tracking
  enabled, the reconnect modal is still visible to a human developer).
  Actually: the modal IS suppressed in development because
  `ActionTracking.Enabled=true` in `appsettings.Development.json` sets
  `data-test-mode`. See Risk Notes for a decision on this.
- Does not change the Playwright test runner's parallelism configuration.
- Does not change `FlakyPlaywrightCollection`'s serial execution mode — it
  remains as the quarantine seam for any post-set residue.

---

## Design Commitments (Decided Up Front)

### TrackedComponentBase: opt-in, not global

Components inherit from `ComponentBase` by default. `TrackedComponentBase`
is an opt-in base class. Session 4's coverage sweep identifies which
components need migration based on which tests have `WaitForQuietAsync` calls.
This is the right scope boundary: migrate exactly the components that are
exercised by tests that need the watermark.

### Config flag: `Testing:ActionTracking:Enabled`

The flag gates ALL behavior:
- JS shim loaded in `App.razor` (server-side conditional on config).
- `TestReadinessProbe` rendered in `MainLayout.razor` (same gate).
- `TrackedComponentBase.TrackAction()` calls `IJSRuntime` only when enabled.
  When disabled, `TrackAction` is `await work()` — no overhead, no
  interop round-trips.
- `data-test-mode="true"` set by the JS shim only when enabled.

Production `appsettings.json` has no `Testing` section → `Enabled` defaults
to `false` (per .NET `IOptions<T>` default-value behavior). The feature is
provably a no-op in production without a `#if DEBUG` smell.

### TrackAction ordering invariant (why it works)

SignalR messages are ordered per connection. The ordering guarantee is:
render diff → JS interop call. Therefore: by the time the JS shim's
`endAction()` sets `data-actions-in-flight="[]"`, the DOM update from the
action's handler has already been applied. `WaitForQuietAsync` is a
deterministic "DOM fully settled" signal for the action that preceded it.

### Reconnect-modal suppression scope

`data-test-mode="true"` is set by the JS shim when `ActionTracking.Enabled`.
In `appsettings.Development.json` (which tests run under), this means the
reconnect modal is suppressed in the Docker compose stack. This is the
intended behavior for test runs. A human developer hitting the app in the
browser during development will NOT see the modal during momentary
SignalR drops — which may be surprising.

Decision: Accept this trade-off. The developer still sees the modal's
JavaScript-driven state transitions (the CSS hides the rendered element; the
JS state machine still runs). If reconnect-modal visibility during development
is important, a follow-up can introduce a separate `Testing:HideReconnectModal`
flag. For now, `ActionTracking.Enabled` covers both concerns.

### `Task.Delay` replacement scope

Only calls where the underlying delay is "wait for a Blazor render to
settle" are replaced. Calls that are "wait for a MudBlazor CSS animation
to finish" may require investigation — the animation itself is not
render-correlated. Session 3 explicitly classifies each `Task.Delay`
callsite as either (a) replaceable with `WaitForQuietAsync` or (b)
animation-bound and documented with a comment. Animation-bound delays
are a known limitation; they may require additional CSS-transition detection
in a follow-up set.

---

## Sessions
### Session 1 of 5: Dabbler.Testing.Blazor lib scaffold and watermark proof

**Steps:**

1. Read `docs/planning/project-guidance.md` and `docs/planning/lessons-learned.md`.
2. Create `src/Libs/Dabbler.Testing.Blazor/Dabbler.Testing.Blazor.csproj`:
   - .NET 8 Razor class library.
   - References `Microsoft.AspNetCore.Components.Web`.
   - Added to `Dabbler.Platform.sln`.
3. Implement `Configuration/TrackedActionsOptions.cs`:
   ```csharp
   public class TrackedActionsOptions
   {
       public bool Enabled { get; set; } = false;
   }
   ```
4. Implement `Hosting/TrackedActionsServiceCollectionExtensions.cs`:
   ```csharp
   public static IServiceCollection AddTrackedActions(
       this IServiceCollection services, IConfiguration configuration)
   {
       services.Configure<TrackedActionsOptions>(
           configuration.GetSection("Testing:ActionTracking"));
       return services;
   }
   ```
5. Implement `Components/TrackedComponentBase.cs`:
   - Inherits `ComponentBase`.
   - Injects `IJSRuntime JS` and `IOptions<TrackedActionsOptions> Options`.
   - Private `ConcurrentQueue<string> _pendingCompletions`.
   - `protected async Task TrackAction(string name, Func<Task> work)`:
     - If `!Options.Value.Enabled`, `await work(); return;`.
     - Else: generate correlation ID, call `JS.InvokeVoidAsync("dabbler.beginAction", id)`,
       `await work()`, enqueue `id` to `_pendingCompletions`, `StateHasChanged()`.
   - Override `OnAfterRenderAsync(bool firstRender)`:
     - While `_pendingCompletions.TryDequeue(out var id)`:
       `await JS.InvokeVoidAsync("dabbler.endAction", id)`.
6. Implement `Components/TestReadinessProbe.razor`:
   ```razor
   @inject IJSRuntime JS
   @inject IOptions<TrackedActionsOptions> Options

   @code {
       protected override async Task OnAfterRenderAsync(bool firstRender)
       {
           if (firstRender && Options.Value.Enabled)
               await JS.InvokeVoidAsync("dabbler.setReady");
       }
   }
   ```
7. Create `wwwroot/dabbler-tracked-actions.js`:
   ```js
   window.dabbler = window.dabbler || {};
   const html = () => document.documentElement;
   const inFlight = () => JSON.parse(html().dataset.actionsInFlight || "[]");

   dabbler.beginAction = id => {
     html().dataset.actionsInFlight = JSON.stringify([...inFlight(), id]);
   };
   dabbler.endAction = id => {
     html().dataset.actionsInFlight =
       JSON.stringify(inFlight().filter(x => x !== id));
     html().dataset.lastAction = id;
   };
   dabbler.setReady = () => {
     html().dataset.blazorReady = "true";
     html().dataset.testMode = "true";
   };
   ```
8. Wire into `AcmeCoffeeOnline.Web`:
   - `Program.cs`: `builder.Services.AddTrackedActions(builder.Configuration);`
   - `App.razor`: conditionally render `<script src="_content/Dabbler.Testing.Blazor/dabbler-tracked-actions.js"></script>`
     only when `IOptions<TrackedActionsOptions>.Value.Enabled`. (Inject the
     options via `@inject` in `App.razor`.)
   - `MainLayout.razor`: add `<TestReadinessProbe />` (the component is a no-op
     when disabled).
   - `appsettings.Development.json`: add `"Testing": { "ActionTracking": { "Enabled": true } }`.
9. Add `PlaywrightFixture.WaitForQuietAsync(IPage page)`:
   ```csharp
   public async Task WaitForQuietAsync(IPage page) =>
       await Assertions.Expect(page.Locator("html"))
           .ToHaveAttributeAsync("data-actions-in-flight", "[]");
   ```
   And update `SeededSignInAsync` (and any `GotoAsync` navigation helpers)
   to wait for `data-blazor-ready="true"` after navigation:
   ```csharp
   await Assertions.Expect(page.Locator("html"))
       .ToHaveAttributeAsync("data-blazor-ready", "true");
   ```
10. Choose one test currently in `FlakyPlaywrightCollection` as the proof
    test. Identify its flaky component (look at the test's `Task.Delay` calls).
    Migrate that component to inherit `TrackedComponentBase` and replace its
    handler calls with `TrackAction(...)`. Rewrite the test's wait to use
    `WaitForQuietAsync`. Move the test back to `PlaywrightCollection`. Run
    the test 5x to confirm no flakes.
11. bUnit test in `tests/Dabbler.Testing.Blazor.Tests/`: render a
    `TrackedComponentBase`-derived test component, call `TrackAction`,
    assert `data-actions-in-flight` transitions from `["id"]` to `[]`
    after `OnAfterRenderAsync`. (Use bUnit's JSInterop mock.)
12. `dotnet build Dabbler.Platform.sln && dotnet test Dabbler.Platform.sln`.
13. `route(task_type="session-verification")`.
14. Commit. Commit message: `Add Dabbler.Testing.Blazor watermark lib and first flaky test proof`.

**Creates:** `src/Libs/Dabbler.Testing.Blazor/` (full project), `tests/Dabbler.Testing.Blazor.Tests/` (bUnit project).
**Touches:** `Dabbler.Platform.sln`, `src/Apps/AcmeCoffeeOnline.Web/Program.cs`, `App.razor`, `MainLayout.razor`, `appsettings.Development.json`, `PlaywrightFixture.cs`, one currently-flaky component, one currently-flaky test.
**Ends with:** Watermark lib builds, bUnit tests pass, and the chosen flaky test passes 5 consecutive times.
**Progress keys:** `session-1/read-planning`, `session-1/lib-project`, `session-1/config`, `session-1/hosting`, `session-1/base-class`, `session-1/probe`, `session-1/js-shim`, `session-1/web-wiring`, `session-1/fixture-helpers`, `session-1/proof-test`, `session-1/bunit-tests`, `session-1/build-and-test`, `session-1/verification`, `session-1/commit`.

---

### Session 2 of 5: Reconnect-modal suppression and readiness-gate rollout

**Steps:**

1. Read planning docs.
2. Add suppression rule to `ReconnectModal.razor.css`:
   ```css
   [data-test-mode="true"] .components-reconnect-modal,
   [data-test-mode="true"] #components-reconnect-modal {
       display: none !important;
   }
   ```
   Inspect `ReconnectModal.razor` to confirm the exact CSS class or ID
   used; adjust the selector to match.
3. Update `PlaywrightFixture` first-navigation logic: every `GotoAsync`
   call in the fixture (currently wrapped in helpers like
   `NavigateViaDrawerAsync`) must await `data-blazor-ready="true"` before
   returning. Centralize: add a `private async Task WaitForReadyAsync(IPage page)`
   helper and insert it into all navigation helpers. Remove any existing
   `WaitForLoadStateAsync(LoadState.NetworkIdle)` calls that immediately
   precede an assertion (they are now redundant with the ready gate — keep
   post-GotoAsync ones only as they still help with initial page load).
4. Run the full Playwright suite to confirm no regressions from the
   navigation-gate change. Document any test that now fails because the
   ready signal is never set for a non-Blazor-Server page (e.g., the
   DevIdp login page, which is a separate app). For those, the
   `data-blazor-ready` wait is skipped on routes outside the base URL
   domain.
5. bUnit test asserting `TestReadinessProbe` fires `dabbler.setReady` only
   on `firstRender=true`.
6. `dotnet build && dotnet test`.
7. `route(task_type="session-verification")`.
8. Commit. Commit message: `Reconnect-modal test suppression and readiness-gate rollout`.

**Creates:** nil (only modifications).
**Touches:** `ReconnectModal.razor.css`, `PlaywrightFixture.cs`, `tests/Dabbler.Testing.Blazor.Tests/`.
**Ends with:** All existing Playwright tests pass with the new readiness gate; reconnect modal is not visible during test runs.
**Progress keys:** `session-2/read-planning`, `session-2/modal-css`, `session-2/readiness-gate`, `session-2/full-suite`, `session-2/bunit-test`, `session-2/build-and-test`, `session-2/verification`, `session-2/commit`.

---

### Session 3 of 5: `Task.Delay` replacement across ~9 test files

**Steps:**

1. Read planning docs.
2. Grep for `Task.Delay(` in `tests/AcmeCoffeeOnline.Playwright/`. For each
   callsite, classify:
   - **Render-wait** (after a click, waiting for a Blazor re-render): replace
     with `await _fixture.WaitForQuietAsync(page)`. Migrate the called
     component to `TrackedComponentBase` if not already done.
   - **Animation-wait** (after a MudBlazor dropdown open, waiting for CSS
     transition to complete): document with a comment:
     `// Animation delay: MudBlazor {ComponentName} transition, not render-correlated.`
     Keep the `Task.Delay` but reduce to the minimum that passes reliably.
     Flag in the activity log for a future follow-up if the delay is >200ms.
   - **Auth-wait** (post-sign-in stability): replace with the `data-blazor-ready`
     gate, which Session 2 already wired.
3. For each render-wait replacement:
   - Identify the component handling the click that triggers the render.
   - Migrate it to `TrackedComponentBase` and wrap its relevant handlers with
     `TrackAction`.
   - Update the test to use `WaitForQuietAsync`.
4. Run the affected test files in isolation. Then run the full suite.
5. For any test whose `Task.Delay` cannot be replaced because it is
   animation-bound: add a comment and document it in the activity log.
   Do NOT move those tests to `FlakyPlaywrightCollection` unless they
   remain flaky after the annotation — animation waits are deterministic
   (fixed duration), not flaky by nature.
6. `dotnet build && dotnet test`.
7. `route(task_type="session-verification")`.
8. Commit. Commit message: `Replace Task.Delay render-waits with WaitForQuietAsync`.

**Creates:** nil.
**Touches:** Up to ~9 test files in `tests/AcmeCoffeeOnline.Playwright/`, corresponding components in `src/Apps/AcmeCoffeeOnline.Web/Components/` and `src/Libs/Dabbler.Security.Blazor/Components/`.
**Ends with:** Zero `Task.Delay` render-wait calls remain. Animation-wait calls are annotated. Full suite passes.
**Progress keys:** `session-3/read-planning`, `session-3/callsite-audit`, `session-3/component-migrations`, `session-3/test-rewrites`, `session-3/full-suite`, `session-3/build-and-test`, `session-3/verification`, `session-3/commit`.

---

### Session 4 of 5: Component coverage sweep and bUnit baseline

**Steps:**

1. Read planning docs.
2. Identify all components in `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/`
   and `src/Libs/Dabbler.Security.Blazor/Components/` that:
   - Are exercised by Playwright tests (search for `data-testid` values
     that appear in both `.razor` and test files).
   - Have non-trivial async event handlers (i.e., `async Task On*Async()`
     or `async Task *Async()` called from click/change events).
   - Do NOT yet inherit from `TrackedComponentBase`.
   Produce a migration list. For components with handlers, propose migration.
   For components that are purely display (no event handlers), skip.
3. Execute the migration for the proposed components:
   - Change `@inherits ComponentBase` (or remove implicit base) to
     `@inherits TrackedComponentBase` (or `TrackedComponentBase` in code-behind).
   - Wrap each relevant handler: `private Task SaveAsync() => TrackAction("save", () => _service.SaveAsync(_model));`.
   - Ensure the component still builds; no behavior change.
4. Expand bUnit test coverage in `tests/Dabbler.Testing.Blazor.Tests/`:
   - Test that `TrackAction` with `Enabled=false` is a transparent pass-through
     (no JSInterop calls).
   - Test that a handler throwing an exception does not leave an orphaned
     in-flight ID (the `finally` block in `TrackAction` must enqueue the
     completion even on failure).
   - Test that concurrent `TrackAction` calls (two actions triggered before
     the first render) correctly maintain multiple in-flight IDs.
5. Add a short entry to `docs/planning/project-guidance.md` under Testing
   Conventions: "Components with async user-action handlers that are
   exercised by Playwright tests should inherit `TrackedComponentBase` and
   wrap handlers with `TrackAction`." Reference the lib path and config flag.
6. `dotnet build && dotnet test`.
7. `route(task_type="session-verification")`.
8. Commit. Commit message: `TrackedComponentBase coverage sweep and bUnit edge cases`.

**Creates:** nil (only modifications).
**Touches:** Multiple components in `src/Apps/` and `src/Libs/`, `tests/Dabbler.Testing.Blazor.Tests/`, `docs/planning/project-guidance.md`.
**Ends with:** All targeted components migrated. bUnit suite green including edge cases.
**Progress keys:** `session-4/read-planning`, `session-4/coverage-audit`, `session-4/component-migrations`, `session-4/bunit-edge-cases`, `session-4/guidance-update`, `session-4/build-and-test`, `session-4/verification`, `session-4/commit`.

---

### Session 5 of 5: Drain `FlakyPlaywrightCollection`, change log, Step 9 reorg

> **MANDATORY first read**: `cumulative-flake-investigation.md` in this
> folder. Session 1 met its spec criterion (5/5 × 2 from a fresh Web
> container) but discovered a separate cumulative-state flake under
> sustained use (~10+ runs against the same warm Web container). The
> investigation doc enumerates the hypotheses tried + dropped, the
> hypotheses NOT tried (a prioritized menu for Session 5), and the
> user's "appears to grow even when we rebuild" observation that points
> at a cross-rebuild-persistent layer. Session 5 inherits the
> investigation; do not re-derive it. **Do not skip the diagnostic
> instrumentation step (#2 below) — Session 1's lesson is that
> guess-and-patch without diagnostics burned hours and produced no fix.**

**Steps:**

1. Read planning docs + `cumulative-flake-investigation.md`.
2. **Add diagnostic capture FIRST** — before any other code change:
   - Extend `PlaywrightFixture.cs:96-101` to capture all browser console
     messages (not just errors).
   - Instrument the JS shim
     (`src/Libs/Dabbler.Testing.Blazor/wwwroot/dabbler-tracked-actions.js`)
     to `console.log` each `beginAction` / `endAction` invocation.
   - Wrap `WaitForQuietAsync` calls in the proof test with a try/catch
     that dumps page state on failure (watermark value, switch checked
     state, snackbar presence, captured console).
3. Run the 20-iteration stress test once with diagnostic capture
   enabled. Use the captured output to pinpoint the failure layer:
   - If `beginAction` logs are absent at failure time → SignalR / circuit
     dispatch issue.
   - If `beginAction` logs are present but `endAction` is missing →
     post-finally render or `OnAfterRenderAsync` issue.
   - If both are present but the attribute is `null` → JS shim or
     attribute-set timing issue.
4. Pick exactly ONE targeted fix from the `cumulative-flake-investigation.md`
   "What We Did NOT Try" menu, based on what the diagnostic revealed.
   Re-run stress test. Repeat at most twice. If still flaky, route the
   diagnostic output to `route(task_type="analysis")` for a second
   opinion before continuing.
5. Target: 20/20 over 3 consecutive batches (60/60) for `E2E_WF_04`
   alone before proceeding to the FlakyPlaywrightCollection drain.
6. For each remaining test in `FlakyPlaywrightCollection`
   (post-Session-2: `MultiOrganizationWorkflowFlakyTests.cs` is `[Skip]`'d
   for a non-watermark reason, and `ProductAdministrationWorkflowFlakyTests.cs`
   holds `E2E_WF_05` which Session 2 moved here as a confirmed cumulative-state
   flake — see the "Cross-Test Confirmation" section in
   `cumulative-flake-investigation.md` for the cross-test evidence;
   verify if any others arrived since Session 2 close):
   - Identify the root cause of the flake per the TODO comment that
     must exist (per project convention).
   - If the root cause is render-timing (and the watermark is now in
     place for the relevant component), move the test to
     `PlaywrightCollection` and run it 5x under full parallelism to
     confirm.
   - If the root cause is a genuine intermittent app bug, file a stub
     spec or open issue; add `[Skip("root cause: ...")]` with a note.
   - If the root cause is still investigation-needed, add a
     `// TODO(drain-flaky): <current hypothesis>` comment and leave in
     `FlakyPlaywrightCollection` — do not pretend to have fixed it.
7. Run the entire Playwright suite 5 consecutive times. Flake count
   from previously-converted tests must be zero.
8. Generate `docs/session-sets/playwright-sync-watermark-and-playbook/change-log.md`
   via `route(task_type="summarization")`.
9. `dotnet build && dotnet test`.
10. Cross-provider verification.
11. Commit all remaining changes. Commit message:
    `Drain FlakyPlaywrightCollection; watermark and playbook set complete`.
12. Send `send_session_complete_notification()`. Then Step 9 reorg review:
    - `Dabbler.Testing.Blazor` is already a shared Dabbler lib — no
      promotion needed. Confirm it is listed in the solution index / README.
    - The Playwright playbook (no `Task.Delay`, web-first assertions,
      readiness gate, reconnect suppression) is a strong candidate for a
      `docs/planning/playwright-blazor-server.md` reference doc, and for
      promotion to `../dabbler-ai-orchestration` as a portable guide.
      Propose adding `playwright-blazor-server.md` in Step 9; propose
      promotion to orchestration repo after W2 close.
    - Lessons-learned candidate: the SignalR ordering invariant (render
      diff → JS interop call guarantees DOM settled before watermark fires)
      — worth documenting so future sessions don't re-derive it.
    - If the cumulative-flake root cause is identified and fixed in this
      session, propose a lessons-learned entry capturing the diagnosis
      pattern: *"Diagnostic instrumentation BEFORE code changes when the
      failure mode is non-obvious — Session 1 of this set burned hours
      patching speculatively when 30 minutes of console-log instrumentation
      would have pinpointed the layer."*
    - If `CircuitOptions` tightening (added in Session 1 redo at
      `Program.cs`) becomes redundant after the real fix lands, decide
      whether to keep it (defensible on its own merits) or revert it.

**Creates:** `change-log.md`, possibly `docs/planning/playwright-blazor-server.md` (if Step 9 accepts).
**Touches:** `FlakyPlaywrightCollection` test files, `docs/planning/lessons-learned.md` (if Step 9 accepts).
**Ends with:** Set complete. `FlakyPlaywrightCollection` contains only [Skip]ped tests with root-cause notes. Zero flakes on 5 consecutive suite runs for previously-converted tests.
**Progress keys:** `session-5/read-planning`, `session-5/flaky-collection-drain`, `session-5/five-run-validation`, `session-5/change-log`, `session-5/build-and-test`, `session-5/verification`, `session-5/commit-push-notify`, `session-5/step-9-reorg`.

---

## What This Set Preserves

| Existing Asset | Status |
|---|---|
| All existing Playwright tests | Must pass after every session |
| `FlakyPlaywrightCollection` collection infrastructure | Preserved; may shrink in session 5 |
| MudBlazor component library | Unchanged |
| `ReconnectModal.razor` UX (non-test) | Unchanged for human users who run without `data-test-mode` |
| All existing Dabbler lib public APIs | Unchanged; new lib added |
| Existing `ComponentBase` inheritance in unmigrated components | Unchanged; migration is opt-in |

## Risk Notes

- **`OnAfterRenderAsync` JS interop adds a SignalR round-trip per render.**
  When `ActionTracking.Enabled=true`, every render that processes completions
  does an extra interop call. In development this is negligible. In production
  `Enabled=false` and there is zero overhead. If the development overhead
  is noticeable (e.g., grid renders visibly slower with many items), the
  interop call can be debounced or batched in `OnAfterRenderAsync` — defer
  this optimization unless it becomes a pain point.

- **Concurrent actions in the same component.** If two `TrackAction` calls
  are in-flight simultaneously (e.g., user double-clicks), both IDs are in
  the queue and both must complete before `data-actions-in-flight` returns
  to `[]`. This is correct behavior. The bUnit test in Session 4 covers this.

- **Components that never call `StateHasChanged` after `TrackAction` returns.**
  If a handler does work but doesn't trigger a re-render (e.g., it modifies
  a field that no component binds to), `OnAfterRenderAsync` may not fire
  and the completion ID stays in the queue indefinitely. `TrackAction` calls
  `StateHasChanged()` unconditionally after `work()` to force at least one
  render cycle. Verify this covers the case.

- **DevIdp / login pages.** The `data-blazor-ready` attribute is set by
  `TestReadinessProbe` in `MainLayout.razor` of `AcmeCoffeeOnline.Web`. Pages
  served by `DevIdp` (the login app, a separate server) will never set it.
  `WaitForReadyAsync` in the fixture must skip the wait for URLs that don't
  belong to the Web app's base URL. Session 2 addresses this.

- **First session's proof test choice.** The proof test in Session 1 should be
  one of the tests in `FlakyPlaywrightCollection` — not just any test. Proving
  the watermark on a test that was already reliable has low signal. Confirm
  the chosen test is actually flaky-under-parallelism before migrating it.
