# Packaging And Template Readiness Spec

> **Purpose:** Validate that the platform delivers significant value to outside
> consumers, then package it (NuGet libraries + VS template) so future consumers
> can adopt it without inheriting accidental historical defaults.
> **Created:** 2026-04-15
> **Revised:** 2026-05-15 — added comprehensive documentation work (Sessions 2-3),
> tri-provider platform-value review (Session 4), and tri-provider packaging-
> approach review (Session 5) ahead of the existing packaging sessions.
> **Session Set:** `docs/session-sets/packaging-and-template-readiness/`
> **Prerequisite:** None. Documentation work that earlier specs deferred to a
> `phase-2-docs-readiness` set is absorbed into Sessions 2-3 of this set.
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification

---

## Session Set Configuration

```yaml
requiresUAT: true
requiresE2E: true
uatScope: per-set
```

> Rationale: The packaging and template paths produce user-visible behavior
> (the install experience, scaffolded apps, opt-in stub mode) that automation
> cannot fully judge. A single end-of-set UAT checklist covers the cumulative
> consumer-facing surface. Playwright E2E coverage is required for any
> consumer-facing UI introduced or changed by template scaffolding.

---

## Project Overview

This set has two phases:

1. **Establish that the platform is worth packaging** (Sessions 2-4). Refresh
   the platform-facing documentation, author a consolidated overview, and run a
   tri-provider review (Opus, GPT-5.4, Gemini-Pro) to confirm the platform
   delivers significant value to someone trying to build an application. Any
   value gaps surfaced here are dispositioned before packaging begins.

2. **Package the validated platform for outside consumption** (Sessions 5-11).
   Articulate a packaging approach, get it reviewed by the same tri-provider
   panel, then execute: surface freeze, NuGet smoke proof, VS template story,
   consumer validation, Playwright E2E coverage, and human UAT.

The work should reflect the narrowed, proven, accessibility-aware contract. It
should not quietly reintroduce broader historical defaults just because package
or template tooling makes them easy to scaffold.

Treat future generated apps and the Access metadata-harvesting CLI as important
downstream consumers, but keep the packaging and template defaults generic
enough that no single consumer becomes the design center.

## Parallelization Notes

This is the last planned Phase 2 set, so no downstream cross-set overlap is
assumed.

Inside this set:

- Sessions 4 (value review) and 5 (packaging-approach articulation + review)
  are independent and could theoretically run in parallel worktrees. In
  practice, run them sequentially — the value review's gap list can usefully
  inform what the packaging approach must surface.
- Once the packable surface and template contract are frozen (Session 6),
  NuGet smoke proof (Session 7) and template wiring (Session 8) can proceed
  in parallel across disjoint paths.

## Session Plan

### Session 1 of 11: Repository Cleanup (Residuals)

> **History:** The bulk of the originally planned repository cleanup was
> executed as a direct commit on 2026-04-16 (`8730107`, "Repository cleanup
> and packaging-and-template-readiness spec update") — 17 completed
> session-set folders, 43 obsolete planning documents, 6 obsolete docs-level
> files, the `samples/` folder, `FOLDERS_AND_FILES_TO_SAVE.md`, and stale
> references in `CLAUDE.md`/`GEMINI.md`/`AGENTS.md` were already removed.
> This session, run 2026-05-15, narrows Session 1 to the residual cleanup
> that accumulated between that direct commit and today's spec revision.
> The 36 completed session sets currently under `docs/session-sets/` are
> retained as recent reference material (operator decision, 2026-05-15).

**Steps:**
1. Read `docs/planning/project-guidance.md` and
   `docs/planning/lessons-learned.md`.
2. Delete orphan top-level session-state directories:
   `composable-crud-helpers/` and `unified-master-details-composite/`. These
   are stale duplicates of their canonical session-set folders under
   `docs/session-sets/` (which are complete, VERIFIED, with `change-log.md`).
3. Delete one-off Python scripts at the repo root that are not referenced
   anywhere in the codebase: `gen_checklist.py`, `run_changelog.py`,
   `run_verification.py`, and `run_gate_tests.py`. Each was a one-shot
   script for a specific past session and serves no ongoing purpose; the
   underlying capabilities live in `ai_router/` and `uat_runner/`.
4. Remove orphan tests for directories that were deleted weeks ago:
   delete `tests/AcmeCoffeeOnline.Web.Tests/TemplateSeedSmokeTests.cs`
   (4 tests; `template-seed/` deleted 2026-05-04 in `f80ec18`) and
   remove the `AiRouterConfig_LoadsConsolidatedPromptTemplates` test
   from `tests/AcmeCoffeeOnline.Web.Tests/WorkflowAssetSmokeTests.cs`
   (`ai-router/` deleted in `e0d7126` after migration to the pip-installed
   `ai_router` package). The other test in `WorkflowAssetSmokeTests.cs`
   (`RepoRoot_ResolvesToSolutionDirectory`) stays.
5. Build and test to confirm no regressions.

**Creates:** Nothing.
**Touches:** Deletes 7 paths and trims 1 test method: 2 orphan top-level
directories, 4 top-level Python scripts, 1 orphan test file, and the
`AiRouterConfig_LoadsConsolidatedPromptTemplates` method.
**Ends with:** Build and tests pass; no broken references.
**Progress keys:** `session-1/delete-orphan-dirs`,
`session-1/delete-one-off-scripts`, `session-1/remove-orphan-tests`,
`session-1/build-and-test`

---

### Session 2 of 11: Refresh Existing Platform-Facing Documentation

**Steps:**
1. Read `docs/planning/project-guidance.md` and
   `docs/planning/lessons-learned.md`.
2. Inventory every consumer-facing doc: top-level `README.md`, `CLAUDE.md` /
   `GEMINI.md` / `AGENTS.md` (consumer-relevant portions only), all of `docs/`
   that a future platform consumer might read, and per-library READMEs under
   `src/Libs/*/`. Capture the inventory as a working list inside this session
   (no committed inventory file required).
3. For each doc on the list, route a refresh pass via
   `route(task_type="documentation")` to Opus — the doc may need an accuracy
   check against current code, a scope correction (removing references to
   retired features), or a tightening pass. Cross-provider verification of
   each refresh follows the standard Step 7 procedure.
4. Identify docs that are stale enough to delete or that duplicate other docs;
   merge or remove with the operator's approval before proceeding.
5. Build and run any markdown-aware checks the repo already has (link
   validation if configured). Spot-check the refreshed docs render correctly.

**Creates:** Refreshed consumer-facing docs (in place); a short refresh log
inside this session's review folder summarizing what changed and what was
removed.
**Touches:** `README.md`, `docs/**/*.md`, `src/Libs/**/README.md`,
consumer-relevant portions of `CLAUDE.md` / `GEMINI.md` / `AGENTS.md`.
**Ends with:** Every consumer-facing doc reflects the platform's current
state; the refresh log is committed alongside the session review.
**Progress keys:** `session-2/inventory-docs`,
`session-2/refresh-existing-docs`, `session-2/merge-or-delete-stale`,
`session-2/markdown-checks`

---

### Session 3 of 11: Author Consolidated Platform Overview

**Steps:**
1. Read `docs/planning/project-guidance.md`,
   `docs/planning/lessons-learned.md`, and the refresh log from Session 2.
2. Route via `route(task_type="documentation")` to Opus to author
   `docs/platform-overview.md` — the canonical entry-point doc for a future
   platform consumer. The overview must cover: what the platform is, the
   problems it solves, the libraries that ship and what each provides, the
   intended adoption path, the opt-in stub-mode story, the accessibility
   posture, and a pointer to per-library deep-dive docs.
3. Wire the overview into the doc graph: link to it from the root `README.md`
   and from any `docs/` index docs that already exist. Add reciprocal links
   from per-library READMEs back to the overview where relevant.
4. Cross-provider verify the overview via Step 7 (the cross-provider verifier
   is positioned to catch missing pieces — accept its critique seriously).
5. Build and run markdown checks.

**Creates:** `docs/platform-overview.md`; updated cross-links from existing
docs.
**Touches:** `README.md`, `docs/`, `src/Libs/**/README.md`.
**Ends with:** A future consumer can read one doc and understand what the
platform is and how to adopt it.
**Progress keys:** `session-3/draft-overview`,
`session-3/wire-doc-graph`, `session-3/cross-provider-verify`,
`session-3/markdown-checks`

---

### Session 4 of 11: Tri-Provider Platform Value Review

**Steps:**
1. Read `docs/planning/project-guidance.md` and
   `docs/planning/lessons-learned.md`.
2. Assemble the review prompt: the refreshed docs (Session 2), the consolidated
   overview (Session 3), the high-level repo layout, and the explicit question:
   *"Does this platform provide significant value for someone trying to build
   an application? What is its value proposition? What gaps would deter an
   external adopter?"* The prompt should be identical across the three
   providers and self-contained — assume each reviewer has no prior context.
3. The orchestrator (Opus) writes its own assessment of the review prompt
   directly — no `route()` call to Opus, since Opus→Opus is wasteful and the
   orchestrator already has the full context loaded. Save the orchestrator's
   assessment as `value-review-opus.md` inside this session-set folder.
4. Route the same prompt to the other two providers, one call each:
   - `route(task_type="architecture", force_model="gpt-5-4")`.
   - `route(task_type="architecture", force_model="gemini-pro")`.
   If a `force_model` parameter isn't supported in the operator-tuned config,
   the orchestrator may directly invoke each provider via the underlying router
   primitive — the important property is that both non-Opus providers actually
   run. Save each output as `value-review-gpt-5-4.md` and
   `value-review-gemini-pro.md`.
5. The orchestrator (Opus) synthesizes all three assessments — its own plus
   the two routed responses — into
   `docs/session-sets/packaging-and-template-readiness/platform-value-review.md`.
   The synthesis must record: each reviewer's verdict, areas of agreement,
   areas of disagreement, and a consolidated gap list with a disposition for
   each gap (fix-now, fix-during-packaging, accept-as-limitation, or defer).
6. If the review surfaces a critical value gap — something that would render
   packaging premature — pause and check with the human before continuing.
   Otherwise record the dispositions and proceed.

**Creates:** `platform-value-review.md` synthesis inside this session-set
folder; the three raw assessments (`value-review-opus.md`,
`value-review-gpt-5-4.md`, `value-review-gemini-pro.md`) preserved alongside.
**Touches:** This session-set folder.
**Ends with:** A written, tri-provider-validated answer to "is this platform
worth packaging" with explicit gap dispositions.
**Progress keys:** `session-4/assemble-prompt`,
`session-4/orchestrator-assessment`, `session-4/route-gpt-5-4`,
`session-4/route-gemini-pro`, `session-4/synthesize`,
`session-4/disposition-gaps`

---

### Session 5 of 11: Packaging Approach Articulation And Tri-Provider Review

**Steps:**
1. Read `docs/planning/project-guidance.md`,
   `docs/planning/lessons-learned.md`, and `platform-value-review.md` from
   Session 4 (any gap dispositions feed into the packaging approach).
2. The orchestrator (Opus) drafts `packaging-approach-proposal.md` inside
   this session-set folder directly — no `route()` call to Opus, since the
   orchestrator already has the full context loaded. The proposal must cover:
   which `src/Libs/*` libraries package (and which stay internal), NuGet
   metadata (package id naming, versioning strategy, targeted frameworks,
   license, repo link), how `Dabbler.Platform.Stubs` is offered as an explicit
   opt-in rather than a default, the VS template surface (what the template
   scaffolds, what defaults it embeds, how it stays generic across future
   CLI-driven consumers), and the consumer-install validation strategy. The
   draft itself doubles as Opus's review input for Step 4.
3. Route the proposal for review to the other two providers, one call each:
   - `route(task_type="architecture", force_model="gpt-5-4")`.
   - `route(task_type="architecture", force_model="gemini-pro")`.
   Each given the same self-contained prompt with the explicit question:
   *"Does this packaging approach deliver the platform's value cleanly to
   outside consumers? What would you change?"*. Save outputs as
   `packaging-review-gpt-5-4.md` and `packaging-review-gemini-pro.md`.
4. The orchestrator (Opus) synthesizes its own proposal-author perspective
   plus the two routed reviews into `packaging-approach-review.md` inside
   this session-set folder. Same shape as Session 4's synthesis: verdicts,
   agreements, disagreements, and an adjustment list with dispositions.
5. Apply small-scope adjustments inline (proposal-text edits, naming changes,
   defaults adjustments). For larger adjustments that change session 6-11
   scope, record them as inputs to Session 6's surface freeze rather than
   inflating this session.

**Creates:** `packaging-approach-proposal.md` (Opus-authored, doubles as the
Opus review input) and `packaging-approach-review.md` (synthesis) inside this
session-set folder; the two routed reviews
(`packaging-review-gpt-5-4.md`, `packaging-review-gemini-pro.md`) preserved
alongside.
**Touches:** This session-set folder.
**Ends with:** A written, tri-provider-validated packaging approach ready
for the surface freeze.
**Progress keys:** `session-5/draft-proposal`,
`session-5/route-gpt-5-4`, `session-5/route-gemini-pro`,
`session-5/synthesize`, `session-5/apply-adjustments`

---

### Session 6 of 11: Packable Surface Freeze And Workstream Scan

**Steps:**
1. Read the Session 5 deliverables and treat them as the binding inputs.
2. Freeze the packable library surface, consumer-install expectations, and the
   template contract that Phase 2 is actually ready to support, applying the
   adjustments from Session 5.
3. Reconfirm how reusable stub mode is offered as an intentional opt-in for
   future consumers rather than an accidental default.
4. Record any conservative intra-set parallelization opportunities and the
   acceptance criteria for packaging and template readiness.

**Creates:** Packaging readiness ledger and frozen packable-surface contract
**Touches:** Planning notes and this spec if clarification is needed
**Ends with:** The set has explicit boundaries before package and template work
begins
**Progress keys:** `session-6/apply-review-findings`,
`session-6/packable-surface-freeze`, `session-6/stub-opt-in-contract`,
`session-6/workstream-scan`

---

### Session 7 of 11: Package Validation And Consumer Smoke Proof

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
**Progress keys:** `session-7/package-validation`,
`session-7/package-metadata`, `session-7/consumer-smoke-tests`,
`session-7/packaged-contract-check`

---

### Session 8 of 11: VS Template And Stub-Opt-In Readiness

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
**Progress keys:** `session-8/template-contract`,
`session-8/stub-opt-in`, `session-8/platform-generic-defaults`,
`session-8/cli-consumer-compatibility`

---

### Session 9 of 11: Consumer Validation And Template Smoke Proof

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
**Progress keys:** `session-9/consumer-validation`,
`session-9/template-smoke-proof`, `session-9/catch-up-decision`

---

### Session 10 of 11: Playwright E2E Tests For Packaging And Template Features

**Steps:**
1. Read `docs/planning/project-guidance.md` and
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
**Progress keys:** `session-10/identify-test-scope`,
`session-10/write-playwright-tests`, `session-10/regression-check`,
`session-10/build-and-test`

---

### Session 11 of 11: Human UAT Checklist And Closeout

**Steps:**
1. Read `docs/planning/project-guidance.md` and
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
**Progress keys:** `session-11/generate-checklist`,
`session-11/verify-checklist`, `session-11/handoff`,
`session-11/change-log`
