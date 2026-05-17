# AI Assignment — crud-scaffolding-remediation

## Session 1: Cross-platform schema applicator

### Recommended orchestrator
Claude Sonnet 4.6 @ effort=medium

### Rationale
This session involves mechanical C# console app development with a clear, verifiable contract (byte-identical output to a script). Sonnet 4.6 is the cheapest-capable model with strong repo-specific .NET conventions context for this type of task.

### Estimated routed cost
low

| Step | Action | Routing Decision |
|------|--------|------------------|
| 1 | Audit `Apply-SupplierSchema.ps1` to catalog substitutions | route(Gemini Pro) |
| 2 | Create `Dabbler.SchemaApplicator` console app | Orchestrator |
| 3 | Add unit tests for byte-identical output | Orchestrator |
| 4 | Verify cross-platform execution | Orchestrator |
| 5 | Update documentation to reference the new applicator | route(Sonnet) |

### Actuals (filled after the session)
- Orchestrator used: claude-code Opus 4.7 @ effort=high (DEVIATION — see below)
- Total routed cost: ~$0.20 (~$0.02 ai-assignment authoring + $0.17 cross-provider session-verification + ~$0.01 next-session-recommendation call)
- Deviations from recommendation: Ran on Opus instead of the recommended Sonnet 4.6. Human approved continuing on Opus rather than swapping mid-session. Step 1 audit + Step 5 doc updates were handled in-conversation rather than routed (small scope, well-defined edits — within Delegation Discipline thresholds).
- Notes for next-session calibration: Opus overhead was modest because most of the work was mechanical .NET code (single-file, deterministic — the orchestrator's strength is wasted there). Session 2 brings child-process orchestration + multi-file generation; same calculus likely applies. Round 1 ISSUES_FOUND was a real spec-compliance gap (crud-service-template.md got only a cross-link, not an explicit deprecation note) — the verifier earned its keep.

## Session 2: Repo-aware `dabbler-crud-slice` wrapper command

### Recommended orchestrator
Claude Sonnet 4.6 @ effort=medium

### Rationale
This session orchestrates child processes and generates convention-correct C# helper files, which aligns with Sonnet 4.6's documented strength in this repo. The task is more complex than Session 1 but remains mechanical implementation, not deep architectural reasoning requiring Opus.

### Estimated routed cost
low

| Step | Action | Routing Decision |
|------|--------|------------------|
| 1 | Create `Dabbler.CrudSlice` app, invoke `dotnet new` | Orchestrator |
| 2 | Wire invocation of the Session 1 schema applicator | Orchestrator |
| 3 | Implement Web registration-helper emitter | Orchestrator |
| 4 | Implement API registration-helper emitter | Orchestrator |
| 5 | Add integration tests | Orchestrator |
| 6 | Delete or deprecate `SupplierScaffoldingExtensions.cs` | Orchestrator |

### Actuals (filled after the session)
- Orchestrator used: <fill at session close>
- Total routed cost: <fill at session close>
- Deviations from recommendation: <fill at session close>
- Notes for next-session calibration: <fill at session close>

## Session 3: Security/catalog registration generator

### Recommended orchestrator
Claude Sonnet 4.6 @ effort=medium

### Rationale
The core task is extracting code patterns from `Program.cs` and emitting templated C# files, which is well within Sonnet 4.6's capabilities. The pattern-matching analysis is a good candidate for routing, while the orchestrator handles the file generation and bug fix.

### Estimated routed cost
moderate

| Step | Action | Routing Decision |
|------|--------|------------------|
| 1 | Extract registration shapes from `Program.cs` | route(Gemini Pro) |
| 2 | Add `--emit-registrations` output mode | Orchestrator |
| 3 | Add unit tests for generated registration blocks | Orchestrator |
| 4 | Fix duplicate `/suppliers` page registration bug | Orchestrator |
| 5 | Wire the registration generator into the wrapper pipeline | Orchestrator |

### Actuals (filled after the session)
- Orchestrator used: <fill at session close>
- Total routed cost: <fill at session close>
- Deviations from recommendation: <fill at session close>
- Notes for next-session calibration: <fill at session close>

## Session 4: Documentation reconciliation, section-8 additions, and end-to-end proof

### Recommended orchestrator
Claude Sonnet 4.6 @ effort=medium

### Rationale
This session is dominated by checklist-driven documentation edits and executing the end-to-end proof. Opus is not required for this level of reconciliation; Sonnet is the cheapest-capable model for Markdown editing and running verification commands.

### Estimated routed cost
moderate

| Step | Action | Routing Decision |
|------|--------|------------------|
| 1 | Reconcile doc-accuracy item 1 (wiring) | route(Sonnet) |
| 2 | Reconcile doc-accuracy item 2 (nav menu) | route(Sonnet) |
| 3 | Reconcile doc-accuracy item 3 (Playwright) | route(Sonnet) |
| 4 | Add applicability note to `README.md` | route(Sonnet) |
| 5 | Add reauthorization gate to `non-mvp-roadmap.md` | route(Sonnet) |
| 6 | Add recipe for extending a generated page | route(Sonnet) |
| 7 | Run end-to-end proof re-scaffold and verify | Orchestrator |
| 8 | Update `FEEDBACK_REMEDIATION_SUMMARY.md` | route(Sonnet) |

### Actuals (filled after the session)
- Orchestrator used: <fill at session close>
- Total routed cost: <fill at session close>
- Deviations from recommendation: <fill at session close>
- Notes for next-session calibration: <fill at session close>

**Next-session orchestrator recommendation (Session 2):**
Claude Sonnet 4.6 @ effort=medium
Rationale: Session 2's task is primarily mechanical orchestration and convention-based code generation, which does not justify the architectural reasoning overhead of Opus used in Session 1, making the cheaper Sonnet model the most efficient capable choice.
