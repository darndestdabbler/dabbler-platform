# E2E Persona And Route Matrix

> **Purpose:** Freeze the start context, route sequence, and key route outcomes
> for each approved representative workflow.
> **Created:** 2026-04-15
> **Source Inputs:** `docs/planning/e2e-executable-workflow-specs.md`,
> `docs/planning/e2e-representative-workflow-catalog.md`,
> `src/Apps/AcmeCoffeeOnline.Web/Components/Layout/MainLayout.razor`,
> `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Admin/UserManagement.razor`,
> `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Admin/ProductManagement.razor`,
> `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Orders/OrdersPage.razor`, and
> `tests/AcmeCoffeeOnline.Web.Tests/WebGuardrailTests.cs`.

---

## Workflow Matrix

| Workflow Id | Persona | Auth Mode | Start Org | Route Sequence | Key Route Outcome |
| --- | --- | --- | --- | --- | --- |
| `E2E-WF-04` | `SystemAdmin` | Seeded Session | Internal or system-wide admin context | `/` -> `/admin/products` | Product admin page loads and allows one browser-driven availability mutation |
| `E2E-WF-05` | `SystemAdmin` | Seeded Session | Internal or system-wide admin context | `/` -> `/admin/products` -> add-product dialog | Modal form opens, blocks one invalid submit path, then creates a product successfully |
| `E2E-WF-02` | Dual-membership internal plus customer user | Seeded Session | Internal organization | `/` -> `/customers` -> `Switch org` -> `/orders` -> `/admin/users` | Internal scope allows staff routes; retail-business scope changes shell context and denies `/admin/users` |
| `E2E-WF-01` | Regional sales rep with scoped `BackofficeUserAdmin` authority | Seeded Session | One non-manageable start scope, then a managed retail organization | `/` -> `/admin/users` -> `Switch org` -> `/admin/users` | Initial warning state blocks actions; switched scope shows banner and one grant or revoke path |
| `E2E-WF-03` | Retail-business `Customer` | Real Magic Link | Retail-business customer scope | `/login` -> magic-link completion -> `/orders` -> order-detail surface | Real auth reaches a scoped orders journey and denies a foreign order |

## Route Outcome Details

### `E2E-WF-04`

- `/admin/products` must remain `SystemAdmin`-only.
- The representative route proof is page reachability plus one row-level UI
  mutation.

### `E2E-WF-05`

- `/admin/products` remains the only required route.
- The dialog path is the primary browser-only seam, not additional route depth.

### `E2E-WF-02`

- `/customers` should succeed before the organization switch.
- `data-testid="active-organization-label"` should change after the switch.
- `/orders` should stay reachable in the customer scope.
- `/admin/users` should land on access denied after the switch.

### `E2E-WF-01`

- `/admin/users` should first show the warning state when the active scope
  lacks `CanManageUsers`.
- After switching to the managed retail organization, `/admin/users` should
  show `data-testid="admin-scope-banner"` with the active organization name.
- The workflow should prove one scoped admin action only.

### `E2E-WF-03`

- `/login` must remain the public entry route for the representative real-auth
  lane.
- `/orders` must open through the signed-in customer context, not through
  helper-only direct session injection.
- The foreign-order denial branch should end at access denied or not found, but
  it must not render foreign order data.

## UAT Inheritance Notes

Later `full-app-uat` work should inherit this same route and persona matrix
instead of redefining workflow entry points from scratch.

The likely human-confirmation add-ons are:

- clarity of the active-organization label after switching scope
- clarity of the `/admin/users` warning state before the allowed scope is
  chosen
- clarity of product-dialog validation messaging and keyboard flow
- clarity of the real magic-link sign-in experience
