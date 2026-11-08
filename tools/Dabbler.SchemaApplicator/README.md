# Dabbler.SchemaApplicator

Cross-platform post-process for the `dabbler-crud-page` scaffolding template.
Replaces the per-entity `Apply-{Entity}Schema.ps1` PowerShell script with a
single .NET 10 console app that runs on Windows, Linux, and macOS without a
PowerShell dependency.

## Usage

```bash
dotnet run --project tools/Dabbler.SchemaApplicator -- \
  --entity Supplier \
  --directory src/Apps/MyApp.Web/Components/Pages/Suppliers
```

Arguments:

| Flag | Required | Default | Description |
|---|---|---|---|
| `--entity` | yes | — | Singular PascalCase entity name (`Supplier`). Drives the `{Entity}List.razor` and `{Entity}Detail.razor` filenames the tool reads/writes, and the `{Entity}Dto` typename referenced in emitted `FilterProperty` entries. |
| `--directory` | yes | — | Folder containing `{Entity}List.razor` and `{Entity}Detail.razor`. |
| `--schema` | no | derived | Path to the `.dabbler-entity.json` schema. Defaults to `{directory}/{entity-lower}.dabbler-entity.json`, then `{directory}/{Entity}.dabbler-entity.json`, then the same names under `--project-root`. |
| `--project-root` | no | derived | Closest ancestor of `--directory` containing a `*.csproj`. Used only when `--schema` is not given. |

## Substitution contract

The tool reads three BEGIN/END regions and replaces each region's body with
content derived from the schema's `properties[]`:

| File | Region marker | Body content |
|---|---|---|
| `{Entity}List.razor` | `@* DABBLER-SCAFFOLD:GRID-COLUMNS:BEGIN *@` … `:END *@` | One `<PropertyColumn>` per `isInGrid: true` property. `Sortable="false"` added when `isSortable: false`. |
| `{Entity}List.razor` | `// DABBLER-SCAFFOLD:FILTER-PROPERTIES:BEGIN` … `:END` | One `FilterProperty<{Entity}Dto>.{Text,Bool,Numeric,Date}` per `isFilterable: true` property. |
| `{Entity}Detail.razor` | `@* DABBLER-SCAFFOLD:FORM-FIELDS:BEGIN *@` … `:END *@` | One MudBlazor field per `isInForm: true` property, excluding the primary key. `MudTextField` for text, `MudCheckBox` for bool, `MudNumericField` for integer/decimal, `MudDatePicker` for `datetime`. |

Supported `type` values (case-insensitive): `string`/`str`/`text`,
`bool`/`boolean`, `int`/`int32`/`short`/`int16`, `long`/`int64`,
`decimal`/`money`, `double`/`float`, `datetime`. Other types fall into the
**Unknown** classification — the tool emits a visible hand-edit placeholder
rather than broken markup, and the slice still compiles.

### Byte-identical-output guarantee

Outside the BEGIN/END marker regions, the input file is preserved verbatim
(encoding, line endings, EOF). Inside each region, the engine emits
`BEGIN_MARKER + "\r\n" + body + "\r\n" + indent + END_MARKER`, where `body`
is the per-property snippets joined with `"\r\n"` and each snippet's own
internal newlines are `"\n"`. This mirrors the PowerShell script's
behaviour exactly; the test suite (`tests/Dabbler.SchemaApplicator.Tests`)
asserts byte-equality against the reference Supplier scaffold under both an
idempotency run (committed file → engine → same file) and a fresh-stamp run
(template file → engine → committed file).

The tool writes UTF-8 without BOM and does not insert or strip trailing
newlines.

## Idempotency

Re-running the tool on the same files produces no diff. This is required
behaviour, not a happy accident — the byte-identity test in
`tests/Dabbler.SchemaApplicator.Tests/SchemaApplicatorTests.cs` is the
regression gate.

## Relationship to `Apply-{Entity}Schema.ps1`

The PowerShell script remains in the template (`templates/Dabbler.Templates/
content/dabbler-crud-page/`) and in closed scaffolds (e.g.,
`src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Suppliers/`) for backwards
compatibility, but it is **deprecated**. New work runs this C# tool;
re-applications produce byte-identical output, so a one-time migration off
PowerShell is a no-diff change.
