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
TBD

**Next-session orchestrator recommendation (Session 3):**
TBD

---

## Session 3: Prompt update + end-to-end smoke + close

### Recommended orchestrator
TBD (filled after Session 2)

### Actuals (filled after session)
TBD
