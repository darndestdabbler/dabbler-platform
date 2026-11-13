# Change Log: Playwright Sync Watermark and Playbook (Sessions 1-5, Completed 2026-04-29)

This change log documents the work undertaken to eliminate Playwright timing flakes in Blazor Server applications. The core goal was to introduce a DOM-watermark-based test synchronization primitive to reliably account for Blazor Server's SignalR-driven render model.

## Cross-Cutting Findings and Outcomes

The initiative successfully developed and integrated a robust synchronization mechanism, `TrackedComponentBase` with `TrackAction` and `WaitForQuietAsync`, into the test infrastructure. A significant challenge was the identification and resolution of a subtle race condition where Blazor component handlers were being torn down during search-induced re-renders, causing clicks to dispatch to non-existent handler IDs. This was definitively traced using diagnostic instrumentation.

Key lessons learned include:
*   **Diagnostic-first discipline:** Prioritizing comprehensive browser console capture and JS shim logging was critical in pinpointing the root cause of the elusive race condition.
*   **Watermark Coverage:** The watermark mechanism needs to cover data-loading callbacks (e.g., search, debounced inputs) in addition to direct user action handlers to provide complete synchronization.
*   **`@rendermode InteractiveServer`**: Applying `InteractiveServer` render mode per-page is not a universal solution for timing issues and can sometimes exacerbate them or introduce new issues.
*   **Cumulative Flake Misdiagnosis:** Initial observations of "cumulative-state" flakes were re-evaluated as per-iteration race conditions whose probability simply appeared to accumulate over repeated runs.
*   **Test Infrastructure Resilience:** Test infrastructure components, especially those involving JS interop, must degrade gracefully on prerender or non-interactive paths to avoid runtime errors in production library contexts.

The `E2E_WF_04` watermark race is now definitively closed. The watermark mechanism is resilient to JS-interop unavailability, allowing its safe application to data-loading callbacks without breaking prerender or non-interactive paths.

## Session Details and Deliverables

### Session 1 (Opus 4.7, 2026-04-28/29)

**Goal:** Establish foundational watermark primitives and initial Playwright integration.

**Deliverables:**
*   **`src/Libs/Dabbler.Testing.Blazor`:** Created new Razor class library.
*   **`TrackedComponentBase`:** Introduced base component for watermark integration.
*   **`TestReadinessProbe.razor`:** Component for Blazor readiness signalling.
*   **`TrackedActionsOptions` & `AddTrackedActions`:** Configuration and DI extension for tracking actions.
*   **`wwwroot/dabbler-tracked-actions.js`:** JavaScript shim to maintain `data-actions-in-flight` and `data-blazor-ready` attributes on the `<html>` element.
*   **AcmeCoffeeOnline.Web Integration:** Wired the new components and services into the application.
*   **`PlaywrightFixture.cs`:** Added `WaitForQuietAsync(IPage)` and `WaitForReadyAsync(IPage)` helpers.
*   **`ProductManagement.razor` Migration:** Migrated to `TrackedComponentBase` and wrapped `ToggleAvailabilityAsync` in `TrackAction`.
*   **`E2E_WF_04` Rework:** Rewrote `E2E_WF_04` to use `WaitForQuietAsync` and moved it out of `FlakyPlaywrightCollection`.
*   **Diagnostic Finding:** Identified a residual cumulative-flake failure mode under sustained use, documented in `cumulative-flake-investigation.md`.

### Session 2 (Opus 4.7 Medium)

**Goal:** Refine readiness probes and address initial flake observations.

**Deliverables:**
*   **CSS Suppression:** Added `[data-test-mode="true"] .components-reconnect-modal` CSS rule to suppress reconnect modals during tests.
*   **Centralized `WaitForReadyAsync`:** Consolidated `WaitForReadyAsync` into `PlaywrightFixture` navigation helpers.
*   **bUnit Test for `TestReadinessProbe`:** New test for `TestReadinessProbe`'s `firstRender` behavior.
*   **Cumulative Flake Identification:** Identified `E2E_WF_05` as a second cumulative-state flake and moved it to `FlakyPlaywrightCollection`.
*   **Cross-Test Confirmation:** Confirmed that the cumulative flake was platform-wide, not specific to a watermarked component.

### Session 3 (Sonnet 4.6 Medium)

**Goal:** Optimize Playwright test synchronization by replacing `Task.Delay` calls.

**Deliverables:**
*   **`Task.Delay` Refactoring:** Replaced 30 `Task.Delay` calls across 8 test files and `PlaywrightFixture.cs`:
    *   **14 removals:** Redundant with existing web-first assertions.
    *   **13 annotations:** Kept with comments for animation/debounce delays (not render-correlated).
    *   **3 kept as-is:** Used in polling loops.
*   **Verifier Endorsement:** Confirmed that web-first assertions are superior to `WaitForQuietAsync` as replacements when already covered.

### Session 4 (Sonnet 4.6 Medium)

**Goal:** Expand watermark coverage and improve testability of `TrackedComponentBase`.

**Deliverables:**
*   **Component Migration:** Migrated `ProductDetail`, `CustomerDetail`, and `OrderDetail` components to `TrackedComponentBase`.
*   **Library Exclusion:** Explicitly excluded `Dabbler.Security.Blazor` (production libraries should not depend on test libraries without specific authorization).
*   **Virtual `RequestRender()` Seam:** Added a virtual `RequestRender()` method to `TrackedComponentBase` (defaults to `StateHasChanged`, overridable for test harnesses).
*   **bUnit Edge-Case Tests:** Split bUnit tests for `TrackedComponentBase` due to bUnit 2.6 deadlock: disabled-path via bUnit, enabled-path via plain xUnit with `RecordingJSRuntime` mock.
*   **`project-guidance.md`:** Documented new testing conventions.

### Session 5 (Opus 4.7 High)

**Goal:** Final cumulative-flake hunt, diagnosis, and resolution.

**Deliverables:**
*   **Diagnostic Instrumentation:** Implemented browser console capture in `PlaywrightFixture`, `console.log` in JS shim, and `WaitForQuietAsync` try/catch diagnostic dump on failure.
*   **Diagnostic Finding:** Conclusively ruled out JS shim, attribute-set timing, and `OnAfterRenderAsync` drain. The root cause was the click event dispatching to a DOM element whose Blazor handler-id was being torn down by a search-induced re-render before `TrackAction` could be invoked.
*   **Four Fix Iterations:**
    *   **Iteration 1 (Reverted):** Per-page `@rendermode InteractiveServer` led to worse outcomes.
    *   **Iteration 2 (Reverted):** `TrackAction`-wrap `OnSearchAsync` + `WaitForQuietAsync` after `FillAsync`. Achieved 59/60 isolated passes but broke `/admin/products` with HTTP 500 for `BackofficeUserAdmin` due to `InvalidOperationException` in JS interop during that role's render path.
    *   **Iteration 3 (Reverted):** NetworkIdle test-only fix narrowed the race window but did not close it.
    *   **Iteration 4 (LANDED):** Re-applied search-watermark and hardened `TrackedComponentBase.TrackAction` with `TryInvokeJsAsync`, which catches `InvalidOperationException` and silently degrades to a no-op. This definitively closed the watermark race.
*   **New bUnit Test:** Added for the silent-degrade path.
*   **`SmokeTests.AdminNavigation`:** Regression fixed.
*   **Second-Opinion Analysis:** Routed to `gemini-pro` ($0.0145) which correctly diagnosed the click-during-rerender race after Iteration 1.
*   **`FlakyPlaywrightCollection` Drain:** `E2E_WF_05` TODO updated with Iteration 4 context and candidate fix path (wrap dialog-open in `TrackAction`). Retained in flaky collection due to unreproducible isolation failure. `E2E_WF_02` remains `[Skip]` (deterministic failure, not flaky).
*   **Build Status:** `dotnet build clean` passed. Non-Playwright tests: 715 passed / 0 failed across 7 assemblies. Full Playwright suite: 135 passed / 1 failed / 2 skipped (the 1 residual is a pre-existing flake).