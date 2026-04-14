# Notes-Driven App Alignment Spec

> **Purpose:** Align `src/Apps/` with the simplified notes-driven library
> surface so the demo app proves the intended platform workflows without
> depending on advanced or deprecated features.
> **Created:** 2026-04-14
> **Session Set:** `docs/session-sets/notes-driven-app-alignment/`
> **Prerequisite:** `notes-driven-core-surface-simplification` is complete and
> its compatibility notes are current.
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification

---

## Project Overview

This session set makes the demo app the proof that the simplified library
surface is sufficient.

The target is not to preserve every existing app/library integration point. The
target is to ensure the app demonstrates the notes-driven workflows using only
the retained core surfaces.

## Session Plan

### Session 1 of 4: App Dependency Audit And Migration Map

**Steps:**
1. Audit where the app still depends on advanced, isolated, or deprecated
   library functionality.
2. Create a migration map from old surfaces to the retained core surfaces or
   focused replacements.
3. Identify any app pages or layouts that still reflect pre-simplification
   library assumptions.

**Creates:** App dependency audit and migration map
**Touches:** Planning docs and app-alignment notes
**Ends with:** Every remaining app dependency on the old broad surface is known
before edits begin
**Progress keys:** `session-1/app-dependency-audit`,
`session-1/migration-map`, `session-1/alignment-baseline`

---

### Session 2 of 4: Query, Detail, Report, And Dashboard Alignment

**Steps:**
1. Update list/query/detail/report/dashboard app pages to use only the retained
   notes-driven library surface.
2. Remove app-side reliance on advanced grid/filter/query/report behaviors that
   were isolated or retired.
3. Preserve user-facing behavior that still belongs to the notes-driven
   contract.

**Creates:** Aligned app pages using the simplified library surface
**Touches:** `src/Apps/AcmeCoffeeOnline.Web/` and related tests
**Ends with:** The app's core workflows run on the supported component surface
instead of the broader historical one
**Progress keys:** `session-2/query-alignment`,
`session-2/detail-alignment`, `session-2/report-alignment`,
`session-2/dashboard-alignment`

---

### Session 3 of 4: Shell, Navigation, Security, And Workflow Proof

**Steps:**
1. Align shell, navigation, popup, and security-admin usage with the simplified
   core surfaces.
2. Remove app-side dependencies on isolated shell/catalog/dashboard extras.
3. Add or strengthen integration and Playwright coverage that proves the
   notes-driven workflows end to end.
4. Confirm at least one representative mutating workflow remains backend
   verified.

**Creates:** Aligned shell/security usage and stronger end-to-end proof
**Touches:** App layout/pages and app/integration/Playwright tests
**Ends with:** The app is a trustworthy proof that the narrowed library
contract still supports the intended workflows
**Progress keys:** `session-3/shell-alignment`,
`session-3/security-alignment`, `session-3/e2e-proof`,
`session-3/backend-verification`

---

### Session 4 of 4: Acceptance Baseline, Rebaseline, And Closeout

**Steps:**
1. Capture the accepted notes-driven app baseline after the alignment pass.
2. Revisit later session sets, especially template work, and update assumptions
   based on what the app actually needed.
3. Append one catch-up session only if the app-alignment work exposed a direct
   omission that belongs in this set.
4. Generate `change-log.md` for this session set.

**Creates:** Acceptance baseline and `change-log.md`
**Touches:** Planning docs, session-set specs, and closeout artifacts
**Ends with:** The app now serves as the living proof harness for the narrowed
platform surface
**Progress keys:** `session-4/acceptance-baseline`,
`session-4/queue-rebaseline`, `session-4/change-log`
