# Security Dashboard Behavioral Fixes Spec

> **Purpose:** Resolve the three behavioral bugs surfaced by the
> `security-dashboard-uat` human pass — BackofficeUser sees no orders,
> SystemAdmin appears unable to edit orders, and elevating a user to
> SystemAdmin from the User Assignments tab silently fails.
> **Created:** 2026-04-17
> **Session Set:** `docs/session-sets/security-dashboard-behavioral-fixes/`
> **Prerequisite:** `security-dashboard-polish` complete. The UX pass is
> not strictly required to land first, but doing so keeps the UAT re-runs
> from entangling the two sets.
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Context

Three of the 13 UAT findings describe observed behaviors, not visual
polish. They need reproduction against real data before a fix can be
chosen, and they demand real-DB integration tests per the
[E2E testing standards memory](../../../.claude/projects/c--Users-denmi-source-repos-dabbler-platform/memory/feedback_e2e_testing_standards.md) —
no mocks.

UAT quotes (from
[`security-dashboard-uat-checklist.json`](../security-dashboard-uat/security-dashboard-uat-checklist.json)):

1. **Orders visibility** — "Jordan Ellis can't see any orders" when
   signed in as `jdoe@acmecoffee.com` (a BackofficeUser). Also reported
   on the Control Permissions Runtime Change row.
2. **SystemAdmin cannot edit Orders** — "It doesn't seem that I can make
   edits to Orders as a SystemAdmin. Products is OK though."
3. **SystemAdmin elevation silently fails** — "I tried to elevate Jordan
   Ellis to SystemAdmin, and that did not work."

The remediation plan, with diagnostic notes and file pointers, is in
[`.claude/plans/ok-i-finished-the-ancient-llama.md`](../../../.claude/plans/ok-i-finished-the-ancient-llama.md).

## Goals

1. BackofficeUser `jdoe@acmecoffee.com` sees the orders they are entitled
   to on `/orders`, and the rule governing that visibility is documented
   either in `security-config.json` (as a row filter) or in the
   controller's authorization logic.
2. SystemAdmin has a clearly discoverable path to edit an Order (status,
   line items — exact scope to be clarified in Session 2 kickoff), and
   the edit path is covered by a Playwright test.
3. Assigning SystemAdmin to a user from User Assignments persists to the
   user-role store, takes effect after the target user re-authenticates,
   and is covered by a bUnit test (UI flow) and a real-DB integration
   test (store write).

## Architecture Context

### Files that will be investigated

- `src/Apps/AcmeCoffeeOnline.Api/Controllers/OrdersController.cs`
- `src/Apps/AcmeCoffeeOnline.Api/Services/` (order service + row-filter
  pipeline, whichever applies)
- `src/Apps/AcmeCoffeeOnline.Web/Data/OrderDataProvider.cs`
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Orders/OrdersPage.razor`
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Orders/OrderList.razor`
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Orders/OrderDetail.razor`
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Orders/OrderDetailPage.razor`
- `src/Apps/AcmeCoffeeOnline.Web/security-config.json` (row filters,
  endpoint permissions)
- `src/Libs/Dabbler.Security/Persistence/EfCoreUserRoleStore.cs`
- `src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerUserRoleManager.razor`
- `src/Libs/Dabbler.Security/` (claims refresh, cookie rebuild if relevant)

### Known facts from earlier exploration

- No row filter on Order for BackofficeUser in the current config. Only
  the Customer-role filter exists.
- `OrderDataProvider` does not apply a role-based filter; it delegates
  entirely to the API.
- `OrderList.razor` is `ReadOnly="true"`; edits happen in the detail
  page, so "can't edit" may be a UX discoverability issue rather than an
  authorization bug.
- `EfCoreUserRoleStore.AddRoleAsync` has no SystemAdmin-specific gate at
  the persistence layer.

Do not assume any of these are the root cause. Session 1 reproduces
each bug before changing code.

---

## Session Plan

### Session 1 of 3: Orders visibility for BackofficeUser

**Steps:**
1. Read `docs/planning/guiding-principles.md` and
   `docs/planning/lessons-learned.md`.
2. Reproduce the UAT observation: sign in as `jdoe@acmecoffee.com`, hit
   `/orders`, capture what the UI shows and what
   `GET /api/orders` returns (status, count, payload shape).
3. If the API returns zero rows, trace server-side: is
   `GetOrders` requiring a role jdoe doesn't have? Is a row filter being
   applied via the Dabbler.Security entity pipeline? Is the SQL
   `WHERE` clause restricting by `CustomerId` based on the caller's
   principal?
4. If the API returns rows but the UI drops them, trace
   `OrderDataProvider` → `OrderList.razor`.
5. Decide whether BackofficeUser should see all orders, orders in their
   branch/org, or something narrower. Default recommendation: all
   orders, since they are office staff. Confirm with the human before
   coding if the rule is not already written down.
6. Fix the identified root cause.
7. Add a real-DB integration test: seed a BackofficeUser and a few
   orders; assert the API returns them. No mocks.
8. Add a Playwright test: jdoe signs in, navigates to `/orders`, sees a
   non-empty grid.
9. Build and test. Log via `log.log_step()`.

**Creates:** Integration test, Playwright test, possibly a row filter in
`security-config.json` or a controller fix
**Touches:** `OrdersController.cs`, `security-config.json`, potentially
`OrderDataProvider.cs`
**Ends with:** jdoe sees the expected orders; rule is documented in
config or test.
**Progress keys:** `session-1/repro`, `session-1/trace`,
`session-1/decision`, `session-1/fix`, `session-1/integration-test`,
`session-1/playwright`, `session-1/build-and-test`

---

### Session 2 of 3: Order edit path for SystemAdmin

**Steps:**
1. Read planning docs.
2. Kickoff clarification (via session prompt): ask the human which
   edit they expected to perform — order status, line items, something
   else? `OrderList` is read-only by design; the edit path is in
   `OrderDetail.razor`. If the expectation is grid-inline editing, scope
   a small change. If it is detail-page editing, investigate why the
   buttons appear not to work for SystemAdmin.
3. Reproduce under SystemAdmin: sign in as `admin@acmecoffee.com`,
   click an order, attempt the expected edit, capture the failure.
4. If the detail-page buttons are disabled or hidden, trace Control
   Permissions for `UpdateStatusButton` / `DeleteOrderButton` /
   `EditOrderButton` (as applicable) and confirm SystemAdmin resolution.
5. If the buttons are enabled but the mutation fails, trace through
   `OrdersController` and check `UpdateOrderStatus` endpoint permissions
   plus any entity-level Update gate.
6. Fix the identified root cause.
7. Add a real-DB integration test exercising the SystemAdmin edit path.
8. Add a Playwright test: admin signs in, edits an order, assert the DB
   reflects the change.
9. Build and test. Log via `log.log_step()`.

**Creates:** Integration test, Playwright test; possibly a
`security-config.json` fix or a UI fix on the detail page
**Touches:** `OrderDetail.razor`, `OrderDetailPage.razor`, possibly
`OrdersController.cs`, possibly `security-config.json`
**Ends with:** SystemAdmin has a clear, working edit path and it is
covered end-to-end.
**Progress keys:** `session-2/clarify-scope`, `session-2/repro`,
`session-2/trace`, `session-2/fix`, `session-2/integration-test`,
`session-2/playwright`, `session-2/build-and-test`

---

### Session 3 of 3: SystemAdmin role elevation

**Steps:**
1. Read planning docs.
2. Reproduce: as admin, open User Assignments, select Jordan Ellis,
   assign SystemAdmin, save. Capture what the UI shows immediately,
   what `DabblerUserRoleManager` calls, and what
   `EfCoreUserRoleStore.AddRoleAsync` writes (or does not write).
3. Determine the root cause. Hypotheses, in priority order:
   a. UI state resets before Save fires (select value dropped between
      binding and button click).
   b. Save succeeds but the UI doesn't refresh the row, giving the
      appearance of failure.
   c. The elevated user's claims cookie doesn't refresh, so even after
      a successful save, a new nav to `/admin/security` still denies.
   d. A store-level validation we haven't found yet.
4. Fix the identified root cause. If it is (c), decide whether to
   force sign-out on role change or surface an instruction to the user.
5. Add a bUnit test covering the UI save flow.
6. Add a real-DB integration test asserting
   `EfCoreUserRoleStore.AddRoleAsync` writes the assignment and
   `GetRolesAsync` returns it.
7. Add a Playwright test: admin elevates a test user; the test user
   signs in in a second context; the admin nav appears. If sign-out is
   required for claims refresh, the test exercises that too.
8. Build and test. Log via `log.log_step()`.

**Creates:** bUnit test, integration test, Playwright test, possibly a
claims-refresh helper
**Touches:** `DabblerUserRoleManager.razor`, possibly
`EfCoreUserRoleStore.cs`, possibly claims/cookie infrastructure
**Ends with:** SystemAdmin elevation from the UI works end-to-end with
visible confirmation and the target user sees elevated access after
re-authenticating.
**Progress keys:** `session-3/repro`, `session-3/root-cause`,
`session-3/fix`, `session-3/bunit`, `session-3/integration-test`,
`session-3/playwright`, `session-3/build-and-test`

---

## Human Re-run After Session 3

After session 3 completes, hand back to the human for a targeted re-run
of these rows in `security-dashboard-uat-checklist.json`:

- Entity Access Tab — Runtime Editing
- Control Permissions Tab — Runtime Change
- User Assignments Tab — Role Assignment Editing
- Cross-Cutting Quality — Persistence Round-Trip

## What This Set Preserves

| Existing Asset | Status |
|---|---|
| All existing Playwright tests | Must continue passing |
| All non-Playwright tests | Must continue passing |
| Row-filter pipeline for the Customer role on Order | Preserved |
| `DabblerPageAuthorize` attribute and usage | Preserved |

## Risk Notes

- **Real-DB tests are mandatory.** Per the E2E standards memory, the
  integration tests added here must hit a real database instance, not a
  mock. Use the existing test database setup used elsewhere in
  `tests/`.
- **Claims refresh is subtle.** If session 3 concludes that claims
  caching is the cause of the silent elevation failure, be careful with
  the fix: forcing a sign-out of the target user from an admin action
  is a privileged side effect and should be explicit (confirmation
  dialog) rather than silent.
- **Seeded order data.** Session 1's repro depends on enough seeded
  orders existing for a BackofficeUser path. If the seed has a gap,
  widen it as part of session 1 rather than chasing a phantom bug.
