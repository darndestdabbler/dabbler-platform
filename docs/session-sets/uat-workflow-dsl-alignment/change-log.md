# Change Log — uat-workflow-dsl-alignment

> **Set status:** Complete (2 of 2 sessions VERIFIED, 2026-04-30)
> **Goal:** Align the AI-led session workflow + cross-repo documentation
> with the DSL-driven UAT/E2E pipeline shipped in
> `uat-dsl-watermark-integration` and `w0-cli-suppressions-and-role-pinning`.
> Retire the deprecated manual-pairing pattern (`*UatCoverageTests.cs`
> + `// begin STEP NNN` + `uat-coverage-review` gate) for new sets;
> closed sets keep what they have.

## What landed

### Sibling repo (`dabbler-uat-dsl`) — Session 1

Three reference docs added under `dabbler-uat-dsl/docs/`:

- `language-reference.md` — closed verb list, structured `VERIFY` forms,
  scope rules, watermark synchronization, locator rules, closed-list
  extension policy.
- `cli-reference.md` — `validate` / `compile` subcommands, every flag
  (`--catalog`, `--active-role`, `--suppressions`, `--emitter`,
  `--output`), JSON output shapes, exit codes,
  `ValidationErrorKind` table, common workflows, known limitations.
- `data-contracts.md` — component catalog schema (1.0 / 1.1 with
  `isWatermarked`), suppressions schema with match algorithm,
  round-trip contract diagram.

The sibling `README.md` now links the three new docs and points back
to `dabbler-platform/docs/COMPATIBILITY.md` plus a "How this fits in"
pointer to the AI-led workflow.

### dabbler-platform — Session 1

- `docs/COMPATIBILITY.md` created — version matrix covering W0 Core
  1.2.0 / Cli 1.2.1 / Emitters 1.1.0 ↔ component-catalog schema 1.1
  ↔ runner expectations, plus the bump procedure.
- `docs/planning/project-guidance.md` — refactored the
  `Catalog-Grounded UAT Checklist Authoring` and
  `The UAT/E2E DSL — closed verb list` Conventions to LINK to the
  new sibling docs instead of restating verb syntax + CLI flags.
  Integration commentary (watermark pairing rule, W2 runner role
  inference, click-driven-cursor known limitation) retained.

### Workflow rails — Session 2

- `docs/ai-led-session-workflow.md` — added the new
  `UAT/E2E pipeline (the DSL-driven path)` section describing the
  four-stage flow (`uat-script-author` → W0 compile → W2 runner →
  human UAT). Added the `Migration: from manual *UatCoverageTests.cs
  pairing to the DSL pipeline` sub-section. Refactored the
  `E2E Coverage Before UAT` section so structural parity (W0
  compile) replaces the retired `uat-coverage-review` AI gate.
  Removed `uat-coverage-review` from the Task Types table and Rule
  #11. Mirrored retirement in `template-seed/docs/ai-led-session-workflow.md`.
- `docs/planning/session-set-authoring-guide.md` — `requiresUAT:
  true` field semantics describe the DSL pipeline; deliverables
  list updated to the W0-compiled test; spec template snippet
  contains a `requiresE2E: true` step shape pointing at
  `uat-script-author` + W0 compile + W2 runner.
- `docs/planning/session-set-authoring-guide.platform-addendum.md`
  — E2E test location notes the W0-compiled output (one
  `*UatTests.cs` per checklist), W0 toolchain doc reference added,
  `requiresE2E: true` heuristic refers to the pipeline.
- `docs/planning/project-guidance.md` —
  - `Three-Step UAT Authoring Discipline` Convention collapsed to a
    single paragraph that points at the workflow doc's pipeline
    section.
  - `Step-block alignment between UAT and E2E` Convention collapsed;
    the `// begin STEP NNN` pattern is documented as
    closed-sets-only.
  - `Playwright pre-testing per UAT item` Convention rewritten as
    `Compiled-test proof per UAT item` (W2 runner pass = coverage
    proof) plus an `Authoring goes through the router`
    (`uat-script-author`) sub-bullet.
  - `Human UAT Philosophy` principles refactored to describe
    structural parity (the Playwright `.cs` is the W0 compiler
    output of the DSL checklist; drift is a compiler bug).
  - `The UAT/E2E DSL — sibling-repo references` Convention's verb
    list intro adjusted (closed sets only retain inline
    `// begin STEP NNN` blocks).
- `ai-router/router-config.yaml` — `uat-coverage-review` removed
  from `task_type_overrides`, `delegation.always_route_task_types`,
  `task_type_params`, `complexity.task_type_scores`, and
  `verification.auto_verify_task_types`. Retirement note placed
  next to the remaining `uat-plan-generation` entry.
- `ai-router/prompt-templates/task-prompts.md` — migration note at
  file top; the `# uat-coverage-review` section deleted.

### Open-spec migrations (scope-creep risk note)

Two open specs still referenced the retired task type at the start
of Session 2; both updated to use the DSL pipeline:

- `docs/session-sets/sensitive-field-masking-audit-uat-remediation/spec.md`
  — Standards section now references the DSL pipeline; Steps 4–7
  rewritten to author via `uat-script-author`, compile via W0, run
  via W2. Legacy hand-authored
  `SensitiveFieldMaskingUatCoverageTests.cs` marked for deletion in
  favour of the W0-compiled `SensitiveFieldMaskingUatTests.cs`.
- `docs/session-sets/unified-master-details-composite/spec.md` —
  Session 5 Step 3 rewritten to invoke the W2 runner instead of
  `route(task_type="uat-coverage-review")`. Step 5 verification
  reference, the `UAT coverage parity is mechanical, not
  approximate` risk note, and the Exit Criteria all updated.

### Intentionally preserved

- `docs/planning/lessons-learned.md` historical entries on
  `uat-coverage-review` — they document why the manual pattern
  existed and why the DSL pipeline replaced it.
- Closed session sets' specs, activity logs, and review files
  retain `uat-coverage-review` references as historical record.
- `scripts/session10_*.py` historical orchestration script
  references are retained as closed-set artifacts.
- `ai-router/router-metrics.jsonl` is the append-only metrics log;
  historical records remain.

## Verification

- **Session 1:** Cross-provider verification routed to gemini-pro
  (after a same-provider Opus mis-route was discarded per the
  known router penalty). Verifier flagged the Three-Step
  Discipline collapse — a false positive (out of scope for
  Session 1, scheduled for Session 2 Step 4 per spec). Disposition
  recorded in `session-reviews/session-001.md`. **VERIFIED** for
  Session 1.
- **Session 2:** Cross-provider verification routed to gemini-pro
  at tier 2 ($0.0811, 19.5s). Verdict **VERIFIED** with empty
  issues array. Verifier confirmed spec compliance, internal
  coherence, closed-set preservation, open-set coherence, and
  migration-note clarity. Format note: verifier emitted the JSON
  block but used key `status` instead of schema-required `verdict`;
  semantically unambiguous, accepted as-is.

## Build sanity

`dotnet build Dabbler.Platform.sln` clean both sessions (0 errors,
184 pre-existing warnings — identical baseline). Doc/config-only
set; full unit test run was performed in Session 1 (709/709 pass)
and skipped in Session 2 since no source surface changed.

## What this set does NOT do

- Does not author or rewrite any UAT checklist. The
  `masking-audit-uat-dsl-rebuild` set (or, more precisely, the
  existing `sensitive-field-masking-audit-uat-remediation` set
  whose spec was migrated in Session 2) consumes the new rails.
- Does not change W0 toolchain code. The new `dabbler-uat-dsl/docs/`
  folder is doc-only.
- Does not retire `uat-script-author` — that task type is the spine
  of the new pipeline.
- Does not change `tools/uat-suppressions.json` data.
- Does not address the click-driven-route-cursor W0 gap; multi-page
  UATs continue to use blanket suppression entries until that is
  resolved in a future set.

## Next-set recommendation

Per the role-rearchitecture roadmap and the UAT-truthful pipeline
plan, the natural next sets in priority order:

1. **`sensitive-field-masking-audit-uat-remediation`** — open and
   gated on this set; its spec was migrated in Session 2 to use
   the DSL pipeline. Running this set is the first proof that the
   newly-aligned rails produce a runnable checklist end-to-end.
2. **`transactional-system-columns`** (Set C of the role
   rearchitecture roadmap) — gated on the masking-audit UAT
   passing. Adds `OrganizationId` + `RoleId` columns to
   transactional entities so every write records which role/org
   it was made under.

The `unified-master-details-composite` set was also migrated in
Session 2; it is large enough to be its own initiative and can run
in parallel with the role-rearchitecture work as the human's
priorities allow.
