# E2E Playwright Fixture Strategy

> **Purpose:** Freeze the shared harness, reset approach, persona ownership,
> backend-truth verification pattern, and initial workflow slice for
> `docs/session-sets/e2e-workflow-implementation/`.
> **Created:** 2026-04-15
> **Source Inputs:** `docs/planning/e2e-executable-workflow-specs.md`,
> `docs/planning/e2e-seed-reset-and-verification-plan.md`,
> `docs/planning/e2e-persona-route-matrix.md`,
> `docs/planning/e2e-playwright-implementation-handoff.md`,
> `tests/AcmeCoffeeOnline.Playwright/Infrastructure/PlaywrightFixture.cs`,
> `tests/AcmeCoffeeOnline.Playwright/SmokeTests.cs`, and
> `tests/AcmeCoffeeOnline.Web.Tests/WebGuardrailTests.cs`.

---

## Shared Harness Baseline

Session 2 should extend the existing `PlaywrightFixture` instead of replacing
it.

Keep these current behaviors as the baseline:

- auto-start the Web and API hosts when `PLAYWRIGHT_BASE_URL` or
  `PLAYWRIGHT_API_BASE_URL` are not already reachable
- preserve browser-failure capture for console errors, page exceptions, and
  relevant HTTP or request failures
- keep seeded-session, magic-link, session-info, and API helper access in one
  place so workflow tests stay focused on user actions

Prefer small, workflow-focused helper additions over a new abstraction layer.
The Session 2 helper backlog should stay close to the existing fixture style:

- system-admin sign-in convenience that still uses `SeededSignInAsync`
- `/admin/products` navigation readiness helper
- product lookup helpers keyed by exact name so tests do not depend on hardcoded
  product IDs
- browser helper for reading `data-testid="active-organization-label"` and
  switching organizations through the real shell UI
- narrow API-backed verification helpers for scoped admin membership lookup and
  scoped order lookup

Do not introduce a page-object-model layer unless Session 2 proves that direct
fixture helpers plus Playwright locators are no longer maintainable.

## Reset And Data Strategy

Shared reset rules for the representative suite are:

- a workflow that mutates persisted state must restore the original state even
  when assertions fail midway
- cleanup should live with the helper that created or changed the state rather
  than in a broad after-the-fact sweep
- durable sentinel records should be identified by exact name plus API lookup,
  not by brittle IDs copied into tests
- newly created automation data should use a recognizable prefix so cleanup only
  touches test-owned rows

Workflow-specific reset posture:

- `E2E-WF-04`: read the sentinel product first, toggle through the browser UI,
  then restore the original availability in teardown
- `E2E-WF-05`: create one uniquely named product, verify it through the API,
  then delete or neutralize it through a focused cleanup helper
- `E2E-WF-02`: no persistent reset beyond sign-out because organization changes
  are session-scoped
- `E2E-WF-01`: revert the scoped grant or revoke so shared seeded role state
  stays reusable
- `E2E-WF-03`: read-only coverage, so no persistent reset beyond clearing
  pending magic-link messages before the run

## Persona Ownership

Use the accepted shared personas as durable E2E fixtures rather than creating
per-test guardrail-only personas inside browser tests.

Keep these persona decisions frozen:

- `admin@acmecoffee.com` remains the `SystemAdmin` persona for `E2E-WF-04` and
  `E2E-WF-05`
- `james.smith@example.com` remains the real magic-link customer persona for
  `E2E-WF-03`
- the dual-membership user and regional sales rep should become reusable seeded
  E2E personas with stable organization names and stable target memberships,
  not ad hoc personas constructed inside each test run
- guardrail-only persona creation in `WebGuardrailTests` is useful proof of the
  model but should not be the long-term seed pattern for the Playwright suite

If the current seeded data does not yet expose the required dual-membership or
regional-sales-rep shape cleanly, Session 2 should add or promote E2E-owned
seed fixtures before implementing those workflows.

## Backend-Truth Verification Pattern

Use browser authentication first, then derive verification context from the
signed-in session.

The baseline pattern for mutation or scope-sensitive workflows is:

1. Sign in through the supported browser path for that workflow.
2. Read session info through `GetSessionInfoAsync` when the test needs the real
   app user ID or active-organization context for verification.
3. Use focused API-backed helper reads to capture the before state.
4. Perform the primary action in the browser UI.
5. Re-read the narrow backend truth needed to prove the workflow.
6. Run compensating cleanup when the workflow mutated persisted state.

Apply that pattern conservatively:

- `E2E-WF-04` and `E2E-WF-05` should verify product truth by exact name or
  product ID after browser actions
- `E2E-WF-01` should verify organization-scoped role assignment for the
  targeted membership, and only in the active organization
- `E2E-WF-02` stays primarily UI-driven because its value is shell context and
  route behavior, not persistence
- `E2E-WF-03` should use scoped order reads to confirm that the opened order is
  visible to the signed-in customer and that the denial branch does not leak
  foreign data

If the current API does not expose one of these verification reads directly,
Session 2 may add a narrow helper seam that still respects the current security
and test guardrails rather than bypassing the app with raw database access from
Playwright.

## Selector Discipline

Selector rules for the implementation set are:

- prefer existing `data-testid`, role, label, and visible-name hooks first
- add row-level or dialog-level test IDs only at the specific seams where the
  current UI is not durable enough for representative proof
- avoid broad instrumentation across whole pages when a single workflow seam is
  the real instability point
- keep selectors local to the relevant helper or test file so later workflow
  additions do not copy brittle locator strings everywhere

## Initial Workflow Slice

Keep the first implementation slice fixed as `E2E-WF-04` then `E2E-WF-05`.

This slice should establish:

- the reusable `/admin/products` harness
- the sentinel product lookup and restore path
- the unique-name product create-and-cleanup path
- the suite's initial browser-driven backend-truth pattern
- the one deliberate keyboard and validation workflow before the set expands
  into organization-switching and real-auth coverage
