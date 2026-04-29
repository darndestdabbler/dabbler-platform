# AI Assignment: uat-dsl-watermark-integration

This document tracks the AI orchestrator, recommendations, and actual usage for each session in this set.

## Session 1: Catalog crawler — flag watermarked routes

### Recommended orchestrator
N/A — Session 1 authors this document.

### Rationale
Source-level file scan and schema addition; mechanical work under 50 lines per file, no complex reasoning tasks beyond session-verification.

### Estimated routed cost
Low (session-verification only).

| Step | Action | Routing Decision |
|------|--------|------------------|
| 1 | Read planning docs | Direct |
| 2–4 | Crawler audit + schema + detection impl | Direct (mechanical, single-file edits) |
| 5 | Re-crawl verification | Direct (unit tests serve as proxy; compose stack not available) |
| 6 | Unit tests | Direct |
| 7 | Build + test | Direct |
| 8 | Session verification | `route(session-verification)` → opus (cross-provider target; see actuals) |

### Actuals (filled after session)
- Orchestrator used: claude-code / claude-sonnet-4-6 @ effort=high
- Total routed cost: $0.35 (session-verification, two calls due to console encoding retry — both hit claude-opus cross-provider)
- Deviations from recommendation: None structural. Router selected opus for session-verification because GPT cross-provider was not available as tier-2; same-provider opus was used per existing config.
- Notes: Verifier noted InheritsTrackedRegex is not line-start-anchored (trivial risk in controlled codebase, not blocking).

**Next-session orchestrator recommendation (Session 2):**
`claude-code` / `claude-opus-4-7` @ effort=medium  
Rationale: Session 2 extends the `dabbler-uat-dsl` grammar + compiler across multiple interconnected files (grammar, AST, emitter, tests, NuGet pack). Multi-file structured code generation benefits from Opus-class reasoning; medium effort is sufficient for a well-scoped grammar extension.

---

## Session 2: DSL grammar + compiler — WAIT FOR ACTIONS QUIET verb

### Recommended orchestrator
`claude-code` / `claude-opus-4-7` @ effort=medium

### Rationale
Grammar extension spans grammar file, parser, AST, C# emitter, and tests — multi-file, structured, and logic-precise. Opus-class reasoning avoids emitter regressions.

### Estimated routed cost
Low-moderate (code-review of grammar changes; session-verification).

| Step | Action | Routing Decision |
|------|--------|------------------|
| 1 | Read planning + spec | Direct |
| 2–4 | Grammar + parser + AST + emitter | Direct (mechanical implementation per spec) |
| 5 | Grammar tests | Direct |
| 6 | DSL reference update | Direct |
| 7 | NuGet pack | Direct |
| 8 | Build + test | Direct |
| 9 | Session verification | `route(session-verification)` → cross-provider |
| 10 | Commit | Direct |

### Actuals (filled after session)
- Orchestrator used: claude-code / claude-opus-4-7[1m] @ effort=high (recommendation was effort=medium; opus-4-7 ran at effort=high per the harness default)
- Total routed cost: $0.30 (one session-verification call to opus, cross-provider unavailable for tier-2 anthropic so same-provider opus was used per the existing fallback ladder; consistent with feedback_self_verify_when_no_cross_provider but the router still routed)
- Deviations from recommendation: Effort drift (high vs medium). Pack version bumped 1.0.0 → 1.1.0 to signal grammar addition (not in spec but hygienic).
- Notes: Verifier flagged EmitWatermarkWaits coexistence as transitional debt for Session 3 — the existing implicit-emission flag stays in place this session; Session 3's prompt-template change should adopt the explicit verb and the implicit flag's removal can land then. No structural code changes required.

**Next-session orchestrator recommendation (Session 3):**
`claude-code` / `claude-opus-4-7` @ effort=high
Rationale: Per routed analysis (gemini-pro, $0.0023) — Session 3's mix (precise prompt-template editing, conditional UAT-author rule, diagnostic-first triage if the smoke flakes, end-of-set reorganization review) benefits from high-capability reasoning. Expected routed cost low-to-moderate; primary spend is the delegated calls (uat-script-author, summarization, session-verification) not the orchestrator itself.

---

## Session 3: Prompt update + end-to-end smoke + close

### Recommended orchestrator
`claude-code` / `claude-opus-4-7` @ effort=high

### Actuals (filled after session)
TBD
