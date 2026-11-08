# Template Scaffolding Follow-Up Fixes Spec

> **Purpose:** Stabilize the external scaffolding contract after real consumer smoke validation exposed template-topology drift, stale wrapper documentation, and wrong-convention native CRUD template defaults. Session 1 backfills the fixes already implemented on 2026-05-27 into the session-set framework so the work is governed, reviewable, and traceable; later sessions resolve the remaining template-contract decisions and packaging warnings.
> **Created:** 2026-05-27
> **Session Set:** `docs/session-sets/template-scaffolding-followup-fixes/`
> **Prerequisite:** `packaging-and-template-readiness`, `packaging-and-template-readiness-uat-remediation`, and `crud-scaffolding` are closed. This set is a follow-up remediation pass over the shipped template and CRUD scaffolding surfaces from those sets.
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Session Set Configuration

```yaml
tier: full
requiresUAT: false
requiresE2E: false
uatStyle: ad-hoc
uatScope: none
prerequisites:
   - slug: packaging-and-template-readiness
      condition: complete
   - slug: packaging-and-template-readiness-uat-remediation
      condition: complete
   - slug: crud-scaffolding
      condition: complete
```

> **Rationale:**
> - `tier: full` because this set uses the router for step planning, next-session recommendation, and cross-provider session verification.
> - `requiresUAT: false` / `requiresE2E: false` because the work is confined to template output, packaging, smoke tests, and CLI-facing documentation rather than shipping browser workflows in AcmeCoffeeOnline.
> - `uatStyle: ad-hoc` is the correct default for a non-UAT set under the current authoring guide.

---

## Project Overview

This set captures the consumer-facing follow-up work discovered while re-validating the template/package story against the current repo state.

The key findings entering the set are:

1. The public `dabbler-app` contract had drifted: docs and smoke tests still tolerated a monolithic scaffold even though the intended source had already moved to a split web/api/shared/tests layout.
2. The public CRUD scaffolding guide advertised dead wrapper flags (`--output`, `--plural`) and omitted the required `--web-root` / `--api-root` / namespace arguments actually enforced by `Dabbler.CrudSlice`.
3. The native `dabbler-crud-page` template emitted the wrong host conventions (`Api:BaseUrl`, no `X-Internal-Api-Key`, no `UserIdentityHandler`), and the native `dabbler-crud-service` template omitted a one-line DI helper entirely.
4. Two residual issues remain intentionally open after Session 1's remediation pass:
   - `dabbler-min` is still monolithic and needs an explicit contract decision rather than accidental drift.
   - Packing `Dabbler.Templates` still emits `NU5110` / `NU5111` for `Apply-T1EntitySchema.ps1`.

This set should keep the scope tight: fix the published scaffolding contract, prove it with smoke coverage, and document the remaining decisions. It should **not** expand into a larger architecture rewrite of the generated app hosts. In particular, making the scaffolded `dabbler-app` API host production-complete and removing direct web-host data wiring are acknowledged follow-up architecture concerns, but they are outside this set unless the human explicitly broadens scope.

## Parallelization Notes

Run sequentially. All sessions touch the same template/docs/smoke-test surfaces and build on the prior session's conclusions.

## Cross-Set Dependencies

This set depends on three prior sets and declares them in both the prose preamble and the machine-readable `prerequisites:` field above:

- `packaging-and-template-readiness` — established the public NuGet/template surface that this set is remediating.
- `packaging-and-template-readiness-uat-remediation` — fixed the first post-packaging defects and provides the current known-good baseline for template runtime behavior.
- `crud-scaffolding` — introduced `Dabbler.CrudSlice` plus the native CRUD templates whose documentation and default wiring are corrected here.

## Session Plan

### Session 1 of 3: Backfill Split Scaffold And CRUD Remediation

> **History:** The code changes for this session were implemented directly on 2026-05-27 during investigation of customer-reported scaffolding defects and follow-up Gemini findings. This session backfills those landed changes into the session-set workflow, re-runs the focused validations, and records the outcome in session-set artifacts.

**Steps:**
1. Read `docs/planning/project-guidance.md`, `docs/planning/lessons-learned.md`, and this spec. Confirm the already-landed remediation scope that must be backfilled into the session ledger.
2. Backfill the `dabbler-app` contract fixes: align root/adopter/generated docs and template metadata with the split scaffold, lock scaffolded API/shared target frameworks to `.NET 10`, and exclude template `bin/obj` content from package inputs.
3. Backfill the split-topology regression gate: harden `tests/Dabbler.Packaging.Smoke/PackagingSmokeTests.cs` so `dabbler-app` smoke validation asserts the web/api/shared/tests solution layout instead of only proving a build.
4. Backfill the CRUD remediation: align public wrapper docs with the real CLI, make native `dabbler-crud-page` emit the host's actual HTTP wiring conventions, and make native `dabbler-crud-service` emit its own API DI helper.
5. Re-run the focused validation bundle and record the results in the session artifacts:
   - `dotnet pack templates/Dabbler.Templates/Dabbler.Templates.csproj -c Release -o tmp/<...>`
   - fresh custom-hive native CRUD template smoke proof
   - `tests/Dabbler.Packaging.Smoke/PackagingSmokeTests.cs`
   - `tests/Dabbler.CrudSlice.Tests/WrapperOrchestrationTests.cs`

**Creates:**
- `templates/Dabbler.Templates/content/dabbler-crud-service/Services/T1EntityApiRegistrationExtensions.cs`
- Session-set artifacts for Session 1 (`ai-assignment.md`, `activity-log.json`, `session-state.json`, `session-reviews/session-001.md`)

**Touches:**
- `README.md`
- `docs/getting-started.md`
- `docs/scaffolding-templates/README.md`
- `tools/Dabbler.CrudSlice/README.md`
- `templates/Dabbler.Templates/Dabbler.Templates.csproj`
- `templates/Dabbler.Templates/content/dabbler-app/.template.config/template.json`
- `templates/Dabbler.Templates/content/dabbler-app/README.md`
- `templates/Dabbler.Templates/content/dabbler-app/src/DabblerApp.Api/DabblerApp.Api.csproj`
- `templates/Dabbler.Templates/content/dabbler-app/src/DabblerApp.Shared/DabblerApp.Shared.csproj`
- `templates/Dabbler.Templates/content/dabbler-app/tests/DabblerApp.Web.Tests/SampleItemListTests.cs`
- `templates/Dabbler.Templates/content/dabbler-crud-page/.template.config/template.json`
- `templates/Dabbler.Templates/content/dabbler-crud-page/Data/T1EntityScaffoldingExtensions.cs`
- `templates/Dabbler.Templates/content/dabbler-crud-service/.template.config/template.json`
- `tests/Dabbler.Packaging.Smoke/PackagingSmokeTests.cs`

**Ends with:** Session 1 is recorded as a verified backfill of the split `dabbler-app` and CRUD-remediation work, and the focused template/package tests pass from current source.

**Progress keys:** `session-1/backfill-dabbler-app-contract`, `session-1/harden-packaging-smoke`, `session-1/backfill-crud-remediation`, `session-1/focused-validation`

---

### Session 2 of 3: Resolve Minimal Template Contract

**Steps:**
1. Read Session 1 artifacts plus the current `dabbler-min` template source, packaging smoke coverage, and CRUD-wrapper documentation.
2. Route an architecture/analysis review that answers one explicit question: should `dabbler-min` remain intentionally single-project, or should it split to match the full `dabbler-app` topology now that wrapper/docs support same-root consumers? Save the raw output in this session-set folder.
3. Implement the chosen contract:
   - If `dabbler-min` stays single-project, make that intentional in docs and smoke coverage, and prove same-root CRUD wrapper compatibility explicitly.
   - If `dabbler-min` splits, align template source, metadata, generated README, and smoke coverage with the new topology.
4. Re-run the relevant pack/scaffold/build smoke checks and document the consumer contract in the touched docs.

**Creates:**
- `docs/session-sets/template-scaffolding-followup-fixes/dabbler-min-contract-review.md`

**Touches:**
- `templates/Dabbler.Templates/content/dabbler-min/**`
- `docs/getting-started.md`
- `docs/scaffolding-templates/README.md`
- `tests/Dabbler.Packaging.Smoke/PackagingSmokeTests.cs`
- `tests/Dabbler.CrudSlice.Tests/WrapperOrchestrationTests.cs` (if same-root proof needs explicit coverage)

**Ends with:** `dabbler-min` is no longer an accidental middle state; its topology is intentional, documented, and covered by smoke validation.

**Progress keys:** `session-2/review-dabbler-min-contract`, `session-2/implement-dabbler-min-contract`, `session-2/validate-dabbler-min-contract`

---

### Session 3 of 3: Remove Template Pack Warning And Close Out

**Steps:**
1. Read Sessions 1-2 artifacts and inspect the remaining `NU5110` / `NU5111` warning path for `Apply-T1EntitySchema.ps1`.
2. Replace or relocate the page-template schema-application asset so `Dabbler.Templates` packs without those warnings, preferring a supported cross-platform/non-install-script shape over a warning-suppression hack.
3. Re-run the full template validation bundle for this set: pack, install to a custom hive, scaffold the relevant templates, restore/build, and run the targeted smoke tests.
4. Author `change-log.md`, update the final session artifacts, and close the set.

**Creates:**
- `docs/session-sets/template-scaffolding-followup-fixes/change-log.md`

**Touches:**
- `templates/Dabbler.Templates/content/dabbler-crud-page/**`
- `templates/Dabbler.Templates/Dabbler.Templates.csproj`
- `docs/scaffolding-templates/README.md` (if the invocation story changes)
- `tests/Dabbler.Packaging.Smoke/PackagingSmokeTests.cs` (if warning cleanup changes validation steps)

**Ends with:** `Dabbler.Templates` packs cleanly without the current schema-script warnings, the session set has a change-log, and the external scaffolding contract is documented end-to-end.

**Progress keys:** `session-3/remove-pack-warning`, `session-3/full-template-validation`, `session-3/closeout`

---

## End-Of-Set Deliverables

- Verified Session 1 backfill record for the split `dabbler-app` and CRUD-remediation work
- Explicit `dabbler-min` consumer-contract decision with matching smoke proof
- Clean `Dabbler.Templates` pack output without the current schema-script warnings
- `change-log.md` summarizing the remediation set and any deferred follow-up architecture work