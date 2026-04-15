# End-To-End Workflow Design Spec

> **Purpose:** Design representative, traceable end-to-end mini-workflows
> before implementing them in Playwright.
> **Created:** 2026-04-15
> **Session Set:** `docs/session-sets/e2e-workflow-design/`
> **Prerequisite:** `app-coverage-conformance` is complete and its accepted
> coverage baseline is current.
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification

---

## Project Overview

This set translates the now-stable domain, security, and app baseline into a
deliberate E2E design.

The target is a representative suite, not exhaustive browser automation. Each
chosen workflow should justify itself by exercising meaningful platform seams,
state transitions, navigation paths, validation rules, or backend-truth checks
that are not already proven well enough at lower layers.

Use `docs/planning/example-of-end-to-end-mini-workflow-test.md` as a seed, but
expand it into a traceable design set that clearly maps what belongs in bUnit,
integration tests, Playwright, and later human UAT.

Treat `docs/planning/distributor-acceptance-baseline.md` as the current
business-language baseline for workflow naming, persona framing, and
representative route expectations.

## Parallelization Notes

No downstream Phase 2 session set is marked safe to overlap at authoring time.
Session 1 must still reassess that assumption.

Inside this set, once the representative workflow catalog is frozen, individual
workflow designs and traceability records can be drafted in parallel across
disjoint scenarios.

## Session Plan

### Session 1 of 3: Review, Representative Scope, And Queue Scan

**Steps:**
1. Review the accepted app-proof baseline and all remaining Phase 2 session-set
   specs before defining the E2E suite.
2. Freeze the representative-only selection criteria for Playwright so the
   suite remains requirement-driven instead of expanding opportunistically.
3. Record the traceability expectations for navigation, validation,
   multi-organization behavior, and backend-truth checks.
4. Capture any conservative adjustments needed in downstream specs, including
   any newly safe parallelization opportunities.

**Creates:** E2E scope ledger and traceability rules
**Touches:** This spec, downstream Phase 2 specs if needed, and planning notes
**Ends with:** The design work has explicit inclusion rules and downstream
assumptions are current
**Progress keys:** `session-1/review-scan`,
`session-1/representative-scope`, `session-1/traceability-rules`,
`session-1/parallelization-update`

---

### Session 2 of 3: Workflow Catalog And Layered Coverage Map

**Steps:**
1. Expand the example mini-workflow document into a full catalog of candidate
   representative workflows, edge cases, and expected backend-truth checks.
2. Include scenarios that materially exercise multi-organization behavior,
   regional-sales-rep administration, role-aware navigation, and meaningful
   validation boundaries.
3. Explicitly map which UI behaviors are covered by unit or integration tests,
   bUnit, Playwright, and later UAT so the browser suite remains intentional.
4. Freeze the subset that should move forward into Playwright implementation.

**Creates:** Representative workflow catalog and layered coverage matrix
**Touches:** Planning docs and downstream implementation handoff notes
**Ends with:** The Playwright target suite is deliberate, traceable, and sized
for representative proof
**Progress keys:** `session-2/workflow-catalog`,
`session-2/edge-case-selection`, `session-2/layered-coverage-map`,
`session-2/playwright-scope-freeze`

---

### Session 3 of 3: Executable Design Artifacts And Closeout

**Steps:**
1. Convert the selected workflow catalog into implementation-ready artifacts
   such as scenario specs, data prerequisites, reset requirements, backend
   verification expectations, and route or persona matrices.
2. Update the downstream Playwright and full-app UAT specs where the design
   work clarified scope or sequencing.
3. Append one catch-up session only if the design work exposed a direct omission
   that belongs inside this set.
4. Generate `change-log.md` for this session set.

**Creates:** Implementation-ready E2E design artifacts and `change-log.md`
**Touches:** Planning docs, downstream Phase 2 specs if needed, and closeout
artifacts
**Ends with:** The implementation set can start from an approved representative
workflow design instead of improvising browser coverage
**Progress keys:** `session-3/executable-design`,
`session-3/downstream-handoff`, `session-3/catch-up-decision`,
`session-3/change-log`
