# Verification Round 1

ISSUES FOUND

### 1. Critical — generated page slice is not actually a runnable CRUD UI
**Where**
- `Components/Pages/T2EntityPlural/T2EntityPluralPage.razor`
- `Components/Pages/T2EntityPlural/T1EntityDetailPage.razor`
- `Components/Pages/T2EntityPlural/T1EntityDetail.razor`

**Problem**
- The list page emits a **New** button to `/T9RouteName/new`.
- The only generated detail route is `@page "/T9RouteName/{T1EntityIdT8PkRoute}"`.
- There is **no generated `/new` route wrapper**.
- The detail component exposes `OnDeleted` but never renders a delete action or calls `ApiClient.DeleteAsync`.

So the generated UI slice is not self-sufficient CRUD:
- **Create** navigation is broken at runtime.
- **Delete** is not available in the generated page flow.

**Why this matters**
This fails MVP gate **B1** (“self-sufficient generated slice”) and the session purpose (“runnable CRUD slice”). The smoke test only proves compilation, not an actually usable CRUD page flow.

**Recommended fix**
Generate either:
- a dedicated `.../new` page wrapper that hosts `<T1EntityDetail IsNew="true" />`, or
- an optional route pattern that handles both new/edit,

and add a delete action in `T1EntityDetail.razor` wired to `ApiClient.DeleteAsync`.

---

### 2. Major — `--schema` on `dabbler-crud-page` is effectively a no-op and is not required
**Where**
- `templates/Dabbler.Templates/content/dabbler-crud-page/.template.config/template.json`
- `Components/Pages/T2EntityPlural/Apply-T1EntitySchema.ps1`

**Problem**
- Session 3 spec says `--schema` is **required** for the page template.
- The template marks `schema` as `"isRequired": false`.
- The `schema` symbol is not used for any replacement/output.
- The emitted PowerShell script does **not** inherit the `--schema` value passed to `dotnet new`; it falls back to `T1Entity.dabbler-entity.json` naming or requires the user to pass `-SchemaPath` again manually.

So:
- `dotnet new dabbler-crud-page --schema some/custom/path.json` does not actually use that path.
- If the schema file is not named `<Entity>.dabbler-entity.json`, the generated script will not find it unless the user repeats the path later.

**Why this matters**
This is a direct spec miss against Session 3 step 2 and MVP gate **B4**. The schema is supposed to be the primary per-property input; here the CLI accepts it but does not consume it.

**Recommended fix**
At minimum:
- make `schema` required in `template.json`, and
- stamp the provided path into the generated script or a generated config file so the post-process uses the same path automatically.

---

### 3. Major — schema post-process does not correctly handle some documented supported types
**Where**
- `Components/Pages/T2EntityPlural/Apply-T1EntitySchema.ps1`

**Problem**
The script claims support for:
- `DateTime`
- `DateOnly`
- `Guid`
- `DateTimeOffset` via classification regex

But emitted form code is not valid/safe for several of those:

#### Date-like types
`DateOnly` / `DateTimeOffset` are classified as `Date`, but generation always emits:
```razor
<MudDatePicker Date="@_record.$name"
               DateChanged="@((DateTime? v) => { _record.$name = v ?? default; _session?.NotifyChanged(); })" />
```
That is not compatible with `DateOnly` or `DateTimeOffset` DTO properties.

Also, for nullable `DateTime?`, clearing the picker becomes `default` instead of `null`.

#### Guid
`Guid` falls into the fallback path:
```razor
Value="@_record.$name?.ToString()"
ValueChanged="@((string v) => { /* hand-edit: parse Guid from v */ _session?.NotifyChanged(); })"
```
Issues:
- `?.` is invalid on non-nullable `Guid`.
- The generated field is explicitly non-functional because it never parses back into the DTO.

**Why this matters**
This is a real correctness defect in the schema-driven post-process. A schema using these “supported” types can produce broken or non-compiling output.

**Recommended fix**
Either:
- generate type-specific code for `DateTime`, `DateOnly`, `DateTimeOffset`, `Guid`, etc., or
- reject unsupported types with a clear error instead of claiming support.

---

### 4. Major — smoke test does not verify the DI-resolution requirement from the spec
**Where**
- `tests/Dabbler.Templates.Tests/CrudPageTemplateTests.cs`
- `tests/Dabbler.Templates.Tests/Fixtures/SmokeApp/Program.cs`

**Problem**
The spec says Session 3 should assert compile **and** that `Add{Entity}Scaffolding` resolves needed services from DI.

Current test only:
- checks generated files exist,
- inspects source text of `SmokeScaffoldingExtensions.cs`,
- restores/builds the scaffolded project.

It does **not**:
- call `AddSmokeScaffolding(...)`,
- build a service provider,
- resolve `SmokeApiClient` and `IGridDataProvider<SmokeDto>`.

`SmokeApp/Program.cs` also never calls the generated extension.

**Why this matters**
The session “Ends with” condition is not fully proven. The current test would not catch DI registration breakage.

**Recommended fix**
Extend the smoke to either:
- modify the fixture `Program.cs` to call `AddSmokeScaffolding(builder.Configuration)` and compile that path, or
- add a small runtime verification in the generated project/test harness that builds a `ServiceCollection`, calls the extension, and resolves both services.

---

### Minor notes
- `isSortable` from the schema is not applied when generating grid columns.
- `isReadOnly` is not honored for generated `MudCheckBox` fields.

---

Everything else I checked is broadly aligned:
- unified request/response contract usage looks correct,
- no `@bind-Value` in generated form fields,
- no `--filter-mode`,
- the page-side API client / grid-provider bridge is structurally correct,
- the Session 2 QBE wiring remains intact.

But due to the issues above, this is **not VERIFIED** yet.

---

# Verification Round 2

ISSUES FOUND

### 1. Major — `DateTime` scaffolding is still not compile-safe for common non-nullable DTO properties
**Where**
- `templates/Dabbler.Templates/content/dabbler-crud-page/Components/Pages/T2EntityPlural/Apply-T1EntitySchema.ps1`

**Problem**
The round-2 fix correctly drops `DateOnly`, `DateTimeOffset`, and `Guid` into the `Unknown` fallback, but the generated `Date` path now assumes the DTO property is nullable:

```razor
<MudDatePicker Date="@_record.$name"
               DateChanged="@((DateTime? v) => { _record.$name = v; _session?.NotifyChanged(); })" />
```

If the DTO property is a non-nullable `DateTime`, the generated assignment is invalid:

```csharp
_record.SomeDate = v; // DateTime? -> DateTime
```

That means a schema using `"type": "DateTime"` can still produce non-compiling output, and the current schema format does not carry nullability so the script cannot safely distinguish `DateTime` vs `DateTime?`.

**Why this matters**
This is a real regression in compile safety. Round 1’s type issue is not fully closed because one of the still-classified “supported” types can now break the generated slice. The smoke test does not catch it because the fixture schema has no date field.

**Recommended fix**
Pick one of these and make it consistent:

1. **Best:** extend the schema to encode nullability, then emit correct code for `DateTime` vs `DateTime?`.
2. **Safe MVP fallback:** treat `DateTime` as unsupported/`Unknown` until nullability is representable, so the slice still compiles and the developer gets an explicit hand-edit marker.
3. **Less ideal but compile-safe:** emit code that compiles for non-nullable properties too, with clearly documented semantics.

---

Everything else from round 1 looks closed:
- `/new` route wrapper + delete flow: fixed
- `--schema` required and stamped into the script: fixed
- `Guid` / `DateOnly` / `DateTimeOffset` fallback now compiles: fixed
- smoke test now exercises `AddSmokeScaffolding(...)` from `Program.cs`: fixed
- minor `isSortable` / checkbox `ReadOnly`: fixed

So this is **not VERIFIED yet** because of the remaining `DateTime` generation defect.

---

# Verification Round 3

VERIFIED

### 1. DateTime emission is now compile-safe for both `DateTime` and `DateTime?`
In the `Date` switch arm, the emitted code is now:

```razor
<MudDatePicker Date="@_record.$name"
               DateChanged="@((DateTime? v) => { _record.$name = v ?? default; _session?.NotifyChanged(); })" />
```

That closes the round-2 compile defect:

- **If DTO property is `DateTime`**
  - `Date="@_record.$name"` is fine: `DateTime` implicitly converts to `DateTime?`.
  - `_record.$name = v ?? default;` is fine: `v ?? default` is `DateTime`.

- **If DTO property is `DateTime?`**
  - `Date="@_record.$name"` is exact-type compatible.
  - `v ?? default` is `DateTime`, which implicitly converts to `DateTime?` on assignment.

So the previously reported `DateTime? -> DateTime` assignment failure is gone.

### 2. Hand-edit cue is adequate
Yes. The comment in the generator clearly states:

- why this shape was chosen,
- what the semantic trade-off is,
- and the exact hand edit to make for nullable DTOs:

```csharp
_record.$name = v;
```

That is sufficient for MVP scaffolding. A developer maintaining or rerunning the scaffold can understand the issue and fix it locally when null-round-tripping matters.

Small wording nit only: “boxed `MinValue`” is not the most precise description; the important fact is simply “clearing yields `MinValue`, not `null`.” But that is editorial, not a blocker.

### 3. New defect introduced by the fix
No new **compile-safety** defect introduced by this change.

The only remaining caveat is the one you already documented:

- for nullable date properties, clearing the picker will not round-trip `null`;
- it will produce `DateTime.MinValue` semantics instead.

That is an intentional MVP trade-off, not a compile break.

### Operator-decision boundary
- **If the requirement is compile-safe scaffolding under current schema limitations:** this is acceptable and should be considered closed.
- **If the requirement is correct null semantics for nullable dates:** that still requires schema/model nullability awareness and is a product/design enhancement, not a remaining round-2 compile defect.

### Non-blocking nit
Unrelated to this fix, the top-of-file comment still says `DateOnly` and `Guid` are “supported property types,” while the classifier now intentionally routes them to `Unknown`. That’s a documentation mismatch, but not a new defect from this DateTime change.

Within the stated round-3 scope, this is adequately resolved.
