# CRUD Scaffolding Remediation Spec

> **Purpose:** Close the concrete gaps between the shipped CRUD scaffolding templates (Set `crud-scaffolding`, closed 2026-05-16) and a genuinely drop-in CRUD onboarding workflow for this repo. Delivers a repo-aware wrapper command, a security/catalog registration generator, a cross-platform schema applicator, and reconciles all open documentation drift.
> **Created:** 2026-05-17
> **Session Set:** `docs/session-sets/crud-scaffolding-remediation/`
> **Prerequisite:** `crud-scaffolding` (closed 2026-05-16).
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Session Set Configuration

```yaml
requiresUAT: false
requiresE2E: false
uatScope: none
```

**Rationale.** This set produces generator tooling, generated code files, and documentation. No user-visible behavior ships. The closeout proof is a reference re-scaffold of the `Supplier` entity using the new wrapper command, verified by the existing `CrudServiceTemplateTests` / `CrudPageTemplateTests` smoke fixtures plus manual inspection of the generated registration files against the real wiring in `Web/Program.cs` and `Api/Program.cs`. UAT and E2E are unnecessary at this scope.

---

## Project Overview

### Motivation

Three independent reviews (GPT-5.x architectural review, sibling-repo Claude harvester, orchestrator-verified drift audit) converge on the same finding: the CRUD scaffolding templates work in isolation but do not produce drop-in integration code for this repo. The exemplar bug is `SupplierScaffoldingExtensions.cs`, which reads `Api:BaseUrl` (line 47) while the host app reads `Services:ApiBaseUrl` (Web/Program.cs:291), omits the `X-Internal-Api-Key` header, and omits `UserIdentityHandler`. The generated extension is non-functional as written.

Beyond that config-key drift, every new CRUD entity today requires hand-adding protectable-page, entity, and endpoint registrations into `Api/Program.cs` and `Web/Program.cs` — the exact surfaces where the duplicate `/suppliers` page registration bug (Web/Program.cs:101+:103) already proves the process is error-prone. And the schema-application post-process (`Apply-{Entity}Schema.ps1`) is PowerShell-only, blocking linux CI and macOS development.

This set fixes the existing scope. It does not expand it.

### Scope

1. **Repo-aware `dabbler-crud-slice` wrapper command.** A single CLI entry point that invokes both `dabbler-crud-service` and `dabbler-crud-page` templates, runs the cross-platform schema applicator, and emits convention-correct API + Web registration helper files. The generated helpers match the real production wiring (correct config key, `X-Internal-Api-Key` header, `UserIdentityHandler`, named `HttpClient`). The wrapper is the integration entry point; the two existing templates remain the underlying primitives.

2. **Security/catalog registration generator.** Produces a separately-generated file containing protectable-pages, entity, and endpoint registration code matching the shapes at `Api/Program.cs:94-103` and `:135-139` and `Web/Program.cs:155-162` and `:189-194`. Consumers copy-paste from the generated file into their `Program.cs`; the generator does NOT edit `Program.cs`.

3. **Cross-platform schema applicator.** Replaces `Apply-{Entity}Schema.ps1` with a `dotnet tool` or pure C# CLI step that performs identical per-property substitution on all platforms (Windows, linux, macOS).

4. **Documentation reconciliation and section-8 additions.**
   - Reconcile the three open doc-accuracy items from `FEEDBACK_REMEDIATION_SUMMARY.md`: `AddSupplierScaffolding` usage vs. direct DI wiring, nav-menu-entry claim, Playwright contradiction.
   - Fix the duplicate `/suppliers` page registration at `Web/Program.cs:101+:103`.
   - Add applicability note to `docs/scaffolding-templates/README.md`.
   - Add reauthorization gate to `docs/scaffolding-templates/non-mvp-roadmap.md`.
   - Add recipe: "Extend a generated CRUD page with a workflow action" under `docs/scaffolding-templates/recipes/`.

### Non-goals

These items are explicitly out of scope for this set. They remain deferred per `docs/scaffolding-templates/non-mvp-roadmap.md` and per harvester recommendation 8c:

- **No Granularity 3 push.** No DI auto-edits, no nav auto-registration, no policy auto-registration via Roslyn modification of user-owned files. The wrapper EMITS registration code in a separately-generated file; it does NOT edit `Program.cs`, `DbContext`, or nav menus.
- **No reflection-driven discovery** (`dotnet tool` variant that scans assemblies for registered entities).
- **No `--regenerate` flag or drift detector.**
- **No OData template suite.**
- **No master-detail or multi-entity templates.**

### Decomposition justification

Four sessions, not three. The schema applicator has a self-contained, platform-parity-verifiable deliverable (identical output to the PowerShell script on linux/macOS/Windows) that warrants its own session. Bundling it with the wrapper would produce one oversized session with two distinct failure modes and no clean intermediate checkpoint. Each of the four sessions ends at a natural pass/fail boundary:

| Session | Pass/fail signal |
|---|---|
| 1 — Schema applicator | C# CLI produces byte-identical output to `Apply-SupplierSchema.ps1` on all three platforms |
| 2 — Wrapper command | `dabbler-crud-slice` invokes templates + applicator and emits registration helpers using correct config key, headers, handler |
| 3 — Registration generator | Generated registration file matches the exact shapes in `Api/Program.cs:94-103, :135-139` and `Web/Program.cs:155-162, :189-194` |
| 4 — Docs + proof | All doc-accuracy items reconciled, recipes added, full Supplier re-scaffold proves the pipeline end-to-end |

---

## Parallelization Notes

**Internal sequencing:** Strictly sequential (1 → 2 → 3 → 4). Session 2 depends on Session 1's schema applicator. Session 3 depends on Session 2's wrapper integration surface. Session 4 depends on all three tools being complete for the end-to-end proof.

**External sequencing:** No upstream blockers. The only technical prerequisite (`crud-scaffolding`) is closed. The set may begin as the next session set whenever scheduling permits, and may run in parallel with non-scaffolding session sets if multiple orchestrators are active.

---

## Session Plan

### Session 1 of 4: Cross-platform schema applicator

**Purpose.** Replace the PowerShell-only `Apply-{Entity}Schema.ps1` post-process with a cross-platform C# CLI tool that performs identical per-property template substitution on Windows, linux, and macOS.

**Steps:**

1. Audit `Apply-SupplierSchema.ps1` to catalog every substitution it performs (file paths, token patterns, property-level replacements). Document the full substitution contract as a checklist.
2. Create `tools/Dabbler.SchemaApplicator/` as a minimal .NET console app (or `dotnet tool` manifest). Implement the substitution engine in C#, reading the same inputs the PowerShell script reads.
3. Add unit tests (`Dabbler.SchemaApplicator.Tests`) that feed the Supplier schema definition and assert byte-identical output files against a snapshot of the PowerShell script's output.
4. Verify the tool runs on linux (CI runner image) and macOS (if available) or at minimum under `dotnet run` without PowerShell. Document any platform-specific path-separator handling.
5. Update `docs/scaffolding-templates/crud-service-template.md` and `crud-page-template.md` to reference the new applicator as the recommended post-process, noting the PowerShell script remains available but is deprecated.

**Creates:**
- `tools/Dabbler.SchemaApplicator/` (console app / dotnet tool)
- `tools/Dabbler.SchemaApplicator.Tests/` (unit tests)

**Touches:**
- `docs/scaffolding-templates/crud-service-template.md`
- `docs/scaffolding-templates/crud-page-template.md`

**Ends with:** `dotnet run --project tools/Dabbler.SchemaApplicator` produces output identical to `Apply-SupplierSchema.ps1` for the Supplier entity. Unit tests pass. No PowerShell dependency in the execution path.

**Progress keys:** `schema-applicator-impl`, `schema-applicator-tests`, `schema-applicator-docs`

---

### Session 2 of 4: Repo-aware `dabbler-crud-slice` wrapper command

**Purpose.** Deliver the single CLI entry point that orchestrates both templates plus the schema applicator, and emits convention-correct API + Web registration helper files that match this repo's real production wiring.

**Steps:**

1. Create `tools/Dabbler.CrudSlice/` as a .NET console app. Accept arguments: entity name, plural name, schema definition path, output root. Wire invocation of `dotnet new dabbler-crud-service` and `dotnet new dabbler-crud-page` as child processes.
2. Wire invocation of the Session 1 schema applicator as a post-process step.
3. Implement the registration-helper emitter. For the **Web** side, generate a `{Entity}ScaffoldingExtensions.cs` that:
   - Reads `Services:ApiBaseUrl` first, `ApiBaseUrl` fallback, hard default (matching `Web/Program.cs:291-300`).
   - Sets `X-Internal-Api-Key` header from `DabblerSecurity:InternalApiKey`.
   - Attaches `.AddHttpMessageHandler<UserIdentityHandler>()`.
   - Registers the typed `HttpClient` and grid-data provider.
4. For the **API** side, generate a `{Entity}ApiRegistrationExtensions.cs` that registers the entity service in DI (matching `Api/Program.cs:162` shape).
5. Add integration tests that invoke `Dabbler.CrudSlice` for a test entity and assert:
   - Both template outputs exist.
   - Schema substitution was applied.
   - Generated `{Entity}ScaffoldingExtensions.cs` contains `Services:ApiBaseUrl`, `X-Internal-Api-Key`, `UserIdentityHandler`.
   - Generated extensions do NOT contain `Api:BaseUrl` (the wrong key).
6. Delete or deprecate the existing `SupplierScaffoldingExtensions.cs` wrong-convention exemplar, replacing it with the wrapper's output.

**Creates:**
- `tools/Dabbler.CrudSlice/` (console app)
- `tools/Dabbler.CrudSlice.Tests/` (integration tests)
- Template: generated `{Entity}ScaffoldingExtensions.cs` (Web side)
- Template: generated `{Entity}ApiRegistrationExtensions.cs` (API side)

**Touches:**
- `src/Apps/AcmeCoffeeOnline.Web/Data/SupplierScaffoldingExtensions.cs` (replaced)
- `docs/scaffolding-templates/README.md` (reference the wrapper as the recommended entry point)

**Ends with:** Running `dotnet run --project tools/Dabbler.CrudSlice -- --entity Supplier --plural Suppliers --schema <path> --output <path>` produces all template outputs plus convention-correct registration helpers. Integration tests pass. The generated Web extension reads `Services:ApiBaseUrl`, sets `X-Internal-Api-Key`, and attaches `UserIdentityHandler`. The old wrong-convention `SupplierScaffoldingExtensions.cs` is removed or replaced.

**Progress keys:** `crud-slice-wrapper-impl`, `crud-slice-registration-emitter`, `crud-slice-tests`, `supplier-extension-fixed`

---

### Session 3 of 4: Security/catalog registration generator

**Purpose.** Produce the protectable-pages, entity, and endpoint registration code that currently must be hand-added into `Web/Program.cs` and `Api/Program.cs`, eliminating the manual-wiring drift surface.

**Steps:**

1. Extract the registration shapes from the reference Supplier wiring:
   - **API entities:** `Api/Program.cs:94-103` — entity registration calls.
   - **API endpoints:** `Api/Program.cs:135-139` — endpoint mapping calls.
   - **Web protectable pages:** `Web/Program.cs:155-162` — page registration calls.
   - **Web endpoints:** `Web/Program.cs:189-194` — web-side endpoint mapping.
   Document each shape as a Scriban/interpolation template.
2. Add a `--emit-registrations` output mode to `Dabbler.CrudSlice` (or a companion generator invoked by the wrapper). This mode writes a single `{Entity}Registrations.g.cs` file containing:
   - A clearly-commented `// — Copy into Api/Program.cs RegisterEntities() —` block.
   - A clearly-commented `// — Copy into Api/Program.cs MapEndpoints() —` block.
   - A clearly-commented `// — Copy into Web/Program.cs RegisterPages() —` block.
   - A clearly-commented `// — Copy into Web/Program.cs MapEndpoints() —` block.
3. Add unit tests asserting the generated registration blocks match the exact shapes extracted in Step 1 (entity names, method signatures, parameter patterns).
4. Fix the duplicate `/suppliers` page registration bug: remove the duplicate line at `Web/Program.cs:101` or `:103` (whichever is the duplicate) as part of verifying the generator produces only one registration per page.
5. Wire the registration generator into the wrapper's default pipeline so `dabbler-crud-slice` emits the registration file alongside the scaffolding extensions.

**Creates:**
- Template: generated `{Entity}Registrations.g.cs`
- Registration shape templates (Scriban or string interpolation) under `tools/Dabbler.CrudSlice/Templates/`

**Touches:**
- `tools/Dabbler.CrudSlice/` (add registration generation to pipeline)
- `tools/Dabbler.CrudSlice.Tests/` (add registration shape assertions)
- `src/Apps/AcmeCoffeeOnline.Web/Program.cs` (fix duplicate `/suppliers` registration)

**Ends with:** `dabbler-crud-slice` for a test entity emits a `{Entity}Registrations.g.cs` file whose blocks, when copy-pasted into `Api/Program.cs` and `Web/Program.cs`, match the existing Supplier registration shapes exactly. The duplicate `/suppliers` page registration is fixed. Unit tests pass.

**Progress keys:** `registration-shapes-extracted`, `registration-generator-impl`, `registration-generator-tests`, `duplicate-page-fix`

---

### Session 4 of 4: Documentation reconciliation, section-8 additions, and end-to-end proof

**Purpose.** Reconcile all open doc-accuracy items, add the harvester-recommended documentation, and prove the complete wrapper pipeline end-to-end by re-scaffolding Supplier through the new tooling.

**Steps:**

1. **Reconcile doc-accuracy item 1** (`FEEDBACK_REMEDIATION_SUMMARY.md`): Update `CRUD_SCAFFOLDING_FINAL_SUMMARY.md` and `docs/scaffolding-templates/README.md` to establish a single source of truth for integration wiring. The wrapper's generated `{Entity}ScaffoldingExtensions.cs` is the documented integration mechanism; remove or annotate any references to the old manual `AddSupplierScaffolding()` pattern.
2. **Reconcile doc-accuracy item 2**: Correct the nav-menu-entry claim. The page template does NOT generate a nav menu entry; menu wiring is deferred to Granularity 3 (already in `non-mvp-roadmap.md`). Update `docs/scaffolding-templates/crud-page-template.md` to state this explicitly.
3. **Reconcile doc-accuracy item 3**: Resolve the Playwright/manual-verification contradiction in `CRUD_SCAFFOLDING_FINAL_SUMMARY.md`. State clearly: smoke tests are automated via `CrudServiceTemplateTests` / `CrudPageTemplateTests`; end-to-end browser verification is manual until a Playwright suite is added (deferred).
4. **Add applicability note** (harvester 8a) to the top of `docs/scaffolding-templates/README.md`:
   - **For:** single-entity CRUD/admin slices (suppliers, shippers, payment methods, lookup tables).
   - **Not for:** workflow-driven flows with status state machines, transition gates, multi-entity orchestration, domain-specific commands. Author those by hand. The scaffolder supports entity-maintenance surfaces adjacent to a workflow, but not the workflow itself.
5. **Add reauthorization gate** (harvester 8c) to `docs/scaffolding-templates/non-mvp-roadmap.md`: "Granularity 3 and beyond items are deferred until existing MVP templates have been used on at least 2-3 real entities by a downstream consumer. Reauthorization review required before beginning any deferred item."
6. **Add recipe** (harvester 8d): Create `docs/scaffolding-templates/recipes/extend-crud-page-with-workflow-action.md`. Worked example showing: partial class extension of a generated detail page, DI registration for a domain action service, button-group addition, controller endpoint, API client method. Explicit note that the recipe is for adjacent domain actions, not for replacing a hand-authored workflow page.
7. **End-to-end proof re-scaffold.** Delete the existing Supplier scaffold outputs. Run `dabbler-crud-slice` for `Supplier`. Verify:
   - All generated files are present (service, page, schema-applied, extensions, registrations).
   - Generated `SupplierScaffoldingExtensions.cs` uses `Services:ApiBaseUrl`, sets `X-Internal-Api-Key`, attaches `UserIdentityHandler`.
   - Generated `SupplierRegistrations.g.cs` blocks match the shapes in `Api/Program.cs` and `Web/Program.cs`.
   - Existing smoke tests (`CrudServiceTemplateTests`, `CrudPageTemplateTests`) still pass.
   - No duplicate `/suppliers` page registration.
8. Update `FEEDBACK_REMEDIATION_SUMMARY.md` to mark all three doc-accuracy items as resolved, referencing the specific commits/files.

**Creates:**
- `docs/scaffolding-templates/recipes/extend-crud-page-with-workflow-action.md`

**Touches:**
- `CRUD_SCAFFOLDING_FINAL_SUMMARY.md`
- `FEEDBACK_REMEDIATION_SUMMARY.md`
- `docs/scaffolding-templates/README.md`
- `docs/scaffolding-templates/crud-page-template.md`
- `docs/scaffolding-templates/non-mvp-roadmap.md`
- `src/Apps/AcmeCoffeeOnline.Web/Data/SupplierScaffoldingExtensions.cs` (verified replacement from Session 2)
- `src/Apps/AcmeCoffeeOnline.Web/Program.cs` (verified duplicate fix from Session 3)

**Ends with:** All three `FEEDBACK_REMEDIATION_SUMMARY.md` doc-accuracy items are reconciled. Applicability note, reauthorization gate, and recipe are committed. A clean Supplier re-scaffold through the wrapper produces convention-correct, non-drifted output. All existing smoke tests pass. The set is complete.

**Progress keys:** `doc-accuracy-reconciled`, `applicability-note-added`, `reauthorization-gate-added`, `recipe-added`, `e2e-proof-complete`

---

## End-of-set deliverables

- **`tools/Dabbler.SchemaApplicator/`** — Cross-platform C# CLI replacing `Apply-{Entity}Schema.ps1`. Unit-tested.
- **`tools/Dabbler.CrudSlice/`** — Repo-aware wrapper command. Single entry point invoking both templates, schema applicator, and registration generator. Integration-tested.
- **Convention-correct generated files** — `{Entity}ScaffoldingExtensions.cs` (Web-side DI/HttpClient with correct config key, headers, handler), `{Entity}ApiRegistrationExtensions.cs` (API-side DI), `{Entity}Registrations.g.cs` (copy-paste registration blocks for `Program.cs`).
- **Duplicate `/suppliers` page registration fixed** in `Web/Program.cs`.
- **`SupplierScaffoldingExtensions.cs` replaced** — old wrong-convention exemplar removed; wrapper-generated version in place.
- **Doc reconciliation complete** — all three `FEEDBACK_REMEDIATION_SUMMARY.md` items resolved in source docs.
- **`docs/scaffolding-templates/README.md`** — applicability note added at top.
- **`docs/scaffolding-templates/non-mvp-roadmap.md`** — reauthorization gate added.
- **`docs/scaffolding-templates/recipes/extend-crud-page-with-workflow-action.md`** — first recipe.
- **End-to-end proof** — Supplier re-scaffolded through the wrapper with verified output.

---

## Anti-patterns this set MUST avoid

1. **Editing user-owned files.** The wrapper and generators emit registration code in separately-generated files. They MUST NOT modify `Program.cs`, `DbContext`, navigation menus, or any hand-authored file via Roslyn rewriting, text insertion, or any other mechanism. That is Granularity 3 and remains deferred.

2. **Scope creep into deferred roadmap items.** No reflection-driven discovery, no `--regenerate` / drift detection, no OData templates, no master-detail templates. If an implementation choice pulls toward any of these, stop and defer. The reauthorization gate (added in Session 4) codifies this boundary.

3. **Platform-specific post-processes.** The entire pipeline must run without PowerShell. If any step reintroduces a `.ps1` dependency, it violates the schema-applicator deliverable's purpose.

4. **Generating code that doesn't match the host's real conventions.** Every generated config key, header, handler, and registration shape must be verified against the actual `Web/Program.cs` and `Api/Program.cs` wiring — not against assumed or documented conventions. The `Api:BaseUrl` → `Services:ApiBaseUrl` drift is the cautionary exemplar.

5. **Treating the wrapper as a library.** `Dabbler.CrudSlice` is a CLI tool invoked by a developer, not a runtime dependency. It MUST NOT be referenced by application projects. It lives under `tools/`, not `src/`.