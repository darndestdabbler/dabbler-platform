# Executable E2E Workflow Specs

> **Purpose:** Turn the approved representative workflow catalog into
> implementation-ready scenario specs for the downstream
> `e2e-workflow-implementation` session set.
> **Created:** 2026-04-15
> **Source Inputs:** `docs/planning/e2e-representative-workflow-catalog.md`,
> `docs/planning/e2e-layered-coverage-matrix.md`,
> `docs/planning/e2e-playwright-implementation-handoff.md`,
> `src/Apps/AcmeCoffeeOnline.Web/Components/Layout/MainLayout.razor`,
> `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Admin/ProductManagement.razor`,
> `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Admin/UserManagement.razor`,
> `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Orders/OrdersPage.razor`,
> `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Orders/OrderList.razor`, and
> `tests/AcmeCoffeeOnline.Playwright/Infrastructure/PlaywrightFixture.cs`.

---

## Shared Browser Hooks Already Present

The current app already exposes a small set of stable browser hooks that the
implementation session should prefer before introducing new test IDs:

| Surface | Current Hook |
| --- | --- |
| Main authenticated shell | `data-testid="main-shell"` |
| Active organization summary | `data-testid="active-organization-summary"` |
| Active organization label | `data-testid="active-organization-label"` |
| Organization switch entry | button labeled `Switch org` in `MainLayout.razor` |
| Admin scope banner | `data-testid="admin-scope-banner"` on `/admin/users` |
| Access denied proof | `data-testid="access-denied-page"` |
| Auth shell proof | `data-testid="auth-shell"` |

Implementation should reuse these hooks where possible and add new explicit
test IDs only when the existing text or role-based selectors are not durable
enough for a representative workflow.

## Workflow Specs

### `E2E-WF-04` System-Admin Product Availability Toggle

- `Persona`: `SystemAdmin` via seeded session
- `Primary UI`: `/admin/products`
- `Current UI Grounding`: `ProductManagement.razor` already exposes the search
  field, the `Add Product` button, the product grid, and a row-level
  availability `MudSwitch`
- `Existing Helper Reuse`: `PlaywrightFixture.SeededSignInAsync`,
  `GetSessionInfoAsync`, `GetApiJsonAsync`, and `PatchApiAsync`
- `Happy Path`:
  1. Sign in as `admin@acmecoffee.com`
  2. Open `/admin/products`
  3. Search for the sentinel product by exact name
  4. Read its current `IsAvailable` value through the API helper
  5. Toggle the row switch through the browser UI
  6. Assert success feedback and the visible row state change
  7. Re-read the product through the API helper and confirm
     `IsAvailable != before`
- `Backend Verification`: API lookup by product name or product ID before and
  after the click
- `Reset Requirement`: restore the original availability in teardown even on
  assertion failure
- `Representative Failure Branch`: none beyond guaranteed teardown reset
- `Implementation Note`: if the current switch selector is too brittle, add a
  narrow product-row test ID on the admin grid rather than widening the test to
  pagination or bulk actions

### `E2E-WF-05` System-Admin Add-Product Validation And Keyboard Path

- `Persona`: `SystemAdmin` via seeded session
- `Primary UI`: `/admin/products`
- `Current UI Grounding`: `ProductManagement.razor` already exposes an inline
  `MudDialog` with `Name`, `Roast Level`, `Caffeine`, `Grind Type`,
  `Unit Price`, `Available`, `Description`, and `Image URL`, plus a disabled
  `Create` button when the form is invalid
- `Happy Path`:
  1. Sign in as `admin@acmecoffee.com`
  2. Open `/admin/products`
  3. Open the `Add Product` dialog from the browser UI
  4. Confirm initial focus lands on the first editable field
  5. Use `Tab` to move across the main dialog controls in order
  6. Exercise one invalid branch such as blank `Name` or `Unit Price < 0.01`
  7. Confirm submit stays blocked or validation messaging appears
  8. Correct the values and create one uniquely named product
  9. Confirm success feedback and API visibility of the created product
- `Backend Verification`: API lookup by unique product name and comparison of
  the persisted values that matter for cleanup
- `Reset Requirement`: delete or otherwise neutralize the created product by
  unique-name helper after the test
- `Representative Failure Branch`: one blocked-submit validation path only
- `Implementation Note`: if MudBlazor focus behavior proves hard to observe
  reliably, assert focus on the first field and one later control rather than
  turning this into a full tab-order inventory

### `E2E-WF-02` Dual-Membership Shell Scope Shift

- `Persona`: one email address with internal `BackofficeUser` membership and
  retail-business `Customer` membership
- `Primary UI`: `/`, `/customers`, `/orders`, `/admin/users`, and the shell
  `Switch org` menu
- `Current UI Grounding`: `MainLayout.razor` already renders the active
  organization label and `Switch org` menu; existing smoke and guardrail tests
  already prove `/customers`, `/orders`, and `/admin/users` are routed surfaces
- `Happy Path`:
  1. Sign in through seeded session using the dual-membership persona
  2. Assert the initial active organization label shows internal scope
  3. Open `/customers` and confirm access succeeds in internal scope
  4. Open the shell `Switch org` menu and choose the retail-business
     organization
  5. Assert the active organization label changes
  6. Open `/orders` and confirm access succeeds in retail-business scope
  7. Confirm internal-staff-only affordances are no longer visible or no longer
     reachable
- `Backend Verification`: not required
- `Reset Requirement`: none beyond sign-out because organization switching is
  session-scoped
- `Representative Edge Branch`: direct navigation to `/admin/users` after the
  switch lands on access denied instead of stale authorized content
- `Implementation Note`: prefer browser-driven switching through the menu over
  helper-only route navigation so the test proves the shell interaction itself

### `E2E-WF-01` Regional Sales Rep Scoped User Administration

- `Persona`: regional sales rep with scoped `BackofficeUserAdmin` access in at
  least two retail-business organizations
- `Primary UI`: `/admin/users` and the shell `Switch org` menu
- `Current UI Grounding`: `UserManagement.razor` already renders a warning
  state when `CanManageUsers` is false, a scope banner when access is allowed,
  and action buttons for `Grant UserAdmin` or `Remove UserAdmin`
- `Happy Path`:
  1. Sign in through seeded session using the multi-organization regional sales
     rep persona
  2. Start in a scope without `CanManageUsers` and confirm the warning state
     blocks elevation actions
  3. Switch to one managed retail organization through the shell UI
  4. Open `/admin/users` and confirm the scope banner reflects the chosen
     organization
  5. Trigger one `Grant UserAdmin` or `Remove UserAdmin` action from the grid
  6. Assert success snackbar text and updated row state
  7. Verify through backend lookup that the targeted membership changed only in
     the active organization
  8. Switch to a second organization and confirm the prior target row or action
     does not leak into the new scope
- `Backend Verification`: organization-scoped role assignment lookup for the
  targeted membership before and after the UI action
- `Reset Requirement`: revert the grant or revoke so seeded role state remains
  reusable
- `Representative Edge Branch`: initial unauthorized or non-manageable scope
  warning on `/admin/users`
- `Implementation Note`: if selector stability is poor inside the current grid,
  add row-level IDs keyed by organization membership ID rather than automating
  broad filter behavior

### `E2E-WF-03` Retail-Business Magic-Link Orders Journey

- `Persona`: retail-business `Customer`
- `Auth Mode`: real magic-link flow
- `Primary UI`: `/login`, `/orders`, and the order-detail surface reached from
  the orders workflow
- `Current UI Grounding`: `OrdersPage.razor` renders the orders page shell and
  `OrderList.razor` renders the query-page grid; current smoke coverage already
  proves magic-link sign-in and `/orders` route access
- `Happy Path`:
  1. Clear pending magic-link messages for the customer email
  2. Request a sign-in link from `/login`
  3. Wait for the dev-inbox magic-link payload and complete sign-in through the
     browser
  4. Open `/orders`
  5. Choose a visible order from the grid and enter the detail surface through
     the UI rather than by constructing the destination directly
  6. Confirm the visible order identity matches a scoped API read for the same
     session
- `Backend Verification`: scoped API read of the customer's visible orders plus
  one known out-of-scope order ID for the denial branch
- `Reset Requirement`: none for read-only coverage
- `Representative Edge Branch`: direct navigation to a known foreign order
  stays blocked and does not render foreign data
- `Implementation Note`: the current design intentionally leaves the precise
  order-detail entry gesture open until implementation confirms whether the
  query page opens detail by row click, tab navigation, or another existing UI

## Shared Assertion Discipline

- Keep one representative edge branch per workflow.
- Prefer browser-visible proof plus one backend confirmation over many UI-only
  assertions.
- Use helper-backed setup and teardown only; the primary user action must still
  happen in the browser.
- If implementation discovers missing selectors, add narrowly scoped test IDs
  on the specific workflow seam instead of broad instrumentation across every
  page.
