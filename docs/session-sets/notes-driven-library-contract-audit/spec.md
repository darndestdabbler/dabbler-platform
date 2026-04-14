# Notes-Driven Library Contract Audit Spec

> **Purpose:** Audit the reusable library surface against
> `docs/planning/user-functionality-notes.md`, document which parts remain core,
> and force explicit decisions on functionality that exceeds the notes-driven
> contract.
> **Created:** 2026-04-14
> **Session Set:** `docs/session-sets/notes-driven-library-contract-audit/`
> **Prerequisite:** `rebuild-consolidation` is complete and
> `docs/planning/notes-driven-library-contract-audit.md` exists as the current
> baseline.
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification

---

## Project Overview

This session set does not trim code immediately.

It produces the evidence package and decision ledger needed to simplify
`src/Libs/` safely:

- compare current library surfaces to the notes-driven contract
- map real `src/Apps/` consumption
- identify what remains core, what should become advanced/non-core, and what
  should likely be removed
- package the out-of-scope features into explicit human decisions with
  recommendations

The output of this set becomes the entry gate for all later simplification,
app-alignment, and template work.

## Session Plan

### Session 1 of 5: Surface Inventory And Contract Mapping

**Steps:**
1. Inventory the current public-facing surfaces under `src/Libs/` that affect
   reusable component, page, report, and security behavior.
2. Compare each surface to `docs/planning/user-functionality-notes.md`.
3. Record an initial classification for each surface: keep as core, simplify in
   place, rebuild focused replacement, isolate as advanced/non-core, or remove.

**Creates:** Surface inventory and initial classification notes
**Touches:** Planning docs under `docs/planning/`
**Ends with:** The repo has one auditable baseline describing what the notes
contract actually requires from the reusable libraries
**Progress keys:** `session-1/surface-inventory`,
`session-1/notes-mapping`, `session-1/initial-classification`

---

### Session 2 of 5: App And Template Consumption Map

**Steps:**
1. Map where `src/Apps/` currently consumes each major library surface.
2. Check whether `template-seed/` or workflow assets assume any of the same
   surfaces or defaults.
3. Record which functionality is actively paying for itself versus which remains
   mostly speculative.

**Creates:** App/template consumption map and evidence notes
**Touches:** Planning docs under `docs/planning/`
**Ends with:** Every major library feature can be described as proven by app
usage, template usage, both, or neither
**Progress keys:** `session-2/app-usage-map`,
`session-2/template-usage-map`, `session-2/evidence-summary`

---

### Session 3 of 5: Decision Ledger And Human Decision Queue

**Steps:**
1. Turn the audit into a durable decision ledger with feature-level
   recommendations.
2. For each out-of-scope or ambiguous feature, record whether it is used in the
   app, how strongly the notes support it, the maintenance/testing cost, and a
   recommendation to keep, isolate, or remove.
3. Update later session-set specs if the audit changes their assumptions or
   ordering.

**Creates:** Decision ledger and explicit human decision queue
**Touches:** Planning docs and later session-set specs if assumptions drift
**Ends with:** Later sessions no longer need to rediscover what is core versus
advanced
**Progress keys:** `session-3/decision-ledger`,
`session-3/human-decision-queue`, `session-3/spec-rebaseline`

---

### Session 4 of 5: Coverage Baseline And Queue Rebaseline

**Steps:**
1. Map current unit, bUnit, integration, and Playwright coverage to the
   retained core surfaces.
2. Record test gaps that later simplification and app-alignment sessions must
   close before removing or isolating functionality.
3. Revisit the remaining session-set queue and append one catch-up session only
   if the audit exposed a directly related omission that belongs in this set.

**Creates:** Coverage-gap baseline and queue rebaseline notes
**Touches:** Planning docs and this session set's active spec
**Ends with:** The repo has an explicit coverage baseline and a clear answer on
whether this audit needs one more planning session before closeout
**Progress keys:** `session-4/coverage-baseline`,
`session-4/test-gap-map`, `session-4/queue-rebaseline`

---

### Session 5 of 5: Coverage Gate And Closeout

**Steps:**
1. Convert the Session 4 gap map into an explicit coverage gate for retained
   core reusable surfaces, including where dedicated `Dabbler.Components` and
   `Dabbler.Reports` tests must exist before simplification retires adjacent
   functionality.
2. Rebaseline downstream session-set assumptions so simplification starts with
   a documented quality bar for retained non-security reusable surfaces.
3. Generate `change-log.md` for this session set.

**Creates:** Coverage-gate handoff and `change-log.md`
**Touches:** Planning docs, downstream session-set specs if sequencing or
assumptions change, and this session set's closeout artifacts
**Ends with:** The audit closes with both a decision ledger and an explicit
coverage gate for the retained core surface
**Progress keys:** `session-5/coverage-gate`,
`session-5/downstream-rebaseline`, `session-5/change-log`
