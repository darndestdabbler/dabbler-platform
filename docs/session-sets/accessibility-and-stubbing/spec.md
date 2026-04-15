# Platform Accessibility And Stubbing Spec

> **Purpose:** Finish the direct proof of the retained platform surface and
> establish reusable DI-driven stubbing as a supported platform capability.
> **Created:** 2026-04-15
> **Session Set:** `docs/session-sets/accessibility-and-stubbing/`
> **Prerequisite:** `notes-driven-core-surface-simplification`,
> `notes-driven-app-alignment`, and `notes-driven-template-encoding` are
> complete, and `docs/planning/phase-2-platform-roadmap.md` remains the Phase 2
> source of truth.
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification

---

## Project Overview

Phase 2 begins by proving the reusable platform directly instead of depending
on app smoke coverage to imply correctness.

This set has two durable goals:

- close remaining direct contract-test and accessibility gaps across the
  retained platform surface
- promote DI-driven stubbing from an ad hoc test technique into a supported
  platform feature that works consistently in bUnit, stub or demo modes, and
  future generated apps

Treat `docs/planning/notes-driven-supported-library-surface.md`,
`docs/planning/notes-driven-app-acceptance-baseline.md`, and
`docs/planning/user-functionality-notes.md` as binding inputs. This set should
not re-open retired catalog/query-report assumptions while strengthening the
retained reusable seams.

## Parallelization Notes

Initial assumption: no downstream Phase 2 session set is marked safe to run in
parallel at authoring time. Session 1 must re-evaluate that assumption against
the current repo state and explicitly record any safe overlap before it is
used.

Inside this set, the most likely safe parallel slices are:

- `Dabbler.Components` contract and accessibility work versus
  `Dabbler.Reports.Blazor` contract work once the shared stub abstractions are
  frozen
- bUnit harness work versus WCAG evidence capture once the rendering seams and
  provider contracts are stable

Session 1 revalidated the broad sequential Phase 2 queue and found only one
safe cross-set overlap: `stubbed-uat` may begin its Session 1 review and
Session 2 checklist or harness preparation after this set completes Session 3
and freezes the reusable stub abstractions. `stubbed-uat` Session 3 remains
blocked until this set's Session 4 has produced enough stub-backed proof and
reviewable surfaces to avoid false negatives.

## Session Plan

### Session 1 of 5: Review, Rebaseline, And Parallelization Scan

**Steps:**
1. Review the current `notes-driven-template-encoding` state, the Phase 2
   roadmap, and all remaining Phase 2 session-set specs before implementation
   starts.
2. Confirm the retained platform surface and app baseline still match the
   assumptions this set is about to prove directly.
3. Record any adjustments needed to this session set or downstream Phase 2
   specs, including any newly safe cross-set or intra-set parallelization that
   is conservative enough to use.
4. Freeze the initial coverage targets for components, reports, and
   security-adjacent UI seams so later sessions are working from one audit
   ledger.

**Creates:** Accessibility-and-stubbing review ledger and updated execution
notes
**Touches:** This spec, downstream Phase 2 specs if needed, and planning notes
**Ends with:** The set starts from current assumptions and any safe
parallelization is explicitly documented rather than implied
**Progress keys:** `session-1/review-ledger`,
`session-1/queue-adjustments`, `session-1/parallelization-scan`,
`session-1/coverage-target-freeze`

---

### Session 2 of 5: Contract Audit And Accessibility Baseline

**Steps:**
1. Audit retained `Dabbler.Components`, `Dabbler.Reports.Blazor`, and
   security-adjacent UI surfaces against the supported-surface docs and Phase 2
   roadmap expectations.
2. Identify direct coverage gaps, with special attention to
   `ParameterPromptForm`, `ReportViewer`, role-aware UI state, and keyboard or
   focus behavior.
3. Define the WCAG 2.1 AA verification approach for reusable components and
   pages, combining automated tooling with focused manual validation guidance.
4. Capture a durable gap ledger and accessibility evidence plan that later
   sessions can implement directly.

**Creates:** Contract-gap ledger, accessibility baseline, and WCAG evidence
plan
**Touches:** Planning docs and test-work notes
**Ends with:** The remaining reusable-surface and accessibility proof work is
known before implementation begins
**Progress keys:** `session-2/contract-audit`,
`session-2/report-gap-ledger`, `session-2/accessibility-baseline`,
`session-2/wcag-evidence-plan`

---

### Session 3 of 5: Reusable Stub Contracts And Provider Infrastructure

**Steps:**
1. Define the reusable DI abstractions needed for stub and real provider
   swapping across query and grid data, report data, metadata and layout,
   navigation and workspace state, and security or organization context where
   required.
2. Implement the initial stub providers and DI registration seams in a way that
   supports bUnit tests, stub or demo app modes, and future generated apps
   without making stubs a repo-internal trick.
3. Add guidance or examples showing how the new stubs are intended to be
   consumed from tests and stub-backed app modes.
4. Keep the contract platform-generic so the future Access metadata CLI remains
   only one consumer rather than the design center.

**Creates:** Reusable stub abstractions, default stub providers, and DI wiring
guidance
**Touches:** `src/Libs/`, app/demo-mode seams where needed, and related docs
**Ends with:** The repo has a first-class stub architecture that later tests,
UAT, and packaging work can build on directly
**Progress keys:** `session-3/stub-abstractions`,
`session-3/provider-seams`, `session-3/di-wiring`,
`session-3/stub-consumer-guidance`

---

### Session 4 of 5: Direct Proof With bUnit And Accessibility Evidence

**Steps:**
1. Add or strengthen direct reusable-surface tests for retained component and
   report contracts using the new stub seams where appropriate.
2. Close coverage gaps for rendering contracts, validation, accessibility
   semantics, role-aware and organization-aware UI state, and keyboard or focus
   behavior where practical.
3. Capture WCAG 2.1 AA evidence for the reusable surfaces using the agreed
   automated and focused manual checks.
4. Ensure stub-backed proof exists in both dedicated test homes and at least
   one stub or demo app consumption path.

**Creates:** Expanded bUnit or direct UI contract tests and accessibility
evidence artifacts
**Touches:** `tests/Dabbler.Components.Tests/`,
`tests/Dabbler.Reports.Blazor.Tests/`, security-adjacent UI tests, and
supporting docs
**Ends with:** The retained reusable surface has direct contract proof and
accessibility evidence instead of relying on inference
**Progress keys:** `session-4/components-proof`,
`session-4/reports-proof`, `session-4/accessibility-evidence`,
`session-4/stub-backed-consumption`

---

### Session 5 of 5: Stub-Mode Guidance, Queue Rebaseline, And Closeout

**Steps:**
1. Document the accepted reusable stub capability and its intended consumers,
   including bUnit, stub or demo modes, early UAT, and future generated apps.
2. Revisit the remaining Phase 2 queue and update assumptions based on what the
   direct accessibility and stubbing work actually required.
3. Append one catch-up session only if the work exposed a direct omission that
   belongs inside this set.
4. Generate `change-log.md` for this session set.

**Creates:** Stub-mode guidance, queue rebaseline notes, and `change-log.md`
**Touches:** Planning docs, downstream Phase 2 specs, and closeout artifacts
**Ends with:** Later UAT, app, documentation, and packaging work can rely on a
proven reusable platform baseline
**Progress keys:** `session-5/stub-guidance`,
`session-5/queue-rebaseline`, `session-5/catch-up-decision`,
`session-5/change-log`
