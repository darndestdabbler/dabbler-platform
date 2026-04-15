# E2E Workflow Implementation Review Ledger

> **Purpose:** Freeze the Session 1 review verdict, current browser-proof
> baseline, and queue decisions for
> `docs/session-sets/e2e-workflow-implementation/`.
> **Created:** 2026-04-15
> **Source Inputs:** `docs/planning/app-coverage-conformance-accepted-baseline.md`,
> `docs/planning/multi-org-security-accepted-baseline.md`,
> `docs/planning/e2e-workflow-scope-ledger.md`,
> `docs/planning/e2e-playwright-implementation-handoff.md`,
> `docs/planning/e2e-executable-workflow-specs.md`,
> `docs/planning/e2e-seed-reset-and-verification-plan.md`,
> `docs/planning/e2e-persona-route-matrix.md`,
> `tests/AcmeCoffeeOnline.Playwright/Infrastructure/PlaywrightFixture.cs`,
> `tests/AcmeCoffeeOnline.Playwright/SmokeTests.cs`,
> `tests/AcmeCoffeeOnline.Web.Tests/WebGuardrailTests.cs`, and the remaining
> downstream Phase 2 session-set specs.

---

## Session 1 Verdict

The current `e2e-workflow-implementation` spec still matches the accepted
multi-organization, app-proof, and E2E design baselines.

No downstream Phase 2 spec text changes are warranted from this review.

The durable Session 1 outcomes are:

- keep the representative workflow order frozen as
  `E2E-WF-04` -> `E2E-WF-05` -> `E2E-WF-02` -> `E2E-WF-01` -> `E2E-WF-03`
- extend the existing Playwright harness instead of introducing a second test
  framework or a page-object layer
- promote the temporary multi-organization and scoped-admin guardrail personas
  into durable E2E-owned fixtures instead of recreating one-off personas inside
  each browser test
- keep backend-truth confirmation narrow and workflow-driven so Playwright
  proves end-to-end seams without retaking lower-layer proof already covered by
  unit, integration, and bUnit tests

## Existing Browser-Proof Baseline

The repo already has meaningful browser and guardrail proof that Session 2
should extend rather than duplicate.

Current proof already covers:

- login-shell versus authenticated-shell rendering
- seeded-session and real magic-link sign-in paths
- representative route-access and access-denied behavior for focused personas
- organization-switching and scoped-admin guardrails in the web-test lane
- one backend-confirmed product mutation, although the current Playwright smoke
  test still triggers that mutation through `PatchApiAsync` instead of the UI

The implementation set should therefore spend its first effort on moving the
approved representative workflows onto browser-driven interactions, reusable
fixture helpers, and explicit teardown rules instead of widening route-access
coverage.

## Implementation Guardrails

- treat the accepted app-proof baseline as the reason Playwright stays
  representative rather than exhaustive
- keep primary user actions in the browser, and reserve helper calls for setup,
  teardown, and backend-truth confirmation
- preserve the current browser-failure policy from `PlaywrightFixture` so
  console errors, runtime errors, page exceptions, and relevant failed requests
  still fail the suite
- keep auth entry paths on the approved seeded-session and real magic-link
  helpers instead of adding new backdoors
- add new test IDs only on narrow workflow seams when current roles, labels, or
  existing `data-testid` hooks are not durable enough

## Queue And Parallelization Decisions

### Cross-Set

Keep the strict downstream dependency chain:

`e2e-workflow-implementation` -> `full-app-uat` ->
`phase-2-docs-readiness` -> `packaging-and-template-readiness`

No downstream Phase 2 session set is safe to overlap with this set.

The later UAT, documentation, and packaging work still depends on a stabilized
representative suite plus the UAT handoff that this set must produce.

### Intra-Set

The current intra-set parallelization note remains appropriate once the Session
1 fixture strategy is frozen.

After the shared harness, reset helpers, persona fixtures, and backend-truth
pattern are stable:

- Session 2 may implement `E2E-WF-04` and `E2E-WF-05` together because both
  stay within `/admin/products` and share the same system-admin harness
- later workflow files may be implemented in parallel across disjoint test
  files once the shared switch-organization helper and shared seeded personas
  are established
- stabilization and closeout work remain blocked on the representative suite
  being green enough to trust, so Session 4 still should not start early

## First Implementation Slice

The safest initial representative slice remains `E2E-WF-04` followed by
`E2E-WF-05`.

That order is still the most conservative approach because:

- `E2E-WF-04` upgrades an already-proven backend mutation into a browser-driven
  mutation on the same admin surface
- the shared system-admin sign-in, product lookup, row targeting, and teardown
  helpers created for `E2E-WF-04` carry directly into `E2E-WF-05`
- `E2E-WF-05` then adds the suite's one deliberate keyboard and validation path
  without forcing organization-switching or real-auth complexity into the first
  implementation pass
