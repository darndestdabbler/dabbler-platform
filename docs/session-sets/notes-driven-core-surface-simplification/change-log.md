# Change Log: Notes-Driven Core Surface Simplification
> **Status:** Complete
> **Completed:** 2026-04-14
> **Session Set:** `docs/session-sets/notes-driven-core-surface-simplification/`
> **Source Inputs:** `docs/planning/notes-driven-supported-library-surface.md` and `docs/planning/notes-driven-library-compatibility-notes.md`

## Summary

This session set simplified the `src/Libs/` reusable surface around the notes-driven contract defined by the preceding audit. The work focused on tightening public APIs in place, de-emphasizing or hiding advanced features not required by the core contract, and marking unproven composite surfaces for retirement. Key retirement candidates included `DabblerQueryReportPage<T>` and `DabblerCatalogPage`, which are now marked as obsolete. Dedicated `Dabbler.Components` and `Dabbler.Reports.Blazor` contract-test homes were established to protect the retained non-security reusable surface with direct tests. The set completed as planned in five sessions, and no catch-up session was appended.

## Session Outcomes

- **Session 1: Supported Surface Definitions And Coverage Gate Baseline**
  Translated the audit decisions into an explicit supported-surface definition document. Established dedicated `Dabbler.Components` and `Dabbler.Reports.Blazor` contract-test homes and added baseline tests. Updated library-facing documentation to narrow the advertised feature set before simplification work began.

- **Session 2: Grid, Filter, And Query Simplification**
  Simplified `DabblerDataGrid<T>`, `DabblerFilterGrid<T>`, and `DabblerQueryPage<T>` by de-emphasizing non-core features like grouping, presets, quick filters, and advanced export helpers. Added contract tests to the `Dabbler.Components` test home to prove the retained core grid/filter/query behavior remained intact.

- **Session 3: Combo, Detail, Popup, And Report Surface Simplification**
  Narrowed combo-box contracts by removing add-new and grouping features. Marked `DabblerQueryReportPage<T>` as compatibility-only and steered users toward composing `DabblerReportPage` directly. Added contract tests for the retained form, popup, combo, and report surfaces.

- **Session 4: Shell, Dashboard, Catalog, And Security Surface Simplification**
  Simplified `DabblerShell` and `DabblerDashboardPage` around the core left-nav, workspace, and landing-page behaviors. Marked `DabblerCatalogPage` as a retirement candidate and isolated dashboard favorites/recent-items widgets as non-core. Updated security UI guidance to align with the notes-driven runtime authorization model.

- **Session 5: Rebaseline, Compatibility Notes, And Closeout**
  Captured final compatibility notes and preferred API replacements for downstream app-alignment work. Rebaselined the specs for the `notes-driven-app-alignment` and `notes-driven-template-encoding` session sets to reflect the new simplified library baseline. Confirmed no catch-up session was required.

## Retained Core Surface

The simplification work retained a narrower, notes-driven core surface now protected by dedicated contract-test homes.

- **Grid, Filter, and Query:** `DabblerQueryPage<T>`, `DabblerDataGrid<T>`, and `DabblerFilterGrid<T>` for core list, search, sort, page, and QBE workflows.
- **Forms and Popups:** `DabblerComboBox<T>`, `DabblerMultiComboBox<T>`, `DabblerDetailForm<T>`, and `DabblerRecordPopup<T>` for lookup and record-editing workflows.
- **Shell and Dashboard:** `DabblerShell` for left-nav/workspace composition and `DabblerDashboardPage` for core landing-page behavior.
- **Reports:** `DabblerReportPage`, `ParameterPromptForm`, and `ReportViewer` for the standard report prompting and viewing workflow.
- **Test Homes:** New contract tests in `tests/Dabbler.Components.Tests` and `tests/Dabbler.Reports.Blazor.Tests` now provide direct coverage for this retained surface.

## Compatibility And Migration Notes

Advanced library features were de-emphasized or marked for retirement to guide downstream app and template alignment.

- **Hidden APIs:** Advanced features on `DabblerDataGrid<T>` and `DabblerFilterGrid<T>` (e.g., grouping, presets, pinning) were hidden from IntelliSense to de-emphasize them as non-core.
- **Obsolete APIs:** `DabblerCatalogPage` and `DabblerQueryReportPage<T>` were marked with `Obsolete` to signal their retirement-candidate status and guide consumers to preferred replacements.
- **Compatibility Paths:** Dashboard favorites/recents and `DabblerTopMenuBar` were retained as compatibility-only paths that render placeholders or require explicit provider configuration.

## Downstream Handoff

This session set hands off a simplified and test-protected library surface to the `notes-driven-app-alignment` and `notes-driven-template-encoding` session sets. The handoff artifact `docs/planning/notes-driven-library-compatibility-notes.md` provides explicit migration guidance for downstream work to align with the new, narrower supported contract.