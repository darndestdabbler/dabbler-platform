# Notes-Driven App Acceptance Baseline

> **Purpose:** Document the final accepted application baseline after the `docs/session-sets/notes-driven-app-alignment/` work set.
> **Created:** 2026-04-14
> **Source Inputs:** `docs/planning/notes-driven-library-compatibility-notes.md`, `docs/planning/notes-driven-app-dependency-audit.md`, and `tests/AcmeCoffeeOnline.Web.Tests/NotesDrivenAlignmentGuardrailTests.cs`

---

## Summary

The app now serves as the proof harness for the narrowed notes-driven platform
surface. It demonstrates that the retained shell, query, detail, report, and
dashboard surfaces are sufficient for the intended workflows without restoring
deprecated pages or default compatibility-era plumbing.

For the current distributor-era domain handoff layered on top of this technical
baseline, use `docs/planning/distributor-acceptance-baseline.md` as the
source-of-truth artifact.

## Accepted App Baseline

### Core Workflows

The accepted app baseline is centered on the retained reusable surfaces:

- `DabblerShell` for the main layout, navigation frame, and workspace
  composition
- `DabblerQueryPage<T>` for the main product, customer, and order list/search
  workflows
- `DabblerReportPage` for report prompting, execution, and display
- `DabblerDetailForm<T>` and `DabblerRecordPopup<T>` for record creation and
  editing
- `DabblerComboBox<T>` for single-select lookup workflows

### Query And Export Behavior

The accepted baseline keeps CSV export enabled on the main query pages. This is
now an explicit app-level workflow choice rather than a broad assumption that
every advanced grid helper should remain front-and-center by default.

### Report Viewer Behavior

The accepted baseline keeps PDF export enabled on the report pages while
continuing to leave print and zoom controls out of the app experience.

### Routing And Focused Aliases

The `/products/catalog` route remains as a focused alias to the retained
product query workflow. It delegates authorization through
`DabblerPageAuthorize("/products")` and renders `ProductList` instead of
reintroducing a broad catalog-browsing surface.

### Shell, Admin, And Security

The accepted shell baseline removes catalog and usage-tracking provider wiring
from the default app shell. It also removes `DabblerSecurityNav` from
`MainLayout` in favor of the app's focused, role-aware admin menu plus
`DabblerRoleSwitcher`, keeping admin navigation aligned with the app's actual
role model instead of library-default assumptions.

### Dashboard Composition

The accepted dashboard baseline uses navigation cards, quick links, and focused
search widgets. Favorites and recent-items widgets remain compatibility-only
paths and are not part of the default app experience.

## Not Part Of The Accepted App Baseline

- no app use of `DabblerCatalogPage`
- no app use of `DabblerQueryReportPage<T>`
- no app use of `PageViewMode`
- no default shell wiring for `IArtifactCatalogProvider` or
  `IUsageTrackingProvider`
- no default `DabblerSecurityNav` composition in the app shell
- no dashboard favorites/recent-items defaults

## Supported But Not Required

`DabblerMultiComboBox<T>` remains a supported library holdover for multiselect
lookup workflows, but the accepted app baseline does not require it to prove
the narrowed platform story.

## Downstream Implications

`docs/session-sets/notes-driven-template-encoding/` should treat this document
as the app-level handoff for scaffolding decisions.

- do not scaffold catalog or usage-tracking provider wiring by default
- do not scaffold dashboard favorites/recent-items widgets by default
- do not scaffold `DabblerSecurityNav` or role assumptions that belong to the
  old library-default admin path
- treat query-page CSV export as a deliberate app-level opt-in, not a universal
  starter default
- preserve the report-page PDF-export baseline without reintroducing print or
  zoom controls
- treat multiselect lookup support as optional add-on work rather than part of
  the minimal starter story
