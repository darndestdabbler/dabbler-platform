# Notes-Driven Core Surface Simplification Spec

> **Purpose:** Simplify `src/Libs/` around the notes-driven contract, trimming
> or isolating advanced functionality that the audit found to be outside the
> supported core surface.
> **Created:** 2026-04-14
> **Session Set:** `docs/session-sets/notes-driven-core-surface-simplification/`
> **Prerequisite:** `notes-driven-library-contract-audit` is complete and its
> decision ledger plus retained-surface coverage gate are the current source of
> truth.
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification

---

## Project Overview

This session set performs the first real simplification pass on `src/Libs/`.

It should prefer tightening the supported contract in place before choosing any
focused rebuild. Rebuilds are allowed only when trimming in place would leave a
misleading public API or a hard-to-test implementation.

The immediate goal is not to remove every extra code path. The goal is to make
the supported surface obvious, narrow, and testable.

Before retiring adjacent non-security functionality, this set should establish
dedicated `Dabbler.Components` and `Dabbler.Reports` contract-test homes so
the retained reusable surface is protected directly instead of relying only on
app smoke coverage.

The decision ledger from `notes-driven-library-contract-audit` is expected to
drive removal/retirement of unproven public surfaces, not just softer
de-emphasis. In particular, `DabblerQueryReportPage<T>` and
`DabblerCatalogPage` should be treated as retirement candidates unless new
evidence appears during simplification work.

## Session Plan

### Session 1 of 5: Supported Surface Definitions And Coverage Gate Baseline

**Steps:**
1. Translate the audit decisions and retained-surface coverage gate into
   explicit supported-surface definitions for the major library areas:
   grid/filter/query, combo/detail/popup, shell/dashboard/catalog, reports,
   and security UI.
2. Establish dedicated `Dabbler.Components` and `Dabbler.Reports`
   contract-test homes for the retained non-security reusable surface before
   retirements begin.
3. Update library-facing docs so they no longer advertise out-of-scope
   features as first-class defaults.

**Creates:** Supported-surface definitions, contract-test homes, and updated
library-facing guidance
**Touches:** Library docs, planning guidance, and reusable-surface tests
**Ends with:** The repo describes the intended supported contract and has a
dedicated test home ready before retirements begin
**Progress keys:** `session-1/supported-surface`,
`session-1/coverage-gate-baseline`, `session-1/docs-narrowing`

---

### Session 2 of 5: Grid, Filter, And Query Simplification

**Steps:**
1. Simplify `DabblerDataGrid<T>`, `DabblerFilterGrid<T>`, and
   `DabblerQueryPage<T>` around the notes-driven workflow.
2. Remove, de-emphasize, or isolate the advanced features the audit marked as
   non-core, such as grouping, clipboard/csv helpers, pinning, presets, quick
   filter, and column-visibility toggles.
3. If the existing surfaces cannot be cleaned up safely, introduce focused
   replacements with clearer contracts and compatibility notes.
4. Add tests in the dedicated `Dabbler.Components` contract-test home that
   prove the retained core grid/filter/query behavior still works before
   adjacent extras are retired.

**Creates:** Narrowed grid/filter/query surface and supporting tests
**Touches:** `src/Libs/Dabbler.Components/` and relevant tests/docs
**Ends with:** Query-style workflows are easier to understand and no longer
   lead with out-of-scope behavior
**Progress keys:** `session-2/grid-simplification`,
`session-2/filter-simplification`, `session-2/query-surface`,
`session-2/grid-filter-tests`

---

### Session 3 of 5: Combo, Detail, Popup, And Report Surface Simplification

**Steps:**
1. Keep the notes-aligned combo/detail/popup behavior as core.
2. Narrow combo-box behavior around search, multiple columns, multiselect, and
   cascading coordination, allowing multiselect to survive without preserving a
   distinct `DabblerMultiComboBox<T>` public surface if the main combo contract
   can absorb it cleanly.
3. Isolate or remove add-new and grouping features unless the decision ledger
   explicitly retained them.
4. Confirm report surfaces remain aligned with the notes-driven report/query
   behavior and retire advanced composed report-query surfaces that are not yet
   proven, especially `DabblerQueryReportPage<T>`.
5. Add or update tests in the dedicated `Dabbler.Components` and
   `Dabbler.Reports` contract-test homes for the retained core behavior before
   broader combo/report surfaces are retired.

**Creates:** Simplified combo/report/detail contract and tests
**Touches:** `src/Libs/Dabbler.Components/`,
`src/Libs/Dabbler.Reports.Blazor/`, and relevant tests/docs
**Ends with:** Form, popup, combo, and report surfaces align with the notes
without advertising speculative extras
**Progress keys:** `session-3/combo-contract`,
`session-3/detail-popup-contract`, `session-3/report-contract`,
`session-3/form-report-tests`

---

### Session 4 of 5: Shell, Dashboard, Catalog, And Security Surface Simplification

**Steps:**
1. Simplify `DabblerShell`, dashboard, and navigation surfaces around the top
   menu, left nav, pinned-item, workspace, and dashboard behaviors required by
   the notes.
2. Remove or isolate favorites/recent-items widgets and retire generic catalog
   abstractions, including `DabblerCatalogPage`, when the audit did not justify
   them as part of the primary contract.
3. Keep the security UI surfaces aligned with the notes-driven runtime
   authorization model, while pruning any admin surfaces the audit explicitly
   marked as non-core.
4. Add tests in the dedicated `Dabbler.Components` contract-test home that
   prove the retained shell/dashboard core behavior still works, while keeping
   security validation in the existing security-focused test suites.

**Creates:** Simplified shell/dashboard/security contract and tests
**Touches:** `src/Libs/Dabbler.Components/`,
`src/Libs/Dabbler.Security.Blazor/`, and relevant tests/docs
**Ends with:** The reusable shell and admin surfaces match the notes-driven
platform story more closely
**Progress keys:** `session-4/shell-simplification`,
`session-4/dashboard-simplification`, `session-4/catalog-decision`,
`session-4/security-surface`, `session-4/shell-security-tests`

---

### Session 5 of 5: Rebaseline, Compatibility Notes, And Closeout

**Steps:**
1. Capture compatibility notes for app-alignment work, including any deprecated
   APIs, replacements, and intentionally isolated advanced surfaces.
2. Revisit the later session-set queue and update assumptions based on the
   simplification results.
3. Append one catch-up session only if the simplification work exposed a direct
   omission that belongs inside this set.
4. Generate `change-log.md` for this session set.

**Creates:** Compatibility notes and `change-log.md`
**Touches:** Planning docs, session-set specs, and closeout artifacts
**Ends with:** The repo is ready for app-alignment work without ambiguity about
what the supported library surface now is
**Progress keys:** `session-5/compatibility-notes`,
`session-5/queue-rebaseline`, `session-5/change-log`
