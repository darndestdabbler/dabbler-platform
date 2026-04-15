# Stubbed UAT Scope Ledger

> **Purpose:** Freeze the Session 1 review scope, queue decisions, reviewer
> issue format, and exit criteria for
> `docs/session-sets/stubbed-uat/`.
> **Created:** 2026-04-15
> **Source Inputs:** `docs/planning/phase-2-platform-roadmap.md`,
> `docs/planning/reusable-platform-stub-guide.md`,
> `docs/planning/accessibility-and-stubbing-evidence.md`,
> `docs/planning/accessibility-and-stubbing-closeout.md`, the current
> `/platform/stub` implementation, and the remaining Phase 2 session-set specs

---

## Session 1 Verdict

The current `stubbed-uat` spec still matches the accepted Phase 2 baseline.

No rebaseline of this session set's core purpose is needed.

The durable Session 1 outcomes are:

- keep the review entry point at `/platform/stub`
- treat the early UAT lane as design, layout, focus, labeling, shell, and
  page-to-page navigation proof only
- rely on the accepted reusable stub baseline to avoid false negatives from
  incomplete backend or app wiring
- keep later distributor, security, app-proof, Playwright, and full-app UAT
  work out of scope unless a finding clearly belongs there as a follow-on

## Frozen Review Scope

### Entry Route

The review starts at `/platform/stub`.

### Reviewable Surfaces

The current stub-backed scope is limited to the retained surfaces already
backed by direct proof and app-hosted stub consumption:

- `DabblerShell` through the stub host page, including primary layout,
  navigation chrome, and workspace tab behavior
- `DabblerQueryPage<T>` through `PlatformStubCustomerSearch`
- `PlatformStubDetail` as the shell-to-workspace detail target
- `DabblerReportPage` through `PlatformStubSalesReport`
- the report prompt and viewer surfaces exercised by the stub report flow,
  including `ParameterPromptForm` and `ReportViewer`

### Reviewable Navigation Paths

- application load to shell render and default home-page selection
- navigation menu to the stub customer query page and initial grid render
- navigation menu to recent customer records and detail-tab opening
- navigation menu to the stub report page and report-tab opening
- workspace tab switching between open search, detail, and report surfaces
- keyboard navigation through the skip link, shell navigation, workspace tabs,
  query filters, report prompt, and report toolbar affordances

### Why This Scope Is Safe Now

- `accessibility-and-stubbing` is complete and accepted
- the reusable DI-driven stub seams are frozen and already support app-hosted
  consumption through `/platform/stub`
- the retained shell, query, prompt, and report surfaces already have direct
  proof plus WCAG-oriented evidence, so early human review is less likely to
  produce false negatives caused by missing infrastructure
- the stub host was intentionally built to prove shell integration and
  page-to-page navigation through the real app pipeline without pretending to
  be full-app functional validation

## Deferred And Excluded Work

The following areas are intentionally out of scope for this early UAT lane:

- business-rule correctness, backend truth, and data-content validation:
  belongs to `app-coverage-conformance`, representative E2E, or later
  full-app UAT
- multi-organization switching, organization-scoped roles, and related
  authorization behavior:
  belongs to `multi-org-security`
- distributor-specific domain naming, seeded personas, and content coherence:
  belongs to `distributor-reframe`
- representative end-to-end mutation workflows and database-backed assertions:
  belongs to `e2e-workflow-design`, `e2e-workflow-implementation`, and later
  `full-app-uat`
- broad catalog, compatibility-only query/report surfaces, or retired starter
  defaults:
  excluded by the narrowed platform baseline and should not be reintroduced by
  this set
- packaging, consumer-install validation, and template readiness:
  belongs to the final Phase 2 packaging and documentation work

## Queue And Parallelization Decisions

### Cross-Set

No new cross-set parallelization is safe beyond the accepted sequential queue.

This set should still hand its findings forward, but Session 1 did not justify
changing the Phase 2 order or editing downstream specs.

### Intra-Set

Once this frozen scope is accepted:

- Session 2 checklist authoring can proceed
- Session 2 reviewer-harness preparation can proceed in parallel with checklist
  authoring

### Coordination Rules

- reviewers should log only issues inside the frozen scope and explicitly mark
  deferred observations when they belong to later sets
- Session 2 should build its checklist and harness from this ledger instead of
  re-opening scope
- Session 3 should feed findings into downstream specs only when the findings
  materially justify a change

## Reviewer Issue Format

Each captured issue should include these fields:

- `id`
- `title`
- `surface`
- `reproductionSteps`
- `observedBehavior`
- `expectedBehavior`
- `severity`
- `status`
- `screenshotUrl`

### Severity Scale

- `High`: blocks navigation or a core interaction inside the frozen review
  scope
- `Medium`: incorrect but still usable layout, focus, labeling, or navigation
  behavior
- `Low`: minor cosmetic or non-blocking presentation issue

### Status Buckets

- `New`
- `Triaged`
- `Remediate`
- `Defer`
- `Closed`

### Capture Notes

- focus on design, layout, visible focus, labeling, shell behavior, workspace
  behavior, and navigation issues
- do not file data-correctness, missing-backend, or future-feature issues as
  early-UAT defects when they are already deferred by scope
- when an observation belongs to a later session set, keep it in the findings
  ledger but mark it `Defer` with the intended downstream owner

## Exit Criteria

This session set should only be considered complete when:

- the frozen stub-backed checklist has been executed against the accepted scope
- all findings have been captured using the issue format above
- each finding has been triaged to `Remediate`, `Defer`, or `Closed`
- the resulting findings ledger clearly separates confirmed issues, deferred
  items, and observations reserved for later full-app work
- downstream remediation inputs are recorded only where the findings justify
  them
- `change-log.md` is generated after the findings and handoff work are done

## Execution Notes For Later Sessions

- Session 2 should create a lightweight JSON checklist that maps directly to
  the frozen reviewable surfaces and navigation paths in this ledger
- Session 2 should add reviewer guidance for launching the stub host quickly,
  but it should avoid introducing a heavyweight harness that competes with the
  later Playwright lane
- Session 3 should capture remediation inputs conservatively so this early UAT
  informs later work without preempting the distributor, multi-org, or E2E
  sets
