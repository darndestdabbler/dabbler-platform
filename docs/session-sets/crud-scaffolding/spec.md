# CRUD Scaffolding Spec

> **Purpose:** Ship the Dabbler CRUD scaffolding templates so adding a new entity to a Dabbler-platform application reduces from hand-authoring six+ files to a `dotnet new dabbler-crud-service Customer && dotnet new dabbler-crud-page Customer` invocation that produces a self-contained, compiling, runnable CRUD slice.
> **Created:** 2026-05-16
> **Session Set:** `docs/session-sets/crud-scaffolding/`
> **Prerequisite:** `crud-scaffolding-spike` (closed 2026-05-16) — the spike's [`scaffolding-approach-proposal.md`](../crud-scaffolding-spike/scaffolding-approach-proposal.md) (as adjusted INLINE after Session 3) and [`scaffolding-approach-review.md`](../crud-scaffolding-spike/scaffolding-approach-review.md) (tri-provider review) are this set's design inputs.
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Session Set Configuration

```yaml
requiresUAT: false
requiresE2E: false
uatScope: none
```

> **Rationale:** This set delivers `dotnet new` templates and their consumer smoke tests, not user-visible UI behavior. The closeout proof is a pack → install → scaffold → build smoke cycle (per the lesson *Author Template Content And Consumer Smoke Test In The Same Session*), not a human UAT pass. The scaffolded CRUD pages, once invoked in a consuming app, would themselves be subject to UAT — but that is the consumer's set, not this one. E2E coverage of the platform's CRUD primitives is already covered by `composable-crud-helpers` and the existing AcmeCoffeeOnline Products / Customers / Orders pages; the scaffolder's output is structurally identical to that proven code.

---

## Project Overview

The `crud-scaffolding-spike` resolved the design questions for a code-generation scaffolding system. The tri-provider review converged on **APPROVE WITH ADJUSTMENTS / NEEDS REWORK** with seven BLOCKING + four SUBSTANTIVE adjustments inherited as MVP gates for this implementation set:

### MVP gates (from the spike's review)

1. **Self-sufficient generated slice.** Granularity 1 + Granularity 2 produce a runnable CRUD feature with no hand-authored glue. The minimal `{Entity}ApiClient` and `{Entity}GridDataProvider` move INTO MVP scope.
2. **Working QBE filtering, not a TODO.** The generated `{Entity}Service` calls into the platform's existing QBE → LINQ expression engine; the generated code is functional out of the box.
3. **Unified request/response contract across all layers.** ONE request DTO (one of `GridDataRequest` or `PagedRequest` — see Session 1's contract decision) flows through UI → API client → controller → service. `CompositeFilterDescriptor` HTTP transport is locked at spec time (POST `/query` with `[FromBody]` is the strong candidate; alternative is a JSON-encoded query-string parameter).
4. **Schema-file is the primary per-property input** (review gate B4). No reflection-over-loaded-assembly in MVP. The CLI shape is:
   - **Required** explicit identity / namespace parameters: `--entity`, `--api-namespace` / `--web-namespace` / `--shared-namespace` / `--dto-namespace`, `--pk-type` (default `int`).
   - **Required for templates that enumerate fields** (the page template's MudDataGrid columns + MudForm fields, the filter-config field list, the service's per-property handling): `--schema <path-to-{entity}.dabbler-entity.json>`. The schema file is a small JSON document the developer (or AI agent) authors before running the template; it lists each property's name, type, and per-surface flags (`IsInGrid`, `IsInForm`, `IsFilterable`, `IsSortable`, `IsReadOnly`). The implementation set ships a JSON-schema document for `*.dabbler-entity.json` so editors validate it.
   - Reflection-driven discovery is deferred to a future `dotnet tool` variant — `dotnet new` is purely declarative.
5. **Pure `dotnet new` CLI shape.** No post-generation `ITemplateOperationProvider` hook. Files are emitted by parameter substitution; per-property iteration uses the standard template-engine constructs available to `dotnet new` (e.g., `<choices>`, sub-templates, or a schema-file-driven post-process).
6. **`@bind-Value` is forbidden in generated form fields.** The generator emits `Value="@_record.X" ValueChanged="@((T v) => { _record.X = v; _session.NotifyChanged(); })"` per the platform's CRUD Form Conventions (`project-guidance.md`). This was a code-correctness defect in the spike's sample that must NOT propagate to the templates.
7. **No `--filter-mode` flag.** If/when OData ever ships, it lands as a separate template suite (`dabbler-crud-page-odata`, etc.), never as a flag on the existing template.

### Non-MVP roadmap items (in scope to declare; out of scope to implement)

- **Granularity 3** — DI registration automation, navigation registration, security-policy registration, smoke-test scaffolding. Lands in a follow-on set after this set proves Granularities 1 + 2 in production.
- **Reflection-over-assembly discovery + DbContext introspection** — moves to a `dotnet tool` variant in a follow-on set.
- **Schema Evolution / Drift Support** — a generate-to-temp + diff workflow OR a Roslyn analyzer flagging entity properties not represented in scaffolded code. Preserves "generate once, own the code" while reducing the field-add friction both tri-provider reviewers flagged.
- **OData template suite** — separate template suite, deferred until demand signal emerges.
- **Multi-entity slices** (one-to-many, master-detail) — deferred.

### Scope explicit non-goals (carry-over from the spike)

- No runtime configuration composite (the rejected `MasterDetailsConfig<TEntity>` pattern stays rejected).
- No reflection-driven UI at runtime (generated `PropertyColumn` and `MudTextField` definitions are explicit).
- No replacement for explicit `MudDataGrid` column definitions.
- No in-place regeneration / `--regenerate` flag.
- No automatic test generation beyond smoke tests.

---

## Parallelization Notes

The set is **sequential**. Each session builds on the prior session's templates and the smoke-test closure of the prior session. No internal parallelization is justified. Externally, this set can run in parallel with `security-dashboard-composite-migration` (disjoint files; no shared session-state).

---

## Session Plan

### Session 1 of 5: Contract And QBE Helper Foundation

**Purpose:** Lock the design decisions the spike review left as MVP gates so the template work in subsequent sessions has stable inputs.

**Steps:**

1. Read `project-guidance.md`, `lessons-learned.md`, `session-set-authoring-guide.md`, the adjusted [spike proposal](../crud-scaffolding-spike/scaffolding-approach-proposal.md), and the spike's [tri-provider review](../crud-scaffolding-spike/scaffolding-approach-review.md).
2. **Contract unification (review gate B3).** Inventory `GridDataRequest`, `PagedRequest`, and their response shapes across UI (`Dabbler.Components.Data`), API (`AcmeCoffeeOnline.Api`), and shared models. Route a structural analysis via `route(task_type="analysis", complexity_hint=55)` to recommend whether to (a) consolidate on one type with adapters at the edges, (b) keep both with documented translation, or (c) introduce a new unified type. The session authors a `request-contract-decision.md` capturing the decision and the rationale.
3. **QBE → LINQ helper inventory (review gate B2).** Locate the existing QBE expression engine in `src/Libs/Dabbler.Components/`. Decide the exact extension-method API the generated service code will call: `query.ApplyQbeFilter(request.Filter)` is the placeholder name from the spike; the session verifies the real API and either uses it directly or adds the extension method if it does not yet exist. Output: `qbe-helper-api.md` documenting the API and a unit test if the extension method had to be added.
4. **HTTP transport for `CompositeFilterDescriptor` (review gate B3 cont.).** Decide whether GET with `[FromQuery]` is feasible for the QBE filter graph (probably not — nested object graphs serialize poorly into query strings) or whether POST `/api/{entity}s/query` with `[FromBody]` is the correct shape. Document the decision in `request-contract-decision.md`. The controller template in Session 3 will follow it.
5. Build + test to confirm any helper additions land green. Cross-provider verify.

**Creates:** `request-contract-decision.md`, `qbe-helper-api.md`. Possibly a new extension method file under `src/Libs/Dabbler.Components/` if the QBE helper needs broadening.
**Touches:** This session-set folder; possibly one helper file under `Dabbler.Components`.
**Ends with:** Three locked decisions (one request DTO, one QBE helper API, one HTTP transport shape) that the next four sessions consume without re-litigating.
**Progress keys:** `session-1/contract-analysis`, `session-1/qbe-helper-inventory`, `session-1/transport-decision`, `session-1/build-test`.

---

### Session 2 of 5: `dabbler-crud-service` Template

**Purpose:** Ship the service-tier template (interface + implementation + controller) and its consumer smoke test.

**Steps:**

1. Read Session 1's contract + QBE decisions.
2. Author the `templates/dabbler-crud-service/` template folder under the existing `templates/Dabbler.Templates/` packaging project. The template's `.template.config/template.json` declares parameters: `--entity`, `--api-namespace`, `--dto-namespace`, `--shared-namespace`, `--pk-type` (default `int`), `--auth-policy-prefix` (default same as entity name), and **`--schema <path>` (required when the service emits per-property filter/sort handling).** The schema file path is resolved relative to the working directory at template-invocation time.
3. Author the file templates: `Services/I{Entity}Service.cs`, `Services/{Entity}Service.cs`, `Controllers/{PluralEntity}Controller.cs`. The implementation:
   - Uses the unified request DTO from Session 1 across the trio.
   - Calls the QBE→LINQ helper from Session 1 (no TODO).
   - Uses `OrderBy` then `ThenBy` for multi-column sort (not repeated `OrderBy`).
   - Places auth checks BEFORE mutations in the controller (per the spike review's S3 adjustment).
   - Generates the matching `[DabblerEndpointAuthorize("…")]` attributes with policy names derived from `--auth-policy-prefix`.
4. Author the consumer smoke test under `tests/Dabbler.Templates.Tests/`: commit a fixture `tests/Dabbler.Templates.Tests/Fixtures/smoke.dabbler-entity.json` describing a minimal entity (Id + Name + IsActive), install the local nupkg, run `dotnet new dabbler-crud-service --entity Smoke --api-namespace SmokeApi.Api --dto-namespace SmokeApi.Shared.DTOs --shared-namespace SmokeApi.Shared --schema Fixtures/smoke.dabbler-entity.json`, build the resulting project, assert it compiles.
5. Build + test. Cross-provider verify.

**Creates:** `templates/Dabbler.Templates/templates/dabbler-crud-service/...`, `tests/Dabbler.Templates.Tests/CrudServiceTemplateTests.cs`.
**Touches:** `templates/Dabbler.Templates/Dabbler.Templates.csproj` (template package list).
**Ends with:** A green smoke test that compiles a fresh `dabbler-crud-service` scaffold against the existing `Dabbler.Components` library.
**Progress keys:** `session-2/template-author`, `session-2/smoke-test`, `session-2/build-test`.

---

### Session 3 of 5: `dabbler-crud-page` Template With Bundled ApiClient + GridDataProvider

**Purpose:** Ship the page-tier template AND the matching minimal `{Entity}ApiClient` + `{Entity}GridDataProvider` so Granularity 1 produces a runnable slice (MVP gate B1).

**Steps:**

1. Read Sessions 1 and 2.
2. Author the `templates/dabbler-crud-page/` template folder. Parameters: `--entity`, `--web-namespace`, `--shared-namespace`, `--api-base-url-config-key` (default `Api:BaseUrl`), `--pk-type` (default `int`), and **`--schema <path>` (required — the page template enumerates fields on the grid, in the form, and in the filter config; the schema file is the per-property input).**
3. Author the file templates:
   - `Components/Pages/{PluralEntity}/{PluralEntity}Page.razor` — thin route wrapper.
   - `Components/Pages/{PluralEntity}/{Entity}List.razor` — reusable list component (grid + filter panel).
   - `Components/Pages/{PluralEntity}/{Entity}Detail.razor` — detail/edit page using `EditSession<TDto>` and `DirtyAwareActionRow`.
   - `Data/{Entity}ApiClient.cs` — typed `HttpClient` wrapper around the controller endpoints emitted in Session 2.
   - `Data/{Entity}GridDataProvider.cs` — implements `IGridDataProvider<{Entity}Dto>`, bridges the page's `GridDataRequest` to the `ApiClient` call.
   - `Data/{Entity}ScaffoldingExtensions.cs` — a generated `IServiceCollection` extension method (`Add{Entity}Scaffolding(this IServiceCollection services)`) that registers both `HttpClient<{Entity}ApiClient>` and `IGridDataProvider<{Entity}Dto>` so the consumer only has to call one line in `Program.cs`.
4. **Generated form fields use `Value`/`ValueChanged` per the platform's CRUD Form Conventions** — `@bind-Value` is forbidden. The `{Entity}Detail.razor` template must comply.
5. Consumer smoke test under `tests/Dabbler.Templates.Tests/`: a multi-template scenario that runs both `dabbler-crud-service` and `dabbler-crud-page` (each passing `--schema Fixtures/smoke.dabbler-entity.json` — the same fixture authored in Session 2) into a single project skeleton with the matching DTO + entity types pre-staged, then `dotnet build`. Assert compile + that `Add{Entity}Scaffolding` resolves all needed services from the DI container.
6. Build + test. Cross-provider verify.

**Creates:** `templates/Dabbler.Templates/templates/dabbler-crud-page/...`, `tests/Dabbler.Templates.Tests/CrudPageTemplateTests.cs`.
**Touches:** `templates/Dabbler.Templates/Dabbler.Templates.csproj`.
**Ends with:** A green smoke test that compiles and DI-resolves a fresh `dabbler-crud-page` + `dabbler-crud-service` pair end-to-end without hand-edits.
**Progress keys:** `session-3/template-author`, `session-3/api-client-template`, `session-3/grid-provider-template`, `session-3/scaffolding-extensions`, `session-3/end-to-end-smoke`.

---

### Session 4 of 5: Documentation, Examples, Roadmap Markers

**Purpose:** Document the templates, their parameters, their customization points, and the explicitly-deferred follow-on work so a developer (human or AI agent) can find and use them.

**Steps:**

1. Author `docs/scaffolding-templates/README.md`: overview, list of templates, prerequisites, quick-start.
2. Author `docs/scaffolding-templates/crud-service-template.md`: parameter reference, generated file map, customization patterns (e.g., adding a non-CRUD endpoint, custom validation, alternate auth policy mapping).
3. Author `docs/scaffolding-templates/crud-page-template.md`: parameter reference, generated file map, customization patterns (custom column rendering, custom filter fields, custom MudForm validation, additional actions in the `CustomActions` slot).
4. Author `docs/scaffolding-templates/non-mvp-roadmap.md`: explicit list of deferred items with rationale for each: Granularity 3 (DI/nav/policy/test automation), reflection / DbContext discovery (→ `dotnet tool` variant), Schema Evolution / Drift Support, OData template suite, multi-entity slices.
5. Route a documentation-quality review via `route(task_type="documentation")` to catch parameter-name inconsistencies or example-vs-template drift before the next session's end-to-end pass.
6. Cross-provider verify.

**Creates:** `docs/scaffolding-templates/README.md`, `docs/scaffolding-templates/crud-service-template.md`, `docs/scaffolding-templates/crud-page-template.md`, `docs/scaffolding-templates/non-mvp-roadmap.md`.
**Touches:** None outside `docs/scaffolding-templates/`.
**Ends with:** A docs entry-point for the templates that an AI agent or new developer can land on cold and use to scaffold a new entity in under 5 minutes.
**Progress keys:** `session-4/readme`, `session-4/service-docs`, `session-4/page-docs`, `session-4/roadmap-docs`, `session-4/doc-review`.

---

### Session 5 of 5: End-To-End Reference Scaffold + Closeout

**Purpose:** Prove the templates by scaffolding a representative new entity end-to-end in the demo app, run the full test suite, and close the set with a change log.

**Steps:**

1. Pick a representative entity NOT yet implemented in `AcmeCoffeeOnline.Web` — e.g., `Supplier` (or `Shipment`, `Promotion`, whatever fits the demo app's narrative without colliding with existing pages).
2. Pre-stage the EF entity (`Supplier.cs`) and DTO (`SupplierDto.cs`) by hand (this is the developer's job; the templates fill in the rest). Author `supplier.dabbler-entity.json` listing the entity's properties + per-surface flags — this is the schema-file input for both templates.
3. Run:
   ```bash
   dotnet new dabbler-crud-service --entity Supplier --api-namespace AcmeCoffeeOnline.Api --dto-namespace AcmeCoffeeOnline.Shared.DTOs --shared-namespace AcmeCoffeeOnline.Shared --schema supplier.dabbler-entity.json
   dotnet new dabbler-crud-page --entity Supplier --web-namespace AcmeCoffeeOnline.Web --shared-namespace AcmeCoffeeOnline.Shared --schema supplier.dabbler-entity.json
   ```
4. Hand-edit only what is unavoidable: the EF DbSet registration on `AppDbContext`, the AutoMapper profile for the `Supplier` ↔ `SupplierDto` mapping, the `AddSupplierScaffolding()` call in `Program.cs`, the navigation menu entry (since Granularity 3 navigation automation is deferred), and any seed data.
5. Run `dotnet build` and the non-Playwright test projects (e.g., `dotnet test tests/AcmeCoffeeOnline.Api.Tests/`, `dotnet test tests/AcmeCoffeeOnline.Web.Tests/`, `dotnet test tests/Dabbler.Templates.Tests/`) for the suite-wide build/unit-test pass. Then exercise the Playwright E2E suite **one test at a time, against the already-up compose stack** — never via a full-solution `dotnet test Dabbler.Platform.sln` invocation (that runs Playwright tests in parallel against the same containers and produces unreadable failures or hangs; see feedback memory `feedback_playwright_one_at_a_time`). Sequence: `docker compose -f docker/docker-compose.infra.yml up -d` (persistent), `docker compose -f docker/docker-compose.app.yml up --build -d` (rebuilt for Session 5 code changes), then `dotnet test tests/AcmeCoffeeOnline.Playwright/ --list-tests` to enumerate cases, then one `dotnet test tests/AcmeCoffeeOnline.Playwright/ --filter "FullyQualifiedName=<one test>"` per test, streaming output per call. Confirm the new `/suppliers` route loads and the CRUD flow works. Capture a screenshot if practical.
6. Author the per-session `change-log.md` covering the whole set: templates shipped, contract decisions, non-MVP roadmap, the reference scaffold in the demo app. Route as `summarization` with `max_tier=1` per the platform convention.
7. Author the next-session-set recommendation routed via `route(task_type="analysis")`. The current master plan ([`project_role_rearchitecture_roadmap`](C:/Users/denmi/.claude/projects/c--Users-denmi-source-repos-dabbler-platform/memory/project_role_rearchitecture_roadmap.md) memory) names Set D (`sensitive-field-masking-audit`) as next; the routed analysis confirms or proposes a deviation.
8. Cross-provider verify the full session output. Commit, push, mark complete, notify. Run the Step 9 reorganization review.

**Creates:** New `Components/Pages/Suppliers/` folder + `Data/SupplierApiClient.cs` + `Data/SupplierGridDataProvider.cs` + `Services/ISupplierService.cs` + `Services/SupplierService.cs` + `Controllers/SuppliersController.cs` (all generated; minimal hand-edits noted in commit message), `docs/session-sets/crud-scaffolding/change-log.md`.
**Touches:** `AppDbContext`, AutoMapper profile, `Program.cs`, navigation menu, seed data — all hand-edited.
**Ends with:** The demo app has a new working `/suppliers` CRUD page produced almost entirely by the templates, with the hand-edits limited to the four documented integration points. The change log explains the path from spike to shipped templates. The set is marked complete; the next-set recommendation has been authored.
**Progress keys:** `session-5/pre-stage-entity`, `session-5/run-templates`, `session-5/integration-edits`, `session-5/full-test`, `session-5/change-log`, `session-5/next-set-recommendation`, `session-5/reorganization-review`.

---

## End-of-set deliverables

- Two `dotnet new` templates shipped under `templates/Dabbler.Templates/templates/dabbler-crud-{service,page}/`.
- Three consumer smoke tests (one per template + one end-to-end pair) under `tests/Dabbler.Templates.Tests/`.
- Four documentation files under `docs/scaffolding-templates/`.
- A new working `/suppliers` (or equivalent) CRUD page in the AcmeCoffeeOnline demo app as the reference scaffold.
- `docs/session-sets/crud-scaffolding/change-log.md` summarizing the set.

---

## Anti-patterns this set MUST avoid

- **`@bind-Value` in any generated form field.** (Review gate B6.) Forbidden by platform convention; bypasses `EditSession<T>` dirty tracking.
- **`// TODO: Apply filter expression here`** in any generated service. (Review gate B2.) The QBE→LINQ helper must be wired in at template time.
- **Mismatched request DTOs between layers.** (Review gate B3.) One DTO, one transport shape, end-to-end.
- **Post-generation reflection hooks** in the template engine. (Review gate B5.) `dotnet new` stays pure file substitution.
- **`--filter-mode` or any "swap filter representation" flag.** (Review gate B7.) OData, if it ever ships, is a separate template suite.
- **Generated controller mutations BEFORE auth checks.** (Review gate S3.) Auth first, mutation second, always.
- **Repeated `OrderBy` for multi-column sort.** (Review gate S1.) `OrderBy` then `ThenBy`.
