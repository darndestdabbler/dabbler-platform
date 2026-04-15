# E2E Seed, Reset, And Verification Plan

> **Purpose:** Freeze the fixture backlog, reset expectations, and backend
> verification contract for the approved representative workflows.
> **Created:** 2026-04-15
> **Source Inputs:** `docs/planning/e2e-executable-workflow-specs.md`,
> `docs/planning/e2e-playwright-implementation-handoff.md`,
> `tests/AcmeCoffeeOnline.Playwright/Infrastructure/PlaywrightFixture.cs`, and
> `tests/AcmeCoffeeOnline.Web.Tests/WebGuardrailTests.cs`.

---

## Workflow Fixture Matrix

| Workflow Id | Seed Data Needed Before Test | Backend Verification Helper | Reset Or Cleanup Requirement |
| --- | --- | --- | --- |
| `E2E-WF-04` | One stable sentinel product with a known exact name and durable ID lookup path | Product readback by exact name and by ID | Restore original `IsAvailable` state in teardown |
| `E2E-WF-05` | Unique product-name prefix reserved for tests and a cleanup path keyed by name | Product search by unique name after create | Delete or neutralize the created product after verification |
| `E2E-WF-02` | One dual-membership persona that starts internal and can switch to one retail-business customer scope | None required | No persistent reset beyond sign-out |
| `E2E-WF-01` | One regional-sales-rep persona with scoped admin rights in at least two retail organizations, plus one eligible target membership per managed organization | Scoped admin-role lookup by organization membership ID | Revert the grant or revoke so later runs start clean |
| `E2E-WF-03` | One retail-business customer with seeded orders, plus one known foreign order ID for denial coverage | Scoped order lookup for the signed-in user and foreign-order lookup for denial | No persistent reset for read-only flow |

## Shared Helper Backlog

The downstream implementation set should treat these helpers as the minimum
shared harness:

- `Seeded sign-in`: reuse `PlaywrightFixture.SeededSignInAsync`
- `Magic-link sign-in`: reuse `ClearMagicLinksAsync` and `WaitForMagicLinkAsync`
- `Session readback`: reuse `GetSessionInfoAsync`
- `Organization switch helper`: add a browser helper that opens `Switch org`,
  chooses an organization by visible name, and waits for
  `data-testid="active-organization-label"` to update
- `Product readback helper`: wrap the current `GetApiJsonAsync` pattern for
  exact-name lookup and stable ID extraction
- `Product cleanup helper`: add a delete or neutralize path for uniquely named
  products created by `E2E-WF-05`
- `Scoped admin verification helper`: add a backend lookup for whether one
  organization membership currently holds `BackofficeUserAdmin`
- `Scoped order verification helper`: add a helper that reads orders for the
  current customer and returns one visible order plus one known foreign order ID

## Reset Rules

- A workflow that mutates persisted state must leave the repo in its original
  state even when an assertion fails midway.
- Reset logic should live close to the workflow helper that created or changed
  the state, not as an unrelated suite-wide cleanup pass.
- Sentinel records should be durable and named clearly enough that later
  sessions can tell stable fixtures from user-created sample data.
- Unique test-created records should use a recognizable prefix so the cleanup
  helper can remove only automation-owned rows.

## Persona And Data Ownership Notes

- Reuse existing seeded personas where they already satisfy the workflow:
  `admin@acmecoffee.com` for `SystemAdmin` and `james.smith@example.com` for the
  customer magic-link lane.
- Promote the current guardrail-only organization fixtures into reusable E2E
  fixtures only if the exact personas and organizations match the intended
  workflow. Otherwise, create E2E-specific seed data instead of overloading the
  guardrail fixtures.
- The dual-membership and regional-sales-rep personas should be treated as
  shared suite fixtures because both depend on stable organization names and
  predictable switch targets.

## Backend Verification Contract

- Use API-backed truth checks for `E2E-WF-01`, `E2E-WF-04`, and `E2E-WF-05`.
- Keep `E2E-WF-02` UI-only because its value is visible shell and route
  behavior, not persistence.
- Use API confirmation in `E2E-WF-03` only to prove that the opened order stays
  inside the customer's scope and that the denial branch does not leak foreign
  data.
- Keep verification helpers read-only except where teardown explicitly needs a
  compensating mutation.
