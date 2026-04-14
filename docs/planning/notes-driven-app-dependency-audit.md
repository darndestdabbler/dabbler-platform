# Notes-Driven App Dependency Audit

> **Purpose:** Capture the current `src/Apps/` dependency baseline for
> `docs/session-sets/notes-driven-app-alignment/` Session 1 so the follow-on
> implementation sessions can remove compatibility-era assumptions in a
> targeted way.
> **Created:** 2026-04-14
> **Source Inputs:** `docs/planning/notes-driven-library-compatibility-notes.md`
> and `docs/session-sets/notes-driven-app-alignment/spec.md`

---

## Audit Summary

The app is already centered on retained core surfaces such as
`DabblerShell`, `DabblerQueryPage<T>`, `DabblerReportPage`,
`DabblerDetailForm<T>`, `DabblerRecordPopup<T>`, `DabblerComboBox<T>`, and the
runtime security components.

The remaining alignment work is concentrated in a small set of
compatibility-era choices:

- shell/dashboard provider plumbing for catalog and usage tracking
- dashboard favorites/recent-items defaults
- broad catalog-style browsing instead of focused query workflows
- explicit opt-ins for advanced query export actions
- explicit report viewer controls that need a business-need review
- a security-nav integration that still assumes library-default role names

The app does not currently show evidence of direct `DabblerCatalogPage`,
`DabblerQueryReportPage<T>`, or `PageViewMode` usage. The remaining catalog
work is behavioral, not a direct dependency on the deprecated components.

## Remaining Dependencies And Migration Map

| Current app dependency | Current usage in app | Status | Preferred replacement or retained surface | Planned session | Notes |
| --- | --- | --- | --- | --- | --- |
| `CatalogProvider` and `UsageTrackingProvider` registrations | `Program.cs` registers `AcmeCoffeeCatalogProvider` and `InMemoryUsageTrackingProvider`; `MainLayout.razor` injects and passes them into `DabblerShell` | Optional compatibility | Keep `DabblerShell`, but remove provider-backed compatibility plumbing unless a focused workflow still needs it | Session 3 | This is the main shell-level compatibility path still tying the app to catalog/usage-tracking extras |
| Favorites and recent-items widgets | `MainLayout.razor` home config and `Dashboard.razor` both add `WidgetType.RecentItems` and `WidgetType.Favorites` | Reduce | Keep `DabblerDashboardPage`, but align it around navigation cards, quick links, and focused search | Session 2 | The compatibility notes treat favorites/recent-items as optional compatibility behavior rather than the dashboard default |
| Broad catalog browsing route | `ProductCatalog.razor` keeps a dedicated `/products/catalog` browsing experience and `MainLayout.razor` exposes a top-level Catalog button | Replace | Move product discovery toward focused query/list workflows built on `DabblerQueryPage<ProductDto>` | Session 2 | No evidence shows direct `DabblerCatalogPage` usage, but the user flow still preserves the older broad catalog assumption |
| CSV export opt-ins on query pages | `ProductList.razor`, `CustomerList.razor`, and `OrderList.razor` each enable `ShowCsvExport="true"` | Reduce | Keep `DabblerQueryPage<T>` and only preserve CSV export where a confirmed workflow still needs it | Session 2 | CSV export remains available as an explicit opt-in, but it is no longer part of the default notes-driven query experience |
| Explicit report viewer controls | `OrdersByCustomer.razor` and `SalesByProduct.razor` enable `ShowPdfExport`, `ShowPrint`, and `ShowZoom` | Retain | Keep `DabblerReportPage`, then confirm which viewer controls still belong in the accepted notes-driven workflow | Session 2 | The report page itself is retained; the follow-up question is whether the extra controls are still part of the app baseline or should be pared back |
| Security navigation role-name mismatch | `MainLayout.razor` still renders `DabblerSecurityNav`, but the inline comment says the component's default role assumptions do not match the app's actual role names | Reduce | Keep the retained security core, but align admin navigation around the app's actual role model | Session 3 | This is not a simplification blocker for Session 2, but it is a clear shell/security alignment task |
| Combined query/report obsolete surface check | No app file currently shows `DabblerQueryReportPage<T>` or `PageViewMode` usage | Retain | Continue separate `DabblerQueryPage<T>` and `DabblerReportPage` composition | Later/Optional | Session 1 confirmed this deprecated migration target is already absent from the app code |

## Already Aligned And Safe To Keep

| Surface | Current app usage | Why it stays |
| --- | --- | --- |
| `DabblerShell` | Main layout composition and workspace shell | It remains the preferred reusable shell default |
| `DabblerQueryPage<T>` | Product, customer, and order list pages | It is the retained standard query/list composition surface |
| `DabblerReportPage` | Orders-by-customer and sales-by-product pages | It remains the retained report prompting/execution/display surface |
| `DabblerDetailForm<T>` and `DabblerRecordPopup<T>` | Product, customer, order, and popup editing flows | They are the retained record-editing surfaces |
| `DabblerComboBox<T>` | Product lookup workflows | It is the preferred single-select lookup surface |
| Security runtime components | `DabblerPageAuthorize`, `DabblerSecured`, role switching, security dashboard/navigation integration | The security core remains part of the accepted platform surface |

## Pages And Layouts Carrying Pre-Simplification Assumptions

| Page or layout | Pre-simplification assumption still present | Follow-up target |
| --- | --- | --- |
| `Components/Layout/MainLayout.razor` | Shell still assumes provider-backed catalog and usage-tracking extras, plus a top-level catalog entry point | Session 3 |
| `Components/Pages/Dashboard.razor` | Dashboard still treats recent/favorites widgets as a default experience | Session 2 |
| `Components/Pages/Products/ProductCatalog.razor` | Product discovery still leans on broad catalog browsing instead of focused query workflows | Session 2 |
| `Components/Pages/Products/ProductList.razor` | Advanced CSV export is still surfaced on the standard query page | Session 2 |
| `Components/Pages/Customers/CustomerList.razor` | Advanced CSV export is still surfaced on the standard query page | Session 2 |
| `Components/Pages/Orders/OrderList.razor` | Advanced CSV export is still surfaced on the standard query page | Session 2 |
| `Components/Pages/Reports/OrdersByCustomer.razor` | Report surface still assumes the broader viewer-control set is part of the accepted default | Session 2 |
| `Components/Pages/Reports/SalesByProduct.razor` | Report surface still assumes the broader viewer-control set is part of the accepted default | Session 2 |

## Risks If Left Unchanged

- Compatibility-only dashboard and shell paths would remain part of the app's
  accepted baseline, which conflicts with the simplification handoff.
- Future library cleanup would turn today's optional migration work into a
  breaking refactor under time pressure.
- The app would continue proving a broader historical surface than the one the
  platform now intends to support.
