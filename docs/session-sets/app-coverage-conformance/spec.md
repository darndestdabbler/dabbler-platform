# Application Coverage And Conformance Spec

> **Purpose:** Ensure the distributor application fully exercises the platform
> and aligns with `docs/planning/user-functionality-notes.md` without becoming
> product-specific scaffolding.
> **Created:** 2026-04-15
> **Session Set:** `docs/session-sets/app-coverage-conformance/`
> **Prerequisite:** `multi-org-security` is complete and its accepted security
> baseline is current.
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification

---

## Project Overview

After the domain and security model settle, the sample application becomes the
durable proof harness for the Phase 2 platform.

This set should:

- audit the distributor workflows against the architecture notes and retained
  platform capabilities
- split mixed-responsibility methods into better unit or integration seams
- fill meaningful gaps in business logic, API behavior, organization-aware
  services, and security evaluation coverage

Treat `docs/planning/distributor-acceptance-baseline.md` and
`docs/planning/user-functionality-notes.md` as the binding business-story and
workflow inputs for this set's coverage matrix.

The output should be broad enough to prove the platform while still resisting
the temptation to hard-code highly product-specific workflows into the sample
app architecture.

## Parallelization Notes

No downstream Phase 2 session set is marked safe to overlap at authoring time.
Session 1 must still look for conservative changes if the app baseline proves
more stable than expected.

Inside this set, feature-area audits and test-gap implementation can proceed in
parallel across disjoint modules once the coverage matrix and seam-refactor plan
are frozen.

## Session Plan

### Session 1 of 4: Review, Coverage Matrix, And Queue Scan

**Steps:**
1. Review the accepted distributor and multi-organization security baselines
   plus all remaining Phase 2 session-set specs before implementation starts.
2. Build a coverage and conformance matrix against
   `docs/planning/user-functionality-notes.md`, the supported platform surface,
   and the current distributor workflows.
3. Identify where mixed-responsibility methods or blurred seams are preventing
   the intended unit, integration, bUnit, or API proof.
4. Record any conservative adjustments to downstream Phase 2 specs, including
   newly safe parallelization opportunities if they now exist.

**Creates:** Coverage and conformance matrix plus seam-refactor plan
**Touches:** This spec, downstream Phase 2 specs if needed, and planning notes
**Ends with:** The remaining proof work is organized by explicit gaps instead
of guesswork
**Progress keys:** `session-1/review-scan`,
`session-1/coverage-matrix`, `session-1/seam-refactor-plan`,
`session-1/parallelization-update`

---

### Session 2 of 4: Architecture Conformance And Seam Refactoring

**Steps:**
1. Refactor mixed-responsibility code paths into cleaner unit-testable and
   integration-testable seams where the current structure blocks reliable proof.
2. Tighten organization-aware service, API, and UI boundaries so the app
   demonstrates the platform architecture more clearly.
3. Preserve or improve the accepted distributor workflows while reducing hidden
   coupling and test friction.
4. Update any architecture notes that need to reflect the cleaned-up seams.

**Creates:** Cleaner conformance-oriented app and service seams
**Touches:** `src/Apps/`, related libraries, and architecture notes
**Ends with:** The app is easier to prove rigorously without changing its role
as the sample platform harness
**Progress keys:** `session-2/seam-refactors`,
`session-2/service-boundaries`, `session-2/api-conformance`,
`session-2/architecture-notes`

---

### Session 3 of 4: Coverage Gap Closure Across Test Layers

**Steps:**
1. Add or strengthen comprehensive unit coverage for pure logic and business
   rules.
2. Add or strengthen integration coverage for non-UI boundaries, especially
   organization-aware services, API behavior, and security evaluation.
3. Expand bUnit or page-level coverage where the app still needs direct UI
   proof beyond the reusable-surface contract tests.
4. Keep the resulting test pyramid aligned with the Phase 2 roadmap instead of
   drifting toward exhaustive browser-only proof.

**Creates:** Expanded unit, integration, and app-level UI proof
**Touches:** `tests/` plus any small source changes needed to improve test
clarity
**Ends with:** The distributor app becomes a reliable proof harness across the
intended test layers
**Progress keys:** `session-3/unit-coverage`,
`session-3/integration-coverage`, `session-3/app-ui-coverage`,
`session-3/test-pyramid-alignment`

---

### Session 4 of 4: Acceptance Baseline, Queue Rebaseline, And Closeout

**Steps:**
1. Capture the accepted application coverage and conformance baseline for
   downstream E2E and UAT work.
2. Revisit the remaining Phase 2 queue and update only the assumptions that the
   app-proof work materially changed.
3. Append one catch-up session only if the work exposed a direct omission that
   belongs inside this set.
4. Generate `change-log.md` for this session set.

**Creates:** Coverage-conformance baseline and `change-log.md`
**Touches:** Planning docs, downstream Phase 2 specs if needed, and closeout
artifacts
**Ends with:** Later E2E, UAT, documentation, and packaging work can rely on a
stable app-proof baseline
**Progress keys:** `session-4/conformance-baseline`,
`session-4/queue-rebaseline`, `session-4/catch-up-decision`,
`session-4/change-log`
