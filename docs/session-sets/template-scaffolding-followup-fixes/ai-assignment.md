# AI Assignment — template-scaffolding-followup-fixes

This assignment covers a 3-session follow-up set that backfills the already-landed template and CRUD remediation work, resolves the remaining `dabbler-min` contract decision, and removes the last template-pack warning from `Dabbler.Templates`.

## Session 1: Backfill Split Scaffold And CRUD Remediation

### Recommended orchestrator (engine + model + effort)
Claude Code (`claude-sonnet-4-6`, `low`)

### Rationale
Session 1 is mostly ledger and validation work over already-landed code changes. The direct editing surface is known, the validations are targeted, and the routed work is limited to ai-assignment authoring and cross-provider session verification.

### Estimated routed cost
Low

| Step | Action | Routing Decision |
| :--- | :--- | :--- |
| 1 | Read planning docs and confirm the landed remediation scope to backfill. | direct work |
| 2 | Backfill the `dabbler-app` contract fixes in docs/template metadata/package inputs. | direct work |
| 3 | Backfill the split-topology regression gate in `PackagingSmokeTests.cs`. | direct work |
| 4 | Backfill the CRUD docs/native-template remediation. | direct work |
| 5 | Re-run the focused validation bundle and capture the results. | direct work |
| 6 | Cross-provider session verification and review save. | `route(task_type="session-verification", max_tier=2)` |

### Actuals (filled after the session)
- Orchestrator used: Codex (`gpt-5.4`, `medium`)
- Total routed cost: `$0.7606`
- Deviations from recommendation: Used Codex / GPT-5.4 / medium instead of the routed Claude Sonnet / low recommendation because the remediation work was already in flight in this conversation before the session-set backfill was authored.
- Notes for next-session calibration: Session 1 was overwhelmingly mechanical once the underlying code fixes were in place. The main routed cost came from session governance artifacts (`ai-assignment.md`, next-session recommendation, session verification), not from code generation.

**Next-session orchestrator recommendation:** claude-code / claude-opus-4-6 / medium
Rationale: Session 2's primary task is an architectural review requiring nuanced reasoning to decide the `dabbler-min` template's future topology. Claude Opus excels at this type of analysis and can then reliably execute the implementation, which involves either a significant template refactor or targeted documentation and smoke-test hardening. The effort is medium due to the combination of strategic decision-making and non-trivial code modification.

## Session 2: Resolve Minimal Template Contract

### Recommended orchestrator (engine + model + effort)
Claude Code (`claude-opus-4-6`, `medium`)

### Rationale
Session 2 turns on a real architecture decision: whether `dabbler-min` should remain intentionally single-project or split to match `dabbler-app`. That decision should be made by routed analysis, but the session still benefits from a high-capability orchestrator because the outcome changes template topology, docs, and smoke coverage together.

### Estimated routed cost
Moderate

| Step | Action | Routing Decision |
| :--- | :--- | :--- |
| 1 | Read Session 1 artifacts plus the current `dabbler-min` template source and smoke coverage. | direct work |
| 2 | Review the `dabbler-min` topology question and record the recommendation. | `route(task_type="architecture")` |
| 3 | Implement the chosen contract across template source, metadata, docs, and smoke coverage. | direct work |
| 4 | Re-run pack/scaffold/build smoke checks and document the resulting consumer contract. | direct work |
| 5 | Cross-provider session verification and review save. | `route(task_type="session-verification", max_tier=2)` |

### Actuals (filled after the session)
- Orchestrator used:
- Total routed cost:
- Deviations from recommendation:
- Notes for next-session calibration:

## Session 3: Remove Template Pack Warning And Close Out

### Recommended orchestrator (engine + model + effort)
Codex (`gpt-5.4`, `medium`)

### Rationale
Session 3 is a tooling-focused closeout: root-cause the remaining `NU5110` / `NU5111` warning path, implement a clean packaging fix, rerun the full template validation bundle, and generate the set closeout artifacts. That mix is still engineering-heavy, but narrower than Session 2's topology decision.

### Estimated routed cost
Low to moderate

| Step | Action | Routing Decision |
| :--- | :--- | :--- |
| 1 | Read Sessions 1-2 artifacts and inspect the remaining warning path. | direct work |
| 2 | Design the warning-free asset strategy if the fix path is not obvious from local inspection. | `route(task_type="analysis")` only if needed |
| 3 | Implement the warning cleanup in the template/package surfaces. | direct work |
| 4 | Re-run the full template validation bundle for the set. | direct work |
| 5 | Generate `change-log.md` and final closeout prose. | `route(task_type="summarization", max_tier=1)` |
| 6 | Cross-provider session verification and review save. | `route(task_type="session-verification", max_tier=2)` |

### Actuals (filled after the session)
- Orchestrator used:
- Total routed cost:
- Deviations from recommendation:
- Notes for next-session calibration: