# Notes-Driven Library Contract Audit

> **Purpose:** Turn `docs/planning/user-functionality-notes.md` into the
> controlling contract for reusable platform code under `src/Libs/`, while
> grounding decisions in current `src/Apps/` usage and the actual public
> library surface.
> **Created:** 2026-04-14
> **Status:** Session 3 decision ledger completed

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

## Session 2 Outcome

Session 2 mapped the Session 1 surface inventory to real repo consumption:

- confirmed which reusable surfaces are used directly by
  `src/Apps/AcmeCoffeeOnline.Web`
- distinguished direct app usage from indirect usage that only appears through
  composed surfaces
- confirmed that `template-seed/` still proves workflow assets only, not the
  Dabbler UI/report/security surface
- recorded which library features are already earning their keep versus which
  still look speculative

This session still does not trim code. It sharpens the evidence for the
decision ledger that Session 3 will produce.

---

## Session 3 Outcome

Session 3 turned the audit baseline into a durable decision ledger:

- converted the Session 1 classification and Session 2 consumption evidence
  into feature-level keep/collapse/isolate/retire recommendations
- separated proven core capabilities from broader historical public surfaces
- recorded the remaining policy-level decisions that still need a human owner
- rebaselined later session-set specs so downstream work starts from the
  decision ledger instead of reopening the same classification debate

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

## App Consumption Map

| Surface | App usage verdict | Evidence strength | Pay-for-itself signal | Evidence notes |
|---|---|---|---|---|
| `DabblerShell` | `app` | `direct` | `yes` | `MainLayout.razor` uses it as the primary application frame and passes metadata, navigation, report, catalog, usage-tracking, and dashboard configuration into the shell |
| `DabblerSecurityNav` | `app` | `direct` | `yes` | `MainLayout.razor` renders it directly as part of the authenticated shell |
| `DabblerRoleSwitcher` | `app` | `direct` | `yes` | `MainLayout.razor` renders it directly in the app bar |
| `DabblerRecordPopup<T>` | `app` | `direct` | `yes` | `MainLayout.razor` uses `DabblerRecordPopup<ProductDto>` for leaf-record viewing from shell flows |
| `DabblerQueryPage<T>` | `app` | `direct` | `yes` | `CustomerList.razor`, `ProductList.razor`, and `OrderList.razor` all use it as the standard search/list surface |
| `DabblerFilterGrid<T>` | `app` | `indirect-through-composition` | `yes` | The app relies on its QBE/filter-status behavior through every `DabblerQueryPage<T>` list page, but does not reference it directly |
| `DabblerDataGrid<T>` | `app` | `indirect-through-composition` | `yes` | The app relies on result-grid behavior through `DabblerQueryPage<T>`, while direct grid code in `src/Apps/` uses MudBlazor grids for page-specific child collections |
| `DabblerDetailForm<T>` | `app` | `direct` | `yes` | `CustomerDetail.razor`, `ProductDetail.razor`, and `OrderDetail.razor` all use it as the standard edit/detail surface |
| `DabblerComboBox<T>` | `app` | `direct` | `yes` | `ProductComboBox.razor` uses it for searchable multi-column product selection in order-entry flows |
| `DabblerMultiComboBox<T>` | `neither` | `none` | `speculative` | No direct app usage was found in the current rebuild even though multiselect remains part of the notes contract |
| `DabblerDashboardPage` | `app` | `direct` | `partial` | `Dashboard.razor` uses it directly, but the configured widget set still leans on favorites, recent-items, and embedded-search behaviors that the notes do not strongly require |
| `DabblerCatalogPage` | `neither` | `none` | `speculative` | No direct app usage was found; the app's `/products/catalog` experience is a custom MudBlazor page rather than `DabblerCatalogPage` |
| `DabblerReportPage` | `app` | `direct` | `yes` | `OrdersByCustomer.razor` and `SalesByProduct.razor` both use it as the report run/view surface |
| `ParameterPromptForm` / `ReportViewer` | `app` | `indirect-through-composition` | `yes` | These report primitives are not referenced directly by the app, but their value is proven through the app's `DabblerReportPage` usage |
| `DabblerQueryReportPage<T>` | `neither` | `none` | `speculative` | No direct app usage was found for the combined query/report surface |
| Security UI/runtime surfaces | `app` | `direct` | `yes` | `DabblerSecurityDashboard`, `DabblerSecured`, `DabblerPageAuthorize`, `AddDabblerSecurity`, and `AddDabblerSecurityBlazor` are all integrated into real app auth and permission flows |
| Metadata/navigation/catalog/usage provider contracts | `app` | `direct` | `yes` | `Program.cs`, `AcmeCoffeeMetadataProvider`, `AcmeCoffeeNavigationDataProvider`, and dashboard providers show the app actively paying for the shell/runtime integration points |

---

## Template And Workflow Asset Consumption

`template-seed/` currently proves workflow reuse, not reusable UI-library
reuse:

- `template-seed/README.md` positions the seed as a minimal starter for the
  AI-led workflow, centered on `ai-router/`, instruction files, workflow docs,
  and a starter session-set skeleton
- `template-seed/AGENTS.md`, `CLAUDE.md`, and `GEMINI.md` assume the session
  orchestration workflow, not `Dabbler.Components`, `Dabbler.Security`, or
  `Dabbler.Reports`
- no project references or direct Dabbler library usage were found under
  `template-seed/`

Current template verdict:

- workflow assets: `proven`
- Dabbler UI/report/security surfaces: `not yet assumed by the template`

That means Session 2 evidence comes almost entirely from `src/Apps/`, and
template alignment work should remain downstream from library simplification and
app-alignment decisions.

---

## Proven Vs Speculative Summary

### Proven By Current App Usage

- `DabblerShell` and the shell-adjacent navigation/runtime contracts
- `DabblerQueryPage<T>` and the composed filter/grid search pattern
- `DabblerDetailForm<T>`
- `DabblerComboBox<T>`
- `DabblerRecordPopup<T>`
- `DabblerReportPage`
- security UI/runtime surfaces

### Proven Indirectly Through Composition

- `DabblerFilterGrid<T>` and `DabblerDataGrid<T>` are clearly paying for
  themselves through `DabblerQueryPage<T>`, but the app does not yet prove that
  their broader public options need to remain first-class supported features
- `ParameterPromptForm` and `ReportViewer` are justified as report
  infrastructure through `DabblerReportPage`, even though the app does not bind
  them directly

### Partially Proven And Still Challenged

- `DabblerDashboardPage` is used directly, so the dashboard concept is proven,
  but favorites, recent-items, and embedded-search widgets still exceed the
  strongest notes-backed requirement set

### Mostly Speculative Or Unproven

- `DabblerCatalogPage`
- `DabblerQueryReportPage<T>`
- `DabblerMultiComboBox<T>` as a distinct public surface in the current app
- broad artifact-catalog defaults rather than the narrower shell/navigation
  model the app actually uses

The clearest negative evidence is that the app built a custom
`/products/catalog` page instead of adopting `DabblerCatalogPage`, and the
template seed does not currently depend on any of these broader surfaces.

---

## Session 3 Decision Ledger

| Feature Area | App usage | Notes support | Maintenance/testing cost | Recommendation | Decision notes |
|---|---|---|---|---|---|
| `DabblerQueryPage<T>` core query composition | `direct` | `strong` | `medium` | `keep as core` | Keep the query-page composition pattern as a first-class supported surface because the app proves it directly and the notes depend on it |
| `DabblerDataGrid<T>` core CRUD/search behavior | `indirect-through-composition` | `partial` | `medium` | `keep as core` | Keep the grid behaviors needed by list/query workflows, but do not treat the broader grid extras as part of the supported default contract |
| Grid extras: grouping, aggregates, clipboard, CSV export, column pinning, detail expansion | `none` | `none` | `high` | `remove/retire when simplification starts` | No direct app proof or notes support justifies carrying these higher-cost behaviors as first-class reusable defaults |
| `DabblerFilterGrid<T>` QBE/filter-summary behavior | `indirect-through-composition` | `strong` | `medium` | `keep as core` | Keep the structured QBE filter grid and English summary as core because they directly match the notes-driven contract |
| Filter extras: quick filter, presets, expression line, column toggles | `none` | `none` | `medium` | `remove/retire when simplification starts` | These broaden the filter surface beyond the notes-backed QBE workflow without current app proof |
| `DabblerComboBox<T>` search/multi-column/cascading behavior | `direct` | `strong` | `medium` | `keep as core` | The app and notes both justify searchable multi-column selection as a primary reusable surface |
| `DabblerMultiComboBox<T>` multiselect capability | `none` | `strong` | `low` | `keep capability but collapse public surface` | Multiselect stays in scope, but current evidence does not justify preserving a distinct public component if the same capability can live behind the main combo surface |
| Combo add-new and grouping behavior | `none` | `none` | `medium` | `remove/retire when simplification starts` | These features complicate the combo contract and do not support the current notes-driven app workflows |
| `DabblerDetailForm<T>` and `DabblerRecordPopup<T>` | `direct` | `strong` | `medium` | `keep as core` | Detail and popup editing remain core because the app uses them directly and the notes rely on them |
| `DabblerReportPage`, `ParameterPromptForm`, and `ReportViewer` | `direct` / `indirect-through-composition` | `strong` | `medium` | `keep as core` | Report prompting and viewing remain part of the proven notes-driven surface |
| `DabblerQueryReportPage<T>` | `none` | `none` | `medium` | `remove/retire when simplification starts` | The combined query/report page is neither notes-backed nor app-proven, so later work should retire it instead of treating it as a likely supported surface |
| `DabblerDashboardPage` core landing-page behavior | `direct` | `strong` | `medium` | `keep as core` | Keep the dashboard shell/page concept, but split the core landing-page surface from optional widget families |
| Dashboard favorites and recent-items widgets | `direct` | `weak` | `medium` | `isolate as advanced/non-core` | The app currently uses these widgets, but the notes do not justify them as default supported behavior for the reusable platform |
| `DabblerShell` plus navigation/runtime provider contracts | `direct` | `strong` | `medium` | `keep as core` | The shell, navigation, pinned-item, and workspace integration points are directly used and belong in the supported platform story |
| `DabblerCatalogPage` and broad catalog browsing abstractions | `none` | `weak` | `medium` | `remove/retire when simplification starts` | The app already proved a custom catalog experience instead, so the reusable generic catalog browser should not remain part of the primary contract |
| Security UI/runtime authorization surfaces | `direct` | `strong` | `medium` | `keep as core` | Runtime authorization, page/control security, role switching, and row filtering remain foundational and strongly notes-backed |
| External-login and MFA breadth inside the security story | `partial` | `policy-dependent` | `medium` | `isolate as advanced/non-core` | Keep the core runtime authorization surface, but treat broader identity-policy breadth as an explicit product/security decision rather than default reusable UI scope |

### Decision Rules For Later Sessions

- keep capabilities that are directly proven by the app or strongly required by
  the notes
- collapse separate public surfaces when the capability matters but the extra
  abstraction is not currently earning its keep
- isolate advanced widget families only when there is some current product
  value, but the notes do not justify carrying them as defaults
- retire speculative surfaces early when neither the app nor the notes support
  keeping them in the primary reusable contract

---

## Human Decision Queue

These items still need explicit human ownership because the remaining choice is
policy-level or intentionally product-shaped rather than simple engineering
cleanup:

- **Inline editing versus popup-only editing:** the notes describe inline
  editing as helpful but not mandatory, while popup editing is already proven
  by the app. Decide whether inline editing stays a supported core capability
  worth its extra complexity and testing cost, or whether popup editing becomes
  the standard default editing model.
- **Default dashboard experience:** the dashboard page remains core, but the
  audit does not justify favorites and recent-items widgets as default platform
  behavior. Decide whether a new app should start with a minimal/empty
  dashboard or with a narrowly defined starter widget set.
- **External user authentication breadth:** runtime authorization remains core,
  but the long-term reusable scope for external login and MFA still depends on
  product/security policy choices rather than current app proof alone.

---

## Session 3 Rebaseline For Later Sets

### `notes-driven-core-surface-simplification`

- treat `DabblerQueryReportPage<T>` and `DabblerCatalogPage` as retirement
  candidates, not just generic "maybe advanced" surfaces
- preserve multiselect capability while allowing `DabblerMultiComboBox<T>` to
  disappear as a distinct public surface if the main combo contract can absorb
  it cleanly
- split dashboard core from favorites/recent-items widgets instead of carrying
  the whole dashboard surface as one indivisible contract

### `notes-driven-app-alignment`

- assume the app's custom catalog experience is the proven path unless later
  simplification work produces a new focused replacement
- remove app-side dependence on dashboard favorites/recent-items as defaults
  unless a later human decision explicitly keeps them
- migrate the app away from any retired combo/grid/filter/report extras instead
  of treating those behaviors as compatibility requirements

### `notes-driven-template-encoding`

- do not scaffold retired or isolated surfaces by default, including generic
  catalog browsing, query/report composition, grid/filter extras, combo
  add-new/grouping, or dashboard favorites/recent-items widgets
- encode only the retained core navigation, query/detail/report, and runtime
  authorization story once the simplification and app-alignment sets finish

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

## Session 2 Recommendations For Later Sets

- Session 3 should treat direct app usage as the main signal for what remains
  core, then force explicit justification for everything only proven
  indirectly or not proven at all
- Session 3 should split dashboard core from optional widget families instead
  of carrying the whole dashboard surface as one decision
- later simplification sessions should challenge CSV export, favorites,
  recent-items, embedded search, catalog browsing, and query/report composition
  before trimming the proven list/detail/report/security core
- template work should wait until the library contract is smaller and backed by
  proven app usage rather than today's broader surface area

## Session 3 Recommendations For Later Sets

- simplification work should default to retiring unproven public surfaces
  before spending effort on compatibility shims for them
- app-alignment work should prove the narrowed contract with real pages and
  tests rather than preserving broad historical library affordances
- template work should encode the kept core only after the app proves which
  narrowed defaults are actually worth inheriting
