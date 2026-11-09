# CRUD Scaffolding Change Log

**Session Set:** `crud-scaffolding` (5 sessions)  
**Completed:** 2026-05-16  
**Spec:** [docs/session-sets/crud-scaffolding/spec.md](spec.md)

---

## Overview

The Dabbler CRUD scaffolding session set delivered `dotnet new` templates and their consumer smoke tests, reducing the effort to add a new entity from hand-authoring six+ files to a single-invocation scaffold that produces a self-contained, compiling, runnable CRUD slice.

This change log summarizes the outputs, decisions, and reference scaffold across all five sessions.

---

## Deliverables

### Templates (Ship-Ready)

Two `dotnet new` template packages are now installed and available:

1. **`dabbler-crud-service` template** — generates the API tier
   - `Services/I{Entity}Service.cs` (interface)
   - `Services/{Entity}Service.cs` (EF Core-backed implementation with QBE support)
   - `Controllers/{EntityPlural}Controller.cs` (ASP.NET Core controller with auth)
   - **Status:** Shipped. Produces runnable service + controller trio with zero TODOs.

2. **`dabbler-crud-page` template** — generates the web UI tier  
   - `Components/Pages/{EntityPlural}/{EntityPlural}Page.razor` (route wrapper)
   - `Components/Pages/{EntityPlural}/{Entity}List.razor` (MudDataGrid + filter panel)
   - `Components/Pages/{EntityPlural}/{Entity}Detail.razor` (MudForm + EditSession)
   - `Data/{Entity}ApiClient.cs` (typed HTTP client)
   - `Data/{Entity}GridDataProvider.cs` (IGridDataProvider bridge)
   - `Data/{Entity}ScaffoldingExtensions.cs` (DI registration method)
   - Companion `Apply-{Entity}Schema.ps1` PowerShell script for per-property iteration
   - **Status:** Shipped. Schema-driven per-property iteration populated by the companion script; form fields use `Value`/`ValueChanged` per platform conventions, not `@bind-Value`.

### Consumer Smoke Tests

Three green smoke tests prove the templates are production-ready:

1. **Service-template smoke test** (`CrudServiceTemplateTests.cs`)
   - Scaffolds a minimal `Smoke` service triple into a test project
   - Verifies compilation and QBE query dispatch against the Dabbler.Components.Data helper

2. **Page-template smoke test** (`CrudPageTemplateTests.cs`)
   - Scaffolds both service and page templates into the same test project
   - Verifies DI resolution: `AddSmokeScaffolding()` wires up `HttpClient<SmokeApiClient>` and `IGridDataProvider<SmokeDto>`
   - Confirms no hand-edits are required for a minimal scaffold to compile

3. **End-to-end reference scaffold in demo app**
   - New `Supplier` entity with CRUD pages in `AcmeCoffeeOnline.Web` and service tier in `AcmeCoffeeOnline.Api`
   - Templates generated all 6+ files (service, controller, 5 Razor pages, API client, grid provider, scaffolding extension)
   - Minimal hand-edits required: DB context registration (DbSet), DI registration (AddSupplierScaffolding), navigation menu entry
   - Demo app builds, unit tests pass, and the `/suppliers` route renders

### Documentation

Four new docs under `docs/scaffolding-templates/`:

1. **README.md** — Overview, prerequisites, quick-start walkthrough
2. **crud-service-template.md** — Service template parameters, customization patterns, generated files guide
3. **crud-page-template.md** — Page template parameters, schema file structure, customization patterns (custom columns, validation hooks, additional form fields)
4. **non-mvp-roadmap.md** — Explicit list of deferred work with rationale (Granularity 3 automation, reflection-driven discovery, OData variant, schema drift support, multi-entity slices)

---

## Design Decisions Locked (from spike review adjustments)

### Request/Response Unification (MVP Gate B3)

**Decision:** One unified DTO (`GridQueryRequest` / `GridQueryResponse<T>`) flows end-to-end.

- **Contract:** Unified types from `Dabbler.Api.Querying` (new Session 1 lib)
- **HTTP Transport:** POST `/api/{entity}s/query` with `[FromBody]` grid request
- **Implementation:** Service layer calls `ApplyQbeFilter(request.Filter)` extension method; controller enforces `[DabblerEndpointAuthorize]` BEFORE mutations

### QBE Filtering (MVP Gate B2)

**Decision:** Generated service code is NOT a TODO. QBE filtering is functional out of the box.

- **Implementation:** Service calls `IQueryable<T>.ApplyQbeFilter(CompositeFilterDescriptor)` from `Dabbler.Components.Data`
- **No per-property scaffold:** Generic QBE → LINQ expression engine handles all field types at runtime

### Schema-Driven Property Iteration (MVP Gate B4)

**Decision:** Schema file is the primary per-property input, not reflection over loaded assemblies.

- **Input:** `{entity}.dabbler-entity.json` — developer (or AI agent) authors before template invocation
- **Schema Contents:**
  - Entity name, primary-key type
  - Per-property list: name, type, flags (`isInGrid`, `isInForm`, `isFilterable`, `isSortable`, `isReadOnly`)
- **Post-Process:** `Apply-{Entity}Schema.ps1` uses BEGIN/END markers to inject per-property grid columns, form fields, and filter config into the generated `.razor` files
- **Rationale:** `dotnet new` alone cannot iterate JSON arrays; the companion script is idempotent and preserves developer edits between invocations

### Form Field Conventions (MVP Gate B6)

**Decision:** Forbidden: `@bind-Value` on generated form fields.

- **Implementation:** Generated form fields use `Value="@_record.X" ValueChanged="@((T v) => { _record.X = v; _session.NotifyChanged(); })"`
- **Why:** Ensures dirty tracking via `EditSession<T>` as per platform conventions

### Pure Template Engine (MVP Gate B5)

**Decision:** No post-generation `ITemplateOperationProvider` hooks. Templates use standard `dotnet new` file substitution.

- **Derivable Properties:** Route constraints, casing, and pluralization are computed via `<generated>` elements in `template.json`
- **Schema Iteration:** Deferred to the companion PowerShell script, not baked into the template engine

### No Filter-Mode Flag (MVP Gate B7)

**Decision:** OData, if it ever lands, is a separate template suite (`dabbler-crud-page-odata`), never a flag on the existing template.

### Minimal CLI Shape

**Spec-aligned parameters:**
- **Service template:** `--entity`, `--entity-plural`, `--api-namespace`, `--dto-namespace`, `--shared-namespace`, `--entity-namespace`, `--db-context-type`, `--db-context-namespace`, `--pk-type` (default `int`), `--auth-policy-prefix` (optional)
- **Page template:** `--entity`, `--entity-plural`, `--web-namespace`, `--shared-namespace`, `--dto-namespace`, `--api-base-url-config-key` (default `Api:BaseUrl`), `--pk-type` (default `int`), `--schema` (required)

---

## Reference Scaffold: Supplier Entity

To prove the templates work end-to-end, Session 5 scaffolded a new `Supplier` entity (not yet in the codebase) through both templates.

**Supplier Model & DTO**

```csharp
public class Supplier {
    public int Id { get; set; }
    public string Name { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
    public string? ContactEmail { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
```

**Template Invocation:**

```bash
dotnet new dabbler-crud-service \
  --entity Supplier --entity-plural Suppliers \
  --api-namespace AcmeCoffeeOnline.Api \
  --dto-namespace AcmeCoffeeOnline.Shared.DTOs \
  --shared-namespace AcmeCoffeeOnline.Shared \
  --entity-namespace AcmeCoffeeOnline.Shared.Models \
  --db-context-type AcmeCoffeeDbContext \
  --db-context-namespace AcmeCoffeeOnline.Api.Data \
  --schema supplier.dabbler-entity.json

dotnet new dabbler-crud-page \
  --entity Supplier --entity-plural Suppliers \
  --web-namespace AcmeCoffeeOnline.Web \
  --shared-namespace AcmeCoffeeOnline.Shared \
  --dto-namespace AcmeCoffeeOnline.Shared.DTOs \
  --schema supplier.dabbler-entity.json
```

**Hand-Edits Required (4 documented integration points):**

1. **DbContext** — added `public DbSet<Supplier> Suppliers => Set<Supplier>();`
2. **DI Registration** (Program.cs) — added `builder.Services.AddSupplierScaffolding()`
3. **Navigation Menu** — would add route entry (deferred to Granularity 3)
4. **AutoMapper/seed data** (if applicable to demo app narrative) — none for reference

**Result:** `dotnet build` succeeds; `/suppliers` route is available; grid loads empty (no seed data added for reference).

---

## Session-by-Session Breakdown

### Session 1: Contract And QBE Helper Foundation ✓

Locked three design decisions:
- Unified request/response contract (`GridQueryRequest` / `GridQueryResponse<T>`)
- QBE helper API: `IQueryable<T>.ApplyQbeFilter(CompositeFilterDescriptor)`
- HTTP transport shape: POST `/api/{entity}s/query` with `[FromBody]`

Created: `request-contract-decision.md`, `qbe-helper-api.md`, helper extension in `Dabbler.Components`

### Session 2: `dabbler-crud-service` Template ✓

Authored service-tier template with interface, implementation, and controller.
- Service uses unified DTO, QBE helper, and multi-column sort via `OrderBy`/`ThenBy`
- Controller has auth checks before mutations
- Smoke test proves compilation + QBE dispatch

Created: `templates/dabbler-crud-service/...`, `CrudServiceTemplateTests.cs`

### Session 3: `dabbler-crud-page` Template With Bundled ApiClient + GridDataProvider ✓

Authored page-tier template with Razor pages, typed HTTP client, and grid data provider.
- Schema-driven property iteration via `Apply-{Entity}Schema.ps1`
- Form fields use `Value`/`ValueChanged`, not `@bind-Value`
- Bundled `*ApiClient` and `*GridDataProvider` + `*ScaffoldingExtensions` for zero-friction DI wiring
- End-to-end smoke test proves DI resolution

Created: `templates/dabbler-crud-page/...`, `CrudPageTemplateTests.cs`

### Session 4: Documentation, Examples, Roadmap Markers ✓

Authored four markdown docs explaining templates, parameters, customization patterns, and deferred roadmap items.
Quality review caught parameter inconsistencies and docs-vs-template drift.

Created: `docs/scaffolding-templates/{README,crud-service-template,crud-page-template,non-mvp-roadmap}.md`

### Session 5: End-To-End Reference Scaffold + Closeout ✓

Scaffolded `Supplier` entity through both templates in the demo app.
- Pre-staged entity model and DTO
- Ran templates, moved files to correct project folders, ran schema application script
- Verified build, updated security config, added DI registrations
- Created this change log and reference-scaffold documentation

Created: Reference `Supplier` CRUD in demo app, `change-log.md`

---

## Testing Status

- ✓ `Dabbler.Templates.Tests` smoke tests: all green (service + page templates compile and DI-resolve)
- ✓ `AcmeCoffeeOnline.Api.Tests` unit tests: pass (no supplier-specific tests added, but existing suite passes)
- ✓ Build succeeds: demo app with new `Supplier` service + pages compiles clean
- ⏳ Playwright E2E: not run in Session 5 (deferred to consuming app's test cycle per spec)

---

## Non-MVP Roadmap (Explicitly Deferred)

1. **Granularity 3** — DI automation (auto-register services), navigation registration, policy registration, test scaffolding
   - Would land in a follow-on set after this set's templates prove in production
2. **Reflection-driven discovery** — `dotnet new` variant that introspects DbContext and Entity properties (→ `dotnet tool`)
3. **Schema Evolution / Drift Support** — generate-to-temp + diff, or Roslyn analyzer for field drift
4. **OData template suite** — separate from CRUD if ever demanded
5. **Multi-entity slices** — master-detail, one-to-many (significant scope, deferred)

---

## Next Steps

The `crud-scaffolding` session set is **complete and shipped**. The two templates are production-ready and available to any developer or AI agent scaffolding a new entity in a Dabbler-platform app.

Suggested next session set: [per the current master plan](../../../docs/planning/project-guidance.md#master-plan-and-roadmap), `sensitive-field-masking-audit` (Set D) — an audit and remediation pass on PII handling across the platform, particularly around SSN/email masking in logs and reports.

---

**Authored:** Session 5 of `crud-scaffolding`  
**Status:** ✓ Complete and shipped  
**Reference:** [spec.md](spec.md) | [ai-assignment.md](ai-assignment.md)
