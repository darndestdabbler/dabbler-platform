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

## Recommended Routing for Session 3 of 3: Tri-Provider Review

**Spec Requirement:**
- Route the scaffolding-approach-proposal to two non-Anthropic providers (GPT-5.4, Gemini Pro), one call each.
- Same self-contained prompt for both verifiers (proposal text + review question).
- Orchestrator (Opus) synthesizes the two external reviews + its own proposal-author perspective into `scaffolding-approach-review.md`.
- Author the implementation-set spec.
- Cross-provider verify Session 3's outputs.

**Predicted Session 3 Routing:**

| Step | Task | Recommended Model | Reasoning |
|------|------|-------------------|-----------|
| 2a | Route proposal review to GPT | GPT-5.4 (force_model) | Non-Anthropic provider for independent evaluation; tier 3 for architecture task |
| 2b | Route proposal review to Gemini | Gemini Pro (force_model) | Non-Anthropic provider; tier 2 capable for architecture review |
| 3 | Synthesize reviews + write analysis | Opus (Orchestrator) | Direct authoring; orchestrator has full context (Session 1, Session 2 proposal, two external reviews) |
| 4 | Apply adjustments | Opus (Orchestrator) | Likely mechanical edits to proposal; direct authoring |
| 5 | Author implementation-set spec | Opus (Orchestrator) | Specification writing; direct authoring |
| 7 | Cross-provider verify Session 3 | Sonnet 4.6 (auto-verify) | Different provider from orchestrator (Opus/Anthropic); tier 2 sufficient for spec review |

**Estimated Session 3 Cost:** ~$0.15–$0.25 USD (depending on proposal length and review depth)

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
