# Comprehensive E2E Workflow Coverage Spec

> **Purpose:** Deliver Playwright end-to-end test coverage for every
> user-reachable interaction in the AcmeCoffeeOnline application, organized
> by entity domain with cross-cutting navigation and validation testing.
> **Created:** 2026-04-16
> **Session Set:** `docs/session-sets/e2e-workflow-coverage/`
> **Prerequisite:** `mudblazor-native-rebuild` Session Set must be complete
> (all 8 sessions). The rebuilt MudBlazor-native UI is the test target.
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification

---

## Problem Statement

The current Playwright suite has 11 tests, 7 of which focus on
authentication and permission enforcement. Almost no coverage exists for:

- CRUD workflows (create, edit, save, undo, delete)
- Form validation (required fields, format rules, boundary values)
- Real-UI navigation (clicking nav menu items, grid row clicks, back buttons)
- Filter and grid interactions (filter panels, paging, sorting, debounced refresh)
- Multi-step user journeys (search -> select -> edit -> save -> verify in DB)
- Child entity management (tabs, popup dialogs, add/remove)
- Report parameter forms and export

Human UAT has repeatedly discovered broken controls that should have been
caught by automation. The lessons-learned file documents this pattern:
"Human UAT Must Not Be The First Discovery Point For Broken Controls."

## Goals

1. **Every user-reachable page** has at least one navigation test and one
   interaction test that drives real UI controls.
2. **Every data mutation** (create, update, delete, status change) is
   verified in the database via API call after the UI action.
3. **Every form** has validation tests for required fields and format rules.
4. **Every navigation path** is proven through real UI controls (drawer
   clicks, grid row clicks, back buttons), not `goto()`.
5. **Multi-step workflow sequences** prove that the system hangs together
   across pages and entities.

## What This Set Creates

| Deliverable | Description |
| --- | --- |
| **~65 new Playwright tests** | Organized across 6+ test files by entity domain |
| **PlaywrightFixture extensions** | Drawer navigation helpers, form helpers, grid helpers, dialog helpers |
| **API verification helpers** | Customer, order, order-item, and order-note snapshot types and API methods |
| **Test data management** | Unique-name generators, cleanup registry, archive/restore patterns |
| **Full Docker-backed suite pass** | All ~76 tests (existing + new) passing in Docker compose |

## What This Set Preserves

| Existing Asset | Status |
| --- | --- |
| **Existing 11 Playwright tests** | Kept as-is (SmokeTests, CustomerWorkflowTests, MultiOrganizationWorkflowTests, ProductAdministrationWorkflowTests) |
| **PlaywrightFixture infrastructure** | Extended, not replaced |
| **Docker compose configuration** | Unchanged |
| **Application code** | No changes to app code; tests only |

## Architecture Decisions

### 1. Helper Methods, Not Page Objects

Extend the existing `PlaywrightFixture` pattern with focused helper methods
rather than introducing a heavyweight page-object abstraction layer:

- `NavigateViaDrawerAsync(page, linkText)` — click MudNavLink by text
- `NavigateViaDrawerGroupAsync(page, groupText, linkText)` — expand group,
  click child link
- Form helpers: `FillFormFieldAsync`, `SelectFormDropdownAsync`,
  `ClickSaveAsync`, `ClickUndoAsync`, `AssertSaveDisabledAsync`
- Grid helpers: `GetGridRowCountAsync`, `ClickGridRowAsync`,
  `AssertGridColumnVisibleAsync`, `WaitForGridDataAsync`
- Dialog helpers: `WaitForDialogAsync`, `FillDialogFieldAsync`,
  `SubmitDialogAsync`, `CancelDialogAsync`

### 2. API Verification After Every Mutation

Every test that creates, updates, or deletes data must verify the outcome
via an API call. UI assertions alone are insufficient because rendering can
succeed while the API call fails silently.

### 3. Test Data Isolation

- **Read-only tests:** Use seeded data as-is. No cleanup needed.
- **Create tests:** Generate unique names with
  `$"Playwright {entity} {DateTime.UtcNow:yyyyMMddHHmmssfff}"`. Archive
  or clean up in `finally` block.
- **Update tests:** Snapshot state before modification. Verify after.
  Restore original state in `finally` block.
- **Delete tests:** Create the entity first, then delete it. Self-cleaning.
- Follow the existing archive/restore pattern from
  `ProductAdministrationWorkflowTests`.

### 4. Navigation-First Test Design

Every test that visits a page reachable through the UI must navigate there
through real controls. Direct `goto()` is permitted only for:
- The initial authenticated landing page
- Pages that have no UI entry point (e.g., deep-linked URLs shared externally)
- Setup steps where the navigation path is not under test

### 5. MudBlazor Interaction Patterns

- **MudDataGrid:** Wait for rows via locator assertions, not `NetworkIdle`
- **MudDialog:** Target elements within `[role='dialog']` to avoid overlay
- **MudSelect/MudAutocomplete:** Click input to open popover, find option
  within `.mud-popover-open`, click
- **FilterPanel:** Account for 200ms debounce; wait for grid reload after
  filter change
- **MudTabs:** Click tab header, wait for panel content to appear

---

## Complete Interaction Catalog

### Navigation Shell (7 interactions)

| # | Interaction | Verification |
|---|-------------|-------------|
| 1 | Click Dashboard in drawer | URL is `/dashboard` |
| 2 | Click Orders in drawer | URL is `/orders` |
| 3 | Click Customers in drawer | URL is `/customers` |
| 4 | Click Products in drawer | URL is `/products` |
| 5 | Expand Reports group → click each report link | Correct report URLs |
| 6 | Expand Admin group → click each admin link | Correct admin URLs (admin user) |
| 7 | Hamburger toggle | Drawer collapses and expands |

### Dashboard /dashboard (3 interactions)

| # | Interaction | Verification |
|---|-------------|-------------|
| 8 | Click Orders navigation card | URL is `/orders` |
| 9 | Click Products navigation card | URL is `/products` |
| 10 | Click report quick link | Correct report URL |

### Products List /products (9 interactions)

| # | Interaction | Verification |
|---|-------------|-------------|
| 11 | Grid loads with data | Rows visible, columns match (Name, Roast Level, Caffeine, Grind Type, Unit Price, Available) |
| 12 | Filter by RoastLevel | Grid refreshes, filtered count shown |
| 13 | Filter by price range (min/max) | Grid refreshes, count updates |
| 14 | Filter status text | "Showing X of Y records" correct |
| 15 | Clear filter chip | Grid reloads unfiltered |
| 16 | Grid row click | URL changes to `/products/{id}` |
| 17 | "Add Product" button | Navigates to `/admin/products` |
| 18 | Grid paging | Different data shown on page 2 |
| 19 | Sort by column | Grid reorders |

### Product Detail /products/{id} (11 interactions)

| # | Interaction | Verification |
|---|-------------|-------------|
| 20 | Form loads with data | All fields populated from API |
| 21 | Edit Name → dirty state | Save enabled, Undo enabled |
| 22 | Click Undo | Fields revert to original values |
| 23 | Click Save | Changes persisted — verified via API |
| 24 | Deactivate toggle | API shows `IsAvailable=false` |
| 25 | Activate toggle | API shows `IsAvailable=true` |
| 26 | Back button | URL returns to `/products` |
| 27 | Change select fields (Roast, Caffeine, Grind) | Dirty state activates |
| 28 | **Validation: Name required** | Error message on empty |
| 29 | **Validation: UnitPrice min** | Error on 0 |
| 30 | **Validation: UnitPrice max** | Error on 10000 |

### Orders List /orders (4 interactions)

| # | Interaction | Verification |
|---|-------------|-------------|
| 31 | Grid loads with data | Rows visible, columns correct |
| 32 | Row click → detail | URL is `/orders/{id}` |
| 33 | Grid paging | Pages navigate correctly |
| 34 | Status chip colors | Colors match status values |

### Order Detail /orders/{id} (14 interactions)

| # | Interaction | Verification |
|---|-------------|-------------|
| 35 | Header form loads | CustomerName, OrderDate, Total, Notes visible |
| 36 | Status: Pending → Confirmed | Chip changes, API confirms |
| 37 | Status: Confirmed → Shipped | Chip changes, API confirms |
| 38 | Status: Shipped → Delivered | Chip changes, API confirms |
| 39 | Status: Cancel from Pending | Chip changes, API confirms |
| 40 | Items tab: count and grid content | Tab label "Items (N)", columns correct |
| 41 | Items tab: Add item via product picker | Select product, set qty/discount, click Add → grid + API |
| 42 | Items tab: Remove item | Click delete → removed from grid + API |
| 43 | Items tab: Line total calculation | Correct value displayed |
| 44 | Notes tab: switch tab | Notes grid visible |
| 45 | Notes tab: Add note via dialog | Fill form, submit → grid + API |
| 46 | Notes tab: Edit note via dialog | Modify, submit → grid + API |
| 47 | Notes tab: Delete note | Click delete → removed + API |
| 48 | Back button | URL returns to `/orders` |

### Customers List /customers (2 interactions)

| # | Interaction | Verification |
|---|-------------|-------------|
| 49 | Grid loads with data | Rows visible, columns correct |
| 50 | Grid paging | Pages navigate correctly |

### Customer Detail /customers/{id} (8 interactions)

| # | Interaction | Verification |
|---|-------------|-------------|
| 51 | Form loads with data | All 9 fields populated |
| 52 | Edit field → dirty state | Save/Undo enabled |
| 53 | Click Undo | Fields revert |
| 54 | Click Save | Changes persisted — verified via API |
| 55 | Back button | URL returns to `/customers` |
| 56 | **Validation: FirstName required** | Error message |
| 57 | **Validation: LastName required** | Error message |
| 58 | **Validation: Email required** | Error message |

### Admin: Product Management /admin/products (4 interactions)

| # | Interaction | Verification |
|---|-------------|-------------|
| 59 | Navigate via drawer | URL is `/admin/products` |
| 60 | Edit product dialog | Modify fields → save → API confirms |
| 61 | Search filter | Grid filters by search term |
| 62 | Pagination | Pages navigate correctly |

### Reports (5 interactions)

| # | Interaction | Verification |
|---|-------------|-------------|
| 63 | Orders by Customer: navigate, params, run | Report output renders |
| 64 | Orders by Customer: PDF export | Download initiated |
| 65 | Sales by Product: navigate, params, run | Report output renders |
| 66 | Sales by Product: PDF export | Download initiated |
| 67 | Report validation: missing dates | Disabled state or error |

### Auth Edge Cases (5 interactions)

| # | Interaction | Verification |
|---|-------------|-------------|
| 68 | Product not found (invalid ID) | Not-found alert shown |
| 69 | Order not found (invalid ID) | Not-found alert shown |
| 70 | Access denied: Customer on /customers | Access-denied page |
| 71 | Access denied: BackofficeUser on /admin/products | Access-denied page |
| 72 | Access denied: BackofficeUser on /admin/security | Access-denied page |

### Multi-Step Workflow Sequences (6 sequences)

| # | Workflow | Steps |
|---|---------|-------|
| 73 | Product lifecycle | Drawer → products → row click → edit → save → back → verify grid |
| 74 | Customer lifecycle | Drawer → customers → row click → edit → save → API verify → back |
| 75 | Order notes workflow | Drawer → orders → row click → Notes tab → add → edit → delete → API |
| 76 | Dashboard-to-detail | Dashboard card → entity list → row click → detail → back → dashboard |
| 77 | Filter-search-navigate | Products → set filters → row click → detail → back → filters preserved |
| 78 | Role-scoped navigation | Customer role → verify drawer links, admin hidden |

---

## Session Plan

### Session 1 of 5: Infrastructure Extensions and Navigation Proof

**Target: ~12 tests**

**Steps:**
1. Read `docs/planning/guiding-principles.md` and
   `docs/planning/lessons-learned.md`.
2. Extend `PlaywrightFixture` with drawer navigation helpers:
   - `NavigateViaDrawerAsync(page, linkText)`
   - `NavigateViaDrawerGroupAsync(page, groupText, linkText)`
   - `ClickDashboardNavCardAsync(page, cardLabel)`
3. Extend fixture with customer API helpers: `GetCustomerByIdAsync`,
   `CreateCustomerAsync`, `UpdateCustomerAsync`, customer snapshot type.
4. Extend fixture with order mutation API helpers:
   `UpdateOrderStatusAsync`, `AddOrderItemAsync`, `RemoveOrderItemAsync`,
   `GetOrderNotesAsync`, `AddOrderNoteAsync`, `UpdateOrderNoteAsync`,
   `DeleteOrderNoteAsync`, order/note snapshot types.
5. Create `NavigationTests.cs`:
   - Drawer → Dashboard (verify URL `/dashboard`)
   - Drawer → Orders (verify URL `/orders`)
   - Drawer → Customers (verify URL `/customers`)
   - Drawer → Products (verify URL `/products`)
   - Drawer → Reports group → Orders by Retail Business
   - Drawer → Reports group → Sales by Product
   - Drawer → Admin group → User Management (admin user)
   - Drawer → Admin group → Product Management (SystemAdmin)
   - Hamburger toggle collapses and expands drawer
6. Create `DashboardWorkflowTests.cs`:
   - Dashboard nav card → Orders
   - Dashboard nav card → Products
   - Dashboard quick link → Orders by Retail Business report
7. Build and test (all Playwright tests including new and existing).

**Creates:** Navigation helpers, API verification helpers, NavigationTests,
DashboardWorkflowTests
**Touches:** `PlaywrightFixture.cs`, new helper and test files
**Ends with:** All navigation paths proven through real drawer interaction
**Progress keys:** `session-1/fixture-extensions`,
`session-1/api-helpers`, `session-1/navigation-tests`,
`session-1/dashboard-tests`, `session-1/build-and-test`

---

### Session 2 of 5: Products and Customers CRUD + Validation

**Target: ~18 tests**

**Steps:**
1. Read `docs/planning/guiding-principles.md` and
   `docs/planning/lessons-learned.md`.
2. Create form interaction helpers: `FillFormFieldAsync`,
   `SelectFormDropdownAsync`, `ClickSaveAsync`, `ClickUndoAsync`,
   `AssertSaveDisabledAsync`, `AssertSaveEnabledAsync`.
3. Create grid interaction helpers: `GetGridRowCountAsync`,
   `ClickGridRowAsync`, `WaitForGridDataAsync`.
4. Create `ProductsWorkflowTests.cs`:
   - Products list loads with data, columns visible (navigate via drawer)
   - Product grid row click navigates to detail page
   - Product detail loads existing record, all fields populated
   - Product detail edit → save → verify via API
   - Product detail edit → undo → verify reverted
   - Product detail deactivate/activate toggle → verify via API
   - Product detail back button → returns to `/products`
   - Products filter by RoastLevel → verify filtered count
   - Products filter by price range → verify filtered count
5. Extend `CustomerWorkflowTests.cs` or create new:
   - Customer list loads with data (navigate via drawer)
   - Customer detail loads existing record
   - Customer detail edit → save → verify via API
   - Customer detail edit → undo → verify reverted
   - Customer detail back button
6. Create `ValidationTests.cs`:
   - Product detail: Name required validation
   - Product detail: Unit Price boundary validation (min 0.01, max 9999.99)
   - Customer detail: FirstName required
   - Customer detail: LastName required
   - Customer detail: Email required
7. Build and test.

**Creates:** Form/grid helpers, ProductsWorkflowTests,
CustomerWorkflowTests extensions, ValidationTests
**Touches:** PlaywrightFixture extensions, test files
**Ends with:** Complete CRUD + validation coverage for Products and Customers
**Progress keys:** `session-2/form-helpers`, `session-2/grid-helpers`,
`session-2/products-workflow-tests`, `session-2/customer-workflow-tests`,
`session-2/validation-tests`, `session-2/build-and-test`

---

### Session 3 of 5: Orders CRUD, Status Workflow, and Child Grids

**Target: ~16 tests**

**Steps:**
1. Read `docs/planning/guiding-principles.md` and
   `docs/planning/lessons-learned.md`.
2. Create dialog interaction helpers: `WaitForDialogAsync`,
   `FillDialogFieldAsync`, `SubmitDialogAsync`, `CancelDialogAsync`.
3. Create `OrdersWorkflowTests.cs`:
   - Orders list loads with data, columns visible (navigate via drawer)
   - Order row click navigates to detail
   - Order detail header form loads with data
   - Order detail back button → returns to `/orders`
   - Status workflow: Pending → Confirmed → Shipped → Delivered
     (full lifecycle, API verify each step)
   - Status workflow: Pending → Cancelled (terminal, API verify)
   - Items tab: displays correct item count and grid content
   - Items tab: Add item via product picker → verify grid and API
   - Items tab: Remove item → verify removed from grid and API
   - Items tab: Line total calculation correct
   - Notes tab: switch tab, verify notes grid visible
   - Notes tab: Add note via dialog → verify grid and API
   - Notes tab: Edit note via dialog → verify updated and API
   - Notes tab: Delete note → verify removed and API
4. Add to `ValidationTests.cs`:
   - Note dialog: Note text required
   - Note dialog: Author required
5. Build and test.

**Creates:** Dialog helpers, OrdersWorkflowTests, validation extensions
**Touches:** PlaywrightFixture extensions, ValidationTests
**Ends with:** Complete order lifecycle, child-entity CRUD, and dialog
validation coverage
**Progress keys:** `session-3/dialog-helpers`,
`session-3/orders-list-tests`, `session-3/order-detail-tests`,
`session-3/order-items-tests`, `session-3/order-notes-tests`,
`session-3/order-validation-tests`, `session-3/build-and-test`

---

### Session 4 of 5: Reports, Admin, and Multi-Step Workflows

**Target: ~14 tests**

**Steps:**
1. Read `docs/planning/guiding-principles.md` and
   `docs/planning/lessons-learned.md`.
2. Create `ReportsWorkflowTests.cs`:
   - Orders by Customer: navigate via drawer, fill dates, run report,
     verify output renders
   - Orders by Customer: PDF export triggers download
   - Sales by Product: navigate, fill dates, run, verify
   - Sales by Product: PDF export
   - Report validation: submit without required dates → disabled/error
3. Extend `ProductAdministrationWorkflowTests.cs`:
   - Edit product via admin dialog → verify changes via API
   - Search filter on admin product page
   - Navigate to admin products via drawer
4. Create multi-step workflow tests:
   - Product lifecycle: drawer → products → row click → edit → save →
     back → verify grid updated
   - Customer lifecycle: drawer → customers → row click → edit → save →
     verify API → back
   - Order notes workflow: drawer → orders → row click → Notes tab →
     add → edit → delete → verify API
   - Dashboard-to-detail: dashboard card → entity list → row click →
     detail → back → back to dashboard
   - Filter-search-navigate: products → set filters → row click →
     detail → back → filters preserved
   - Role-scoped navigation: Customer role → verify drawer links correct,
     admin hidden
5. Build and test.

**Creates:** ReportsWorkflowTests, admin test extensions, multi-step
workflow tests
**Touches:** ProductAdministrationWorkflowTests, new test files
**Ends with:** Report interaction coverage, admin enhancements, and
cross-entity user journey proof
**Progress keys:** `session-4/reports-tests`,
`session-4/admin-extensions`, `session-4/multi-step-workflows`,
`session-4/build-and-test`

---

### Session 5 of 5: Edge Cases, Stabilization, and Full Suite Pass

**Target: ~5 new tests + full stabilization**

**Steps:**
1. Read `docs/planning/guiding-principles.md` and
   `docs/planning/lessons-learned.md`.
2. Add edge case tests:
   - Product not found page for invalid ID
   - Order not found page for invalid ID
   - Access denied: Customer role on `/customers`
   - Access denied: BackofficeUser on `/admin/products`
   - Access denied: BackofficeUser on `/admin/security`
3. Review all tests for data isolation:
   - Tests that create data must clean up in `finally` blocks
   - Tests must use unique names with timestamps
   - Tests that modify seeded data must restore original state
4. Run the full Docker-backed suite (Playwright runs from the host against
   the app containers):
   ```bash
   docker compose -f docker/docker-compose.infra.yml up -d        # sqlserver + mailhog
   docker compose -f docker/docker-compose.app.yml   up --build -d # api + web
   dotnet test tests/AcmeCoffeeOnline.Playwright/
   ```
5. Fix any flaky or failing tests. Stabilize retry behavior.
6. Verify all ~76 tests pass cleanly.
7. Generate `change-log.md` for this session set.

**Creates:** Edge case tests, stabilized full suite, change-log.md
**Touches:** All test files (review), Docker compose (if needed)
**Ends with:** All ~76 Playwright tests passing in Docker, every
user-reachable page and interaction covered
**Progress keys:** `session-5/edge-case-tests`,
`session-5/data-isolation-review`, `session-5/full-docker-suite-pass`,
`session-5/stabilization`, `session-5/change-log`

---

## Test Data Strategy

### Test Users By Role

| User | Role | Use In Tests |
| --- | --- | --- |
| `admin@acmecoffee.com` | SystemAdmin | Most CRUD tests, full admin access |
| `kbrown@acmecoffee.com` | BackofficeUserAdmin | No admin/products, no admin/security |
| `jdoe@acmecoffee.com` | BackofficeUser | No admin pages except user management |
| `james.smith@example.com` | Customer | Retail business, scoped to own orders |
| `casey.hart@acmecoffee.com` | Dual-membership | Internal + retail orgs |
| `sloane.reyes@acmecoffee.com` | Regional sales rep | Multiple managed orgs |

### Unique Name Pattern

```csharp
var uniqueName = $"Playwright Product {DateTime.UtcNow:yyyyMMddHHmmssfff}";
```

### Cleanup Pattern

```csharp
ProductSnapshot? original = null;
try
{
    original = await fixture.GetProductByNameAsync(userId, "...");
    // ... test mutations ...
    // ... verify via API ...
}
finally
{
    if (original is not null)
        await fixture.UpdateProductAsync(userId, original); // restore
}
```

---

## Risk Notes

- **MudBlazor rendering latency:** Blazor Server renders on the server
  and pushes diffs over SignalR. Use locator assertions (`ToBeVisibleAsync`,
  `ToHaveCountAsync`) rather than `WaitUntilState.NetworkIdle` alone.
- **MudDialog overlay:** Tests must target elements within
  `page.Locator("[role='dialog']")` to avoid clicking behind the overlay.
- **MudSelect/MudAutocomplete dropdowns:** Click the input to open the
  popover, then find and click the option within `.mud-popover-open`.
- **Filter panel debounce:** The 200ms debounce means tests must wait for
  grid reload after filter changes.
- **Report rendering:** Report output may render asynchronously. Wait for
  specific content rather than assuming completion on page load.
- **Blazor circuit disconnect:** Long-running tests may trigger circuit
  disconnect. Keep interactions flowing without long gaps.
- **Seed data assumptions:** Session 1 should include a diagnostic that
  validates minimum seed data counts before tests run.

## Downstream Impact

This session set completes the testing gap identified in the
`platform-ui-foundation-review` and `full-app-uat` session sets. After
completion, the platform has both the MudBlazor-native UI (from
`mudblazor-native-rebuild`) and comprehensive E2E proof (from this set),
forming a solid foundation for downstream Phase 2 work.
