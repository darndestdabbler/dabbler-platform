# AI Assignment Ledger

> **Purpose:** Per-session record of which AI model handled each reasoning task, plus routing decisions and recommendations for the next session.

> **Session Set:** `crud-scaffolding-spike` (3 sessions total)

---

## Session 1 of 3: Helper And Pattern Inventory

| Step | Task | Model | Reasoning | Cost |
|------|------|-------|-----------|------|
| 1 | Read guidance docs | Orchestrator (Haiku 4.5) | Required reading; no routing needed | $0 |
| 2 | Inventory CRUD helpers | Orchestrator (Haiku 4.5) | Direct code inspection and summarization; routine | $0 |
| 3 | Inventory API patterns | Orchestrator (Haiku 4.5) | Direct code inspection; routine | $0 |
| 4 | Inventory CRUD pages | Orchestrator (Haiku 4.5) | Direct code inspection; routine | $0 |
| 5 | Route structural analysis | Gemini Pro (Tier 2) | Complex reasoning on generation architecture (complexity_hint=50); routed per spec | $0.0276 |
| 6 | Save inventory markdown | Orchestrator (Haiku 4.5) | Direct markdown authoring; routine | $0 |

**Session 1 Total Cost:** $0.0276

**Reasoning for Session 1:**
- Steps 1-4 are inventory and code reading tasks suited to orchestrator direct handling.
- Step 5 is architecture analysis: "What does a generator need to emit at each granularity?" This required reasoning depth (Tier 2 model), so routed to Gemini Pro per delegation discipline.
- Step 6 is direct markdown synthesis from the inventory and routed analysis; no additional routing.

---

## Session 2 of 3: Scaffolding Approach Proposal

| Step | Task | Model | Reasoning | Cost |
|------|------|-------|-----------|------|
| 1 | Read inventory and open design questions | Orchestrator (Opus 4.7) | Required context load; no routing | $0 |
| 2 | Draft scaffolding-approach-proposal.md | Orchestrator (Opus 4.7) | **Direct authoring per spec:** "no `route()` call to the orchestrator's own model." Opus expected as orchestrator; full context applied to design decisions | $0 |
| 3 | Proposal doubles as Opus contribution to Session 3 | Orchestrator (Opus 4.7) | Per feedback memory `feedback_orchestrator_self_contributes_to_review_panel`; disclosed explicitly in proposal | $0 |
| 4 | Build and run markdown checks | (None configured) | No markdown linters found; skipped | $0 |

**Session 2 Total Cost:** $0.00

**Reasoning for Session 2:**
- The spike spec explicitly states Session 2 should be "no `route()` call to the orchestrator's own model." This is by design—the orchestrator (Opus) is trusted to author the proposal directly using the full context of Session 1's inventory and the project's architecture.
- No architectural routing needed; this is an orchestrator-authored synthesis.
- The proposal is rich enough to stand alone for Session 3's tri-provider review without intermediate routing.
- Zero cost reflects the direct-authoring model of this session.

---

## Session 3 of 3: Tri-Provider Review And Synthesize Implementation Spec

| Step | Task | Model | Reasoning | Cost |
|------|------|-------|-----------|------|
| 1 | Read guidance + Session 2 proposal | Orchestrator (Opus 4.7) | Required context load; no routing | $0 |
| 2a | Route proposal review (non-Anthropic #1) | Gemini Pro (call_model direct) | force_model not honored by route(); used call_model() like packaging-readiness Sessions 4-5 | (filled in actuals) |
| 2b | Route proposal review (non-Anthropic #2) | GPT-5.4 (call_model direct) | Tier-3 architectural review; second cross-provider opinion | (filled in actuals) |
| 3 | Synthesize tri-provider review | Orchestrator (Opus 4.7) | Direct authoring per `feedback_orchestrator_self_contributes_to_review_panel`; orchestrator has full proposal-author context; routing to Opus would be paying for the same judgment twice | $0 |
| 4 | Apply small-scope adjustments | Orchestrator (Opus 4.7) | Mechanical edits per disposition; direct authoring | $0 |
| 5 | Author implementation-set spec | Orchestrator (Opus 4.7) | Specification authoring; orchestrator owns the spike output and can be authoritative without routing for second opinion (Session 3 review is the second opinion) | $0 |
| 6 | Cross-provider verify Session 3 | Routed (`session-verification`) | MANDATORY cross-provider per Rule 2; verifier picked automatically (non-Anthropic) | (filled in actuals) |
| 7 | Generate change-log.md (last session) | Routed (`summarization`, tier 1) | Gemini Flash for narrative prose per the convention promoted from `rearchitect-role-administration-dashboard` Session 8 | (filled in actuals) |

### Actuals (filled after the session)

- Orchestrator used: Claude Code / claude-opus-4-7 @ effort=high
- Total routed cost: **~$0.73 USD** (Gemini Pro review $0.0398 + GPT-5.4 review $0.2623 + verification Round 1 GPT-5.4 $0.1869 + verification Round 2 GPT-5.4 $0.1095 + verification Round 3 GPT-5.4 $0.0497 + change-log Gemini Flash $0.0016 + next-set recommendation Gemini Pro $0.0061; rounding noise)
- Deviations from recommendation:
  - Used `call_model()` directly for the two tri-provider reviews (route() does not honor `force_model`). Same pattern as `packaging-and-template-readiness` Sessions 4-5.
  - Required THREE verification rounds (target was 1-2) before VERIFIED. Round 1 surfaced 3 Major + 1 Minor issues — a partial schema-primary capture, a stale `--filter-mode` snippet, a stale Conclusion bullet, and a stale customization-points TODO note. Round 2 confirmed 3 of 4 fixes; the schema-primary fix was incomplete (Section 5 heading still said "Adjunct" and the Session 2 smoke didn't pass `--schema`). Round 3 VERIFIED clean.
  - The orchestrator's `register_session_start()` call OVERWROTE `completedSessions: [1, 2]` from `session-state.json`. The user caught this and asked for a CLAUDE.md re-read. Restored by hand. **Candidate lessons-learned item** for Step 9 review: lightweight-tier orchestrators must preserve `completedSessions[]` when re-registering session start.
- Notes for next-session calibration: This is the last session of the set; next-session-set recommendation lives in `next-set-recommendation.md`.

### Reasoning for Session 3 routing

- Steps 1, 3, 4, 5 are direct orchestrator work (context-laden synthesis and specification authoring). Per the existing convention "When the orchestrator model matches the routing target for a given task type, handle the task in-conversation rather than calling route()," and per `feedback_orchestrator_self_contributes_to_review_panel`, Opus does these directly.
- Steps 2a/2b are the mandatory cross-provider reviews; force_model is required so they are issued via `call_model()` (route() picks the cheapest capable model per task type, which would defeat the explicit force).
- Step 6 (session-verification) ALWAYS routes — Rule 2 of the workflow doc; survives any maxout. The verifier is picked automatically; since the orchestrator is Anthropic, the verifier will be non-Anthropic (gpt-5-4 or gemini-pro).
- Step 7 (change-log) uses `task_type="summarization"` with `max_tier=1` per the convention from `rearchitect-role-administration-dashboard` Session 8.

**Note on Opus-on-Opus review:** Routing the proposal back to Opus via `route(task_type="architecture")` would be paying ~$0.50–$1.00 for a same-provider second opinion. The orchestrator's full-context perspective IS the third panel member, disclosed explicitly in the synthesis. Per `feedback_avoid_upward_routing` and `feedback_orchestrator_self_contributes_to_review_panel`, this is the right call.

---

## Delegation Discipline Summary

**Session 1:**
- Orchestrator handled inventory (5 routine steps).
- Routed architectural analysis (Step 5) to tier-2 model (Gemini Pro) for reasoning depth.
- Result: Balanced cost and reasoning quality for a planning-only session.

**Session 2:**
- Zero routing per spec design.
- Orchestrator (Opus) authored proposal with full context.
- Disclosure of context asymmetry explicit in proposal for Session 3 transparency.

**Session 3 (Predicted):**
- Two external architectural reviews (GPT, Gemini).
- Orchestrator synthesis and spec authoring (direct).
- Cross-provider verification (Sonnet).
- Balanced multi-provider evaluation with orchestrator stewardship.

---

## Notes

- **Session 1 orchestrator:** Claude Haiku 4.5 (smaller, cost-efficient for inventory tasks).
- **Session 2 orchestrator:** Claude Opus 4.7 (as expected per spec; upgraded for proposal authoring).
- **Session 3 orchestrator:** Will be Opus unless the human overrides before session start.
- **All cross-provider verifications:** Use different providers than the orchestrator and follow the router's verification rules (tier matching, preferred pairings, etc.).
