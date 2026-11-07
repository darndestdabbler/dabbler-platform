# UAT Workflow DSL Alignment Spec

> **Purpose:** Align the AI-led session workflow + cross-repo
> documentation with the new DSL-driven UAT/E2E pipeline shipped in
> `uat-dsl-watermark-integration` and
> `w0-cli-suppressions-and-role-pinning`. The pipeline today is:
> author the UAT checklist in DSL → W0 compiles to Playwright `.cs` →
> the W2 runner executes it 5× deterministically → the human runs UAT
> from the same checklist. The current workflow doc, conventions, and
> task-type roster still describe the deprecated *manual* pattern (a
> hand-authored `*UatCoverageTests.cs` paired to the checklist via
> `E2ETestReference` + `// begin STEP NNN` comments + a
> `uat-coverage-review` gate). This set replaces that vocabulary in
> the workflow rails and decides cross-repo doc placement so future
> session sets — starting with the
> `masking-audit-uat-dsl-rebuild` set that is gated on this one —
> consume the new pipeline by default rather than litigating it
> per-set.
> **Created:** 2026-04-30
> **Session Set:** `docs/session-sets/uat-workflow-dsl-alignment/`
> **Prerequisite:** `w0-cli-suppressions-and-role-pinning` complete
> (lands the `--suppressions` + `--active-role` flags + the W2 runner's
> native --suppressions plumbing + the SeededSignInAsync wrapper hook
> that this set's revised workflow rules assume).
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Session Set Configuration

```yaml
requiresUAT: false
requiresE2E: false
uatScope: none
```

> Rationale: This set delivers documentation only — workflow doc edits,
> convention edits, deprecation notices, cross-repo doc placement, and
> a version-compatibility matrix. No application code change, no test
> change, no UI surface change. Per the platform addendum, doc-only
> sets that change neither code nor user-visible behavior are
> `requiresUAT: false` / `requiresE2E: false`. The set's quality bar is
> a routed end-to-end coherence audit (Session 2's verification step) —
> any session set authored against the revised rails should be
> unambiguous about which repo's docs answer which question.

---

## Project Overview

### What this set delivers

1. **A new `docs/` folder in the sibling `dabbler-uat-dsl` repo** with
   three reference documents (`language-reference.md`,
   `cli-reference.md`, `data-contracts.md`) plus a README cross-link.
   These are the canonical source of truth for the W0 toolchain (DSL
   syntax, CLI flags, JSON schemas).
2. **`docs/ai-led-session-workflow.md` updated for the DSL pipeline.**
   The procedure for sets that produce UAT/E2E coverage becomes:
   author checklist in DSL → W0 compile → W2 runner 5× → human UAT.
   The deprecated path (manual `*UatCoverageTests.cs` authoring +
   `// begin STEP NNN` pairing + `uat-coverage-review` gate) is
   retired with a one-paragraph migration note.
3. **`docs/planning/project-guidance.md` Convention edits.** The
   `Catalog-Grounded UAT Checklist Authoring` Convention now reads as
   the spine of the pipeline; the older `Three-Step UAT Authoring
   Discipline` Convention is collapsed into a one-paragraph reference
   that points at the new shape.
4. **`docs/planning/session-set-authoring-guide.md` and the platform
   addendum updated.** The When-UAT-Is-Required + When-E2E-Is-Required
   heuristics now imply the DSL pipeline by default; spec authors no
   longer need to spell out "use uat-script-author + W0 compile + W2
   runner" — they declare `requiresUAT: true` and the workflow doc
   tells them what that means.
5. **`docs/COMPATIBILITY.md`** — a version-compatibility matrix
   covering W0 (Core / Cli / Emitters), the component catalog schema,
   and the W2 runner. Becomes the source of truth that
   `Directory.Packages.props` bumps reference.
6. **`uat-coverage-review` task-type retirement.** Removed from
   `ai-router/router-config.yaml` task list and from
   `ai-router/prompt-templates/task-prompts.md`. Its job —
   verifying step-by-step parity between checklist and E2E test —
   becomes implicit because the `.cs` is the compiled output of the
   checklist; the verifier instead confirms the W2 runner pass.

### What this set does not do

- **Does not author or rewrite any UAT checklist.** The masking-audit
  rebuild that consumes this set's rails is its own focused set
  (`masking-audit-uat-dsl-rebuild`), explicitly gated on this one.
- **Does not change W0 toolchain code.** The new `docs/` folder in
  `dabbler-uat-dsl` is doc-only.
- **Does not retire `uat-script-author`.** That task type is the spine
  of the new pipeline and stays.
- **Does not change `tools/uat-suppressions.json` data.** Only
  documentation about the schema (which moves to dabbler-uat-dsl per
  the doc-placement decision below).
- **Does not address the click-driven-route-cursor W0 gap.** That's
  tracked separately and may become a `w0-click-driven-route-cursor`
  spec; until then, multi-page UATs use blanket suppression entries.

---

## Cross-repo doc placement (the load-bearing decision)

Routed analysis (gemini-pro, $0.0115) ruled on each documentation surface
based on the mental model: tool docs live with the tool (so they version
with it); process docs live with the workflow that orchestrates it;
duplicated content is expensive, cross-references are cheap.

| Item | Repo | Justification |
|---|---|---|
| DSL closed-verb list | dabbler-uat-dsl | Core language vocabulary; tool-fundamental. |
| Grammar reference | dabbler-uat-dsl | Formal language structure; tool implementation. |
| CLI flag reference | dabbler-uat-dsl | Public API of the CLI; versioned with the binary. |
| Suppressions JSON schema | dabbler-uat-dsl | Data contract consumed by W0; part of the tool's interface. |
| Component catalog JSON schema | dabbler-uat-dsl | Data contract consumed by W0; part of the tool's interface. |
| `isWatermarked` field semantics | both | Definition in dabbler-uat-dsl (schema field); usage rule (`WAIT FOR ACTIONS QUIET` pairing) in dabbler-platform. |
| Watermark mechanism (`TrackedComponentBase`, `TrackAction`, `WaitForQuietAsync`) | dabbler-platform | Application-side instrumentation; produces the catalog data, not part of the DSL tool. |
| W2 runner usage | dabbler-platform | Orchestration script tailored to dabbler-platform's session workflow. |
| AI-led session workflow | dabbler-platform | End-to-end process specific to this org's AI-led workflow. |
| `uat-script-author` task type | dabbler-platform | Component of the AI orchestration workflow; lives with the router config. |
| Per-session-set smoke driver pattern | dabbler-platform | Execution pattern specific to dabbler-platform session sets. |
| Version-compatibility matrix | both | Lives in dabbler-platform (`COMPATIBILITY.md`) since dabbler-platform is the integrator; linked from dabbler-uat-dsl's README. |

**The result:** dabbler-uat-dsl gains a `docs/` folder that's
self-contained (anyone using W0 in a fresh project gets the syntax,
CLI, and schemas in one place). dabbler-platform stops restating tool
syntax and instead links into the dabbler-uat-dsl docs; its own
docs (`ai-led-session-workflow.md`, project-guidance, the addendum,
COMPATIBILITY.md) cover process, orchestration, and integration.

---

## Sessions
### Session 1 of 2: dabbler-uat-dsl docs surface + dabbler-platform link refactor

> **Code work happens in `dabbler-uat-dsl`** (new `docs/` folder).
> Spec/activity-log work in `dabbler-platform`.

**Steps:**

1. Read planning docs (`project-guidance.md`, `lessons-learned.md`,
   `session-set-authoring-guide.md`) and the prerequisite set's
   change-log (`w0-cli-suppressions-and-role-pinning/change-log.md`).
2. Author `dabbler-uat-dsl/docs/language-reference.md` covering: the
   closed-verb list (lifecycle / interaction / verification verbs),
   `WAIT FOR ACTIONS QUIET` semantics, the structured-VERIFY forms,
   the quoted-string requirement, the closed-list extension policy.
   Source the verb list verbatim from
   `dabbler-platform/docs/planning/project-guidance.md`'s `The UAT/E2E
   DSL — closed verb list` block; don't paraphrase.
3. Author `dabbler-uat-dsl/docs/cli-reference.md` covering both
   subcommands (`validate`, `compile`), every flag (`--catalog`,
   `--active-role`, `--suppressions`, `--emitter`, `--output`), JSON
   output shape, exit codes, and a "common workflows" section with 3
   examples (validate-only, compile with suppressions, compile with
   active-role). Use `route(task_type="documentation")` for the prose
   if helpful; verify the flag list against the actual `Program.cs`
   parser.
4. Author `dabbler-uat-dsl/docs/data-contracts.md` covering: (a) the
   component catalog JSON schema (entries, `isWatermarked`,
   schemaVersion 1.1), (b) the suppressions JSON schema
   (`checklists`, `errorKind`, `routePathPatterns`, `rationale`,
   wildcard semantics), (c) the round-trip contract (catalog written
   by a crawler the consumer owns; suppressions file owned by the
   consumer; W0 reads both). Note that the *example* catalog and
   *example* suppressions file in dabbler-platform are non-normative.
5. Update `dabbler-uat-dsl/README.md` to link to the three new docs
   plus `dabbler-platform/docs/COMPATIBILITY.md`. Add a one-paragraph
   "How this fits in" pointer back to the dabbler-platform AI-led
   workflow.
6. In `dabbler-platform`: create `docs/COMPATIBILITY.md` with the
   version matrix (W0 Core 1.2.0 / Cli 1.2.1 / Emitters 1.1.0 ↔
   component-catalog schema 1.1 ↔ runner expectations).
7. In `dabbler-platform`: refactor the
   `Catalog-Grounded UAT Checklist Authoring` Convention in
   `project-guidance.md` and the
   `The UAT/E2E DSL — closed verb list` Convention so they LINK to
   the new dabbler-uat-dsl docs instead of restating syntax. Keep the
   integration commentary (when to use which verb, the watermark
   pairing rule) in dabbler-platform; move pure syntax
   (verb list, flag list, schema shape) to a dabbler-uat-dsl link.
8. `dotnet build && dotnet test` in `dabbler-platform` (sanity — no
   code changed, but Directory.Packages.props pinning is verified).
9. Cross-provider session verification.
10. Commit (in both repos; reference each other's slugs). Push deferred
    to human.

**Creates:** `dabbler-uat-dsl/docs/language-reference.md`,
`dabbler-uat-dsl/docs/cli-reference.md`,
`dabbler-uat-dsl/docs/data-contracts.md`,
`dabbler-platform/docs/COMPATIBILITY.md`.
**Touches:** `dabbler-uat-dsl/README.md`,
`dabbler-platform/docs/planning/project-guidance.md`.
**Ends with:** A fresh project pulling W0 1.2.0 from NuGet can read the
sibling docs and learn DSL syntax + CLI flags + data contracts without
touching dabbler-platform. dabbler-platform's project-guidance no
longer duplicates W0 syntax.
**Progress keys:** `session-1/read-planning`,
`session-1/language-reference`, `session-1/cli-reference`,
`session-1/data-contracts`, `session-1/sibling-readme`,
`session-1/compatibility-md`, `session-1/platform-link-refactor`,
`session-1/build-and-test`, `session-1/verification`,
`session-1/commit`.

---

### Session 2 of 2: workflow doc + spec-authoring-guide alignment + retire uat-coverage-review

**Steps:**

1. Read planning docs + Session 1's outcome.
2. Update `docs/ai-led-session-workflow.md`: replace the manual
   `// begin STEP NNN` step-block-pairing procedure with the new
   pipeline. The procedure for `requiresE2E: true` sets becomes:
   (a) author checklist via routed `uat-script-author` against
   `docs/component-catalog/index.json`;
   (b) compile via W0 with `--suppressions` + `--active-role`;
   (c) run via the W2 runner (`python ai-router/uat_runner/cli.py`),
   target ≥5/5 deterministic where the spec demands quality-bar proof;
   (d) hand the checklist to the human for UAT.
   Retire the `uat-coverage-review` gate; the W2 runner pass is the
   coverage proof. Migration note: closed sets keep their committed
   `*UatCoverageTests.cs` files and `// begin STEP NNN` markers in
   place (no retroactive rewrite); new sets use the DSL pipeline.
3. Update `docs/planning/session-set-authoring-guide.md` and the
   `session-set-authoring-guide.platform-addendum.md`: the
   When-UAT-Is-Required + When-E2E-Is-Required heuristics now imply
   the DSL pipeline. Remove the bullet on `E2ETestReference` field
   shape from the deliverables checklist (it's not used in the new
   pipeline). Update the spec template snippet so the Session Plan's
   E2E-required steps reference the routed `uat-script-author` +
   W0 compile + W2 runner shape.
4. Update `docs/planning/project-guidance.md`: collapse the
   `Three-Step UAT Authoring Discipline` lesson-derived Convention
   into a one-paragraph reference that points at the new pipeline
   description in the workflow doc. Keep the historical `lessons-learned.md`
   entries unchanged (they document why the new pipeline exists).
5. Update `ai-router/router-config.yaml`: remove `uat-coverage-review`
   from the task-type roster, the model assignment table, the
   complexity-hint defaults, and the cross-provider verifier list.
6. Update `ai-router/prompt-templates/task-prompts.md`: delete the
   `# uat-coverage-review` section. Add a one-paragraph migration
   note at the top of the file explaining the retirement.
7. End-to-end coherence audit (routed): pass the updated workflow
   doc + project-guidance + authoring-guide + addendum +
   COMPATIBILITY.md + the dabbler-uat-dsl docs through
   `route(task_type="documentation-review", complexity_hint=60)` (or
   `analysis` if no review task type exists). The verifier should
   confirm: any new spec author can read the rails and produce a
   `requiresUAT: true` set without restating tool syntax.
8. `dotnet build && dotnet test` (sanity).
9. Cross-provider session verification.
10. Commit + push (deferred to human) + send notification.
11. Step 9 reorganization review:
    - Candidate: promote a `lessons-learned.md` entry on
      cross-repo doc placement (tool docs vs. process docs) — likely
      promotion-eligible; this set's whole rationale is that pattern.
    - Candidate: retire the `uat-coverage-review` Convention bullets
      in `project-guidance.md` (already covered by Step 6 in this
      session).
    - Candidate: collapse-to-reference the `Three-Step UAT Authoring
      Discipline` Convention (already covered by Step 4 in this
      session). Reorg-review confirmation only.

**Creates:** Possibly nothing new — primarily edits and one removal.
**Touches:** `docs/ai-led-session-workflow.md`,
`docs/planning/session-set-authoring-guide.md`,
`docs/planning/session-set-authoring-guide.platform-addendum.md`,
`docs/planning/project-guidance.md`,
`ai-router/router-config.yaml`,
`ai-router/prompt-templates/task-prompts.md`.
**Ends with:** A spec author writing a new `requiresUAT: true` set
reads `ai-led-session-workflow.md` and gets the DSL pipeline as the
only path; reads `project-guidance.md` and finds integration
commentary, not duplicated W0 syntax; clicks through to
`dabbler-uat-dsl/docs/` for syntax. The `uat-coverage-review` task
type is gone; no future router call can land on it.
**Progress keys:** `session-2/read-planning`,
`session-2/workflow-doc-update`, `session-2/authoring-guide-update`,
`session-2/project-guidance-update`,
`session-2/router-config-update`,
`session-2/coherence-audit`,
`session-2/build-and-test`, `session-2/verification`,
`session-2/commit-push-notify`, `session-2/step-9-reorg`.

---

## What This Set Preserves

| Existing Asset | Status |
|---|---|
| `tools/uat-suppressions.json` data | Unchanged. Only schema-doc location moves. |
| Closed UAT sets' `*UatCoverageTests.cs` files | Unchanged. No retroactive rewrite. |
| `lessons-learned.md` entries on the manual pattern | Unchanged. They document the historical why. |
| `uat-script-author` task type | Unchanged. Spine of the new pipeline. |
| Component catalog schema | Unchanged. Schema doc moves to dabbler-uat-dsl. |
| W0 1.2.x CLI flags | Unchanged. Flag-doc moves to dabbler-uat-dsl. |
| W2 runner code | Unchanged. Runner-usage doc stays in dabbler-platform. |

## Risk Notes

- **Cross-repo commit coordination.** Session 1 commits to both
  `dabbler-uat-dsl` (new `docs/` folder) and `dabbler-platform`
  (link refactor + COMPATIBILITY.md). Per the platform addendum's
  cross-repo convention, the spec lives in dabbler-platform with
  mirror entries in the activity log; commits land in whichever repo
  owns the change.
- **Migration note phrasing matters.** A reader landing on the
  workflow doc after the update should immediately understand: "is
  the manual pattern dead?" (Yes for new sets; closed sets keep
  what they have.) Phrasing the migration note clumsily creates
  confusion that the next spec author then has to re-litigate. The
  routed coherence audit in Session 2 Step 7 is the safety net for
  this.
- **`uat-coverage-review` retirement is scope-creep risk.** The
  task type is referenced in older session-set specs as a workflow
  step. Search for it across `docs/session-sets/**/spec.md` during
  Session 2 — if any *open* spec still references it, decide
  per-spec whether to update the spec or treat the reference as
  historical (closed sets) before retiring the task type from
  router-config.
- **dabbler-uat-dsl README cross-link.** The sibling repo's README
  must end up linking back to dabbler-platform for the
  AI-led-workflow context, OR the sibling docs should be
  self-contained enough that someone reading them in isolation
  doesn't end up confused about where W0 lives in the org's
  process. Decide explicitly in Session 1.
