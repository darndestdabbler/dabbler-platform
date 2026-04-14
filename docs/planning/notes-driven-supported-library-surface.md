# Notes-Driven Supported Library Surface

> **Purpose:** Define the supported reusable contract under `src/Libs/` for
> the notes-driven simplification work.
> **Created:** 2026-04-14
> **Source Inputs:** `docs/planning/notes-driven-library-contract-audit.md`
> and `docs/session-sets/notes-driven-core-surface-simplification/spec.md`

---

## Operating Rule

Treat this document as the explicit supported-surface definition for the major
non-security library areas while the simplification sessions are underway.

Default stance:

- keep the retained core behavior obvious and documented
- move advanced or speculative behavior out of the primary contract story
- retire or isolate surfaces that the audit already marked as non-core unless
  new proof appears in later session work

## Supported Surface Definitions

### Grid, Filter, And Query

**Supported core:**

- `DabblerQueryPage<T>` as the standard query/list composition surface
- `DabblerDataGrid<T>` core behavior for search results, sorting, paging, row
  selection, and CRUD-style list workflows
- `DabblerFilterGrid<T>` core QBE behavior with AND-within-row, OR-across-row,
  operator selection, and English summary output

**Not part of the primary supported default:**

- grid grouping, aggregates, clipboard helpers, CSV export, column pinning,
  and detail expansion
- filter quick filter, presets, expression line output, and column-visibility
  toggles
- query-page affordances that mainly expose the non-core extras above

### Combo, Detail, And Popup

**Supported core:**

- `DabblerComboBox<T>` for searchable, multi-column, cascading selection
  workflows
- retained multiselect capability, even if it eventually stops living behind a
  distinct `DabblerMultiComboBox<T>` public surface
- `DabblerDetailForm<T>` for validated single-record editing
- `DabblerRecordPopup<T>` for modal record display/edit flows

**Not part of the primary supported default:**

- combo add-new flows
- combo grouping headers
- preserving `DabblerMultiComboBox<T>` as a required long-term public API when
  the main combo contract can absorb multiselect cleanly

### Shell, Dashboard, And Catalog

**Supported core:**

- `DabblerShell` as the reusable shell/frame for top menu, left navigation,
  pinned-item entry points, workspace composition, and runtime provider
  integration
- navigation/runtime provider contracts that the app uses to populate the shell
- `DabblerDashboardPage` core landing-page behavior

**Advanced or retirement pressure:**

- dashboard favorites and recent-items widgets are non-core by default
- broad artifact/catalog browsing is not part of the supported primary story
- `DabblerCatalogPage` is a retirement candidate unless later proof appears

### Reports

**Supported core:**

- `DabblerReportPage` for report prompting, execution, and viewing
- `ParameterPromptForm` for report parameter capture
- `ReportViewer` for displaying composed report output

**Not part of the primary supported default:**

- `DabblerQueryReportPage<T>` is a retirement candidate and should not be
  advertised as a first-class reusable default

### Security UI And Runtime

**Supported core:**

- runtime authorization surfaces and role-aware UI behavior
- security navigation, secured rendering, role switching, and security
  dashboard/admin surfaces already protected by the dedicated security test
  suites

**Out of the main simplification target:**

- broader MFA and external-login breadth remains product/policy-shaped and
  should not be treated as the main reusable-surface contract question here

## Contract-Test Home Baseline

The audit coverage gate is binding: retained non-security reusable surfaces
must have dedicated test homes under `tests/` before adjacent functionality is
retired or de-emphasized.

### `tests/Dabbler.Components.Tests`

Owns the retained `Dabbler.Components` contract:

- `DabblerQueryPage<T>`
- `DabblerDataGrid<T>` core behavior
- `DabblerFilterGrid<T>` core behavior
- `DabblerComboBox<T>` and retained multiselect behavior
- `DabblerDetailForm<T>`
- `DabblerRecordPopup<T>`
- `DabblerShell`
- `DabblerDashboardPage` core behavior

### `tests/Dabbler.Reports.Blazor.Tests`

Owns the retained report UI contract:

- `DabblerReportPage`
- `ParameterPromptForm`
- `ReportViewer`

### Existing Security Test Homes

The retained security surface stays in the existing dedicated suites:

- `tests/Dabbler.Security.Tests/`
- `tests/Dabbler.Security.Blazor.Tests/`

## Documentation Rules

Library-facing docs should now:

- lead with the retained query/detail/report/shell/security story
- describe advanced widget families and broader historical affordances as
  non-core or retirement candidates
- avoid presenting `DabblerCatalogPage` and `DabblerQueryReportPage<T>` as
  default first-choice surfaces

## Session 1 Baseline

Session 1 of `notes-driven-core-surface-simplification` establishes this
document, creates the dedicated contract-test homes, and narrows the primary
library READMEs so later sessions can simplify against an explicit, test-owned
contract instead of a broader historical surface.
