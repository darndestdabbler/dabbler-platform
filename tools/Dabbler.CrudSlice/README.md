# Dabbler.CrudSlice

Repo-aware wrapper command for the dabbler-crud-service + dabbler-crud-page
templates plus the cross-platform `Dabbler.SchemaApplicator`. Single CLI entry
point that produces a drop-in CRUD slice whose generated `{Entity}ScaffoldingExtensions.cs`
matches the host's real production wiring (correct config keys, `X-Internal-Api-Key`
header, `UserIdentityHandler`).

This tool is part of the `crud-scaffolding-remediation` session set's Session 2
deliverable. It exists because the dabbler-crud-page template alone emits a
scaffolding extension that:

- Reads `Api:BaseUrl` instead of `Services:ApiBaseUrl` (drift vs. real wiring).
- Omits the `X-Internal-Api-Key` header.
- Omits `UserIdentityHandler` for per-request user identity forwarding.

`Dabbler.CrudSlice` overwrites the template's emitted scaffolding extension with
a convention-correct one, and additionally emits an API-side
`{Entity}ApiRegistrationExtensions.cs` for the `Add{Entity}ApiServices()`
one-liner.

## Usage

```bash
dotnet run --project tools/Dabbler.CrudSlice -- \
  --entity Supplier \
  --entity-plural Suppliers \
  --schema src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Suppliers/supplier.dabbler-entity.json \
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

After running, wire the generated extensions into your `Program.cs`:

```csharp
// Web/Program.cs
builder.Services.AddSupplierScaffolding(builder.Configuration);

// Api/Program.cs
builder.Services.AddSupplierApiServices();
```

The wrapper does NOT edit `Program.cs`, `DbContext`, or navigation menus.
Registration code is emitted in dedicated helper files; the consumer pastes
the one-liner calls into their `Program.cs`. (Granularity 3 auto-wiring is
out of scope — see `docs/scaffolding-templates/non-mvp-roadmap.md`.)

## Flags

| Flag | Default | Purpose |
|------|---------|---------|
| `--entity` | (required) | Singular entity name in PascalCase. |
| `--entity-plural` | (required) | Plural in PascalCase. |
| `--schema` | (required) | Path to the `.dabbler-entity.json` schema file. |
| `--web-root` | (required) | Path to the Web project root. |
| `--api-root` | (required) | Path to the API project root. |
| `--web-namespace` | (required) | Web project root namespace. |
| `--api-namespace` | (required) | API project root namespace. |
| `--shared-namespace` | (required) | Shared project root namespace. |
| `--dto-namespace` | (required) | Namespace containing `{Entity}Dto`. |
| `--entity-namespace` | (required) | Namespace of the EF entity class. |
| `--dbcontext-type` | (required) | DbContext class name. |
| `--dbcontext-namespace` | (required) | DbContext namespace. |
| `--pk-type` | `int` | Primary-key CLR type. |
| `--auth-policy-prefix` | (entity) | Override the auth-policy prefix. |
| `--api-base-url-config-key` | `Services:ApiBaseUrl` | Config key for the API base URL. |
| `--api-base-url-fallback-config-key` | `ApiBaseUrl` | Fallback config key. |
| `--api-base-url-default` | `http://localhost:5011` | Hard default. |
| `--internal-api-key-config-key` | `DabblerSecurity:InternalApiKey` | Internal API key config key. |
| `--internal-api-key-default` | `dev-internal-key-acme` | Hard default. |
| `--template-hive` | (none) | Forwarded as `--debug:custom-hive` to `dotnet new`. |
| `--skip-templates` | off | Skip the `dotnet new` invocations (emit registrations only). |
| `--skip-schema-applicator` | off | Skip the schema-applicator pass. |
