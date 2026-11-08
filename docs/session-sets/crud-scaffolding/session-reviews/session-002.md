# Verification Round 1

{
  "verdict": "ISSUES_FOUND",
  "issues": [
    {
      "severity": "Critical",
      "category": "correctness",
      "title": "Generated CRUD service is not self-sufficient; create/update mapping is effectively a stub",
      "description": "The generated service compiles, but it does not perform correct CRUD for any non-Id fields. `MapToDto` only copies `Id`, while `MapToEntity` and `CopyTo` are empty placeholders. For the smoke entity (`Id`, `Name`, `IsActive`), `CreateAsync` will persist default values instead of the incoming DTO values, `UpdateAsync` will not update fields, and responses omit mapped properties. That fails MVP gate B1 ('Self-sufficient generated slice') even though the compile-only smoke test passes.",
      "location": "templates/Dabbler.Templates/content/dabbler-crud-service/Services/T1EntityService.cs",
      "suggested_fix": "Replace the placeholder mapping seams with a generic working implementation. At minimum, copy same-named readable/writable properties between DTO and entity via reflection or EF `CurrentValues.SetValues`, while excluding server-controlled fields as needed. If you keep overridable seams, make the default bodies fully functional so a fresh scaffold works without manual edits."
    },
    {
      "severity": "Major",
      "category": "spec-compliance",
      "title": "Template CLI contract diverges from the Session 2 spec, and `auth-policy-prefix` is not implemented",
      "description": "The spec requires parameters `entity`, `api-namespace`, `dto-namespace`, `shared-namespace`, `pk-type`, `auth-policy-prefix`, and `schema`. The template instead exposes camelCase/non-spec names (`entityPlural`, `apiNamespace`, `dtoNamespace`, `sharedNamespace`, `dbContextType`, `dbContextNamespace`, `pkRouteConstraint`) and does not declare or honor `auth-policy-prefix` at all. Generated `[DabblerEndpointAuthorize]` values are hard-coded from entity/entityPlural tokens, so consumers cannot align policy names with existing authorization conventions. This is a real contract break for Session 3/5 and for any automation/docs that invoke the spec-defined CLI shape.",
      "location": "templates/Dabbler.Templates/content/dabbler-crud-service/.template.config/template.json; templates/Dabbler.Templates/content/dabbler-crud-service/Controllers/T2EntityPluralController.cs; tests/Dabbler.Templates.Tests/CrudServiceTemplateTests.cs",
      "suggested_fix": "Make the template accept the spec-defined parameter names/aliases, implement `auth-policy-prefix`, and derive policy strings from that symbol. If extra parameters must exist, make them optional/derived rather than required contract surface, or explicitly update the governing spec and downstream docs/tests to match."
    },
    {
      "severity": "Major",
      "category": "documentation",
      "title": "Session contract docs are stale and now contradict the implemented request/response/querying contract",
      "description": "The amended docs still contain old guidance from before the promotion. `request-contract-decision.md` says 'Response DTO: No new type. Reuse the existing `PagedResponse<T>`', but the implementation introduced and uses `Dabbler.Api.Querying.GridQueryResponse<T>`. The same doc also still says the generated GridDataProvider posts `AcmeCoffeeOnline.Shared.DTOs.GridQueryRequest`, despite the promotion to `Dabbler.Api.Querying`. These inconsistencies matter because Sessions 3 and 5 are supposed to build on the Session 1/2 contract docs.",
      "location": "docs/session-sets/crud-scaffolding/request-contract-decision.md",
      "suggested_fix": "Update the decision doc to reflect the actual contract: request and response types live in `Dabbler.Api.Querying`, the response shape is `GridQueryResponse<T>`, and downstream templates/consumers should target that package and namespace."
    },
    {
      "severity": "Minor",
      "category": "test-coverage",
      "title": "Smoke test does not verify the spec-defined CLI shape or any CRUD behavior",
      "description": "The smoke test proves only that the scaffold compiles when invoked with the non-spec camelCase switches. It does not test the spec-defined flags, `auth-policy-prefix`, or any runtime CRUD behavior, which is why the contract and mapping defects above were able to pass green. The current test is useful but insufficient as the sole gate for Session 2.",
      "location": "tests/Dabbler.Templates.Tests/CrudServiceTemplateTests.cs",
      "suggested_fix": "Add at least one generation test that uses the spec-defined parameter names/aliases, and add a minimal behavioral test for generated create/update/query paths against the Smoke fixture to confirm the scaffold is functionally usable, not just compilable."
    }
  ]
}

---

# Verification Round 2

{
  "verdict": "ISSUES_FOUND",
  "issues": [
    {
      "severity": "Major",
      "category": "spec-compliance",
      "title": "Template still exposes non-spec camelCase CLI options",
      "description": "The Session 2 spec names the public parameters as kebab-case (`--api-namespace`, `--dto-namespace`, `--shared-namespace`, `--pk-type`, `--auth-policy-prefix`). The template still declares `apiNamespace`, `dtoNamespace`, `sharedNamespace`, `pkType`, and `authPolicyPrefix`, and the smoke tests only exercise those camelCase forms. Consumers following the documented spec examples will still get invalid-option failures. The Round 2 note explains the divergence but does not resolve it in the template itself.",
      "location": "templates/Dabbler.Templates/content/dabbler-crud-service/.template.config/template.json; tests/Dabbler.Templates.Tests/CrudServiceTemplateTests.cs",
      "suggested_fix": "Expose the public symbols with the spec-defined kebab-case names, or add kebab-case aliases if supported by the templating engine version in use. Then update the smoke tests to scaffold with the spec-defined CLI shape."
    },
    {
      "severity": "Major",
      "category": "correctness",
      "title": "Non-int primary keys still generate incorrect route constraints by default",
      "description": "The spec defines `pk-type` as the caller-controlled input, but the template requires a second manual `pkRouteConstraint` parameter that defaults to `:int`. If a consumer uses a non-int key such as `Guid` or `long` and only sets `pkType`, the generated controller still emits routes like `{id:int}`. That compiles, but the endpoints will not match the actual key type at runtime.",
      "location": "templates/Dabbler.Templates/content/dabbler-crud-service/.template.config/template.json; templates/Dabbler.Templates/content/dabbler-crud-service/Controllers/T2EntityPluralController.cs",
      "suggested_fix": "Derive the route constraint from `pkType` inside the template (for example `int -> :int`, `long -> :long`, `Guid -> :guid`, otherwise empty) and remove or hide `pkRouteConstraint` from the public contract. Add a smoke case for a non-int PK."
    },
    {
      "severity": "Major",
      "category": "correctness",
      "title": "Default create/update mapping copies the primary key from the DTO",
      "description": "`MapToEntity` copies every same-named writable property from the DTO, including `Id`, and `CopyTo` uses `CurrentValues.SetValues(dto)`, which also copies matching mapped properties, including keys. For the common DTO shape that includes `Id`, POST allows explicit key injection and PUT can attempt to change the tracked entity key when the body `Id` differs from the route `id`. In EF Core this can yield incorrect behavior or a runtime exception. The happy-path smoke entity works when IDs match, but the default scaffolded CRUD behavior is still unsafe.",
      "location": "templates/Dabbler.Templates/content/dabbler-crud-service/Services/T1EntityService.cs",
      "suggested_fix": "Exclude the PK property from the default copy in both `MapToEntity` and `CopyTo`, and optionally validate that any DTO `Id` matches the route `id` on update. If generated keys are the norm, do not copy `Id` on create."
    },
    {
      "severity": "Minor",
      "category": "documentation",
      "title": "Request-contract doc still shows a stale generated controller example",
      "description": "`request-contract-decision.md` was updated for `GridQueryResponse<T>`, but the 'Controller shape (generated)' snippet still shows `[DabblerEndpointAuthorize(\"{EntityPlural}Query\")]` and `_service.GetAsync(...)`. The current generator uses auth-policy-prefix-derived names such as `QuerySupplier` or `QueryBackofficeSupplier`, and the service method is `QueryAsync`. The example now contradicts the implemented scaffold.",
      "location": "docs/session-sets/crud-scaffolding/request-contract-decision.md",
      "suggested_fix": "Refresh the controller example so it matches the current scaffold: `QueryAsync`, `GridQueryResponse<T>`, and policy strings derived from `auth-policy-prefix`."
    }
  ]
}

---

# Verification Round 3

{
  "verdict": "ISSUES_FOUND",
  "issues": [
    {
      "severity": "Minor",
      "category": "documentation",
      "title": "Request-contract doc still uses camelCase auth-policy flag in prose",
      "description": "The Round 3 documentation refresh fixed the controller snippet, but the explanatory paragraph below it still says the prefix comes from `--authPolicyPrefix` and shows a camelCase example invocation. The template was moved to spec-compliant kebab-case CLI names via `dotnetcli.host.json`, so this prose is now stale and can mislead consumers.",
      "location": "docs/session-sets/crud-scaffolding/request-contract-decision.md",
      "suggested_fix": "Update the paragraph and example invocation to use `--auth-policy-prefix`, e.g. `--entity Supplier --auth-policy-prefix BackofficeSupplier`."
    },
    {
      "severity": "Minor",
      "category": "documentation",
      "title": "entityPlural parameter help text misstates how auth policy names are derived",
      "description": "The `entityPlural` symbol description says it drives policy names like `GetSuppliers`, but the generated controller derives policy names from `authPolicyPrefix` / `effectiveAuthPolicyPrefix`, which default to the singular `entity` value. For `entity=Supplier` and `entityPlural=Suppliers`, the generated policy is `GetSupplier`, not `GetSuppliers`. This makes template help text inaccurate.",
      "location": "templates/Dabbler.Templates/content/dabbler-crud-service/.template.config/template.json",
      "suggested_fix": "Revise the `entityPlural` description to mention only controller/DbSet naming, or explicitly note that auth policy names come from `auth-policy-prefix` (defaulting to `entity`)."
    }
  ]
}
