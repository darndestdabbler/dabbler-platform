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

### Session 1 of 4: Review, Packable Surface Freeze, And Workstream Scan

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
**Progress keys:** `session-1/review-scan`,
`session-1/packable-surface-freeze`, `session-1/stub-opt-in-contract`,
`session-1/workstream-scan`

---

### Session 2 of 4: Package Validation And Consumer Smoke Proof

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
**Progress keys:** `session-2/package-validation`,
`session-2/package-metadata`, `session-2/consumer-smoke-tests`,
`session-2/packaged-contract-check`

---

### Session 3 of 4: VS Template And Stub-Opt-In Readiness

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
**Progress keys:** `session-3/template-contract`,
`session-3/stub-opt-in`, `session-3/platform-generic-defaults`,
`session-3/cli-consumer-compatibility`

---

### Session 4 of 4: Consumer Validation, Template Smoke Proof, And Closeout

**Steps:**
1. Complete end-to-end consumer validation for the packaging and template paths,
   including any focused smoke tests that prove the expected install or
   scaffold experience.
2. Capture the final readiness notes, limitations, and handoff guidance for
   future external consumers.
3. Append one catch-up session only if the packaging or template work exposed a
   direct omission that belongs inside this set.
4. Generate `change-log.md` for this session set.

**Creates:** Consumer-validation baseline, readiness notes, and `change-log.md`
**Touches:** Tests, docs, template assets, and closeout artifacts
**Ends with:** Phase 2 closes with validated packaging and template readiness
for external-facing consumers
**Progress keys:** `session-4/consumer-validation`,
`session-4/template-smoke-proof`, `session-4/catch-up-decision`,
`session-4/change-log`
