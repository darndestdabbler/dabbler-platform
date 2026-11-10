# Change Log: Post-Rebuild Human UAT

## Summary

Single-session UAT and remediation effort following the MudBlazor-native
rebuild (8 sessions) and E2E workflow coverage (5 sessions). The human
reviewer executed a 29-item checklist. 15 issues were identified, 10 fixed
in-session, 5 deferred with clear tracking.

## Features Created

- **Dev Quick Sign-In** — dropdown on the login page with 6 test personas,
  gated by `SeededSession.Enabled`. Uses JS interop to call the
  seeded-session endpoint with the required secret header.

## Bugs Fixed

### Layout
- Fixed content hidden behind app bar (`pt-16` on MudMainContent)
- Removed duplicate h1 header from Security Dashboard
- Spread report columns to full 9.5" landscape width (both reports)

### Functional
- Product detail Undo — added ValueChanged callbacks to 5 fields (UnitPrice,
  RoastLevel, CaffeineContent, GrindType, IsAvailable)
- Dashboard search — pass search term as `?search=` query parameter; list
  components read `InitialSearchTerm` and apply as initial filter
- Report date pickers — added `Editable=true` with `DateFormat` and
  `Placeholder` for typed date entry
- Admin drawer visibility — `isAdmin` check now considers
  BackofficeUserAdmin across all org memberships, not just active org

### Styling
- Right-aligned currency and date columns with `max-width:130px`
- Added `Bordered="true"` to all 7 MudDataGrid instances

## Documentation Updated

- `docs/planning/lessons-learned.md` — 5 new entries
- `docs/planning/guiding-principles.md` — grid styling convention added

## Deferred Items

| Item | Tracking |
|------|----------|
| App bar identity redesign (hyperlink pattern) | Needs design session |
| Security Dashboard empty tabs | `security-dashboard-wiring` session set |
| Product filter panel investigation | E2E tests pass; needs further diagnosis |
| Filter/grid font size consistency | Acceptable per reviewer |
| Spacing inconsistency across pages | Monitor in future UAT |

## Files Created

- `src/Apps/AcmeCoffeeOnline.Web/wwwroot/dev-quick-signin.js`

## Files Modified

- `src/Apps/AcmeCoffeeOnline.Web/Components/Layout/MainLayout.razor`
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Auth/Login.razor`
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Dashboard.razor`
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Products/ProductsPage.razor`
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Products/ProductList.razor`
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Products/ProductDetail.razor`
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Orders/OrdersPage.razor`
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Orders/OrderList.razor`
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Orders/OrderDetail.razor`
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Customers/CustomerList.razor`
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Admin/ProductManagement.razor`
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Admin/UserManagement.razor`
- `src/Apps/AcmeCoffeeOnline.Web/Services/ShellSessionStateLoader.cs`
- `src/Apps/AcmeCoffeeOnline.Web/Reports/orders-by-customer.rdf.json`
- `src/Apps/AcmeCoffeeOnline.Web/Reports/sales-by-product.rdf.json`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerSecurityDashboard.razor`
- `src/Libs/Dabbler.Reports.Blazor/ParameterPromptForm.razor.cs`
- `docs/planning/lessons-learned.md`
- `docs/planning/guiding-principles.md`
