# Change Log: MudBlazor-Native Platform Rebuild

## Overview

The `mudblazor-native-rebuild` session set rebuilt the platform UI layer from
custom wrapper abstractions to MudBlazor-native patterns across 8 sessions.
The rebuild replaced 20+ custom components with direct MudBlazor component
usage, established route-based navigation, and preserved genuine platform
value (QBE filter model, expression engine, data provider abstractions).

**Completed:** 2026-04-16
**Sessions:** 8 of 8
**Final test count:** 467 (0 failures)

## Key Metrics

- **Components removed:** DabblerDataGrid, DabblerFilterGrid, DabblerFilterCell,
  DabblerFilterRow, DabblerFilterRowPanel, DabblerFilterStatusBar,
  DabblerFilterPresetSelector, DabblerQueryPage, DabblerDetailForm,
  DabblerRecordPopup, DabblerComboBox, DabblerMultiComboBox,
  DabblerDashboardPage, DabblerNavigationCard, DabblerQuickLinksGroup,
  DabblerRecentItems, DabblerFavorites, DabblerEmbeddedSearch,
  DabblerPageToolbar, DabblerPageErrorBanner, DabblerQueryReportPage,
  PageViewMode, plus associated JS/CSS assets
- **Components added:** FilterPanel\<T\> (MudBlazor-native QBE filter UI)
- **Layout rebuilt:** MainLayout.razor with MudLayout, MudAppBar, MudDrawer,
  MudNavMenu, MudMainContent
- **Pages rebuilt:** All entity query pages, detail pages, dashboard, admin
  pages, security dashboard, report pages
- **Tests at start:** 420 | **Tests at end:** 467 | **New tests:** 57
  (17 FilterPanel interaction tests, 5 accessibility assertions,
  34 conformance tests, 1 retained-type assertion)
- **Orphaned code cleaned:** PageViewMode.cs, 11 CSS rules, empty directories

## Session Summaries

### Session 1: Architecture Decisions and Layout Foundation

Confirmed all 8 core design decisions. Removed DabblerShell,
DabblerNavigationPanel, and DabblerTabbedWorkspace from MainLayout. Built new
MudBlazor-native layout with MudLayout, MudAppBar, MudDrawer (left nav with
MudNavMenu), and MudMainContent. Set up MudTheme with platform color palette
and typography. Configured route-based navigation with clean URLs for all
entity and admin areas. Fixed 6 test failures caused by layout changes.
Build: 0 errors, 420 tests passing.

### Session 2: Grid, Filter, and Query Page Patterns

Removed DabblerDataGrid wrapper and all associated support files. Preserved
QBE filter model classes (FilteringCoordinator, FilterGroupCollection,
ExpressionEngine, etc.). Removed custom filter UI components and created new
FilterPanel\<T\> using MudBlazor inputs. Defined query-page composition
pattern with FilterPanel + MudDataGrid + inline toolbar. Rebuilt ProductList
as the proof page. Updated GridDataProviderExtensions.ToServerDataFunc for
MudDataGrid compatibility. Build: 0 errors, 417 tests passing.

### Session 3: Form Patterns and Detail Pages

Removed DabblerDetailForm, DabblerRecordPopup, DabblerComboBox, and
DabblerMultiComboBox. Established MudForm detail page convention with
two-column MudGrid layout and snapshot-based dirty tracking. Established
child-grid convention with MudTabs. Confirmed popup-edit convention using
MudDialog with MudForm. Evaluated MudAutocomplete/MudSelect as combo-box
replacements. Rebuilt ProductDetail and OrderDetail pages as pattern proofs.
Build: 0 errors, 411 tests passing.

### Session 4: Complete App Page Rebuild

Rebuilt CustomerList, OrderList, and Dashboard pages using established
patterns. Replaced DabblerDashboardPage with explicit MudCard navigation
cards, MudList report quick-links, and MudTextField embedded search.
Established inline toolbar and error banner patterns using MudBlazor
components. Confirmed org switching and admin pages already MudBlazor-native.
Removed 12 dashboard widget wrapper files. Build: 0 errors, 409 tests passing.

### Session 5: Security and Reports UI Alignment

Rebuilt DabblerSecurityDashboard to use MudTabs (replacing custom HTML tab
implementation). Rebuilt DabblerRoleManager to use IDialogService with new
RoleFormDialog component. Replaced raw HTML tables with MudSimpleTable across
6 permission matrix components. Rebuilt DabblerReportPage to inherit from
ComponentBase directly with MudBlazor layout conventions. Removed obsolete
DabblerQueryReportPage. Build: 0 errors, 410 tests passing.

### Session 6: Test Suite Rebuild

Audited all 6 test projects against rebuilt component structure. Created 17
FilterPanel bUnit interaction tests and 5 accessibility assertion tests.
Created 34 source-level structural conformance tests verifying MudBlazor-native
patterns across all app pages. Expanded retained-type assertions from 1 to 8
types. Build: 0 errors, 467 tests passing (+57 new tests).

### Session 7: E2E Test Updates and Integration Verification

Updated all Playwright tests to use route-based navigation with drawer nav
helpers. Updated 4 workflow test files (SmokeTests, CustomerWorkflow,
ProductAdministration, MultiOrganization). Added test IDs to app pages for
E2E selectors. Added browser accessibility assertions (lang attr, headings,
ARIA labels, layout spacing, nav drawer presence). Full solution build and all
467 non-E2E tests passing.

### Session 8: Cleanup, Documentation, and Closeout

Removed dead PageViewMode.cs enum, empty ComboBox and artifacts directories.
Cleaned 11 orphaned CSS rules from dabbler-pages.css. Updated
Dabbler.Components README.md to reflect rebuilt architecture.

Documentation updates:
- **README.md:** Updated with MudBlazor-native UI layer, route-based
  navigation, explicit page composition descriptions
- **guiding-principles.md:** Added 5 new principles (explicit composition,
  MudDataGrid direct, MudForm direct, MudLayout shell, filter model
  preservation)
- **lessons-learned.md:** Added 3 lessons (structured rebuild achievable,
  explicit composition reduces cost, preserve domain logic)
- **phase-2-platform-roadmap.md:** Session Set 1 marked SUPERSEDED by rebuild
- **platform-ui-foundation-review-ledger.md:** Resolution section mapping
  each keep/simplify/rethink recommendation to rebuild outcome

Final build: 0 errors, 467 tests passing (0 failures).

## Downstream Impact

This session set supersedes the remaining sessions of
`platform-ui-foundation-review` (Sessions 2 and 3) and replaces Session Set 1
of the Phase 2 roadmap. The MudBlazor-native platform is the new foundation
for all downstream Phase 2 work.
