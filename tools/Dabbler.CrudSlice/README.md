# Dabbler.CrudSlice

Repo-aware wrapper command for the dabbler-crud-service + dabbler-crud-page
templates plus the cross-platform `Dabbler.SchemaApplicator`. Single CLI entry
point that produces a drop-in CRUD slice whose generated `{Entity}ScaffoldingExtensions.cs`
matches the host's real production wiring (correct config keys, `X-Internal-Api-Key`
header, `UserIdentityHandler`).

It exists because the dabbler-crud-page template alone emits a scaffolding
extension that:

- Reads `Api:BaseUrl` instead of `Services:ApiBaseUrl` (drift vs. real wiring).
- Omits the `X-Internal-Api-Key` header.
- Omits `UserIdentityHandler` for per-request user identity forwarding.

`Dabbler.CrudSlice` overwrites the template's emitted scaffolding extension with
a convention-correct one, and additionally emits an API-side
`{Entity}ApiRegistrationExtensions.cs` for the `Add{Entity}ApiServices()`
one-liner.

A third emit is `{Entity}Registrations.g.cs`, a copy-paste reference
file containing four clearly-commented blocks the consumer pastes into the
`AddDabblerSecurity(...)` lambdas in `Api/Program.cs` and `Web/Program.cs` (API
entity + endpoints, Web page + endpoints). Shapes match the host's real Supplier
wiring at `Api/Program.cs:94-103, :135-139` and `Web/Program.cs:155-162, :189-194`,
including the pre-existing identifier drift between Api side
(`Query{Entity}`/`Get{Entity}` — singular) and Web side
(`Query{EntityPlural}`/`Get{Entity}ById` — plural/ById). The file body is wrapped
in `#if false` so it never compiles even if accidentally included in a csproj
compile glob.

Block shapes live as embedded resources under
[`Templates/`](Templates/README.md) and are substituted at render time with
`{{Entity}}` / `{{EntityPlural}}` / `{{PathPlural}}` / `{{FieldRows}}` placeholders.
Edit a `.template` file → next wrapper run picks up the change.

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

For single-project consumers, pass the same directory to both `--web-root`
and `--api-root`.

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
| `--skip-registrations` | off | Skip emitting the `{Entity}Registrations.g.cs` copy-paste reference file. |

## If this library stops being maintained

This tool is MIT-licensed, and its source is in
[`tools/Dabbler.CrudSlice`](https://github.com/darndestdabbler/dabbler-platform/tree/master/tools/Dabbler.CrudSlice)
(with the schema applicator it bundles in `tools/Dabbler.SchemaApplicator`).
If it stops being maintained, copy that source into your own repository,
build the tool from your copy, and own it from there. The code it has
already generated is yours and needs nothing from the tool. Keep the MIT
license notice with the copied code.
