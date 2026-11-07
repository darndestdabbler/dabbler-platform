# QBE Helper API — `crud-scaffolding` Session 1

> **Status:** Decided and implemented 2026-05-16 (Session 1 of 5); placement amended 2026-05-16 (Session 2 of 5).
> **Consumed by:** Sessions 2 (service template) and 5 (reference scaffold).
>
> **Session 2 amendment:** the helper was promoted into the new packable library
> `Dabbler.Api.Querying` (alongside the matching transport DTOs from `request-contract-decision.md`)
> so the scaffolded `dabbler-crud-service` template can call into it as a NuGet package rather
> than via a per-consumer-app code copy. No API changes; the type was relocated and the namespace
> rebranded from `AcmeCoffeeOnline.Api.Infrastructure` to `Dabbler.Api.Querying`.

---

## Location

**File:** `src/Libs/Dabbler.Api.Querying/QueryableExtensions.cs`
**Namespace:** `Dabbler.Api.Querying`
**Class:** `QueryableExtensions` (public static)

---

## Pre-existing vs New

| Question | Answer |
|---|---|
| Did `query.ApplyQbeFilter(...)` already exist? | No. Dabbler.Components has `LocalDataProvider.EvaluateComposite` (in-memory list evaluation only). No server-side `IQueryable<T>` extension existed. |
| Was an extension method added this session? | Yes. Two new extension methods in `QueryableExtensions.cs`. |

---

## Methods

### `ApplyGridCompositeFilter<T>`

```csharp
public static IQueryable<T> ApplyGridCompositeFilter<T>(
    this IQueryable<T> query,
    GridCompositeFilter? filter)
```

**Purpose:** Translates a `GridCompositeFilter` graph into a LINQ `Where` predicate that EF Core's query translator converts to SQL.

**Returns:** The original query unchanged when `filter` is null or produces no leaf conditions.

**Generated service usage:**

```csharp
IQueryable<{Entity}> query = _db.{Entities}.AsNoTracking();
query = query.ApplyGridCompositeFilter(request.Filter);
query = query.ApplyGridQuerySorts(request.Sorts);
int total = await query.CountAsync(ct);
var items = await query.Skip(request.PageIndex * request.PageSize)
                       .Take(request.PageSize)
                       .ToListAsync(ct);
```

### `ApplyGridQuerySorts<T>`

```csharp
public static IQueryable<T> ApplyGridQuerySorts<T>(
    this IQueryable<T> query,
    IReadOnlyList<GridQuerySort> sorts)
```

**Purpose:** Applies a multi-column sort using `OrderBy` for the first column and `ThenBy` for subsequent columns. This avoids the "repeated OrderBy resets prior ordering" defect called out in the spike's tri-provider review (adjustment S1).

**Returns:** The original query when `sorts` is empty.

---

## Supported operators

| Operator string | SQL equivalent | Notes |
|---|---|---|
| `"Equals"` | `= value` | Works on all comparable types; null filter value → `IS NULL` |
| `"Contains"` | `LIKE '%value%'` | String only; skipped on non-string properties |
| `"StartsWith"` | `LIKE 'value%'` | String only |
| `"EndsWith"` | `LIKE '%value'` | String only |
| `"GreaterThan"` | `> value` | Numeric, date, comparable types |
| `"LessThan"` | `< value` | |
| `"GreaterThanOrEqual"` | `>= value` | |
| `"LessThanOrEqual"` | `<= value` | |
| `"IsEmpty"` | `IS NULL OR = ''` | String: null or empty. Non-string: null only |
| `"IsNotEmpty"` | `IS NOT NULL AND != ''` | |
| `"In"` | n/a | Deferred — filter skipped silently |
| Unknown | n/a | Skipped silently |

Operator strings match the constants in `Dabbler.Components.Data.FilterOperators` so no translation layer is needed between the UI filter panel and the server-side extension method.

---

## Behaviour on errors

Any invalid property name or type conversion failure is caught silently per leaf filter — the leaf is skipped and the rest of the query proceeds. This mirrors `LocalDataProvider`'s defensive behaviour.

---

## Unit test location

`tests/AcmeCoffeeOnline.Api.Tests/QueryableExtensionsTests.cs` — pure in-memory LINQ tests (no database required). Covers: null filter passthrough, Contains, Equals on decimal, GreaterThan, IsEmpty on null string, OR logic, single sort asc, multi-column sort (OrderBy + ThenBy), and invalid property name silent skip.
