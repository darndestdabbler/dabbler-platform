# Session 1 Alignment Baseline

This note captures the Session 1 baseline for
`docs/session-sets/notes-driven-app-alignment/` after the dependency audit.
It is the handoff for the implementation sessions that follow.

## Implementation Targets

### Session 2

- remove favorites/recent-items as default dashboard widgets in both
  `MainLayout.razor` and `Dashboard.razor`
- decide whether `ProductCatalog.razor` stays as a compatibility route or is
  replaced by a focused product query workflow
- review `ShowCsvExport` on product, customer, and order query pages and keep
  it only where the app still has a confirmed workflow need
- review `ShowPdfExport`, `ShowPrint`, and `ShowZoom` on report pages and keep
  only the viewer controls that still belong in the accepted notes-driven app

### Session 3

- remove `CatalogProvider` and `UsageTrackingProvider` from the default shell
  path unless a later focused workflow still requires them
- align the top-level navigation away from the broad catalog entry-point
- reconcile `DabblerSecurityNav` with the app's actual role names and admin
  navigation model

## Confirmed Non-Issues

- no current app files show direct `DabblerCatalogPage` usage
- no current app files show direct `DabblerQueryReportPage<T>` usage
- no current app files show direct `PageViewMode` usage
- the app is already on retained surfaces for query, detail, popup, combo,
  report, shell, and runtime security composition

## Working Rule

Follow-on sessions should remove app reliance on compatibility-era behaviors
instead of rebuilding wrappers around them. If a compatibility path is kept, it
should be because the app still needs it deliberately, not because it was part
of the old broad default surface.
