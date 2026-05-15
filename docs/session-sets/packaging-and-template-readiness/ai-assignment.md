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
Moderate (revised 2026-05-15 after dropping the `force_model="opus"` forcing on documentation calls — see `feedback_avoid_upward_routing`. Per-doc refresh now lands on the router's default for `documentation` (Gemini Pro typical) instead of Opus.)

| Step | Action | Routing Decision |
| :--- | :--- | :--- |
| 1 | Read `docs/planning/project-guidance.md` and `docs/planning/lessons-learned.md`. | direct work |
| 2 | Inventory every consumer-facing doc. | `route(task_type="analysis")` |
| 3 | For each doc on the list, route a refresh pass. | `route(task_type="documentation")` — let router pick (Gemini Pro typical); escalation ladder lifts to higher tier if response is truncated. Do NOT pass `force_model="claude-opus-4-7"` — routing Sonnet→Opus on a tier-2 task pays twice. |
| 4 | Identify docs that are stale enough to delete or that duplicate other docs. | `route(task_type="analysis")` |
| 5 | Build and run any markdown-aware checks. | direct work |

### Actuals
- **Orchestrator used:** Claude Code (claude-opus-4-7, high effort) — operator
  declined to swap to the recommended Sonnet @ low effort, kept Opus.
- **Total routed cost:** ~$0.86 (33 calls)
  - 15 documentation-refresh routed calls (gemini-pro, ~$0.31)
  - 15 auto-cross-provider verifier calls on the refresh sweep
    (gpt-5-4-mini, ~$0.23)
  - 1 routed Session-3 next-orchestrator analysis (gemini-pro, $0.003)
  - 3 session-verification rounds (gpt-5-4 each, $0.116 + $0.115 + $0.083 = $0.314)
- **Deviations from recommendation:** Orchestrator ran at Opus high effort
  rather than Sonnet low. Operator-confirmed deviation at session start.
  Cost impact was the orchestrator-side reasoning depth (paid in Anthropic
  conversation tokens, not in routed-call cost). Future sessions in this set
  should still honor the spec-sized orchestrator unless the operator
  explicitly chooses to swap.
- **Notes for next-session calibration:** The 15-doc refresh sweep at
  gemini-pro defaults landed at ~$0.02 average per doc — well under
  conservative estimates. The auto-verifier critiques (gpt-5-4-mini)
  added ~$0.015 per doc on top and surfaced concrete missing-detail
  callouts that would have been missed by the doc-refresh model alone.
  The 3 session-verification rounds were a pattern of "verifier flags
  real concerns but with one context-gap finding per round" — be ready
  to dispute findings via `record_adjudication` and to tighten the
  verification bundle iteratively. The Round-3 ISSUES_FOUND was Minor
  and referred to a transient prompt artifact, not the deliverable;
  retry budget was exhausted at 2/2 retries.
- **Session verification:** ISSUES_FOUND (1 Minor, transient prompt only,
  not deliverable; retry budget 2/2 exhausted; documented in
  disposition.json).
- **Adjudications:** 1 — Round-1 Issue 2
  (`Dabbler.Security.Blazor` `[Authorize(Roles="SystemAdmin")]` example),
  cause=context-gap, resolution=accept-dismissal.

**Next-session orchestrator recommendation (routed analysis, gemini-pro,
$0.003):** Claude Code (`claude-opus-4-7`, medium effort). Routed analysis
overrode the original ai-assignment recommendation (Sonnet low) because
Session 3 authors a canonical, high-leverage entry-point document by
synthesizing from multiple sources, and weak initial drafts trigger
costly verification re-work cycles. The next orchestrator may swap to
Sonnet medium if context permits.

---

## Session 3: Author Consolidated Platform Overview

### Recommended orchestrator (engine + model + effort)
Claude Code (claude-sonnet-4-6, low effort)

### Rationale
Similar to session 2, the orchestrator coordinates the authoring and integration of a single key document via routing. Sonnet is the most cost-effective choice for this orchestration work. If Session 3's first routed draft comes back structurally inadequate at Step 7 verification, escalation should be a re-run with `complexity_hint=70` (lifts to tier 3 once) or — preferred — a follow-up session under an Opus orchestrator that writes the rewrite directly. Do not force `Opus` on the initial routed call.

### Estimated routed cost
Moderate (revised 2026-05-15 — dropped the `force_model="opus"` forcing on the documentation call. See `feedback_avoid_upward_routing`.)

| Step | Action | Routing Decision |
| :--- | :--- | :--- |
| 1 | Read planning docs and the refresh log from Session 2. | direct work |
| 2 | Route to author `docs/platform-overview.md`. | `route(task_type="documentation")` — let router pick. If the cross-provider verifier flags structural gaps at Step 7, re-route with `complexity_hint=70` once; if still inadequate, escalate to an Opus-orchestrator follow-up session per the avoid-upward-routing principle. |
| 3 | Wire the overview into the doc graph. | `route(task_type="refactoring")` |
| 4 | Cross-provider verify the overview. | `route(task_type="session-verification")` |
| 5 | Build and run markdown checks. | direct work |

### Actuals
- **Orchestrator used:** Claude Code (claude-opus-4-7, medium effort) — operator
  chose to stay on Opus medium per Session 2 routed analysis (over the
  spec's Sonnet-low recommendation), citing the high-leverage nature of
  authoring the canonical platform overview.
- **Total routed cost:** ~$0.27
  - 1 routed `documentation` call (gemini-pro, $0.0545) drafting the
    initial overview. Auto-verified inline by gpt-5-4-mini, which flagged
    Major gaps (under-length, missing Components primitives, compressed
    Security baked-in role list, thin adoption path).
  - 3 session-verification rounds (gpt-5-4 each, $0.0997 + $0.1062 + $0.0122
    = $0.218). Round 1 caught a real Major (NuGet adoption-story
    inconsistency between overview and per-library READMEs); Round 2
    caught a sub-issue in the Round-1 fix (over-broad note that swept
    in third-party deps + a remaining first-party NuGet link in
    Dependencies); Round 3 VERIFIED.
  - 1 routed Session-4 next-orchestrator analysis (gemini-pro, $0.0019)
    recommending Claude Code Opus high.
- **Deviations from recommendation:** Orchestrator ran at Opus medium
  rather than Sonnet low. Operator-confirmed at session start per the
  Session 2 routed-analysis re-recommendation. The pattern in this set
  has been "spec said Sonnet low; operator runs Opus" — calibration
  signal for future doc-authoring sessions: the spec-sized Sonnet-low
  estimate may be too low for canonical entry-point doc work.
- **Notes for next-session calibration:**
  - The 595-line overview required orchestrator-side revision to address
    the initial routed draft's gaps (gemini-pro produced ~215 lines vs
    spec target 300-500). For high-leverage canonical-doc work, future
    sessions may want to either (a) route to Sonnet (the existing
    documentation prompt template favors verbose output) or (b) seed
    the prompt with explicit per-section bullet lists derived from the
    source READMEs.
  - The 3-round verification arc reflected the cumulative-fix pattern
    `feedback_verification_round_depth` describes: send full file
    contents on Round 1, then tighten the bundle on retries. Round 2
    introduced its own sub-issue (over-broad fix), which the verifier
    caught — Round 3 was cheap ($0.0122) because the bundle was narrow.
- **Session verification:** VERIFIED (gpt-5-4, Round 3 of 3 — Rounds 1
  and 2 ISSUES_FOUND, fixes applied, Round 3 clean).
- **Adjudications:** None.

**Next-session orchestrator recommendation (routed analysis, gemini-pro,
$0.0019):** Claude Code (`claude-opus-4-7`, high effort). Rationale: the
orchestrator must perform two distinct, high-reasoning tasks — authoring
its own architectural review from scratch and synthesizing three complex
provider assessments. This requires maximum model capability and aligns
with the original spec ai-assignment recommendation.

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