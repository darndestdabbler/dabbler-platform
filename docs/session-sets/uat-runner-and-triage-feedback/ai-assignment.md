# AI Assignment Ledger — uat-runner-and-triage-feedback

Per-session ledger of cheapest-capable orchestrator engine + model + effort,
plus per-step routing decisions. Recommendations are produced via
`route(task_type="analysis")` (Rule 17 — never self-opine).

## Session 1: Runner scaffold, fixture adapter, happy-path execution

### Recommended orchestrator
claude-code claude-haiku-4-5 @ effort=low

### Rationale
This session is mechanical scaffolding: a ~30-line C# adapter, a Python
runner that wires `subprocess` calls to the W0 CLI and `dotnet test`, and
.trx parsing. No architectural reasoning; a fast Haiku-class model is
sufficient and cheaper than the Opus default.

### Estimated routed cost
Low — one `analysis` call (this ledger), one `session-verification` call,
one `summarization`-style next-orchestrator recommendation. No
auto-verified routed work; the W0 CLI and `dotnet test` are the
correctness gates.

| Step | Action | Routing Decision |
|------|--------|------------------|
| 1 | Read project-guidance, lessons-learned, authoring guide | Orchestrator (mechanical reads) |
| 2 | Confirm prerequisites (W0 CLI builds; catalog index.json present) | Orchestrator (filesystem checks) |
| 3 | Add `PlaywrightFixtureUatAdapter.cs` (~30 lines) | Orchestrator (single-file < 50 lines, mechanical) |
| 4 | Add W0 package references via local `nuget.config` | Orchestrator (csproj edit + config file) |
| 5 | Create `ai-router/uat_runner/{__init__.py, runner.py}` | Orchestrator (mechanical scaffolding) |
| 6 | Implement runner.py (CLI subprocess + `dotnet test --filter` + `.trx` parsing) | Orchestrator (deterministic plumbing; no reasoning to delegate) |
| 7 | Happy-path test invocation | Orchestrator (subprocess call + log inspection) |
| 8 | Cross-provider session-verification | **Routed** — `route(task_type="session-verification")` (Rule 2 — always cross-provider) |
| 9 | Commit | Orchestrator (git command) |

### Actuals (filled at end of session)
- Orchestrator used: claude-code claude-opus-4-7 @ effort=high
- Total routed cost: $0.5601 (analysis ai-assignment $0.0072 + Round 1
  session-verification on Opus $0.5515 + Round 2 session-verification
  on Gemini Pro $0.0014). One verifier-flagged Major defect in
  `RunReport.has_failures` (silent swallow when dotnet-test exits
  non-zero with empty .trx) was fixed and Round 2 confirmed PASS.
- Deviations from recommendation: Used Opus 4.7 instead of Haiku 4.5 because
  the human's session-trigger phrase did not specify maxout but the active
  Claude Code instance is on Opus 4.7. The Round 1 verifier finding (Opus)
  was high-quality and caught a real defect, so the cost was worthwhile.
- Notes for next-session calibration: Session 2 introduces the
  `uat-failure-triage` task type (Sonnet, medium effort per spec) plus
  prompt-template authoring and synthetic failure-case scripting. Most
  reasoning is offloaded to the routed task type; orchestrator-side work
  is still mostly mechanical.

**Next-session orchestrator recommendation (Session 2):**
claude-code claude-sonnet-4-6 @ effort=medium
Rationale: Routed analysis (gemini-pro) recommended Sonnet-class for
Session 2 because designing the 4-class triage prompt template benefits
from stronger reasoning than Haiku, while the actual classification work
is offloaded to the new `uat-failure-triage` task type that itself uses
Sonnet. Haiku 4.5 at low effort is a viable downgrade if the human wants
to push more cost-saving — both options are reasonable.

---

## Session 2: Triage stage and four-class taxonomy

### Recommended orchestrator
claude-code claude-haiku-4-5 @ effort=low

### Rationale
Orchestration is mechanical (router-config additions, prompt-template
authoring, synthetic-case scripting). The complex classification work
is delegated to Sonnet via the new `uat-failure-triage` task type.

| Step | Action | Routing Decision |
|------|--------|------------------|
| 1 | Read planning docs | Orchestrator (mechanical reads) |
| 2 | Create triage.py (TriageSession, FailureRecord, TriageResult) | Orchestrator (< 50 lines of structured code) |
| 3 | Add uat-failure-triage to task-prompts.md | Orchestrator (prompt template authoring) |
| 4 | Add uat-failure-triage to router-config.yaml | Orchestrator (config additions) |
| 5 | Wire triage into runner.py | Orchestrator (mechanical integration) |
| 6 | Build + run synthetic failure test cases | Orchestrator (scripted test execution) |
| 7 | Cross-provider session-verification | **Routed** — `route(task_type="session-verification")` (Rule 2) |
| 8 | Commit | Orchestrator (git command) |

### Actuals (filled after the session)
- Orchestrator used: claude-code claude-sonnet-4-6 @ effort=medium
- Total routed cost: ~$1.89 (R1 session-verification opus $0.64 + R2 session-verification
  opus $0.61 + 8x synthetic triage calls ~$0.05 each = ~$0.40 + S3 analysis recommendation $0.01)
- Deviations: Used Sonnet instead of Haiku (active Claude Code instance). One Round-1
  Major defect (budget cap not incremented on exception) fixed; R1 also had a context
  gap (task-prompts.md + router-config.yaml not included in verif prompt), resolved in R2.
  4 Minors fixed inline. 4/4 synthetic failure cases passed at first attempt.
- Notes: APP_FUNCTIONAL_BUG-as-default strategy confirmed correct at 0.99 confidence.
  Decision rules in prompt template are well-calibrated. R2 returned VERIFIED.

**Next-session orchestrator recommendation (Session 3):**
claude-code claude-haiku-4-5 @ effort=low
Rationale (routed analysis): Session 3 is highly procedural — four ~30-line Python modules
following explicit templates, plus a dispatch-table wire-up and end-to-end test. No novel
design reasoning required; Haiku handles well-specified code generation tasks cheaply.

---

## Session 3: Remediation modules

### Recommended orchestrator
claude-code claude-haiku-4-5 @ effort=low

### Rationale
Four small Python modules implementing structured issue-file authoring
or subprocess re-crawl. Straightforward scaffolding with structured
prompt design; no high-level reasoning required of the orchestrator.

---

## Session 4: CLI, workflow integration, backfill, change-log, Step 9 reorg

### Recommended orchestrator
claude-code claude-sonnet-4-6 @ effort=medium

### Rationale
Final session does real-world backfill against the masking-set checklist
(may surface live triage classifications to interpret), runs the Step 9
reorganization review (judgment about lesson promotion), and authors
the change-log via `summarization`. Sonnet is a better fit than Haiku
for the reorg-review judgment; the rest stays cheap via routing.
