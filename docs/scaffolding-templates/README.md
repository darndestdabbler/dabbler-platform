# Dabbler CRUD Scaffolding Templates

The Dabbler CRUD scaffolding templates reduce the effort of adding a new entity to a Dabbler-platform application from hand-authoring six+ files to a two-command invocation that produces a self-contained, compiling, runnable CRUD slice.

## When To Use This Scaffolder

**For:** single-entity CRUD and admin slices — suppliers, shippers, payment methods, lookup tables, reference data, and similarly shaped entity-maintenance surfaces. The templates produce one list page, one detail/edit page, and the supporting service tier for a single entity.

**Not for:** workflow-driven flows with status state machines, transition gates, multi-entity orchestration, or domain-specific commands (e.g., order placement, return-merchandise authorization, payment authorization). Author those pages by hand. The scaffolder is appropriate for entity-maintenance surfaces *adjacent* to a workflow (e.g., the supplier or product catalog the order workflow consumes), but not for the workflow itself.

If a candidate page expresses a sequence of business operations rather than CRUD over a single row shape, the scaffolder is the wrong tool — start from a hand-authored page and adopt the platform's composable helpers (`EditSession<T>`, `<DirtyAwareActionRow>`, `<DeclarativeFilterPanel>`, `<MasterDetailExpansionShell>`, all from `Dabbler.Components`) à la carte.

## The `dabbler-crud-slice` Wrapper

For new work, prefer the repo-aware `dabbler-crud-slice` wrapper CLI over invoking the two underlying templates by hand. The wrapper:

- Invokes `dabbler-crud-service` and `dabbler-crud-page` in one command
- Runs the cross-platform `Dabbler.SchemaApplicator` post-process (no PowerShell dependency)
- Emits convention-correct registration helpers that match the real host wiring (config keys, `X-Internal-Api-Key` header, `UserIdentityHandler`)
- Emits a copy-paste `{Entity}Registrations.g.cs` file with the security/catalog blocks for `Api/Program.cs` and `Web/Program.cs`

Usage:

```bash
dotnet run --project tools/Dabbler.CrudSlice -- \
  --entity Supplier --entity-plural Suppliers \
  --schema supplier.dabbler-entity.json \
  --web-root src/Apps/AcmeCoffeeOnline.Web \
  --api-root src/Apps/AcmeCoffeeOnline.Api \
  --web-namespace AcmeCoffeeOnline.Web \
  --api-namespace AcmeCoffeeOnline.Api \
  --shared-namespace AcmeCoffeeOnline.Shared \
  --dto-namespace AcmeCoffeeOnline.Shared.DTOs \
  --entity-namespace AcmeCoffeeOnline.Shared.Models \
  --dbcontext-type AcmeCoffeeDbContext \
  --dbcontext-namespace AcmeCoffeeOnline.Api.Data
```

For single-project or minimal consumers, point both `--web-root` and `--api-root`
at the same project root. This is the expected invocation for `dabbler-min`
adopters — the template scaffolds one project that hosts both the Blazor
Server UI and the `/auth/*` API endpoints, so the wrapper's emitted
`{Entity}Controller.cs` and `{Entity}ScaffoldingExtensions.cs` both land
inside that single project. `WrapperOrchestrationTests.FullPipeline_endToEnd_against_smoke_fixture`
is the standing regression gate for this same-root pattern.

The two underlying templates (`dabbler-crud-service`, `dabbler-crud-page`) remain documented in [crud-service-template.md](crud-service-template.md) and [crud-page-template.md](crud-page-template.md) for users who need finer-grained control. The wrapper is the recommended entry point for end-to-end onboarding.

## Prerequisites

- `.NET 8` or later
- The `Dabbler.Templates` NuGet package installed locally (see **Installation** below)
- An existing EF entity class and corresponding DTO class pre-staged in your project
- A `.dabbler-entity.json` schema file describing the entity's properties (required for the page template)

## Installation

Install the templates from the local NuGet feed:

```bash
dotnet new install ./templates/Dabbler.Templates/bin/Release/Dabbler.Templates.*.nupkg
```

Or, if templates are already installed, update them:

```bash
dotnet new uninstall Dabbler.Templates
dotnet new install ./templates/Dabbler.Templates/bin/Release/Dabbler.Templates.*.nupkg
```

To verify installation, list available Dabbler templates:

```bash
dotnet new list dabbler
```

## Quick Start

### 1. Prepare Your Entity and DTO

Pre-stage two C# classes in your project:

**Entity** (e.g., `Supplier.cs`):
```csharp
namespace MyApp.Shared.Models
{
    public class Supplier
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ContactEmail { get; set; }
        public bool IsActive { get; set; }
    }
}
```

**DTO** (e.g., `SupplierDto.cs`):
```csharp
namespace MyApp.Shared.DTOs
{
    public class SupplierDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ContactEmail { get; set; }
        public bool IsActive { get; set; }
    }
}
```

### 2. Create a Schema File

Create a `.dabbler-entity.json` schema file describing which properties appear on the grid, form, and filters:

**supplier.dabbler-entity.json**:
```json
{
  "entityName": "Supplier",
  "primaryKey": {
    "name": "Id",
    "type": "int"
  },
  "properties": [
    {
      "name": "Name",
      "type": "string",
      "isInGrid": true,
      "isInForm": true,
      "isFilterable": true,
      "isSortable": true,
      "isReadOnly": false
    },
    {
      "name": "ContactEmail",
      "type": "string",
      "isInGrid": true,
      "isInForm": true,
      "isFilterable": true,
      "isSortable": false,
      "isReadOnly": false
    },
    {
      "name": "IsActive",
      "type": "bool",
      "isInGrid": true,
      "isInForm": true,
      "isFilterable": true,
      "isSortable": false,
      "isReadOnly": false
    }
  ]
}
```

### 3. Scaffold the Service

Generate the service interface, implementation, and controller:

```bash
dotnet new dabbler-crud-service \
  --entity Supplier \
  --entity-plural Suppliers \
  --api-namespace MyApp.Api \
  --dto-namespace MyApp.Shared.DTOs \
  --shared-namespace MyApp.Shared \
  --entity-namespace MyApp.Shared.Models \
  --db-context-type AppDbContext \
  --db-context-namespace MyApp.Api.Data
```

This generates:
- `Services/ISupplierService.cs` — service interface
- `Services/SupplierService.cs` — service implementation with QBE filtering
- `Services/SupplierApiRegistrationExtensions.cs` — one-line DI helper for `Program.cs`
- `Controllers/SuppliersController.cs` — ASP.NET Core controller with CRUD endpoints

### 4. Scaffold the Page

Generate the Blazor pages, data provider, and API client:

```bash
dotnet new dabbler-crud-page \
  --entity Supplier \
  --entity-plural Suppliers \
  --web-namespace MyApp.Web \
  --shared-namespace MyApp.Shared \
  --dto-namespace MyApp.Shared.DTOs \
  --schema supplier.dabbler-entity.json
```

This generates:
- `Components/Pages/Suppliers/` — Blazor pages and helper components
- `Data/SupplierApiClient.cs` — typed HTTP client
- `Data/SupplierGridDataProvider.cs` — grid data binding
- `Data/SupplierScaffoldingExtensions.cs` — convention-correct typed-client + DI registration
- `Components/Pages/Suppliers/Apply-SupplierSchema.ps1` — post-process script to fill in per-property details

### 5. Post-Process (Page Template Only)

Run the schema-application script to fill in per-property grid columns, form fields, and filter configuration:

```powershell
# From the web project root:
.\Components\Pages\Suppliers\Apply-SupplierSchema.ps1 -schema supplier.dabbler-entity.json
```

### 6. Wire Up Integration Points

The generated code requires four hand-edits to integrate into your application:

1. **DbSet registration** (in `AppDbContext.cs`):
   ```csharp
   public DbSet<Supplier> Suppliers { get; set; }
   ```

2. **AutoMapper profile** (add mapping if not already present):
   ```csharp
   CreateMap<Supplier, SupplierDto>().ReverseMap();
   ```

3. **DI registration** (in `Program.cs`):
   ```csharp
   // Service tier (from the API project)
  builder.Services.AddSupplierApiServices();

   // Web tier (from the Web project)
   builder.Services.AddSupplierScaffolding(builder.Configuration);
   ```

4. **Navigation menu** (in your main layout or nav component):
   ```razor
   <NavLink href="/suppliers">Suppliers</NavLink>
   ```

### 7. Build and Test

```bash
dotnet build
dotnet test
```

Then open your app in a browser and navigate to `/suppliers` to verify the CRUD pages are working.

## Templates

This package includes two templates:

- **[dabbler-crud-service](crud-service-template.md)** — generates the service tier (interface, implementation, controller)
- **[dabbler-crud-page](crud-page-template.md)** — generates the web tier (pages, API client, data provider)

## Customization

Both templates are designed to be customized after generation. See the template-specific documentation for common customization patterns:

- Adding non-CRUD endpoints to the controller
- Custom validation in the service
- Custom column rendering or field validation in the page
- Alternate authorization policy names

## Frequently Asked Questions

**Q: Can I use a non-int primary key?**  
A: Yes. Pass `--pk-type long` (or `Guid`, `System.Guid`, etc.) when invoking both templates. The schema file's `primaryKey.type` and the template's `--pk-type` must match.

**Q: Do I have to provide a schema file for the service template?**  
A: No, the service template accepts the `--schema` parameter for symmetry but does not consume it. Only the page template requires it.

**Q: What if I don't provide a schema file for the page template?**  
A: The `--schema` parameter is required for the `dotnet new` invocation (the template won't generate without it). However, you can provide a minimal schema file with just the primary key, and the page template will emit a compilable fallback with an Id-only grid/form. You can then expand the schema file and re-run the `Apply-{Entity}Schema.ps1` post-process script to add more properties later. This is useful for rapid prototyping.

**Q: Can I re-generate the templates over existing files?**  
A: No. The templates are designed for a single-run "generate once, own the code" approach. Once generated, the files are yours to customize. There is no `--regenerate` flag.

**Q: What if I add a new property to my entity?**  
A: Update the `.dabbler-entity.json` schema file and re-run the `Apply-{Entity}Schema.ps1` script (which is idempotent and uses BEGIN/END placeholder markers). This updates grid columns, form fields, and filter configuration without touching other customizations.

**Q: Can I use OData instead of the QBE filter mechanism?**  
A: Not with these templates. The current templates use the platform's Dabbler QBE → LINQ expression engine. An OData template suite is a planned future enhancement (see [non-mvp-roadmap.md](non-mvp-roadmap.md)).

## See Also

- [CRUD Service Template Documentation](crud-service-template.md)
- [CRUD Page Template Documentation](crud-page-template.md)
- [Non-MVP Roadmap](non-mvp-roadmap.md)
- [Dabbler Platform Documentation](../README.md)
