# Registration Block Templates

The five `.template` files in this directory are string-interpolation templates
consumed by `RegistrationReferenceEmitter` to render the four labelled blocks
inside `{Entity}Registrations.g.cs`. Placeholders use `{{Name}}` syntax (Mustache-
style) and are substituted at render time. The substitution engine is intentionally
trivial — these are not Scriban; they are plain templated text.

Placeholders:

| Placeholder       | Source field                  |
|-------------------|-------------------------------|
| `{{Entity}}`      | Singular entity name (PascalCase) |
| `{{EntityPlural}}`| Plural entity name (PascalCase)  |
| `{{PathPlural}}`  | URL path segment (lowercase plural) |
| `{{FieldName}}`   | Property name (from `.dabbler-entity.json`) |
| `{{FieldLabel}}`  | Display label (camelCase split into words) |
| `{{FieldRows}}`   | Concatenated `EntityFieldRow.template` outputs |

Files:

- `ApiEntityBlock.template` — entity registration (paste inside `options.RegisterEntities(...)`).
  Matches Api/Program.cs:94-103 shape.
- `EntityFieldRow.template` — single `fields.Add(...)` line. Repeated once per
  non-PK property to populate `{{FieldRows}}` inside the entity block.
- `ApiEndpointBlock.template` — API-side endpoint registrations (paste inside
  `options.RegisterEndpoints(...)` in `Api/Program.cs`). Matches Api/Program.cs:135-139
  shape — singular `Query{Entity}` / `Get{Entity}` identifiers.
- `WebPageBlock.template` — page registration (paste inside `options.RegisterPages(...)`).
  Matches the Supplier line in Web/Program.cs (`/suppliers` → "Suppliers" → "Supplier management").
- `WebEndpointBlock.template` — Web-side endpoint registrations (paste inside
  `options.RegisterEndpoints(...)` in `Web/Program.cs`). Matches Web/Program.cs:189-194
  shape — pre-existing identifier drift vs. the Api side: `Query{EntityPlural}` /
  `Get{Entity}ById`. Display labels + URL templates are identical to the Api block;
  only the first two registration identifiers differ.

The files are loaded as embedded resources by `RegistrationReferenceEmitter`
(see the `<EmbeddedResource>` glob in `Dabbler.CrudSlice.csproj`). Editing
any `.template` file changes the rendered output the next time
`dotnet new dabbler-crud-slice` runs.
