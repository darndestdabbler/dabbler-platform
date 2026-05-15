# AI Assignment — packaging-and-template-readiness

This assignment guides an 11-session set of work divided into two phases. The first phase (Sessions 2-4) validates that the platform is worth packaging by refreshing its documentation and subjecting its value proposition to a tri-provider review. The second phase (Sessions 5-11) executes the packaging work itself, from articulating and reviewing an approach to implementing NuGet packages, a Visual Studio template, and end-to-end test coverage.

## Session 1: Repository Cleanup (Residuals)

### Recommended orchestrator (engine + model + effort)
Claude Code (claude-opus-4-7, low effort)

### Rationale
The orchestrator is already running from a prior context and has the necessary project-level awareness to complete this residual cleanup. The work is simple file deletion, which Opus can handle directly without routing.

### Estimated routed cost
Low

| Step | Action | Routing Decision |
| :--- | :--- | :--- |
| 1 | Read `docs/planning/project-guidance.md` and `docs/planning/lessons-learned.md`. | direct work |
| 2 | Delete orphan top-level session-state directories. | direct work |
| 3 | Delete one-off Python scripts at the repo root. | direct work |
| 4 | Build and test to confirm no regressions. | direct work |

### Actuals
- **Orchestrator used:** Claude Code (claude-opus-4-7, high effort)
- **Total routed cost:** $0.0832 (analysis routing for ai-assignment.md $0.0410 + session-verification $0.0422)
- **Deviations from recommendation:** Orchestrator effort ran at `high` rather than the recommended `low`. The deviation was harmless — Session 1 reasoning load was light enough that effort level did not materially affect direct work cost; routing-pulled costs were unchanged. For future sessions, the recommended effort tier should be honored.
- **Notes for next-session calibration:** Mid-session, the orchestrator discovered 5 pre-existing deterministic test failures (4 in `TemplateSeedSmokeTests.cs`, 1 in `WorkflowAssetSmokeTests.cs`) for assets deleted weeks ago. These were folded into Session 1 with operator approval; the spec was updated to reflect the added scope. Future sessions should expect orphan-test discoveries when build/test is run after long gaps between cleanup passes.
- **Session verification:** `VERIFIED` (gpt-5-4 via cross-provider routing, $0.0422)

**Next-session orchestrator recommendation:** Claude Code (claude-sonnet-4-6, low effort)

---

## Session 2: Refresh Existing Platform-Facing Documentation

### Recommended orchestrator (engine + model + effort)
Claude Code (claude-sonnet-4-6, low effort)

### Rationale
The orchestrator's role is primarily coordination, inventorying documents and routing them for refresh tasks. Sonnet is the cheapest orchestrator capable of managing this workflow.

### Estimated routed cost
High

| Step | Action | Routing Decision |
| :--- | :--- | :--- |
| 1 | Read `docs/planning/project-guidance.md` and `docs/planning/lessons-learned.md`. | direct work |
| 2 | Inventory every consumer-facing doc. | `route(task_type="analysis")` |
| 3 | For each doc on the list, route a refresh pass. | `route(task_type="documentation", force_model="claude-opus-4-7")` |
| 4 | Identify docs that are stale enough to delete or that duplicate other docs. | `route(task_type="analysis")` |
| 5 | Build and run any markdown-aware checks. | direct work |

### Actuals
- **Total routed cost:** `TBD`
- **Session verification:** `TBD`
- **Session verification notes:**
  - `TBD`

**Next-session orchestrator recommendation:** Claude Code (claude-sonnet-4-6)

---

## Session 3: Author Consolidated Platform Overview

### Recommended orchestrator (engine + model + effort)
Claude Code (claude-sonnet-4-6, low effort)

### Rationale
Similar to session 2, the orchestrator coordinates the authoring and integration of a single key document via routing. Sonnet is the most cost-effective choice for this orchestration work.

### Estimated routed cost
High

| Step | Action | Routing Decision |
| :--- | :--- | :--- |
| 1 | Read planning docs and the refresh log from Session 2. | direct work |
| 2 | Route to author `docs/platform-overview.md`. | `route(task_type="documentation", force_model="claude-opus-4-7")` |
| 3 | Wire the overview into the doc graph. | `route(task_type="refactoring")` |
| 4 | Cross-provider verify the overview. | `route(task_type="session-verification")` |
| 5 | Build and run markdown checks. | direct work |

### Actuals
- **Total routed cost:** `TBD`
- **Session verification:** `TBD`
- **Session verification notes:**
  - `TBD`

**Next-session orchestrator recommendation:** Claude Code (claude-opus-4-7)

---

## Session 4: Tri-Provider Platform Value Review

### Recommended orchestrator (engine + model + effort)
Claude Code (claude-opus-4-7, moderate effort)

### Rationale
The orchestrator must write its own high-quality assessment and synthesize three distinct architectural reviews. Opus is required for this direct reasoning and synthesis work per the spec.

### Estimated routed cost
High

| Step | Action | Routing Decision |
| :--- | :--- | :--- |
| 1 | Read planning and documentation artifacts. | direct work |
| 2 | Assemble the review prompt. | direct work |
| 3 | Write orchestrator's own assessment. | direct work |
| 4 | Route the same prompt to the other two providers. | `route(task_type="architecture", force_model="gpt-5-4")`<br>`route(task_type="architecture", force_model="gemini-pro")` |
| 5 | Synthesize all three assessments. | direct work |
| 6 | If the review surfaces a critical value gap, pause and check with the human. | direct work |

### Actuals
- **Total routed cost:** `TBD`
- **Session verification:** `TBD`
- **Session verification notes:**
  - `TBD`

**Next-session orchestrator recommendation:** Claude Code (claude-opus-4-7)

---

## Session 5: Packaging Approach Articulation And Tri-Provider Review

### Recommended orchestrator (engine + model + effort)
Claude Code (claude-opus-4-7, high effort)

### Rationale
The orchestrator must draft a complex, multi-faceted technical proposal and then synthesize reviews of it. Opus is required for this high-level authoring and reasoning per the spec.

### Estimated routed cost
High

| Step | Action | Routing Decision |
| :--- | :--- | :--- |
| 1 | Read planning docs and `platform-value-review.md`. | direct work |
| 2 | Draft `packaging-approach-proposal.md`. | direct work |
| 3 | Route the proposal for review to the other two providers. | `route(task_type="architecture", force_model="gpt-5-4")`<br>`route(task_type="architecture", force_model="gemini-pro")` |
| 4 | Synthesize its own proposal-author perspective plus the two routed reviews. | direct work |
| 5 | Apply small-scope adjustments inline. | `route(task_type="refactoring")` |

### Actuals
- **Total routed cost:** `TBD`
- **Session verification:** `TBD`
- **Session verification notes:**
  - `TBD`

**Next-session orchestrator recommendation:** Claude Code (claude-sonnet-4-6)

---

## Session 6: Packable Surface Freeze And Workstream Scan

### Recommended orchestrator (engine + model + effort)
Claude Code (claude-sonnet-4-6, low effort)

### Rationale
This session translates the approved proposal into planning artifacts and contracts, a task well-suited to a capable but cost-effective orchestrator. Sonnet fits the "heavy file mechanics" profile for this type of session.

### Estimated routed cost
Moderate

| Step | Action | Routing Decision |
| :--- | :--- | :--- |
| 1 | Read the Session 5 deliverables. | direct work |
| 2 | Freeze the packable library surface, consumer-install expectations, and template contract. | `route(task_type="planning")` |
| 3 | Reconfirm how reusable stub mode is offered. | `route(task_type="documentation")` |
| 4 | Record any conservative intra-set parallelization opportunities. | `route(task_type="planning")` |

### Actuals
- **Total routed cost:** `TBD`
- **Session verification:** `TBD`
- **Session verification notes:**
  - `TBD`

**Next-session orchestrator recommendation:** Claude Code (claude-sonnet-4-6)

---

## Session 7: Package Validation And Consumer Smoke Proof

### Recommended orchestrator (engine + model + effort)
Claude Code (claude-sonnet-4-6, moderate effort)

### Rationale
The work involves modifying project files, package metadata, and authoring tests, which aligns with Sonnet's strength in code-centric orchestration. It can route specific generation or review tasks as needed.

### Estimated routed cost
Moderate

| Step | Action | Routing Decision |
| :--- | :--- | :--- |
| 1 | Validate packability of the relevant libraries and tighten metadata/settings. | `route(task_type="refactoring")` |
| 2 | Add or strengthen consumer-install smoke tests. | `route(task_type="test-generation")` |
| 3 | Verify that the packaged contract matches the documented supported surface. | `route(task_type="code-review")` |
| 4 | Record any package-level limitations. | `route(task_type="documentation")` |

### Actuals
- **Total routed cost:** `TBD`
- **Session verification:** `TBD`
- **Session verification notes:**
  - `TBD`

**Next-session orchestrator recommendation:** Claude Code (claude-sonnet-4-6)

---

## Session 8: VS Template And Stub-Opt-In Readiness

### Recommended orchestrator (engine + model + effort)
Claude Code (claude-sonnet-4-6, high effort)

### Rationale
This session involves heavy file mechanics creating and wiring a VS template, which is Sonnet's ideal use case. The orchestrator will manage many file edits and route any complex logic generation.

### Estimated routed cost
Moderate

| Step | Action | Routing Decision |
| :--- | :--- | :--- |
| 1 | Develop or refine the VS solution template story. | `route(task_type="refactoring")` |
| 2 | Reuse the DI-stub architecture for intentional opt-in. | `route(task_type="refactoring")` |
| 3 | Keep all template defaults platform-generic and aligned with guidance. | `route(task_type="code-review")` |
| 4 | Ensure the template story stays compatible with future CLI-driven consumers. | `route(task_type="analysis")` |

### Actuals
- **Total routed cost:** `TBD`
- **Session verification:** `TBD`
- **Session verification notes:**
  - `TBD`

**Next-session orchestrator recommendation:** Claude Code (claude-sonnet-4-6)

---

## Session 9: Consumer Validation And Template Smoke Proof

### Recommended orchestrator (engine + model + effort)
Claude Code (claude-sonnet-4-6, low effort)

### Rationale
This session focuses on authoring tests and final documentation, a coordination and routing task suitable for a cost-effective Sonnet orchestrator.

### Estimated routed cost
Moderate

| Step | Action | Routing Decision |
| :--- | :--- | :--- |
| 1 | Complete end-to-end consumer validation for the packaging and template paths. | `route(task_type="test-generation")` |
| 2 | Capture the final readiness notes, limitations, and handoff guidance. | `route(task_type="documentation")` |
| 3 | Append one catch-up session only if a direct omission was exposed. | direct work |

### Actuals
- **Total routed cost:** `TBD`
- **Session verification:** `TBD`
- **Session verification notes:**
  - `TBD`

**Next-session orchestrator recommendation:** Claude Code (claude-sonnet-4-6)

---

## Session 10: Playwright E2E Tests For Packaging And Template Features

### Recommended orchestrator (engine + model + effort)
Claude Code (claude-sonnet-4-6, moderate effort)

### Rationale
Authoring Playwright tests is a code-heavy task involving file creation and modification, making Sonnet the most cost-effective orchestrator choice.

### Estimated routed cost
Moderate

| Step | Action | Routing Decision |
| :--- | :--- | :--- |
| 1 | Read `docs/planning/project-guidance.md` and `docs/planning/lessons-learned.md`. | direct work |
| 2 | Identify the user-facing features and workflows introduced or changed. | `route(task_type="analysis")` |
| 3 | Write Playwright E2E tests. | `route(task_type="test-generation")` |
| 4 | Ensure all pre-existing Playwright tests still pass alongside the new ones. | direct work |
| 5 | Build and test the full suite. | direct work |

### Actuals
- **Total routed cost:** `TBD`
- **Session verification:** `TBD`
- **Session verification notes:**
  - `TBD`

**Next-session orchestrator recommendation:** Claude Code (claude-sonnet-4-6)

---

## Session 11: Human UAT Checklist And Closeout

### Recommended orchestrator (engine + model + effort)
Claude Code (claude-sonnet-4-6, low effort)

### Rationale
The session requires generating a structured JSON checklist and a markdown summary, both routing-centric tasks well-suited to the Sonnet orchestrator.

### Estimated routed cost
Moderate

| Step | Action | Routing Decision |
| :--- | :--- | :--- |
| 1 | Read `docs/planning/project-guidance.md` and `docs/planning/lessons-learned.md`. | direct work |
| 2 | Generate a human-UAT checklist JSON. | `route(task_type="uat-script-author")` |
| 3 | Include concrete test data, file paths, and expected outcomes. | (quality constraint on previous step) |
| 4 | Verify the checklist loads in the UAT checklist editor. | direct work |
| 5 | Hand off the checklist to the human for execution. | direct work |
| 6 | Generate `change-log.md` for this session set. | `route(task_type="summarization")` |

### Actuals
- **Total routed cost:** `TBD`
- **Session verification:** `TBD`
- **Session verification notes:**
  - `TBD`