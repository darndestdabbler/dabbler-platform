# Request Contract Decision — `crud-scaffolding` Session 1

> **Status:** Decided 2026-05-16 (Session 1 of 5)
> **Consumed by:** Sessions 2 (service template), 3 (page template), 5 (reference scaffold)

---

## Decision 1: Unified Request DTO

**Chosen:** New `GridQueryRequest` type in `AcmeCoffeeOnline.Shared.DTOs` (with supporting types).

### Rationale

The existing `PagedRequest` in `AcmeCoffeeOnline.Shared.DTOs` is anemic for QBE queries: it supports only a single `SortBy` string and a free-text `SearchTerm`. It cannot represent the nested AND/OR filter graphs that `DeclarativeFilterPanel` emits via `CompositeFilterDescriptor`. Extending `PagedRequest` in-place risks backward compatibility with the existing Products, Customers, and Orders endpoints.

`GridDataRequest` (the UI contract from `Dabbler.Components.Data`) cannot be referenced from the API project — `Dabbler.Components` is a `Sdk.Razor` project with a MudBlazor dependency, which is inappropriate for a REST API.

The cleanest MVP solution: add a thin set of transport types to `AcmeCoffeeOnline.Shared.DTOs` (referenced by both Web and Api) with distinct names to avoid collision with `Dabbler.Components.Data` types in files that use both.

### Types added

| Type | Description |
|---|---|
| `GridQueryRequest` | Root transport DTO: PageIndex, PageSize, Sorts, Filter |
| `GridQuerySort` | Record(PropertyName, Descending) |
| `GridQueryFilter` | Record(PropertyName, Operator string, Value?) — operator strings match `FilterOperators` constants |
| `GridCompositeFilter` | Recursive: Logic (And/Or), Filters[], Children[] |
| `GridFilterLogic` | Enum: And, Or |

**File:** `src/Apps/AcmeCoffeeOnline.Shared/DTOs/GridQueryRequest.cs`

### Mapping in the generated `GridDataProvider`

The generated `{Entity}GridDataProvider.cs` (Session 3) bridges the two type systems:
- Input: `Dabbler.Components.Data.GridDataRequest` from `IGridDataProvider<TDto>.FetchAsync`
- Output: posts `AcmeCoffeeOnline.Shared.DTOs.GridQueryRequest` to the API via typed `HttpClient`

Mapping is mechanical: `Sorts` → `GridQuerySort[]` (1:1), `CompositeFilter` → `GridCompositeFilter` (recursive structural copy). `Groups` are not transported (server-side grouping is not in MVP scope).

### Response DTO

No new type. Reuse the existing `PagedResponse<T>` from `AcmeCoffeeOnline.Shared.DTOs`.

---

## Decision 2: HTTP Transport for `GridCompositeFilter`

**Chosen:** `POST /api/{entity}s/query` with `[FromBody] GridQueryRequest`

### Rationale

`GridCompositeFilter` is a recursive object graph (nested Children list). URL query strings cannot represent nested/recursive structures reliably — all common serialization approaches (JSON-encoded query param, multi-value params for nested objects) break with deep nesting, special characters, and max URL length constraints.

POST with a JSON body is the standard industry pattern for complex query documents ("search APIs"). It is stateless, cacheable at the application layer, and avoids all query-string limitations.

### Controller shape (generated)

```csharp
[HttpPost("query")]
[DabblerEndpointAuthorize("{EntityPlural}Query")]
public async Task<IActionResult> QueryAsync([FromBody] GridQueryRequest request)
{
    var (userId, _) = TryGetUserId();
    var response = await _service.GetAsync(request, HttpContext.RequestAborted);
    // apply field permissions per item
    return Ok(response);
}
```

### Coexistence with existing endpoints

Existing `ProductsController`, `CustomersController`, and `OrdersController` retain their `GET /api/{entity}s [FromQuery] PagedRequest` endpoints unchanged. Scaffolded controllers add a new `POST /api/{entity}s/query` endpoint alongside standard CRUD. No migration of existing endpoints is required.

---

## Decision 3: QBE → LINQ Helper Placement

**Chosen:** `AcmeCoffeeOnline.Api.Infrastructure.QueryableExtensions` (public static class)

### Rationale

The expression-tree builder translates `GridCompositeFilter` to `IQueryable<T>` predicates using BCL `System.Linq.Expressions`. The EF Core LINQ provider translates those expressions to SQL. The builder needs no EF Core-specific API — only `System.Linq.Expressions` — but lives in the API project because that is where all generated services (consumers of the helper) reside.

See `qbe-helper-api.md` for the exact method signatures.
