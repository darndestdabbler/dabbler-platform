# Accepted App Coverage And Conformance Baseline

> **Purpose:** Capture the accepted application coverage and conformance
> baseline after `docs/session-sets/app-coverage-conformance/` so later Phase 2
> E2E, UAT, documentation, and packaging work can rely on one durable handoff.
> **Created:** 2026-04-15
> **Source Inputs:** `docs/planning/distributor-acceptance-baseline.md`,
> `docs/planning/multi-org-security-accepted-baseline.md`,
> `docs/planning/app-coverage-conformance-review-ledger.md`,
> `docs/planning/app-coverage-conformance-seam-refactor-plan.md`,
> `docs/planning/app-coverage-conformance-architecture-notes.md`,
> `docs/planning/user-functionality-notes.md`, the completed
> `docs/session-sets/app-coverage-conformance/activity-log.json`, and routed
> Session 4 analysis.

---

## Summary

The repo now has an accepted app-proof baseline for the distributor sample
application.

The app-specific seams that most clearly violated the intended architecture are
now aligned to API-first or app-service boundaries, and the highest-value app
logic now has direct proof at the unit, integration, and focused bUnit layers.

## Accepted App-Proof Baseline

- user-administration behavior in
  `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Admin/UserManagement.razor`
  now flows through `AdminUserApiClient` instead of direct web-tier security
  database access
- shell organization loading in
  `src/Apps/AcmeCoffeeOnline.Web/Components/Layout/MainLayout.razor` now flows
  through `OrganizationOptionsService` and `ShellSessionStateLoader` instead of
  direct `DabblerSecurityDbContext` reads
- order workflow coordination in
  `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Orders/OrderDetail.razor`
  now lives behind `src/Apps/AcmeCoffeeOnline.Web/Services/OrderDetailState.cs`
  so page-state logic is testable without relying on browser-only proof
- app-specific navigation smart-filter behavior remains owned by
  `src/Apps/AcmeCoffeeOnline.Web/Data/AcmeCoffeeNavigationDataProvider.cs`,
  but it now has direct unit proof instead of relying only on indirect shell
  coverage

## Accepted Proof Posture

- unit coverage now directly proves `OrderDetailState`,
  `ShellSessionStateLoader`, and `AcmeCoffeeNavigationDataProvider`
- API and integration coverage now directly proves higher-risk
  organization-aware admin behavior, including dual-membership cases, invalid
  authenticated-user handling, and scoped grant or revoke boundaries
- focused bUnit coverage now directly proves the API-driven
  `UserManagement.razor` page behavior without requiring a full browser harness
- representative Playwright and guardrail coverage remain valuable for auth,
  role-aware navigation, and one backend-confirmed mutation, but they are no
  longer expected to carry the app-proof burden alone

## What This Baseline Does Not Guarantee

- it does not claim exhaustive page-level proof for every distributor route or
  reusable report surface in the sample app
- it does not replace the need for deliberate representative Playwright design,
  browser-only checks, or later human UAT
- it does not hide the current solution-wide runtime-test instability reported
  outside the targeted Session 3 proof slice; later E2E and UAT work should
  treat those failures as stabilization inputs rather than as reasons to reopen
  this set's lower-layer proof decisions

## Downstream Implications

- `e2e-workflow-design` can now assume the app-proof baseline is organized
  around explicit lower-layer seams instead of mixed-responsibility UI code
- `e2e-workflow-implementation` should treat browser automation as a
  representative proof lane layered on top of the accepted lower-layer
  coverage, not as a substitute for it
- `full-app-uat` can later inherit this baseline as the reason the manual lane
  should stay selective and risk-driven instead of repeating all automation
- `phase-2-docs-readiness` and `packaging-and-template-readiness` can rely on
  the accepted architecture and proof story here once the E2E and UAT
  prerequisites are complete

## Queue Rebaseline

No downstream Phase 2 spec edits are required from this closeout.

The accepted app-proof outcomes match the current queue assumptions closely
enough that later sets can proceed without reordering or new spec text.

## Catch-Up Decision

No Session 5 is warranted.

The original four-session plan delivered the intended durable outcomes inside
the planned scope:

- app-specific architecture-conformance cleanup for admin, shell, and order
  seams
- direct unit, integration, and focused component proof for the highest-value
  app behaviors
- a durable accepted baseline that later E2E, UAT, documentation, and
  packaging work can inherit without reopening this set
