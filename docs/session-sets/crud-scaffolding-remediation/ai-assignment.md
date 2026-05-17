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
- Orchestrator used: claude-code Opus 4.7 @ effort=high (DEVIATION — see below)
- Total routed cost: ~$0.09 (single cross-provider gpt-5-4 session-verification round, $0.0895)
  plus ~$0.002 next-session-recommendation routing (gemini-pro). Sub-$0.10 session total.
- Deviations from recommendation: Ran on Opus 4.7 instead of recommended Sonnet 4.6. Per
  `feedback_honor_spec_sized_orchestrator.md` the human was offered the swap and explicitly
  chose to continue on Opus (active context, Session 1 calibration note said Opus overhead
  was modest for mechanical .NET work). All six implementation steps handled in-conversation
  by the orchestrator (single-file edits and mechanical C# generation under the delegation
  threshold). No routed reasoning required beyond cross-provider verification.
- Notes for next-session calibration: Session 3 is similar shape (pattern extraction + string
  template emission + unit tests + a one-line Program.cs bugfix). The verifier's only
  non-blocker observation about the wrapper unconditionally overwriting the scaffolding
  extension suggests regeneration-safety might be worth surfacing as a potential Session 4
  follow-up — but it is explicitly out of Session 3's scope. The FullPipeline SkippableFact
  actually ran (pack-output present) and gave end-to-end coverage essentially for free.

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
- Orchestrator used: claude-code Opus 4.7 @ effort=high (DEVIATION — see below)
- Total routed cost: ~$0.21 across 3 verification rounds + $0.0036 next-session
  recommendation ($0.0036 gemini-pro analysis). Detail: Round 1 verification
  gpt-5-4 $0.1457 (ISSUES_FOUND — 1 Major endpoint-shape gap, 1 Minor CLI shape
  + Templates/ placement); Round 2 verification gpt-5-4 $0.0417 (Issue 1 fixed
  accepted, Issue 2(a) accepted, Issue 2(b) maintained); Round 3 verification
  gpt-5-4 $0.0192 (VERIFIED after Templates/ extraction + emitter refactor).
- Deviations from recommendation: Ran on Opus 4.7 instead of recommended
  Sonnet 4.6. Per `feedback_honor_spec_sized_orchestrator.md` the human was
  offered the swap at session start and explicitly chose to continue on Opus
  (active context, calibration notes from Sessions 1+2 said Opus overhead was
  modest for mechanical .NET work). Step 1 shape extraction + Step 4 duplicate
  page-fix were handled in-conversation per Delegation Discipline (mechanical,
  single-file, well under the 50-line threshold).
- Notes for next-session calibration: Session 3 cost a 3-round verifier spiral
  despite the substance being mechanical. The Round-1 endpoint-shape gap
  (verifier-caught: Api/Program.cs:135-139 uses singular `QuerySupplier`/`GetSupplier`
  but my single-block emit used the Web-side plural/ById shape) was a real
  correctness defect a closer pre-emission self-review would have caught.
  Sonnet would likely have made the same mistake. The Round-2 → Round-3 spin
  was a literal-spec compliance question (Templates/ directory vs. inline
  string interpolation) the verifier maintained at Minor severity; human
  adjudicated to accept the finding, so the Templates/ refactor + embedded-
  resource loader landed before Round-3 VERIFIED. The verifier-spiral lesson
  (`feedback_verifier_spiral_doc_sessions`) applies: 3 rounds was the
  settling point. Session 4 is documentation + end-to-end proof; the
  Markdown-edit half should route to Sonnet, the proof half handled by the
  orchestrator. Recommendation below.

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

**Next-session orchestrator recommendation (Session 3):**
Claude Sonnet 4.6 @ effort=medium
Rationale (routed gemini-pro analysis): Session 3 is pattern extraction + string-template emission + unit tests + a one-line Program.cs bugfix — mechanical work that a cost-effective workhorse model handles well; Opus would be overkill. Per the established pattern across Sessions 1 and 2, the human may still choose to swap to Opus 4.7 @ high if Claude Code is already active — that decision belongs at session-start time.

**Next-session orchestrator recommendation (Session 4):**
Claude Sonnet 4.6 @ effort=medium
Rationale (routed gemini-pro analysis, 2026-05-17, $0.0036): Session 4 consists
of documentation edits (3 doc-accuracy reconciliations + 3 doc additions per
the spec's Section-8 list) and one final end-to-end proof run of the wrapper
pipeline against Supplier. These tasks do not require Opus-class reasoning;
Sonnet is highly capable of precise text manipulation and following an
execution checklist, making it the most cost-effective choice for this final
reconciliation and proof session. (Note: the routed recommendation literally
said "sonnet-3.5"; substitute the in-repo Sonnet 4.6.) Per the established
pattern across Sessions 1–3, the human may still choose to swap to Opus 4.7
@ high if Claude Code is already active.

This is the LAST session of the set, so Session 4 also runs:
1. **Generate change-log.md** via `route(task_type="summarization")` (per
   the `change-log.md is end-of-set, not per-session` memory + lessons-learned).
2. **Next-session-set recommendation** routed via analysis. The pre-existing
   master-plan roadmap memory (`project_role_rearchitecture_roadmap`) and the
   `project_uat_truthful_pipeline` 3-set plan are candidate inputs for the
   next-set choice.
3. **Step 9 reorganization review** of project-guidance.md + lessons-learned.md
   after notify (per the end-of-session-set reorg memory).
