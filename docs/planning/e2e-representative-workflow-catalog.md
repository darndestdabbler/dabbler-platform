# Representative E2E Workflow Catalog

> **Purpose:** Expand the seed mini-workflow into the approved representative
> Playwright workflow catalog for `docs/session-sets/e2e-workflow-design/`.
> **Created:** 2026-04-15
> **Source Inputs:** `docs/planning/example-of-end-to-end-mini-workflow-test.md`,
> `docs/planning/e2e-workflow-scope-ledger.md`,
> `docs/planning/e2e-workflow-traceability-rules.md`,
> `docs/planning/app-coverage-conformance-accepted-baseline.md`,
> `docs/planning/multi-org-security-accepted-baseline.md`,
> `docs/planning/distributor-acceptance-baseline.md`,
> `docs/planning/user-functionality-notes.md`,
> `tests/AcmeCoffeeOnline.Playwright/SmokeTests.cs`,
> `tests/AcmeCoffeeOnline.Web.Tests/WebGuardrailTests.cs`, and routed Session 2
> analysis.

---

## Design Intent

This catalog replaces the generic example mini-workflow with a repo-specific
representative set.

The goal is not to automate every major route. The goal is to freeze the small
set of browser workflows that still earn their keep after the accepted unit,
integration, bUnit, smoke, and guardrail baselines.

## Candidate Catalog

| Workflow Id | Candidate | Why It Still Belongs In Playwright | Recommendation |
| --- | --- | --- | --- |
| `E2E-WF-01` | Regional sales rep switches retail-business scope and grants or removes scoped `UserAdmin` on `/admin/users`. | Proves the shell organization switcher, active-organization claims, admin grid actions, snackbar feedback, and persisted role assignment all agree in one browser-driven path. | Keep |
| `E2E-WF-02` | Dual-membership user switches from internal `BackofficeUser` context to retail-business `Customer` context and observes role-aware navigation and route outcomes change. | Proves the real shell reacts to an organization switch by changing available navigation and route access, not only service-level scope logic. | Keep |
| `E2E-WF-03` | Retail-business user signs in through the real magic-link path, opens the orders workflow, and confirms order detail stays scoped to that retail business. | Extends the current sign-in smoke into a true browser journey with order-list interaction and a direct-navigation denial branch. | Keep |
| `E2E-WF-04` | `SystemAdmin` toggles product availability from `/admin/products` and the backend reflects the change. | Replaces the current helper-driven availability mutation with a UI-driven mutation and backend-truth confirmation. | Keep |
| `E2E-WF-05` | `SystemAdmin` opens the add-product dialog, uses keyboard navigation, hits representative validation boundaries, then creates the product successfully. | Gives the suite one deliberate browser-only form-wiring and focus path instead of many field-rule permutations. | Keep |
| `E2E-WF-06` | Internal staff updates order status or order notes from the order-detail surface. | Browser value is real, but the current suite first needs stable detail-entry and reset helpers for shell tab flows. | Defer |
| `E2E-WF-07` | Reports workflow for `/reports/orders-by-customer` or `/reports/sales-by-product`, including export affordances. | May become valuable later, but current evidence does not show a browser-specific confidence gap large enough to outrank admin, scope, and mutation flows. | Defer |
| `E2E-WF-08` | Repeat route inventory checks for `/customers`, `/products/catalog`, `/admin/security`, and `/admin/products` across more personas. | Current smoke and guardrail tests already cover the focused route-access story. More of the same would add breadth without a new seam. | Exclude |
| `E2E-WF-09` | End-to-end organization creation or organization management UI. | No current browser surface exists for this workflow in the app, so including it would invent scope instead of designing from reality. | Exclude |

## Frozen Playwright Subset

The approved implementation subset for the downstream
`e2e-workflow-implementation` set is:

| Workflow Id | Scenario | Primary Value |
| --- | --- | --- |
| `E2E-WF-01` | Regional sales rep scoped user administration | Multi-organization admin mutation with backend truth |
| `E2E-WF-02` | Dual-membership shell scope shift | Role-aware navigation and route behavior |
| `E2E-WF-03` | Retail-business magic-link orders journey | Real auth plus scoped read path |
| `E2E-WF-04` | System-admin product availability toggle | Browser-driven mutation with backend truth |
| `E2E-WF-05` | System-admin add-product validation and keyboard path | Representative validation and browser-only focus wiring |

## Workflow Records

### `E2E-WF-01` Regional Sales Rep Scoped User Administration

- `Workflow Id`: `E2E-WF-01`
- `Persona`: regional sales rep seeded with `BackofficeUserAdmin` membership in
  at least two retail-business organizations
- `Auth Mode`: Seeded Session
- `Primary Routes`: `/`, `/admin/users`, `/auth/switch-organization`
- `Business Goal`: prove that a scoped admin can manage user-admin elevation in
  the active retail-business organization without gaining cross-organization
  reach
- `Why Playwright`: lower layers already prove scope rules; this workflow proves
  the real shell switcher, admin page, action buttons, snackbar feedback, and
  persisted assignment work together
- `Preconditions And Seed Data`: one regional-sales-rep persona with at least
  two retail organizations; one target user in each retail organization; one
  target eligible for grant or revoke in the allowed organization
- `Navigation Expectations`: sign in, confirm active organization in the shell,
  open `Admin -> User Management`, switch to the target retail organization,
  verify the scope banner and user grid update to the new organization
- `Validation Expectations`: representative branch is action gating, not broad
  form validation; the page should show only the grant or revoke buttons the
  active scope allows
- `Organization And Permission Expectations`: grant or revoke succeeds only for
  the active retail organization; switching away removes access to the prior
  organization's target rows
- `Backend-Truth Checks`: confirm the target membership gained or lost the
  `BackofficeUserAdmin` assignment in the active organization only
- `Lower-Layer Coverage Already Present`: API and integration tests in
  `tests/AcmeCoffeeOnline.Api.Tests/AdminUserScopeTests.cs`; focused bUnit page
  behavior in `tests/AcmeCoffeeOnline.Web.Tests/Pages/Admin/UserManagementPageTests.cs`
- `Browser-Only Checks`: shell org-switch interaction, banner refresh, visible
  grid scope, and UI feedback after action
- `Out Of Scope`: creating brand-new users, broad role editing, or repeating
  the same grant or revoke path for every persona
- `Happy Path`: switch into a managed retail organization, grant scoped
  `UserAdmin` to an eligible target, and confirm the grid plus backend reflect
  the new assignment
- `Representative Edge Branch`: start in a context without `CanManageUsers`
  and confirm the warning state blocks admin actions until the user returns to
  an authorized organization

### `E2E-WF-02` Dual-Membership Shell Scope Shift

- `Workflow Id`: `E2E-WF-02`
- `Persona`: dual-membership user with internal `BackofficeUser` access and
  retail-business `Customer` access under one email address
- `Auth Mode`: Seeded Session
- `Primary Routes`: `/`, `/customers`, `/orders`, `/admin/users`,
  `/auth/switch-organization`
- `Business Goal`: prove that active-organization switching changes both shell
  navigation and route outcomes for one signed-in user
- `Why Playwright`: this is the clearest browser-only proof that navigation and
  redirects react to a live organization switch instead of a stubbed auth state
- `Preconditions And Seed Data`: dual-membership persona with an internal
  default organization and one retail-business membership
- `Navigation Expectations`: sign in at internal scope, verify internal-staff
  affordances such as `/customers`; switch to retail-business scope; confirm
  the active organization label changes and navigation becomes customer-shaped
- `Validation Expectations`: no extra field validation; the representative
  branch is route and navigation state change
- `Organization And Permission Expectations`: internal scope can reach
  `/customers`; retail-business scope loses `Admin` access and receives route
  denial when navigating directly to `/admin/users`
- `Backend-Truth Checks`: not required because the value here is shell and
  permission behavior rather than a persisted mutation
- `Lower-Layer Coverage Already Present`: shell session loading tests in
  `tests/AcmeCoffeeOnline.Web.Tests/Services/ShellSessionStateLoaderTests.cs`,
  guardrail org-switch coverage in
  `tests/AcmeCoffeeOnline.Web.Tests/WebGuardrailTests.cs`, and navigation data
  provider unit tests in
  `tests/AcmeCoffeeOnline.Web.Tests/Data/AcmeCoffeeNavigationDataProviderTests.cs`
- `Browser-Only Checks`: visible navigation change, active-organization label
  update, and direct-route denial after switching scope
- `Out Of Scope`: role-switcher behavior, Entra ID organization-login flow, or
  every possible menu permutation
- `Happy Path`: internal scope can open `/customers`; after switching to the
  retail-business organization, the same user can still open `/orders` but no
  longer has admin access
- `Representative Edge Branch`: direct navigation to `/admin/users` after the
  switch lands on access denied rather than stale authorized content

### `E2E-WF-03` Retail-Business Magic-Link Orders Journey

- `Workflow Id`: `E2E-WF-03`
- `Persona`: retail-business user in the accepted `Customer` role
- `Auth Mode`: Real Magic Link
- `Primary Routes`: `/login`, `/orders`, order-detail surface opened from the
  orders query page
- `Business Goal`: prove the real customer-facing sign-in path reaches a scoped
  orders workflow and preserves row filtering through detail view
- `Why Playwright`: the current smoke test proves sign-in and route access, but
  not a richer browser journey through the order list and detail interaction
- `Preconditions And Seed Data`: magic-link inbox capture enabled; seeded
  orders for the retail-business user; at least one order belonging to another
  retail business for the denial branch
- `Navigation Expectations`: request magic link, follow the emailed link, open
  `/orders`, choose a visible order, and open its detail surface through the UI
- `Validation Expectations`: representative check is result correctness rather
  than many filter permutations; the list should show only orders for the
  signed-in retail business
- `Organization And Permission Expectations`: the customer can read only their
  own orders; direct navigation to another organization's order detail stays
  blocked
- `Backend-Truth Checks`: compare the displayed order identity or count to a
  scoped API read for the same session, and confirm the blocked order does not
  become visible
- `Lower-Layer Coverage Already Present`: customer row-filter and scope proof in
  API tests, plus current smoke coverage in
  `tests/AcmeCoffeeOnline.Playwright/SmokeTests.cs`
- `Browser-Only Checks`: real magic-link completion, list interaction, and
  detail navigation from the browser UI
- `Out Of Scope`: full retail-order placement, exhaustive grid filtering, or
  multiple customer personas
- `Happy Path`: sign in through magic link, open the orders workflow, and reach
  one scoped order detail through the UI
- `Representative Edge Branch`: attempt direct navigation to a known
  out-of-scope order and confirm the user sees not-found or access-denied
  behavior instead of order data

### `E2E-WF-04` System-Admin Product Availability Toggle

- `Workflow Id`: `E2E-WF-04`
- `Persona`: `SystemAdmin`
- `Auth Mode`: Seeded Session
- `Primary Routes`: `/admin/products`
- `Business Goal`: prove that system administration can change catalog
  availability through the browser UI and persist that change
- `Why Playwright`: the repo already proves backend mutation with helper calls;
  this workflow adds the missing browser-driven control path
- `Preconditions And Seed Data`: one stable product record reserved for toggle
  verification and safe reset
- `Navigation Expectations`: open `Admin -> Product Management`, search for the
  sentinel product, use the availability switch, and observe the UI refresh
- `Validation Expectations`: the representative branch is a successful toggle;
  this workflow is not the place for add-product field validation
- `Organization And Permission Expectations`: only `SystemAdmin` should reach
  `/admin/products`; page access is itself part of the setup guard
- `Backend-Truth Checks`: query the product API before and after the click and
  verify `IsAvailable` changed, then restore the original state in teardown
- `Lower-Layer Coverage Already Present`: product service and API behavior plus
  the current Playwright helper-based toggle in
  `tests/AcmeCoffeeOnline.Playwright/SmokeTests.cs`
- `Browser-Only Checks`: use of the availability switch inside the real admin
  grid and visual confirmation that the row state changed
- `Out Of Scope`: exhaustive product CRUD, pagination inventory, or repeated
  mutation across many products
- `Happy Path`: toggle one stable product from available to unavailable or back
  and confirm persisted state
- `Representative Edge Branch`: none beyond teardown reset; the value of this
  workflow is the single UI-driven mutation

### `E2E-WF-05` System-Admin Add-Product Validation And Keyboard Path

- `Workflow Id`: `E2E-WF-05`
- `Persona`: `SystemAdmin`
- `Auth Mode`: Seeded Session
- `Primary Routes`: `/admin/products`
- `Business Goal`: prove one representative modal form path with real keyboard
  navigation, validation wiring, and successful persistence
- `Why Playwright`: lower layers can prove validation rules, but only a browser
  can prove focus order, modal behavior, blocked create, and eventual create
  from the real form
- `Preconditions And Seed Data`: unique product-name prefix for the test run and
  a cleanup helper that can find or remove the created product
- `Navigation Expectations`: open `Admin -> Product Management`, launch the
  add-product dialog, confirm initial focus lands on the first field, move
  through the main controls with `Tab`, and submit only after entering valid
  values
- `Validation Expectations`: use one invalid branch such as blank `Name`,
  `UnitPrice` below `0.01`, or invalid `ImageUrl`; the create action should
  remain blocked or validation messaging should appear until the values are
  fixed
- `Organization And Permission Expectations`: only `SystemAdmin` should see the
  page and the add-product capability
- `Backend-Truth Checks`: after the successful submit, confirm the new product
  exists through the API and matches the entered values needed for later reset
- `Lower-Layer Coverage Already Present`: data-annotation rules on
  `src/Apps/AcmeCoffeeOnline.Shared/DTOs/ProductDto.cs` and product API/service
  proof
- `Browser-Only Checks`: modal open behavior, focus landing, keyboard path
  across fields, and blocked submit before valid state
- `Out Of Scope`: every field-rule permutation, edit-mode variants, or visual
  polish review
- `Happy Path`: create one uniquely named product successfully after entering a
  valid minimal set of values
- `Representative Edge Branch`: start with an invalid product payload so the
  modal proves validation and blocked create before the values are corrected

## Explicit Exclusions

- Do not expand the suite into route-by-route inventory coverage.
- Do not re-prove lower-layer role or row-filter matrices already covered in
  integration and focused bUnit tests.
- Do not treat report routes as mandatory browser proof until a later set shows
  a browser-specific confidence gap.
- Do not introduce workflows for browser surfaces that the current app does not
  actually expose yet.
