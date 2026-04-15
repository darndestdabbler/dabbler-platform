# E2E Workflow Scope Ledger

> **Purpose:** Freeze the Session 1 review verdict, representative-only
> inclusion rules, and queue decisions for
> `docs/session-sets/e2e-workflow-design/`.
> **Created:** 2026-04-15
> **Source Inputs:** `docs/planning/app-coverage-conformance-accepted-baseline.md`,
> `docs/planning/multi-org-security-accepted-baseline.md`,
> `docs/planning/distributor-acceptance-baseline.md`,
> `docs/planning/example-of-end-to-end-mini-workflow-test.md`,
> `docs/planning/phase-2-platform-roadmap.md`,
> `docs/planning/user-functionality-notes.md`, the current
> `tests/AcmeCoffeeOnline.Playwright/SmokeTests.cs`,
> `tests/AcmeCoffeeOnline.Web.Tests/WebGuardrailTests.cs`, and the remaining
> downstream Phase 2 session-set specs

---

## Session 1 Verdict

The current `e2e-workflow-design` spec still matches the accepted app,
distributor, and multi-organization baselines.

No downstream spec text changes are warranted yet.

The durable Session 1 outcomes are:

- keep Playwright representative and requirement-driven rather than letting it
  expand into broad page-by-page browser proof
- treat the accepted app-proof baseline as the reason lower layers own most
  app logic, validation detail, and organization-aware service behavior
- reserve browser automation for workflows that genuinely need end-to-end
  navigation, real auth context, real browser behavior, or backend-truth
  confirmation after a mutation
- use the accepted seeded personas and distributor wording as the design
  baseline for later workflow records, implementation, and UAT

## Existing Browser-Proof Baseline

The repo already has meaningful browser and guardrail coverage. Session 2 must
expand from that baseline instead of duplicating it.

Current coverage already proves:

- seeded-session and magic-link sign-in paths
- authenticated shell render and auth-layout separation
- route access and access-denied behavior for representative personas
- focused organization-switching and admin-scope guardrails
- one backend-confirmed product mutation through Playwright helper calls

The representative workflow set should therefore add value by driving
meaningful end-to-end user journeys through the UI, not by replaying the same
smoke and route-access checks with larger scripts.

## Representative Playwright Inclusion Rules

Playwright candidates must satisfy at least one of these rules:

- prove a complete multi-page user journey that matters to the distributor app
  and crosses UI, API, and security seams that lower layers cannot prove well
  enough in isolation
- prove real browser behavior that depends on the live DOM, focus order,
  navigation sequencing, redirects, or other browser-only interaction details
- prove the accepted multi-organization persona matrix end to end, especially
  regional-sales-rep administration, dual-membership organization switching,
  and `SystemAdmin` bypass behavior
- prove backend truth after a meaningful mutation where UI success messages are
  not sufficient evidence
- prove a high-risk app seam where the shell, auth state, organization context,
  and business workflow all have to agree

Candidates should usually be excluded when:

- the main value is already covered directly by unit, integration, or bUnit
  tests
- the workflow is only another route-access assertion or shell-smoke variant
- the scenario exercises a reusable-platform rendering contract better proven
  through stub-backed bUnit or earlier accessibility work
- the flow adds breadth without adding a new state transition, persona,
  validation boundary, or backend-truth check

## Frozen Scope Themes For Session 2

Session 2 should build its catalog around these representative themes:

### Organization-Aware Administration

- active-organization-aware `/admin/users` behavior for scoped `UserAdmin`
- regional sales rep or other multi-organization admin persona changing scope
  and proving that manageability changes with the active organization
- `SystemAdmin` behavior only where it proves the contrast with scoped admin

### Retail-Business Workflow Proof

- a retail-business user journey that reaches order surfaces through the real
  sign-in path and proves organization-scoped visibility or actionability
- at least one workflow that uses the actual page flow rather than API helper
  mutations as the primary action surface

### Validation And Focus Wiring

- a small number of representative invalid-input paths that prove validation is
  wired through the real form, not just lower-layer rule execution
- at least one browser-only focus or tab-order expectation on a workflow that
  materially depends on keyboard navigation

### Backend-Confirmed Mutation

- at least one mutation workflow that is driven from the browser UI and then
  confirmed against API or database truth
- prefer mutations that matter to the accepted personas and security model over
  generic CRUD coverage

## Deferred Or Excluded Areas

The following are intentionally not the design center for this set:

- exhaustive browser coverage for every page, menu destination, report, or
  query surface
- replaying all lower-layer proof for organization scoping, row filtering, or
  validation combinations already owned by API, security, unit, or bUnit tests
- broad reusable-platform accessibility proof that already belongs to the
  accepted accessibility and stub baseline
- documentation, package-readiness, or template-consumer concerns that belong
  to later Phase 2 sets

## Queue And Parallelization Decisions

### Cross-Set

Keep the strict downstream dependency chain:

`e2e-workflow-design` -> `e2e-workflow-implementation` -> `full-app-uat` ->
`phase-2-docs-readiness` -> `packaging-and-template-readiness`

No downstream Phase 2 session set is safe to overlap with this set.

The implementation set depends on the frozen design artifacts from this set,
and the later UAT, documentation, and packaging work still depends on the
stabilized automation and human-confirmed baseline that follow.

### Intra-Set

The existing `e2e-workflow-design` parallelization note remains appropriate
once Session 1 artifacts are frozen.

After this ledger and the traceability rules are accepted:

- Session 2 can draft disjoint workflow records in parallel across separate
  scenario groups such as organization-admin flows, retail-business order
  flows, and report or navigation spot checks
- each parallel slice must treat the representative-only rules here as fixed
  so the catalog does not drift into page inventory or speculative coverage

### Coordination Rules

- Session 2 should start from the existing Playwright and guardrail baseline,
  then add only the workflows that close browser-specific confidence gaps
- Session 2 should keep one explicit rationale line for every candidate
  workflow explaining why it belongs in Playwright instead of a lower layer
- later implementation work should favor real browser interactions for the
  chosen workflows and use API helpers only for setup, reset, or backend-truth
  confirmation
- later UAT should inherit the representative subset from this set instead of
  rebuilding its own independent browser scope

## Watchpoints

- the current example mini-workflow is useful as a seed, but Session 2 must
  turn it into distributor- and persona-specific workflow records rather than a
  generic CRUD outline
- admin and organization-switching flows are the clearest place where browser,
  auth, active-organization state, and backend truth need to agree at once
- existing Playwright coverage already proves some auth and permission basics,
  so the new design must show what additional confidence each proposed
  workflow earns
- report and query surfaces may deserve representative spot coverage, but only
  when they add navigation, prompt, or backend-truth value that lower layers
  do not already provide
