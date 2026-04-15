# Phase 2 Documentation Readiness Spec

> **Purpose:** Create the technical and user-facing documentation for the
> validated Phase 2 platform after accessibility, app proof, automation, and
> targeted human confirmation are complete.
> **Created:** 2026-04-15
> **Session Set:** `docs/session-sets/phase-2-docs-readiness/`
> **Prerequisite:** `full-app-uat` is complete, and the direct accessibility
> work from `accessibility-and-stubbing` is complete and accepted.
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification

---

## Project Overview

Documentation should describe the proven Phase 2 platform, not an unstable
intermediate state.

This set should update technical docs, user docs, README guidance, architecture
references, and test-strategy guidance so they reflect:

- the retained reusable platform surface
- reusable stub mode and its intended consumers
- WCAG 2.1 AA expectations and evidence posture
- organization-aware security and seeded personas
- the intended test pyramid, including representative Playwright and split UAT

The guidance must stay platform-first. The future Access metadata tooling is a
downstream consumer, not the center of the documentation story.

## Parallelization Notes

No downstream Phase 2 session set is marked safe to overlap at authoring time.
The packaging and template readiness set still waits for this documentation set
to complete.

Inside this set, once the source-of-truth outline is frozen, technical and
user-facing docs can proceed in parallel across disjoint files.

## Session Plan

### Session 1 of 3: Review, Documentation Audit, And Queue Scan

**Steps:**
1. Review the accepted accessibility, app, E2E, and full-app UAT baselines plus
   the final remaining Phase 2 spec before documentation work starts.
2. Audit the current docs for stale `AcmeCoffeeOnline` assumptions, outdated
   testing guidance, missing stub-mode explanation, and missing
   organization-aware security guidance.
3. Freeze the documentation outline and source-of-truth ownership for the Phase
   2 story.
4. Record any conservative adjustments needed in the final packaging and
   template set, including newly safe parallelization opportunities if any now
   exist.

**Creates:** Documentation gap audit and source-of-truth outline
**Touches:** This spec, the final Phase 2 spec if needed, and planning notes
**Ends with:** The documentation work is organized around a deliberate outline
and current product reality
**Progress keys:** `session-1/review-scan`,
`session-1/doc-gap-audit`, `session-1/source-of-truth-outline`,
`session-1/parallelization-update`

---

### Session 2 of 3: Technical And User Documentation Updates

**Steps:**
1. Update README, architecture docs, workflow docs, and test-strategy docs so
   they describe the validated Phase 2 platform accurately.
2. Document reusable stub mode, accessibility expectations, organization-aware
   security, seeded personas, and the intended testing pyramid.
3. Update user-facing guidance where needed so the distributor story, admin
   flows, and organization switching behavior are understandable.
4. Remove or rewrite stale historical assumptions that no longer belong in the
   Phase 2 documentation set.

**Creates:** Updated technical and user-facing documentation baseline
**Touches:** `README.md`, `docs/`, and related guidance artifacts
**Ends with:** The repo documentation reflects the validated Phase 2 system
instead of earlier intermediate assumptions
**Progress keys:** `session-2/readme-updates`,
`session-2/architecture-docs`, `session-2/user-guidance`,
`session-2/test-strategy-docs`

---

### Session 3 of 3: Documentation Verification, Queue Rebaseline, And Closeout

**Steps:**
1. Verify that the updated documentation is internally consistent with the
   accepted Phase 2 baselines and test evidence.
2. Revisit the final packaging and template readiness set and adjust only the
   assumptions that documentation work materially changed.
3. Append one catch-up session only if the documentation work exposed a direct
   omission that belongs inside this set.
4. Generate `change-log.md` for this session set.

**Creates:** Documentation verification notes and `change-log.md`
**Touches:** Docs, the final Phase 2 spec if needed, and closeout artifacts
**Ends with:** The repo has a stable documentation baseline for external-facing
packaging and template work
**Progress keys:** `session-3/doc-verification`,
`session-3/queue-rebaseline`, `session-3/catch-up-decision`,
`session-3/change-log`
