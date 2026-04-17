# Packaging And Template Readiness Spec

> **Purpose:** Validate external packaging, consumer-install flows, and VS
> template readiness for the proven Phase 2 platform while keeping defaults
> platform-generic.
> **Created:** 2026-04-15
> **Session Set:** `docs/session-sets/packaging-and-template-readiness/`
> **Prerequisite:** `phase-2-docs-readiness` is complete and its documentation
> baseline is current.
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification

---

## Project Overview

This set packages the validated platform for outside consumption and prepares
the template story for future consumers.

The work should reflect the narrowed, proven, accessibility-aware contract. It
should not quietly reintroduce broader historical defaults just because package
or template tooling makes them easy to scaffold.

Treat future generated apps and the Access metadata-harvesting CLI as important
downstream consumers, but keep the packaging and template defaults generic
enough that no single consumer becomes the design center.

## Parallelization Notes

This is the last planned Phase 2 set, so no downstream cross-set overlap is
assumed.

Inside this set, once the packable surface and template contract are frozen,
NuGet smoke proof and template wiring can proceed in parallel across disjoint
paths.

## Session Plan

### Session 1 of 7: Repository Cleanup

**Steps:**
1. Read `docs/planning/guiding-principles.md` and
   `docs/planning/lessons-learned.md`.
2. Delete completed session-set folders that are no longer needed, keeping only
   the active and reference sets listed in `FOLDERS_AND_FILES_TO_SAVE.md`.
3. Delete obsolete planning documents from `docs/planning/` that are not on the
   keep list.
4. Delete obsolete docs-level files from `docs/` that are not on the keep list.
5. Remove the `samples/` folder and clean up its project references from
   `Dabbler.Platform.sln`.
6. Confirm `scripts/dev-tools/*.ps1` are in place (moved in `dev-tools-ui`
   session set) and that `scripts/dev-tools/README.md` still points the human
   to the in-app Dev menu as the preferred UX. No further promotion needed —
   the old `scripts/full-app-uat/` folder was removed in that earlier set.
7. Clean up stale file references in `CLAUDE.md`, `GEMINI.md`, `AGENTS.md`, and
   `docs/planning/guiding-principles.md` that point to deleted files.
8. Delete `FOLDERS_AND_FILES_TO_SAVE.md` after cleanup is complete.
9. Build and test to confirm no regressions.

**Creates:** Leaner repository with only active assets
**Touches:** `Dabbler.Platform.sln`, `CLAUDE.md`, `GEMINI.md`, `AGENTS.md`,
`docs/planning/guiding-principles.md`, `scripts/`
**Ends with:** Build and tests pass with no broken references
**Progress keys:** `session-1/delete-session-sets`,
`session-1/delete-planning-docs`, `session-1/delete-docs`,
`session-1/remove-samples`, `session-1/promote-scripts`,
`session-1/fix-references`, `session-1/build-and-test`

---

### Session 2 of 7: Review, Packable Surface Freeze, And Workstream Scan

**Steps:**
1. Review the accepted documentation baseline plus the broader validated Phase 2
   outputs before packaging work starts.
2. Freeze the packable library surface, consumer-install expectations, and the
   template contract that Phase 2 is actually ready to support.
3. Reconfirm how reusable stub mode should be offered as an intentional opt-in
   for future consumers rather than an accidental default.
4. Record any conservative intra-set parallelization opportunities and the
   acceptance criteria for packaging and template readiness.

**Creates:** Packaging readiness ledger and frozen packable-surface contract
**Touches:** Planning notes and this spec if clarification is needed
**Ends with:** The set has explicit boundaries before package and template work
begins
**Progress keys:** `session-2/review-scan`,
`session-2/packable-surface-freeze`, `session-2/stub-opt-in-contract`,
`session-2/workstream-scan`

---

### Session 3 of 7: Package Validation And Consumer Smoke Proof

**Steps:**
1. Validate packability of the relevant libraries and tighten any package
   metadata or build settings needed for external consumption.
2. Add or strengthen consumer-install smoke tests so package consumers can be
   validated without relying on manual confidence alone.
3. Verify that the packaged contract matches the documented supported surface
   and does not leak compatibility-only defaults as the primary story.
4. Record any package-level limitations that must stay explicit for consumers.

**Creates:** Package-validation baseline and consumer smoke proof
**Touches:** Packable library projects, package metadata, and related tests or
docs
**Ends with:** The platform can be consumed externally through a validated
package path
**Progress keys:** `session-3/package-validation`,
`session-3/package-metadata`, `session-3/consumer-smoke-tests`,
`session-3/packaged-contract-check`

---

### Session 4 of 7: VS Template And Stub-Opt-In Readiness

**Steps:**
1. Develop or refine the VS solution template story so it reflects the proven
   Phase 2 platform contract rather than historical assumptions.
2. Reuse the DI-stub architecture in a way that generated or scaffolded apps
   can intentionally opt into without forcing stub mode as the default
   production story.
3. Keep all template defaults platform-generic and aligned with the narrowed
   shell, query, detail, report, security, and testing guidance.
4. Ensure the template story stays compatible with future CLI-driven consumers
   without being specialized to the Access metadata pipeline.

**Creates:** Template-ready platform contract and stub-opt-in scaffolding path
**Touches:** Template assets, related docs, and any template smoke-proof inputs
**Ends with:** Future consumers can start from the proven platform contract
without inheriting accidental historical defaults
**Progress keys:** `session-4/template-contract`,
`session-4/stub-opt-in`, `session-4/platform-generic-defaults`,
`session-4/cli-consumer-compatibility`

---

### Session 5 of 7: Consumer Validation And Template Smoke Proof

**Steps:**
1. Complete end-to-end consumer validation for the packaging and template paths,
   including any focused smoke tests that prove the expected install or
   scaffold experience.
2. Capture the final readiness notes, limitations, and handoff guidance for
   future external consumers.
3. Append one catch-up session only if the packaging or template work exposed a
   direct omission that belongs inside this set.

**Creates:** Consumer-validation baseline and readiness notes
**Touches:** Tests, docs, template assets, and closeout artifacts
**Ends with:** Packaging and template paths are validated for external consumers
**Progress keys:** `session-5/consumer-validation`,
`session-5/template-smoke-proof`, `session-5/catch-up-decision`

---

### Session 6 of 7: Playwright E2E Tests For Packaging And Template Features

**Steps:**
1. Read `docs/planning/guiding-principles.md` and
   `docs/planning/lessons-learned.md`.
2. Identify the user-facing features and workflows introduced or changed by
   this session set (packaging install paths, template scaffolding, stub
   opt-in behavior, consumer-facing configuration).
3. Write Playwright E2E tests that exercise the supported user paths through a
   real browser, following the testing hierarchy: use the same entry points a
   human would use, verify both persistence and visible UI outcomes, and cover
   validation and error states.
4. Ensure all pre-existing Playwright tests still pass alongside the new ones.
5. Build and test the full suite.

**Creates:** New Playwright test files for packaging/template features
**Touches:** `tests/` Playwright project, possibly test fixtures or helpers
**Ends with:** Browser-level confidence that the packaging and template features
work as intended before human UAT
**Progress keys:** `session-6/identify-test-scope`,
`session-6/write-playwright-tests`, `session-6/regression-check`,
`session-6/build-and-test`

---

### Session 7 of 7: Human UAT Checklist And Closeout

**Steps:**
1. Read `docs/planning/guiding-principles.md` and
   `docs/planning/lessons-learned.md`.
2. Generate a human-UAT checklist JSON against the checklist-editor schema,
   covering the packaging, template, and consumer-facing aspects that
   automation cannot fully judge: clarity of install instructions, template
   naming and discoverability, stub opt-in documentation quality, and overall
   consumer experience.
3. Include concrete test data, file paths, and expected outcomes in each
   checklist item so the reviewer can execute without guessing.
4. Verify the checklist loads in the UAT checklist editor.
5. Hand off the checklist to the human for execution. Treat pending human UAT
   as blocking for session-set completion.
6. Generate `change-log.md` for this session set.

**Creates:** Human-UAT checklist JSON and `change-log.md`
**Touches:** This session-set folder
**Ends with:** Checklist ready for human execution; Phase 2 closes with
validated packaging and template readiness
**Progress keys:** `session-7/generate-checklist`,
`session-7/verify-checklist`, `session-7/handoff`,
`session-7/change-log`
