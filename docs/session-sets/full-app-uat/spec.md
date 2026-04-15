# Full-Application UAT Spec

> **Purpose:** Perform targeted human confirmation against the full application
> after automation is strong, using a subset of representative Playwright flows
> plus a few intentionally human-only checks.
> **Created:** 2026-04-15
> **Session Set:** `docs/session-sets/full-app-uat/`
> **Prerequisite:** `e2e-workflow-implementation` is complete and its UAT
> handoff notes are current.
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification

---

## Project Overview

This set creates the later human-confirmation lane in the Phase 2 roadmap.

The goal is not to re-run the entire automated suite manually. The goal is to
spot-check the highest-value end-to-end workflows, confirm a few outcomes that
automation alone can miss, and capture remediation inputs before documentation
and packaging work begins.

This work should build directly on the representative Playwright inventory and
stay disciplined about what remains intentionally human-only.

Treat `docs/planning/distributor-acceptance-baseline.md` as the user-facing
domain baseline for checklist wording and scenario framing unless a later
accepted Phase 2 baseline supersedes it explicitly.

## Parallelization Notes

No downstream Phase 2 session set is marked safe to overlap at authoring time.
Session 1 must still reassess that assumption using the final automated-suite
state.

Inside this set, checklist authoring and any narrowly scoped database
verification helpers can proceed in parallel once the selected workflow subset
is frozen.

## Session Plan

### Session 1 of 3: Review, Workflow Subset Selection, And Queue Scan

**Steps:**
1. Review the stabilized representative Playwright suite and all remaining
   Phase 2 session-set specs before defining the full-app UAT lane.
2. Freeze the subset of automated workflows that should receive human
   confirmation, plus the intentionally human-only checks that still add value.
3. Record any conservative adjustments needed in downstream specs, including
   any newly safe parallelization opportunities.
4. Define the issue-capture, remediation, and sign-off rules for this set.

**Creates:** Full-app UAT scope ledger and sign-off rules
**Touches:** This spec, downstream Phase 2 specs if needed, and planning notes
**Ends with:** The UAT pass is deliberately scoped and tied to the automated
baseline
**Progress keys:** `session-1/review-scan`,
`session-1/workflow-subset`, `session-1/signoff-rules`,
`session-1/parallelization-update`

---

### Session 2 of 3: Checklist, Human Execution, And Findings Capture

**Steps:**
1. Generate the full-application UAT checklist JSON based on the selected
   Playwright subset and human-only checks.
2. Add selective verification helpers only where UI confirmation is
   insufficient and backend-truth confirmation materially improves confidence.
3. Execute or facilitate the targeted full-app UAT pass.
4. Capture findings in a durable format that distinguishes confirmed defects,
   acceptable differences, and deferred follow-up.

**Creates:** Full-app UAT checklist JSON, selective verification helpers, and
findings ledger
**Touches:** `docs/`, minimal helper scripts if needed, and downstream notes
**Ends with:** The full application has targeted human confirmation against the
most meaningful remaining risks
**Progress keys:** `session-2/checklist-json`,
`session-2/verification-helpers`, `session-2/uat-execution`,
`session-2/findings-ledger`

---

### Session 3 of 3: Remediation Inputs, Final Baseline, And Closeout

**Steps:**
1. Triage the findings and complete any small remediation that clearly belongs
   inside this set, or record follow-up inputs where broader work is required.
2. Capture the accepted full-app UAT baseline and what it does or does not
   guarantee.
3. Update downstream documentation and packaging assumptions only where the UAT
   results justify it.
4. Generate `change-log.md` for this session set.

**Creates:** Full-app UAT baseline, remediation inputs, and `change-log.md`
**Touches:** Planning docs, downstream Phase 2 specs if needed, and closeout
artifacts
**Ends with:** Documentation and packaging work can start from a human-checked
application baseline
**Progress keys:** `session-3/findings-triage`,
`session-3/uat-baseline`, `session-3/downstream-updates`,
`session-3/change-log`
