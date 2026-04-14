# Notes-Driven Library Compatibility Notes

> **Purpose:** Hand off the completed core-surface simplification results to
> app-alignment work without ambiguity about which library surfaces remain
> supported, which survive only for compatibility, and which replacements new
> work should prefer.
> **Created:** 2026-04-14
> **Source Inputs:** `docs/planning/notes-driven-supported-library-surface.md`
> and `docs/session-sets/notes-driven-core-surface-simplification/`

---

## Operating Rule

Treat this document as the migration and compatibility handoff for
`docs/session-sets/notes-driven-app-alignment/`.

New app work should target the retained core surface directly. Compatibility
paths exist only to avoid a forced break while the app alignment set removes
its remaining dependencies on the broader historical surface.

## Preferred Surfaces

### Query, Grid, And Filter

- prefer `DabblerQueryPage<T>` as the standard query/list composition surface
- prefer the retained `DabblerDataGrid<T>` core workflow for sorting, paging,
  row selection, and CRUD-style list operations
- prefer the retained `DabblerFilterGrid<T>` QBE workflow with row-based
  AND/OR behavior and English summary output

### Combo, Detail, And Popup

- prefer `DabblerComboBox<T>` for searchable lookup workflows
- keep `DabblerMultiComboBox<T>` as the accepted multiselect lookup surface
  for now; the simplification work intentionally did not force this behavior
  behind a new API
- keep `DabblerDetailForm<T>` and `DabblerRecordPopup<T>` as the retained
  record-editing surfaces

### Shell, Dashboard, Reports, And Security

- prefer `DabblerShell` left-nav/workspace composition as the reusable shell
  default
- prefer `DabblerDashboardPage` navigation-card, quick-link, and focused-search
  behavior as the dashboard default
- prefer `DabblerReportPage`, `ParameterPromptForm`, and `ReportViewer` for
  report prompting, execution, and display
- keep the security core centered on runtime authorization, secured rendering,
  role switching, security navigation, and the existing admin/dashboard
  surfaces

## Compatibility-Only Or Intentionally Isolated Surfaces

### Query, Grid, And Filter Extras

These members still compile, but app-alignment work should stop depending on
them as part of the default notes-driven experience:

- `DabblerDataGrid<T>` grouping, aggregates, clipboard helpers, CSV export,
  column pinning, detail expansion, popup-form plumbing, and related advanced
  callbacks are hidden from the primary IntelliSense surface
- `DabblerFilterGrid<T>` quick filter, presets, expression-line output, and
  column-visibility toggles are hidden from the primary IntelliSense surface
- `DabblerQueryPage<T>` advanced copy/CSV actions now require explicit opt-in
  rather than leading the default toolbar

### Dashboard And Shell Extras

- `DabblerTopMenuBar` remains available only as an optional compatibility path
  when both catalog and usage-tracking providers are intentionally supplied
- dashboard favorites and recent-items widgets are compatibility-only; when
  usage tracking is absent, `DabblerDashboardPage` now renders an explanatory
  placeholder instead of treating those widgets as default platform behavior

## Deprecated Public APIs And Replacements

| Compatibility surface | Current status | Preferred replacement |
|---|---|---|
| `DabblerCatalogPage` | Marked `Obsolete` and hidden from IntelliSense | Use notes-driven shell navigation plus focused query/report pages for the actual workflow being surfaced |
| `DabblerQueryReportPage<T>` | Marked `Obsolete` and hidden from IntelliSense | Compose `DabblerQueryPage<T>` and `DabblerReportPage` separately |
| `PageViewMode` | Marked `Obsolete` because it exists only to support `DabblerQueryReportPage<T>` | Remove the combined query/report dependency instead of carrying the helper forward |

## App-Alignment Expectations

- treat obsolete or hidden surfaces as migration targets, not compatibility
  requirements to preserve
- move dashboard defaults away from favorites/recent-items unless the app has a
  deliberate usage-tracking-backed compatibility need
- move broad catalog browsing to focused app workflows instead of relying on
  `DabblerCatalogPage`
- keep `DabblerMultiComboBox<T>` usage where multiselect lookup behavior is
  still genuinely needed; it remains a supported holdover for now
- prefer removing app dependencies on advanced grid/filter/query helpers over
  adding new wrappers around those helpers

## Known Compatibility Notes

- `DabblerQueryReportPage<T>` still references the obsolete `PageViewMode`
  helper internally, so any remaining compatibility consumer may see obsolete
  warnings until that dependency is removed during app alignment
- favorites/recent-items compatibility widgets are intentionally tolerant when
  usage tracking is absent; they communicate non-core status via placeholders
  instead of failing at runtime
- no catch-up simplification session was required to publish these notes; the
  existing retained surface and compatibility markers were sufficient for the
  handoff
