# End-To-End Workflow Implementation Spec

> **Purpose:** Implement and stabilize the representative Playwright suite with
> backend-truth verification and Docker-backed execution as the baseline.
> **Created:** 2026-04-15
> **Session Set:** `docs/session-sets/e2e-workflow-implementation/`
> **Prerequisite:** `e2e-workflow-design` is complete and its implementation
> handoff artifacts are current, especially
> `docs/planning/e2e-executable-workflow-specs.md`,
> `docs/planning/e2e-seed-reset-and-verification-plan.md`, and
> `docs/planning/e2e-persona-route-matrix.md`.
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification

---

## Project Overview

This set turns the approved representative workflow design into the Phase 2
browser-proof lane.

The suite should stay intentionally small enough to remain maintainable, but it
must be strong enough to prove:

- representative navigation and mutation workflows
- role-aware and organization-aware browser behavior where real browser proof
  matters
- backend and database truth for important mutations
- selected browser-only accessibility, focus, or navigation checks that lower
  layers cannot prove adequately

## Parallelization Notes

No downstream Phase 2 session set is marked safe to overlap at authoring time.
Session 1 must still reassess that assumption using the final workflow design.

Session 1 confirmed that no downstream Phase 2 session set is safe to overlap
with this implementation set.

Inside this set, once shared fixtures, reset helpers, and persona setup are
stable, disjoint workflow specs can be implemented in parallel across separate
test files.

Session 1 also confirmed that the existing intra-set note remains appropriate:
only after the shared harness, reset helpers, persona fixtures, and backend-
truth verification pattern are frozen may disjoint workflow files proceed in
parallel, while stabilization and closeout work stay blocked on the resulting
representative suite being trustworthy enough for UAT handoff.

## Session Plan

### Session 1 of 4: Review, Fixture Strategy, And Queue Scan

**Steps:**
1. Review the approved E2E design artifacts and all remaining Phase 2
   session-set specs before implementation begins.
2. Freeze the shared Playwright fixture strategy, database reset approach,
   persona setup, and backend-truth verification pattern using the executable
   workflow specs, seed-reset plan, and persona-route matrix from the design
   set.
3. Record any conservative adjustments needed in downstream specs, including
   any newly safe parallelization opportunities.
4. Confirm the initial representative workflow slice that should land first.

**Creates:** E2E fixture strategy and execution baseline
**Touches:** This spec, downstream Phase 2 specs if needed, and test-planning
notes
**Ends with:** The suite has a stable foundation before individual workflows
are implemented
**Progress keys:** `session-1/review-scan`,
`session-1/fixture-strategy`, `session-1/backend-truth-pattern`,
`session-1/parallelization-update`

---

### Session 2 of 4: Shared Harness And Initial Representative Workflows

**Steps:**
1. Implement or refine the shared Playwright harness, Docker-backed execution
   expectations, reset tooling, and persona helpers.
2. Implement the first representative workflows from the approved design set,
   landing `E2E-WF-04` and `E2E-WF-05` first unless stabilization evidence
   forces a narrow reorder, and include the necessary backend or database truth
   assertions.
3. Prove the expected navigation, validation, and mutation outcomes for those
   initial scenarios.
4. Keep the harness reusable enough that later workflows can add coverage
   without duplicating setup logic.

**Creates:** Shared E2E harness and first representative workflow tests
**Touches:** Playwright tests, fixtures, reset helpers, and supporting docs or
scripts
**Ends with:** The suite has a working baseline that proves the implementation
approach end to end
**Progress keys:** `session-2/shared-harness`,
`session-2/reset-tooling`, `session-2/initial-workflows`,
`session-2/backend-truth-assertions`

---

### Session 3 of 4: Multi-Organization Scenarios And Browser-Only Proof

**Steps:**
1. Implement the remaining approved representative workflows, especially the
   scenarios that materially exercise multi-organization behavior, regional
   sales-rep administration, and role-aware navigation, using the frozen route
   and persona matrix from the design set.
2. Add the small number of browser-only accessibility, focus, or navigation
   checks that genuinely require a real browser.
3. Strengthen backend-truth verification for important mutations and filtered
   views where it adds confidence beyond UI assertions.
4. Preserve the representative scope and avoid expanding into exhaustive browser
   coverage.

**Creates:** Full representative Playwright suite for the approved workflow set
**Touches:** Playwright tests, verification helpers, and supporting data setup
**Ends with:** The approved workflow design is implemented with the intended
browser and backend proof
**Progress keys:** `session-3/multi-org-workflows`,
`session-3/regional-rep-scenarios`, `session-3/browser-only-proof`,
`session-3/representative-scope-guard`

---

### Session 4 of 4: Stabilization, UAT Handoff, And Closeout

**Steps:**
1. Stabilize the representative suite, fix brittle paths, and align the final
   workflow inventory to the approved design set.
2. Produce the handoff inputs for the later full-app UAT subset selection,
   including which Playwright outcomes should be human-confirmed.
3. Append one catch-up session only if the implementation work exposed a direct
   omission that belongs inside this set.
4. Generate `change-log.md` for this session set.

**Creates:** Stabilized E2E baseline, UAT handoff notes, and `change-log.md`
**Touches:** Tests, planning docs, downstream Phase 2 specs if needed, and
closeout artifacts
**Ends with:** The repo has a maintainable representative E2E suite and a
clear bridge into later human confirmation
**Progress keys:** `session-4/suite-stabilization`,
`session-4/uat-handoff`, `session-4/catch-up-decision`,
`session-4/change-log`
