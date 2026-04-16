# E2E Representative Suite Baseline

> **Purpose:** Capture the stabilized representative Playwright baseline at the
> end of Session 4 for `docs/session-sets/e2e-workflow-implementation/`.
> **Created:** 2026-04-16
> **Source Inputs:** `docs/planning/e2e-playwright-fixture-strategy.md`,
> `docs/planning/e2e-workflow-implementation-review-ledger.md`,
> `docs/planning/e2e-persona-route-matrix.md`,
> `docs/planning/e2e-executable-workflow-specs.md`, and the current
> `tests/AcmeCoffeeOnline.Playwright/` workflow coverage.

---

The representative Playwright suite remains intentionally small, aligned to the
approved workflow design, and stable enough to hand off to the later
`full-app-uat` session set.

The final workflow inventory still matches the frozen design order:
`E2E-WF-04` -> `E2E-WF-05` -> `E2E-WF-02` -> `E2E-WF-01` -> `E2E-WF-03`.

## Workflow Inventory

- `E2E-WF-04` uses the `SystemAdmin` seeded-session persona on
  `/admin/products`, lives in
  `tests/AcmeCoffeeOnline.Playwright/ProductAdministrationWorkflowTests.cs`,
  and proves browser-driven product-availability mutation with API readback and
  teardown restore of the sentinel product state.
- `E2E-WF-05` uses the `SystemAdmin` seeded-session persona on
  `/admin/products` plus the add-product dialog, lives in
  `tests/AcmeCoffeeOnline.Playwright/ProductAdministrationWorkflowTests.cs`,
  and proves the browser-only focus and keyboard seam, one blocked validation
  branch, created-product API verification, and cleanup of automation-owned
  product rows.
- `E2E-WF-02` uses the dual-membership internal-plus-customer seeded persona
  across `/customers`, the shell `Switch org` menu, `/orders`, and
  `/admin/users`, lives in
  `tests/AcmeCoffeeOnline.Playwright/MultiOrganizationWorkflowTests.cs`, and
  proves real shell scope switching plus access-denied behavior after the user
  leaves internal scope.
- `E2E-WF-01` uses the regional sales rep persona with scoped
  `BackofficeUserAdmin` authority across `/admin/users` and the shell
  `Switch org` menu, lives in
  `tests/AcmeCoffeeOnline.Playwright/MultiOrganizationWorkflowTests.cs`, and
  proves the initial warning state, one scoped grant or revoke path, backend
  verification of the organization-scoped role change, and no leakage into a
  second organization.
- `E2E-WF-03` uses the real magic-link customer lane across `/login`,
  `/orders`, and the order-detail surface, lives in
  `tests/AcmeCoffeeOnline.Playwright/CustomerWorkflowTests.cs`, and proves real
  auth, browser-driven detail entry, scoped order API readback, and foreign
  order denial with no rendered foreign data.

## Suite Stability

- `dotnet test tests/AcmeCoffeeOnline.Playwright/AcmeCoffeeOnline.Playwright.csproj --filter "FullyQualifiedName~E2E_WF_"` passed `5/5`.
- `dotnet build Dabbler.Platform.sln` succeeded with the existing preview-SDK
  notices and `12` warnings.
- `dotnet test Dabbler.Platform.sln` passed `430/430`, including `10`
  Playwright tests.

## Scope Guard

The suite remains representative rather than exhaustive.

It proves the approved browser seams, route outcomes, and backend-truth checks
that genuinely benefit from end-to-end automation, while leaving broader UX
judgment, exploratory feedback, and subjective clarity checks to later human
UAT.

## Maintenance Notes

- `tests/AcmeCoffeeOnline.Playwright/ProductAdministrationWorkflowTests.cs`
  owns `E2E-WF-04` and `E2E-WF-05`.
- `tests/AcmeCoffeeOnline.Playwright/MultiOrganizationWorkflowTests.cs` owns
  `E2E-WF-02` and `E2E-WF-01`.
- `tests/AcmeCoffeeOnline.Playwright/CustomerWorkflowTests.cs` owns
  `E2E-WF-03`.
- `tests/AcmeCoffeeOnline.Playwright/SmokeTests.cs` preserves the baseline
  shell, auth, permission, and focused navigation proof outside the five
  representative workflow tests.
