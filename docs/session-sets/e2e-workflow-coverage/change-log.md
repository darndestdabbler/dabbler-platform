# Change Log: E2E Workflow Coverage Session Set

This session set delivered comprehensive Playwright E2E test coverage for the
AcmeCoffeeOnline application, adding 63 new tests across 5 sessions.

## Summary

- **New Tests:** 63 new Playwright tests across 6 new test files and 1
  extended existing file
- **Infrastructure:** PlaywrightFixture extended with navigation, form, grid,
  and dialog helpers plus customer and order API verification helpers
- **Bug Fixes:** 2 pre-existing application bugs identified and fixed during
  test development
- **Test Suite Status:** 75 total Playwright tests (68 passing, 7 pre-existing
  failures unchanged)
- **Non-Playwright Tests:** All 467 continue to pass

---

## Session Breakdown

### Session 1: Infrastructure Extensions and Navigation Proof (12 tests)

- Extended PlaywrightFixture with drawer navigation helpers
  (`NavigateViaDrawerAsync`, `NavigateViaDrawerGroupAsync`,
  `ClickDashboardNavCardAsync`)
- Added customer API helpers (`GetCustomerByIdAsync`, `GetCustomersAsync`,
  `CreateCustomerAsync`, `UpdateCustomerAsync`, `CustomerSnapshot`)
- Added order mutation API helpers (`UpdateOrderStatusAsync`,
  `AddOrderItemAsync`, `RemoveOrderItemAsync`, order notes CRUD,
  `OrderItemSnapshot`, `OrderNoteSnapshot`)
- Created `NavigationTests.cs` (9 tests): drawer navigation to Dashboard,
  Orders, Retail Businesses, Products; drawer group navigation to Reports and
  Admin sections; hamburger toggle
- Created `DashboardWorkflowTests.cs` (3 tests): nav card to Orders, nav card
  to Products, report quick link

### Session 2: Products and Customers CRUD + Validation (19 tests)

- Extended PlaywrightFixture with form helpers (`GetFormInputAsync`,
  `FillFormFieldAsync`, `ClearFormFieldAsync`, `SelectFormDropdownAsync`,
  `SelectDropdownByLabelAsync`, `ClickSaveAsync`, `ClickUndoAsync`,
  `AssertSaveDisabledAsync`, `AssertSaveEnabledAsync`)
- Extended PlaywrightFixture with grid helpers (`GetGridRowCountAsync`,
  `ClickGridRowAsync`, `WaitForGridDataAsync`)
- Created `ProductsWorkflowTests.cs` (9 tests): list load with columns, row
  click navigation, detail load with field verification, edit-save with API
  verification, edit-undo revert, toggle availability with API verification,
  back button, filter by roast level, filter by price range
- Created `CustomerCrudWorkflowTests.cs` (5 tests): list load with column
  headers, detail load with field verification, edit-save with API
  verification, edit-undo revert, back button
- Created `ValidationTests.cs` (5 tests): product name required, product unit
  price boundary, customer first name required, customer last name required,
  customer email required

### Session 3: Orders Workflow and Child Grids (13 tests)

- Extended PlaywrightFixture with dialog helpers (`WaitForDialogAsync`,
  `FillDialogFieldAsync`, `SubmitDialogAsync`, `CancelDialogAsync`)
- Extended `OrderSnapshot` with Status, Total, OrderDate, ShipDate, Notes,
  OrderItems, OrderNotes, and StatusDisplayName
- Created `OrdersWorkflowTests.cs` (13 tests): list load with columns, row
  click navigation, status chips visible, grid paging, detail header form with
  API cross-verification, status chip display, back button, items tab count
  and grid columns, line total calculation, notes tab switch and grid, read-
  only verification (status buttons hidden, item/note controls hidden), cross-
  page field-to-API verification
- Documented that order detail renders as read-only (`IsReadOnly=true`), so
  spec interactions 36-47 (status transitions, item CRUD, note CRUD) are not
  testable through the current UI

### Session 4: Reports, Admin, and Multi-Step Workflows (14 tests)

- Created `ReportsWorkflowTests.cs` (5 tests): orders by customer
  navigate+run+verify, orders by customer PDF export, sales by product
  navigate+run+verify, sales by product PDF export, missing dates validation
- Extended `ProductAdministrationWorkflowTests.cs` (3 new tests): edit
  product via admin dialog with API verification, search filter, pagination
- Created `MultiStepWorkflowTests.cs` (6 tests): product lifecycle (drawer to
  edit to save to back to verify grid), customer lifecycle (drawer to edit to
  save to API verify to back), order tab navigation, dashboard-to-detail,
  filter-search-navigate, role-scoped navigation (Customer role admin links
  hidden)

### Session 5: Edge Cases, Stabilization, and Full Suite Pass (5 tests)

- Created `EdgeCaseTests.cs` (5 tests): product not-found (500 error due to
  unhandled exception), order not-found (graceful alert via OrderDetailState),
  access denied for Customer role on `/customers`, access denied for
  BackofficeUser on `/admin/products`, access denied for BackofficeUser on
  `/admin/security`
- Comprehensive data isolation review across all 11 test files: confirmed all
  mutation tests use `finally` blocks for cleanup, create tests use unique
  timestamped names, update tests snapshot and restore original state
- Full suite stabilization: all 63 new tests pass, 68 of 75 total pass

---

## Bug Fixes (Session 1)

- **`appsettings.json`:** Removed stale `dabbler-combobox.css` reference
  causing 404 console errors
- **`FilterSummaryBuilder.cs`:** Fixed `ToDictionary` crash when filter
  columns share the same `PropertyName` (range filters like OrderDate from/to)

---

## Known Issues

- **Product Not-Found Handling:** `ProductDetail.OnInitializedAsync` does not
  catch `HttpRequestException` for missing products, causing HTTP 500 instead
  of the `product-not-found` alert. `OrderDetailState` handles this correctly
  (catches 404/403 and sets `Record = null`). The edge case test documents
  this gap.
- **Order Detail Read-Only:** The `/orders/{id}` page renders with
  `IsReadOnly=true`, so status transitions, item add/remove, and note
  add/edit/delete are not exposed in the current UI. These interactions cannot
  be tested through E2E automation until a non-read-only surface is available.
- **Pre-existing Failures:** 7 test failures in `SmokeTests`,
  `ProductAdministrationWorkflowTests`, and
  `MultiOrganizationWorkflowTests` remain unchanged from before this session
  set. These involve layout assertions, admin page timeouts under concurrent
  load, and multi-organization workflow timing.

---

## Files Created

- `tests/AcmeCoffeeOnline.Playwright/NavigationTests.cs`
- `tests/AcmeCoffeeOnline.Playwright/DashboardWorkflowTests.cs`
- `tests/AcmeCoffeeOnline.Playwright/ProductsWorkflowTests.cs`
- `tests/AcmeCoffeeOnline.Playwright/CustomerCrudWorkflowTests.cs`
- `tests/AcmeCoffeeOnline.Playwright/ValidationTests.cs`
- `tests/AcmeCoffeeOnline.Playwright/OrdersWorkflowTests.cs`
- `tests/AcmeCoffeeOnline.Playwright/ReportsWorkflowTests.cs`
- `tests/AcmeCoffeeOnline.Playwright/MultiStepWorkflowTests.cs`
- `tests/AcmeCoffeeOnline.Playwright/EdgeCaseTests.cs`

## Files Modified

- `tests/AcmeCoffeeOnline.Playwright/Infrastructure/PlaywrightFixture.cs`
  (navigation, form, grid, dialog helpers; customer/order API helpers and
  snapshot types)
- `tests/AcmeCoffeeOnline.Playwright/ProductAdministrationWorkflowTests.cs`
  (3 new tests added in session 4)
- `src/Apps/AcmeCoffeeOnline.Web/appsettings.json` (removed stale CSS
  reference)
- `src/Libs/Dabbler.Components/FilterPanel/FilterSummaryBuilder.cs` (fixed
  ToDictionary crash)
