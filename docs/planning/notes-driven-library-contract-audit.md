# Notes-Driven Library Contract Audit

> **Purpose:** Turn `docs/planning/user-functionality-notes.md` into the
> controlling contract for reusable platform code under `src/Libs/`, while
> grounding decisions in current `src/Apps/` usage and the actual public
> library surface.
> **Created:** 2026-04-14
> **Status:** Session 1 baseline completed

---

## Operating Rule

Use `user-functionality-notes.md` as the strict contract for reusable platform
scope.

Default stance:

- evaluate existing code first
- keep working code when it supports the contract cleanly
- prune or isolate features that exceed the contract unless explicitly justified
- allow focused rebuilds only when trimming in place would leave an awkward,
  unstable, or misleading surface

Coordination rule:

- `src/Apps/` proves real workflows
- `src/Libs/` stabilizes reusable patterns around those workflows
- `template-seed/` only encodes the simplified, proven surface

---

## Session 1 Outcome

Session 1 established the first auditable baseline for later simplification
work:

- inventoried the major public-facing surfaces under `src/Libs/`
- mapped those surfaces to the user-functionality notes contract
- recorded an initial classification for each major reusable surface

This session does not trim code. It defines what later sessions should treat as
core, what should likely be simplified, and what should be challenged as
advanced or speculative.

---

## Evidence Summary

### Current App Consumption

The app already depends on a meaningful subset of the library surface:

- `DabblerShell`, `DabblerSecurityNav`, `DabblerRoleSwitcher`, and
  `DabblerRecordPopup` are composed in
  `src/Apps/AcmeCoffeeOnline.Web/Components/Layout/MainLayout.razor`
- `DabblerQueryPage<T>` drives customer, order, and product list pages
- `DabblerDetailForm<T>` drives customer, order, and product detail pages
- `DabblerComboBox<T>` is used in product selection flows
- `DabblerDashboardPage` drives the dashboard page
- `DabblerReportPage` drives the current report pages
- `DabblerSecurityDashboard` and `DabblerSecured` are already part of the app
  admin and permission-aware flows

No direct app usage was found for `DabblerQueryReportPage<T>` or
`DabblerCatalogPage`.

### Library Surface Drift

The current library README and component APIs are broader than the notes-driven
contract:

- `Dabbler.Components/README.md` still leads with grouping, presets, add-new,
  artifact browsing, favorites, and other advanced surfaces
- `DabblerDataGrid<T>` exposes grouping, aggregates, clipboard operations, CSV
  export, column pinning, detail expansion, and popup-form editing
- `DabblerFilterGrid<T>` exposes quick filter, preset import/export, expression
  line output, and column-visibility toggles
- `DabblerComboBox<T>` and `DabblerMultiComboBox<T>` expose add-new and
  grouping behavior in addition to the notes-aligned search/multi-column/multi-
  select surface
- `DabblerDashboardPage`, `DabblerShell`, and `DabblerCatalogPage` expose a
  broader artifact/catalog/favorites/recent-items model than the notes require
- `DabblerQueryReportPage<T>` composes grid, filter, report, clipboard, CSV,
  and PDF behaviors into one broader surface that is not currently proven by app
  usage

### Template Seed Status

`template-seed/` is still workflow/bootstrap oriented and is not yet the place
where the rich Dabbler component surface is proven. That keeps template work
downstream from this audit and the later simplification/app-alignment work.

---

## Surface Inventory

| Surface Area | Primary Types | Notes-Aligned Core Behaviors | Observed Broader Surface |
|---|---|---|---|
| Grid | `DabblerDataGrid<T>` | add flow, Id link, delete flow, optional inline editing, popup editing where appropriate | grouping, aggregate headers, clipboard cut/copy/paste, CSV export, column pinning, detail expansion |
| Filter | `DabblerFilterGrid<T>` | QBE rows, AND within row, OR across rows, English filter summary | quick filter, preset import/export, expression line, column visibility toggles |
| Query page | `DabblerQueryPage<T>` | compose filter + data grid into search/query page | toolbar CSV and clipboard options, broader page-level knobs |
| Combo | `DabblerComboBox<T>`, `DabblerMultiComboBox<T>` | search, multiple columns, multiselect, cascading coordination | add-new flows, grouping headers, value-not-in-list workflows |
| Detail and popup | `DabblerDetailForm<T>`, `DabblerRecordPopup<T>` | validation, save/cancel, two-column detail layout, modal popup flow | mostly aligned; later review should confirm child-grid/tab breadth and popup surface stay focused |
| Dashboard | `DabblerDashboardPage` | configurable landing page | favorites, recent items, embedded search, generic widget model |
| Shell and navigation | `DabblerShell`, nav components, `DabblerCatalogPage` | top menu, left nav, pinned items, workspace navigation | generic artifact catalog browsing and broader metadata-driven artifact plumbing |
| Reports | `DabblerReportPage`, `ParameterPromptForm`, `ReportViewer` | report prompting, running, viewing, and parameterized execution | PDF export is present but still plausibly supportive rather than contract drift |
| Query/report composition | `DabblerQueryReportPage<T>` | none explicitly required as a first-class reusable surface | combined grid/report page with filter/grid/report/export features |
| Security UI/runtime | `DabblerSecurityNav`, `DabblerSecured`, `DabblerRoleSwitcher`, `DabblerSecurityDashboard`, runtime authorization types | runtime role assignment, page/control/endpoint authorization, row filters, role switching | MFA and external login are broader security features that may matter architecturally, but they are not the main reusable component-scope question for this audit |
| Internal support | `ExpressionEngine`, report core/export infrastructure | supports reports and expressions needed by higher-level surfaces | not a primary user-facing reusable contract |

---

## Notes Mapping

### Strong Contract Matches

The following surfaces line up directly with the user-functionality notes:

- `DabblerFilterGrid<T>` for QBE-style AND/OR filtering with an English summary
- `DabblerQueryPage<T>` for search/query workflows built from filter + result
  grid composition
- `DabblerComboBox<T>` and multiselect support for multi-column, cascading
  selection scenarios
- `DabblerDetailForm<T>` for validated, form-driven record editing
- `DabblerRecordPopup<T>` for modal edit/view flows without child grids
- `DabblerDashboardPage` for configurable landing-page behavior
- `DabblerShell` and related nav surfaces for top menu, left nav, pinned-item,
  and workspace navigation
- `DabblerReportPage`, `ParameterPromptForm`, and `ReportViewer` for report
  prompting and viewing
- security UI/runtime surfaces for runtime authorization and role switching

### Partial Contract Matches

These surfaces have a valid notes-aligned core but currently advertise or carry
more functionality than the contract requires:

- `DabblerDataGrid<T>`
- `DabblerFilterGrid<T>`
- `DabblerQueryPage<T>`
- combo-box add-new and grouping behavior
- shell/catalog/dashboard artifact abstractions

### Weak Or Unproven Matches

These surfaces are not currently required as first-class reusable contract
surfaces by the notes and are not proven by current app usage:

- `DabblerQueryReportPage<T>`
- `DabblerCatalogPage` as a broad artifact browser
- favorites and recent-items widgets as default dashboard behaviors

---

## Initial Classification Matrix

| Surface | Notes Fit | Classification | Current Repo Evidence | Recommendation |
|---|---|---|---|---|
| `DabblerDataGrid<T>` | `partial` | `simplify in place` | Core CRUD grid behavior exists, but the public surface also includes grouping, aggregates, clipboard, CSV export, pinning, detail expansion, and popup-form editing | Keep the core grid behavior, but challenge the advanced features as non-core by default |
| `DabblerFilterGrid<T>` | `strong` | `simplify in place` | It directly matches the QBE contract, but the public API also includes quick filter, preset import/export, expression line, and column toggles | Keep the QBE/filter-summary core and narrow the supported contract around it |
| `DabblerQueryPage<T>` | `strong` | `keep as core` | Directly used by list pages and clearly matches the query-form contract | Keep as a core composition primitive, but later trim any toolbar options that only expose non-core grid extras |
| `DabblerComboBox<T>` | `strong` | `keep as core` | App usage and notes both support search, multiple columns, and cascading scenarios | Keep the notes-aligned combo surface as core |
| `DabblerMultiComboBox<T>` | `strong` | `simplify in place` | Multi-select is required by the notes, but a separate public component may or may not be necessary long term | Keep multiselect capability, then decide later whether it remains a separate public surface |
| `DabblerDetailForm<T>` | `strong` | `keep as core` | Used directly by app detail pages and aligns closely with the notes | Keep as core |
| `DabblerRecordPopup<T>` | `strong` | `keep as core` | Used directly by the app and fits the modal popup contract cleanly | Keep as core |
| `DabblerDashboardPage` | `strong` | `keep as core` | App usage proves the landing-page concept, but some widget types go beyond the notes | Keep the dashboard core, while challenging favorites/recent-items as defaults |
| `DabblerShell` | `strong` | `simplify in place` | Core shell/navigation behavior is used directly by the app, but catalog/artifact abstractions broaden the contract | Keep shell and nav as core, but narrow the supported story around the notes-driven workflow/navigation model |
| `DabblerCatalogPage` | `weak` | `isolate as advanced/non-core` | Present in the shell machinery, but not directly used by the app and not clearly required by the notes | Do not treat artifact catalog browsing as a default supported surface without further justification |
| `DabblerReportPage`, `ParameterPromptForm`, `ReportViewer` | `strong` | `keep as core` | Direct app usage and direct notes support for report run/view flows | Keep as core |
| `DabblerQueryReportPage<T>` | `weak` | `isolate as advanced/non-core` | Broad composed surface with no current app proof and no direct notes requirement | Do not carry this as part of the primary supported contract unless a later workflow proves it |
| Security UI and runtime authorization surfaces | `strong` | `keep as core` | `DabblerSecurityDashboard`, `DabblerSecured`, `DabblerRoleSwitcher`, page/control/endpoint authorization, and row filters align strongly with the notes and app usage | Keep as core |
| `ExpressionEngine` and report core/export support | `strong` | `keep as internal support` | Needed by higher-level report/query behavior, but not itself the reusable UI contract under audit | Retain as support infrastructure, not as a primary public-surface decision driver |

---

## Features To Challenge First

These are the clearest candidates for pruning, isolating, or requiring explicit
human approval before they remain part of the supported contract:

- grid grouping and aggregate headers
- clipboard cut/copy/paste behavior
- CSV export buttons and helpers
- column pinning
- filter quick-search above the structured QBE surface
- filter preset import/export
- filter column-visibility toggles
- combo-box add-new flows
- combo-box grouping headers
- dashboard favorites widgets
- dashboard recent-items widgets
- broad artifact catalog browsing abstractions
- `DabblerQueryReportPage<T>` as a first-class reusable page surface

---

## Session 1 Recommendations For Later Sets

- Session 2 should map actual app and template consumption to this matrix
  rather than re-arguing what the notes say
- Session 3 should turn the challenge list into a feature-level human decision
  queue with keep/isolate/remove recommendations
- later simplification work should prefer narrowing the supported contract in
  place before considering any rebuild
- rebuilds should be reserved for surfaces where trimming would leave a
  confusing or unstable public API
