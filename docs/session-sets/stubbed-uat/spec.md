# Stubbed Design And Navigation UAT Spec

> **Purpose:** Use the reusable stub capability to perform early human design
> and navigation spot testing before the full application is fully wired.
> **Created:** 2026-04-15
> **Session Set:** `docs/session-sets/stubbed-uat/`
> **Prerequisite:** `accessibility-and-stubbing` is complete and accepted, with
> reusable stub abstractions frozen and the direct-proof or accessibility
> surfaces ready for review without false negatives.
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification

---

## Project Overview

This set creates the early UAT lane described in the Phase 2 roadmap.

The goal is not exhaustive acceptance testing. The goal is to use stub-backed
UI surfaces to catch layout, focus, labeling, shell, workspace, and
page-to-page navigation issues while they are still cheap to fix.

This work should stay tightly scoped to design and navigation proof. It should
not quietly turn into full-app functional validation or replace the later
representative Playwright and full-application UAT sets.

## Parallelization Notes

Session 1 should re-check the queue, but the current baseline is now simpler:
`accessibility-and-stubbing` has completed and the stub-backed review surfaces
are ready. No new overlap beyond the existing sequential queue is assumed.

Inside this set, checklist authoring and lightweight reviewer-harness setup can
proceed in parallel once the review scope is frozen.

## Session Plan

### Session 1 of 3: Review, Scope Freeze, And Queue Scan

**Steps:**
1. Review the completed accessibility-and-stubbing outputs plus all remaining
   Phase 2 session-set specs before defining the early UAT lane.
2. Confirm which stub-backed pages and navigation paths are mature enough for
   human review without creating false negatives caused by incomplete wiring.
3. Record any conservative adjustments to this set or downstream specs,
   including any newly safe parallelization opportunities.
4. Freeze the review scope, issue-capture format, and exit criteria for this
   set.

Session 1 should treat `/platform/stub` plus the evidence-backed retained
shell, workspace, prompt, and report surfaces from
`docs/planning/accessibility-and-stubbing-evidence.md` as the primary starting
scope.

**Creates:** Early-UAT scope ledger and reviewer issue format
**Touches:** This spec, downstream Phase 2 specs if needed, and planning notes
**Ends with:** The set has a realistic stub-backed scope and any safe
parallelization is explicitly recorded
**Progress keys:** `session-1/review-scan`, `session-1/scope-freeze`,
`session-1/issue-format`, `session-1/parallelization-update`

---

### Session 2 of 3: Checklist And Reviewer Harness

**Steps:**
1. Generate a lightweight JSON UAT checklist for stub-backed design and
   navigation review.
2. Focus the checklist on layout, tab order, visible focus, labeling, menu
   behavior, shell and workspace behavior, and page-to-page navigation.
3. Create any lightweight reviewer instructions, launch profiles, or helper
   scripts needed so the stub-backed review is fast and repeatable.
4. Ensure the checklist and harness clearly distinguish human-only observations
   from issues that should already be covered by automated tests.

**Creates:** Stub-backed UAT checklist JSON and reviewer harness guidance
**Touches:** `docs/`, stub-mode guidance, and any small helper assets required
**Ends with:** Reviewers can exercise the stub-backed UI deliberately instead
of ad hoc
**Progress keys:** `session-2/checklist-json`,
`session-2/reviewer-guidance`, `session-2/harness-prep`,
`session-2/human-vs-automation-boundary`

---

### Session 3 of 3: Early UAT Findings, Remediation Inputs, And Closeout

**Steps:**
1. Execute or facilitate the stub-backed design and navigation spot-testing
   pass using the new checklist.
2. Capture findings in a durable format that clearly separates confirmed issues,
   deferred issues, and observations that belong to later full-app work.
3. Feed the resulting remediation inputs back into the remaining Phase 2 queue,
   updating later specs only where the findings justify it.
4. Generate `change-log.md` for this session set.

**Creates:** Early-UAT findings ledger, remediation inputs, and `change-log.md`
**Touches:** Planning docs, downstream Phase 2 specs if needed, and closeout
artifacts
**Ends with:** Design and navigation issues are surfaced early enough to inform
the rest of Phase 2
**Progress keys:** `session-3/uat-execution`,
`session-3/findings-ledger`, `session-3/remediation-inputs`,
`session-3/change-log`
