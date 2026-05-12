# MudBlazor-Native Platform Rebuild Spec

> **Purpose:** Rebuild the platform UI layer from scratch around MudBlazor
> components and conventions, replacing custom wrapper abstractions that have
> accumulated maintenance burden, operability friction, and framework
> misalignment.
> **Created:** 2026-04-16
> **Session Set:** `docs/session-sets/mudblazor-native-rebuild/`
> **Prerequisite:** `docs/planning/guiding-principles.md`,
> `docs/planning/lessons-learned.md`, and the Session 1 ledger from
> `platform-ui-foundation-review` have been reviewed. The ledger's
> keep/simplify/rethink findings are the starting evidence, but this set goes
> further: the human has decided the tradeoffs for custom components are too
> great and the platform should rebuild around MudBlazor-native patterns.
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification

---

## Project Overview

The `platform-ui-foundation-review` Session 1 inventory confirmed that the
current custom component layer is fighting the framework:

- **DabblerDataGrid** heavily overlaps MudDataGrid with a large custom state
  machine, compatibility shims, and thin direct test coverage.
- **DabblerShell**, **DabblerNavigationPanel**, and
  **DabblerTabbedWorkspace** enforce a non-route workspace model that clashes
  with Blazor routing, browser history, and user expectations.
- **DabblerFilterGrid** contains valuable QBE filter logic but wraps it in too
  much custom UI scaffolding.
- **DabblerDetailForm**, **DabblerComboBox**, and related surfaces reimplement
  patterns MudBlazor already provides well.
- Human UAT exposed numerous broken controls, styling defects, and interaction
  gaps that stem directly from the abstraction layer's complexity.

The lessons-learned file captures the durable version of this finding:
*"Repeated UI Friction Can Mean The Abstraction Layer Is Fighting The
Framework."*

**The human decision:** rebuild the UI platform from scratch, aligning as
closely as possible to MudBlazor components and conventions. Custom
abstractions must earn their existence by clearly adding value that MudBlazor
does not provide natively.

### What This Set Rebuilds

| Layer | Action |
| --- | --- |
| **Dabbler.Components** | Strip custom wrappers. Keep non-UI utilities (expression engine, filter model logic, CSV export). Rebuild UI patterns using MudBlazor components directly. |
| **AcmeCoffeeOnline.Web layout** | Replace DabblerShell/workspace/tab model with MudBlazor layout system and standard Blazor routing. |
| **AcmeCoffeeOnline.Web pages** | Rebuild all entity pages, dashboard, and admin pages using MudBlazor-native patterns and explicit page composition. |
| **Dabbler.Security.Blazor** | Align security admin UI components to the new MudBlazor-native patterns. |
| **Dabbler.Reports.Blazor** | Align report viewer and parameter forms to MudBlazor-native patterns. |
| **UI tests** | Rebuild bUnit tests, update E2E tests for new navigation model. |

### What This Set Preserves

| Layer | Why |
| --- | --- |
| **AcmeCoffeeOnline.Api** | The REST API is not a UI concern. No changes needed. |
| **AcmeCoffeeOnline.Shared** | DTOs, models, and enums are UI-independent. |
| **Dabbler.Security** (core) | Authentication, authorization, and identity logic is not UI. |
| **Dabbler.Reports.Core** | The report definition, data engine, and layout engine are not UI. |
| **Dabbler.Reports.Export** | Export implementations are not UI. |
| **Dabbler.Platform.Stubs** | The DI-stub concept is preserved; implementations may need interface updates. |
| **Filter model logic** | QBE AND/OR row logic, operators, presets, and the expression engine are genuine platform value. |
| **Data provider interfaces** | Keep `IGridDataProvider<T>` and related abstractions where they genuinely simplify data plumbing; remove or simplify where they add indirection without value. |
| **Docker and CI configuration** | Infrastructure is unaffected. |

## Core Design Decisions

These decisions should be confirmed or refined in Session 1 and apply to all
subsequent sessions:

### 1. Route-Based Navigation

Replace the DabblerShell/DabblerTabbedWorkspace event-driven tab model with
standard Blazor routing. Every user-reachable view should have a URL. Browser
back/forward, bookmarks, and deep links should work naturally.

### 2. MudBlazor Layout System

Use `MudLayout`, `MudDrawer`, `MudAppBar`, `MudNavMenu`, and `MudMainContent`
for the application shell. The left navigation panel should be a standard
`MudDrawer` with `MudNavMenu` items that navigate via Blazor routes.

### 3. Explicit Page Composition Over Metadata-Driven Generation

Prefer explicit component composition for app pages rather than generating UI
from entity metadata at runtime. Each page declares its own MudDataGrid
columns, MudForm fields, and layout. This makes pages easier to read, debug,
test, and customize.

The metadata provider pattern may survive in a reduced role for dynamic
scenarios like runtime-configurable admin screens, but the default app pages
should be explicit.

### 4. MudDataGrid Used Directly

Use `MudDataGrid<T>` directly with its `ServerData` callback, built-in column
definitions, sorting, paging, editing modes, and dialog editing. Do not wrap
it in a custom component unless a specific, justified need arises.

### 5. QBE Filter Model Preserved, UI Rebuilt

The AND/OR row-based filter logic, filter operators, presets, and the readable
filter-summary model are genuine platform value. The filter model classes and
expression engine should be preserved. The filter UI should be rebuilt using
MudBlazor input components (MudTextField, MudSelect, MudDatePicker, MudChip,
etc.) rather than custom filter-cell and filter-row wrappers.

### 6. MudForm for Detail Pages

Use `MudForm` directly for detail forms with standard MudBlazor input
components, two-column layout via MudGrid, and built-in validation. Child
entity grids should live in `MudTabs`. Popup edit forms should use
`MudDialog`.

### 7. MudBlazor Theme and Spacing

Use the MudBlazor theme system for colors, typography, and spacing. Define
reusable spacing conventions through theme configuration and shared CSS
classes rather than per-page ad hoc values. This addresses the lessons-learned
finding about centralizing spacing and theme safety early.

### 8. ComboBox Alignment

Evaluate whether `MudAutocomplete<T>` and `MudSelect<T>` satisfy the
platform's combo-box needs (single/multi-select, search, cascading). Build
thin helpers only where MudBlazor's built-in components leave a real gap.

## Parallelization Notes

No default parallelization assumption is required between sessions.

Inside sessions, independent page rebuilds or independent test efforts may
proceed in parallel when the shared patterns from earlier sessions are stable.

## Session Set Configuration

```yaml
requiresUAT: false
requiresE2E: false
```

## Sessions
### Session 1 of 8: Architecture Decisions and Layout Foundation

**Steps:**
1. Read `docs/planning/guiding-principles.md` and
   `docs/planning/lessons-learned.md` per pre-session requirements.
2. Confirm or refine the core design decisions listed above. Record any
   changes in the activity log.
3. Remove the DabblerShell, DabblerNavigationPanel, and
   DabblerTabbedWorkspace components from Dabbler.Components.
4. Create the new application layout using MudBlazor's layout system:
   `MudLayout` with `MudAppBar`, `MudDrawer` (left nav with `MudNavMenu`),
   and `MudMainContent` for routed page content.
5. Set up the MudBlazor theme configuration with the platform's color palette,
   typography scale, and spacing conventions.
6. Configure Blazor routing so that every entity and admin area has a clean
   URL structure (e.g., `/products`, `/products/{id}`, `/customers`,
   `/orders/{id}`, `/admin/users`, `/admin/security`).
7. Create a minimal placeholder page that proves the new layout renders, the
   drawer navigates via routes, the app bar shows user context, and
   browser back/forward works.
8. Build and test the solution. Fix any compilation errors from removed
   components.

**Creates:** New MudBlazor-native application layout, theme configuration,
route structure, and placeholder proof page
**Touches:** `MainLayout.razor`, Dabbler.Components shell folder, app routing,
theme configuration
**Ends with:** The application shell works with route-based navigation and
MudBlazor layout, even though most pages are still placeholders
**Progress keys:** `session-1/architecture-decisions`,
`session-1/shell-removal`, `session-1/layout-foundation`,
`session-1/theme-setup`, `session-1/route-structure`,
`session-1/build-and-test`

---

### Session 2 of 8: Grid, Filter, and Query Page Patterns

**Steps:**
1. Read `docs/planning/guiding-principles.md` and
   `docs/planning/lessons-learned.md`.
2. Remove DabblerDataGrid wrapper. Establish the convention for using
   MudDataGrid directly with `ServerData` callbacks, column definitions,
   sorting, and paging.
3. Preserve the QBE filter model classes (filter descriptors, operators,
   presets, expression engine). Remove the custom filter UI components
   (DabblerFilterGrid, DabblerFilterCell, DabblerFilterRow, etc.).
4. Rebuild the filter UI using MudBlazor input components. The filter panel
   should support the same AND/OR row logic with a readable filter summary
   and preset selection, but built from MudTextField, MudSelect,
   MudDatePicker, MudChipSet, MudButton, etc.
5. Define a lightweight query-page composition pattern: a reusable Razor
   component or convention that wires a filter panel, a MudDataGrid, and a
   page toolbar together. This should be a thin composition layer, not a
   heavyweight wrapper.
6. Rebuild the Products query page (`/products`) as the proof of this pattern:
   filter panel, MudDataGrid with server data, column definitions, add
   button, row click navigation, CSV export.
7. Build and test.

**Creates:** MudDataGrid usage conventions, rebuilt filter UI, query-page
composition pattern, rebuilt Products query page
**Touches:** Dabbler.Components DataGrid and FilterGrid folders, Products page
files, data provider interfaces if needed
**Ends with:** A working Products query page with filter, grid, and toolbar
using only MudBlazor components and the preserved filter model
**Progress keys:** `session-2/grid-removal`, `session-2/filter-model-preserve`,
`session-2/filter-ui-rebuild`, `session-2/query-page-pattern`,
`session-2/products-query-proof`, `session-2/build-and-test`

---

### Session 3 of 8: Form Patterns and Detail Pages

**Steps:**
1. Read `docs/planning/guiding-principles.md` and
   `docs/planning/lessons-learned.md`.
2. Remove DabblerDetailForm and DabblerRecordPopup wrappers.
3. Establish the MudForm-based detail page convention: two-column layout via
   MudGrid, MudBlazor input components with validation, Save/Cancel actions,
   and clear tab order.
4. Establish the child-entity-grid convention: MudTabs containing MudDataGrid
   instances for one-to-many and many-to-many relationships.
5. Establish the popup-edit convention using MudDialog with MudForm content.
6. Evaluate MudAutocomplete and MudSelect against the platform's combo-box
   needs. Build thin helpers only for genuine gaps (e.g., multi-column
   display, cascading coordination). Remove DabblerComboBox and
   DabblerMultiComboBox if MudBlazor covers the need.
7. Rebuild the Products detail page (`/products/{id}`) as form-pattern proof.
8. Rebuild the Orders detail page (`/orders/{id}`) as the complex proof:
   MudForm header fields, MudTabs with OrderItems and OrderNotes child grids,
   and OrderNote popup editing via MudDialog.
9. Build and test.

**Creates:** MudForm detail page conventions, child-grid conventions, popup
conventions, combo-box approach, rebuilt Products and Orders detail pages
**Touches:** Dabbler.Components form and combo-box folders, Products and
Orders detail page files
**Ends with:** Products and Orders detail pages work end-to-end with
MudBlazor-native forms, child grids, and popups
**Progress keys:** `session-3/form-wrapper-removal`,
`session-3/detail-page-pattern`, `session-3/child-grid-pattern`,
`session-3/popup-pattern`, `session-3/combobox-evaluation`,
`session-3/products-detail-proof`, `session-3/orders-detail-proof`,
`session-3/build-and-test`

---

### Session 4 of 8: Complete App Page Rebuild

**Steps:**
1. Read `docs/planning/guiding-principles.md` and
   `docs/planning/lessons-learned.md`.
2. Rebuild the Customers query page (`/customers`) and Customers detail page
   (`/customers/{id}`) using the established patterns.
3. Rebuild the Orders query page (`/orders`) using the query-page pattern.
4. Rebuild the Dashboard page (`/`) using MudBlazor cards, MudGrid layout,
   and MudBlazor components for navigation cards, quick links, recent items,
   and embedded search. Remove DabblerDashboardPage, DabblerNavigationCard,
   DabblerQuickLinksGroup, DabblerRecentItems, DabblerFavorites, and
   DabblerEmbeddedSearch wrappers.
5. Rebuild the page toolbar pattern using MudBlazor components (record count,
   action buttons, CSV export). Remove DabblerPageToolbar.
6. Rebuild the error banner pattern using MudAlert or MudBanner. Remove
   DabblerPageErrorBanner.
7. Rebuild organization switching and user context display in the MudAppBar.
8. Rebuild admin pages (`/admin/users`, `/admin/products`) using the
   established patterns.
9. Build and test.

**Creates:** All remaining app pages rebuilt on MudBlazor-native patterns
**Touches:** Customer pages, Order query page, Dashboard, admin pages, toolbar
and error components
**Ends with:** Every app page renders and navigates correctly using
MudBlazor-native components and Blazor routing
**Progress keys:** `session-4/customers-pages`, `session-4/orders-query`,
`session-4/dashboard-rebuild`, `session-4/toolbar-and-banner`,
`session-4/org-switching`, `session-4/admin-pages`,
`session-4/build-and-test`

---

### Session 5 of 8: Security and Reports UI Alignment

**Steps:**
1. Read `docs/planning/guiding-principles.md` and
   `docs/planning/lessons-learned.md`.
2. Rebuild Dabbler.Security.Blazor admin components to use MudBlazor directly:
   DabblerSecurityDashboard, DabblerRoleManager, DabblerUserRoleManager,
   permission matrix editors, row filter editors, and control permission
   editors.
3. Rebuild security pages (role list, role assignment, user list, user edit,
   audit log) using MudBlazor-native patterns.
4. Align DabblerSecured, DabblerSecurityNav, and DabblerRoleSwitcher to the
   new layout and navigation conventions.
5. Rebuild Dabbler.Reports.Blazor components: ReportViewer using MudBlazor
   layout and typography, ParameterPromptForm using MudForm, and
   DabblerReportPage and DabblerQueryReportPage using the new page patterns.
6. Rebuild report pages (`/reports/orders-by-customer`,
   `/reports/sales-by-product`) using MudBlazor-native patterns.
7. Build and test.

**Creates:** Rebuilt security admin UI and report viewer UI on MudBlazor
**Touches:** Dabbler.Security.Blazor components and pages,
Dabbler.Reports.Blazor components, report app pages
**Ends with:** Security administration and report viewing work end-to-end
using MudBlazor-native components
**Progress keys:** `session-5/security-dashboard-rebuild`,
`session-5/security-pages`, `session-5/security-nav-alignment`,
`session-5/reports-viewer-rebuild`, `session-5/report-pages`,
`session-5/build-and-test`

---

### Session 6 of 8: Test Suite Rebuild

**Steps:**
1. Read `docs/planning/guiding-principles.md` and
   `docs/planning/lessons-learned.md`.
2. Audit existing tests against the rebuilt component and page structure.
   Remove tests for deleted wrappers. Identify coverage gaps.
3. Write bUnit tests for the new MudBlazor-native patterns: query-page
   composition, filter panel interactions (add/remove rows, apply/clear
   filters, preset selection), detail form validation and save flow, child
   grid tab switching, popup form lifecycle, dashboard widget rendering.
4. Write bUnit accessibility assertions for key surfaces: visible focus
   indicators, ARIA attributes, label associations, contrast-safe selected
   states.
5. Update Dabbler.Components.Tests and app-level web tests for the new
   component interfaces and rendering expectations.
6. Update Dabbler.Security.Blazor.Tests and Dabbler.Reports.Blazor.Tests
   for the rebuilt UI components.
7. Build and test. Ensure all tests pass.

**Creates:** Rebuilt bUnit test suite with interaction, accessibility, and
rendering coverage for MudBlazor-native components
**Touches:** All test projects
**Ends with:** The automated test suite validates the rebuilt UI layer with
meaningful interaction and accessibility coverage
**Progress keys:** `session-6/test-audit`, `session-6/bunit-query-page`,
`session-6/bunit-forms`, `session-6/bunit-accessibility`,
`session-6/security-and-reports-tests`, `session-6/build-and-test`

---

### Session 7 of 8: E2E Test Updates and Integration Verification

**Steps:**
1. Read `docs/planning/guiding-principles.md` and
   `docs/planning/lessons-learned.md`.
2. Update Playwright tests for the new route-based navigation model. Tests
   that previously relied on shell tab behavior should now use standard route
   navigation and URL assertions.
3. Verify that existing Playwright workflows (customer CRUD, product admin,
   multi-organization switching, smoke tests) pass against the rebuilt app.
4. Fix any integration issues discovered by E2E execution: data flow gaps,
   navigation regressions, missing API calls, broken form submissions.
5. Add focused browser assertions for styling, spacing, and accessibility
   checks on representative pages.
6. Run the full E2E suite in Docker and confirm all tests pass.
7. Build and test.

**Creates:** Updated Playwright test suite passing against the rebuilt app
**Touches:** Playwright test files, Docker compose if needed, app pages for
integration fixes
**Ends with:** The full automated suite (unit, bUnit, integration, E2E)
passes against the rebuilt platform
**Progress keys:** `session-7/playwright-navigation-update`,
`session-7/workflow-verification`, `session-7/integration-fixes`,
`session-7/browser-accessibility-checks`, `session-7/full-suite-pass`,
`session-7/build-and-test`

---

### Session 8 of 8: Cleanup, Documentation, and Closeout

**Steps:**
1. Read `docs/planning/guiding-principles.md` and
   `docs/planning/lessons-learned.md`.
2. Remove any dead code, unused files, or orphaned components from
   Dabbler.Components, Dabbler.Security.Blazor, and Dabbler.Reports.Blazor
   that survived the rebuild without purpose.
3. Update `README.md` to reflect the rebuilt architecture and MudBlazor-native
   approach.
4. Update `docs/planning/guiding-principles.md` with any new durable
   principles that emerged from the rebuild (e.g., explicit composition rule,
   MudBlazor-first rule, filter-model preservation rule).
5. Update `docs/planning/lessons-learned.md` with any new lessons from the
   rebuild process.
6. Update `docs/planning/phase-2-platform-roadmap.md` to reflect that the
   platform rebuild is complete and downstream session sets should build on
   the MudBlazor-native foundation.
7. Update `docs/planning/platform-ui-foundation-review-ledger.md` with a
   resolution note indicating the rebuild superseded the remaining review
   sessions.
8. Build and test one final time.
9. Generate `change-log.md` for this session set.

**Creates:** Clean codebase, updated documentation, and `change-log.md`
**Touches:** Dabbler.Components cleanup, README, planning docs, guiding
principles, lessons learned, phase-2 roadmap, review ledger
**Ends with:** The platform UI layer is rebuilt on MudBlazor-native patterns,
fully tested, documented, and ready for downstream work
**Progress keys:** `session-8/dead-code-cleanup`,
`session-8/readme-update`, `session-8/guiding-principles-update`,
`session-8/lessons-learned-update`, `session-8/roadmap-update`,
`session-8/review-ledger-resolution`, `session-8/final-build-and-test`,
`session-8/change-log`

## Downstream Impact

This session set supersedes the remaining sessions of
`platform-ui-foundation-review` (Sessions 2 and 3) and replaces Session Set 1
of the Phase 2 roadmap (Platform Contract, Accessibility, and Stubbed UI
Harness). The rebuilt MudBlazor-native platform becomes the new foundation for
all downstream Phase 2 work.

After this set completes, the following Phase 2 roadmap items should proceed
from the rebuilt baseline:

- Stubbed UAT (early design and navigation review against the new layout)
- AcmeCoffeeDistributors domain reframe
- Multi-organization security
- Application coverage conformance
- E2E workflow design and implementation
- Full-application UAT
- Documentation readiness
- Packaging and template readiness

## Risk Notes

- **Scope risk:** A full UI rebuild across 8 sessions is ambitious. Sessions
  should stay disciplined about using MudBlazor defaults and resisting the
  urge to reintroduce custom wrappers. If a session falls behind, defer
  polish to a later session rather than cutting verification.
- **Filter model risk:** The QBE filter model is the most complex preserved
  logic. Session 2 should validate that the filter model integrates cleanly
  with MudBlazor input components before the pattern spreads to all pages.
- **Data provider risk:** If the data provider interfaces need significant
  changes to work with MudDataGrid's ServerData callback, that refactoring
  should happen in Session 2 rather than being discovered later.
- **Security.Blazor complexity:** The security admin UI has many components
  (permission matrices, role managers, row filters). Session 5 should
  prioritize the most-used admin surfaces and defer rarely-used editors if
  time is tight.
