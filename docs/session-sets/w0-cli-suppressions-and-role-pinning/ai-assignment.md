# AI Assignment: w0-cli-suppressions-and-role-pinning

This document tracks the AI orchestrator, recommendations, and actual usage for each session in this set.

## Session 1: W0 CLI `--suppressions` flag

### Recommended orchestrator
N/A — Session 1 authors this document

### Rationale
This document is authored as part of Session 1. The recommendation for Session 2 is provided below.

### Estimated routed cost
low

| Step | Action | Routing Decision |
|------|--------|------------------|
| 1 | Read planning docs | Direct |
| 2 | Audit `Dabbler.Uat.Dsl.Cli` and `tools/uat-suppressions.json` schema | Direct |
| 3 | Add `--suppressions <path>` flag handling to `validate` and `compile` commands | Direct (mechanical, single-file CLI + new matcher class) |
| 4 | Update JSON output shape: `warnings` array, post-downgrade verdict, exit code | Direct |
| 5 | Implement unit tests (no flag, matching, non-matching, wildcard) | Direct |
| 6 | Pack `Dabbler.Uat.Dsl.Cli` 1.2.0 NuGet package | Direct |
| 7 | Run `dotnet build` and `dotnet test` | Direct |
| 8 | Cross-provider session verification | `route(session-verification, max_tier=2)` |
| 9 | Commit changes | Direct |

### Actuals (filled after the session)
- Orchestrator used: claude-code / claude-opus-4-7[1m] @ effort=high
- Total routed cost: ~$0.84 (analysis $0.013 gemini-pro for ai-assignment authoring; session-verification $0.40 + $0.43 — two opus calls, see Deviations)
- Deviations from recommendation: Session-verification routed to **same-provider opus** despite `max_tier=2` and the cross-provider preference; this is the same router-config issue called out in `feedback_self_verify_when_no_cross_provider.md` and the lessons-learned entry promoted 2026-04-29. Round-1 verifier call landed on opus and completed cleanly, but the orchestrator's stdout printer crashed on cp1252 console encoding before the review was saved, forcing a re-call ($0.43) — net $0.83 against opus when self-verify would have cost $0. Action item for the human: the routing-config fix referenced in lessons-learned would have prevented this.
- Notes for next-session calibration: (a) Always save session-review to disk BEFORE attempting to print result.content — the review file is what matters, the console echo is incidental. (b) The verifier produced a clear PASS verdict but did NOT lead with the structured JSON block the verification.md template requires; opus on this prompt size returns a markdown table instead. The orchestrator parsed the human-readable verdict ("✅ PASS — All 15 acceptance criteria met"). One observation was flagged non-blocking and pre-existing (compile-path JSON omits `nearMatches`/`routePath` while validate-path includes them) — left untouched per the verifier's own non-blocking tag and `feedback_fix_broken_tests_no_carry_forward.md`'s "no scope creep" intent.

**Next-session orchestrator recommendation (Session 2):**
`claude-code` / `claude-opus-4-7` @ effort=high
Rationale: Session 2 involves an audit-driven design choice followed by multi-file structured implementation in either grammar/AST/visitor (directive path) or Python runner + W0 CLI flag (persona-inference path); Opus-class structural code reasoning is the right fit.

---

## Session 2: Validator role context

### Recommended orchestrator
`claude-code` / `claude-opus-4-7` @ effort=high

### Rationale
Session 2 starts with a small audit and design decision (directive vs. persona-inference), then implements one of two multi-file structured changes. Opus-class reasoning avoids parser/visitor regressions on the directive path and keeps the runner-side mapping coherent on the inference path.

### Estimated routed cost
moderate

| Step | Action | Routing Decision |
|------|--------|------------------|
| 1 | Read planning docs and Session 1 outcome | Direct |
| 2 | Audit 3–5 recent UAT checklists for persona handling | Direct |
| 3 | Decide directive vs. persona-inference; log reasoning | Direct |
| 4 | Implement chosen path (grammar/AST/visitor OR runner.py + W0 `--active-role` flag) | Direct |
| 5 | Unit tests (happy path, no role context, role-not-in-catalog) | Direct |
| 6 | Pack updated NuGet (1.2.1 if directive path; 1.2.0 already covers CLI flag) | Direct |
| 7 | `dotnet build && dotnet test` in changed repos | Direct |
| 8 | Cross-provider session verification | `route(session-verification, max_tier=2)` |
| 9 | Commit changes | Direct |

### Actuals (filled after the session)
- Orchestrator used: TBD
- Total routed cost: TBD
- Deviations from recommendation: TBD
- Notes for next-session calibration: TBD

**Next-session orchestrator recommendation (Session 3):**
`claude-code` / `claude-opus-4-7` @ effort=high
Rationale: Session 3 coordinates a NuGet bump, recrawl, routed authoring, runner loop, possible flake investigation, change-log generation, and an end-of-set reorg review — high-stakes orchestration that benefits from Opus-class judgment if the smoke flakes mid-loop.

---

## Session 3: Re-run watermark smoke (the deferred 5/5 proof)

### Recommended orchestrator
`claude-code` / `claude-opus-4-7` @ effort=high

### Rationale
Session 3 is primarily orchestration but high-stakes: any smoke flake triggers the diagnostic-instrumentation-first discipline, and the end-of-set reorganization review proposes durable promotions. Opus-class reasoning keeps the loop and the reorg call coherent.

### Estimated routed cost
high

| Step | Action | Routing Decision |
|------|--------|------------------|
| 1 | Read planning docs + Sessions 1–2 outcomes | Direct |
| 2 | Bump `Directory.Packages.props` to W0 1.2.0/1.2.1 | Direct |
| 3 | Re-crawl `docs/component-catalog/index.json` against compose stack | Direct |
| 4 | Author fresh `/admin/products` checklist | `route(uat-script-author)` → sonnet |
| 5 | W0 validate + compile with `--suppressions` (and `--active-role` if S2 chose that path) | Direct |
| 6 | Run UAT runner 5 iterations; quality bar 5/5 PASS | Direct (with diagnostic-first discipline if any iteration fails) |
| 7 | Generate `change-log.md` | `route(summarization, max_tier=1)` → gemini-flash |
| 8 | `dotnet build && dotnet test` (excluding Playwright suite) | Direct |
| 9 | Cross-provider session verification | `route(session-verification, max_tier=2)` |
| 10 | Commit, push, send notification | Direct |
| 11 | Step 9 reorganization review (3 candidates) | Direct (proposals only; human accepts/rejects) |

### Actuals (filled after the session)
- Orchestrator used: TBD
- Total routed cost: TBD
- Deviations from recommendation: TBD
- Notes for next-session calibration: TBD

**Next-session orchestrator recommendation:**
N/A — final session in this set.
