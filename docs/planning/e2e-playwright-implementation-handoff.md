# E2E Playwright Implementation Handoff

> **Purpose:** Give `docs/session-sets/e2e-workflow-implementation/` a concrete
> starting slice, fixture backlog, and scope guard for the approved
> representative workflow set.
> **Created:** 2026-04-15
> **Source Inputs:** `docs/planning/e2e-representative-workflow-catalog.md`,
> `docs/planning/e2e-layered-coverage-matrix.md`,
> `docs/planning/e2e-workflow-scope-ledger.md`, and the current Web, API, and
> Playwright test code.

---

## Approved Workflow Order

Implement in this order unless stabilization work forces a temporary reorder:

1. `E2E-WF-04` system-admin product availability toggle
2. `E2E-WF-05` system-admin add-product validation and keyboard path
3. `E2E-WF-02` dual-membership shell scope shift
4. `E2E-WF-01` regional sales rep scoped user administration
5. `E2E-WF-03` retail-business magic-link orders journey

This order gives the next session set one contained admin mutation first, then
reuses the shell and persona helpers for the organization-aware workflows.

## Seed And Fixture Prerequisites

- Add or verify one stable product sentinel record whose availability can be
  toggled safely and then restored during teardown.
- Add a unique-name product cleanup helper for products created by
  `E2E-WF-05`.
- Add a dual-membership seeded persona that starts in internal scope and can
  switch into one retail-business `Customer` scope.
- Add a regional-sales-rep seeded persona with `BackofficeUserAdmin` authority
  in at least two retail-business organizations. This is stricter than the
  current narrow guardrail fixture and is required for the intended
  representative multi-organization admin proof.
- Add one eligible target membership for grant or revoke in each managed
  retail-business organization used by `E2E-WF-01`.
- Add one retail-business order fixture for the magic-link customer and one
  known out-of-scope order ID for the denial branch in `E2E-WF-03`.

## Helper And Harness Expectations

- Reuse seeded-session sign-in and magic-link helpers instead of introducing
  new auth backdoors.
- Add shell-level helpers for reading the active-organization label and opening
  the `Switch org` menu through the browser UI.
- Add API-backed verification helpers for:
  `products` search by name,
  product availability readback,
  scoped admin role assignment lookup, and
  scoped order lookup for the signed-in retail user.
- Prefer helper-backed setup and teardown only. The primary workflow action
  should still happen in the browser.

## Workflow-Specific Notes

### `E2E-WF-04`

- Use the current product-availability smoke test as the backend-truth seed.
- Move the mutation trigger from API helper to UI switch interaction.
- Always restore the sentinel product to its original availability in teardown.

### `E2E-WF-05`

- Treat this as the suite's one deliberate keyboard or focus workflow.
- Keep validation narrow: one invalid branch, then one success branch.
- Verify the created product through API lookup and clean it up after the test.

### `E2E-WF-02`

- Keep the navigation assertions focused on routes that already exist:
  `/customers`, `/orders`, and `/admin/users`.
- The core proof is that switching organizations changes the shell and route
  outcomes for one user without re-authentication.

### `E2E-WF-01`

- Reuse the org-switch helper from `E2E-WF-02`.
- Target one grant path and one blocked state or revoke path, not a full role
  management matrix.
- Backend verification should assert organization-scoped role assignment, not
  only snackbar text.

### `E2E-WF-03`

- Keep the real magic-link lane because the guardrails require at least one
  representative real-auth workflow.
- The detail-entry assertion should come from UI navigation inside the orders
  workflow, not from direct API helper mutation.
- The denial branch should use a known out-of-scope order ID and confirm no
  foreign order data is rendered.

## Scope Guard

- Do not grow the suite into every route that already appears in smoke tests.
- Do not add organization-creation or other browser workflows that the current
  app does not expose.
- Do not multiply validation cases once one representative modal path exists.
- Defer report workflows and deeper order-detail mutations until the approved
  subset above is stable and proven valuable.
