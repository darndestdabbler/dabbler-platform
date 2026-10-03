# Dabbler.Api.Querying

> Part of the Dabbler Platform. Promoted from `AcmeCoffeeOnline.Shared.DTOs`
> + `AcmeCoffeeOnline.Api.Infrastructure` during the
> `crud-scaffolding` session set so scaffolded services can reference these
> primitives as a NuGet package rather than per-consumer copies.

Transport DTOs and `IQueryable<T>` extension methods for server-side grid
queries on a Dabbler-platform API. The DTOs (`GridQueryRequest`,
`GridQuerySort`, `GridQueryFilter`, `GridCompositeFilter`, `GridFilterLogic`)
flow from a UI grid + filter panel through an API client into a controller
`POST /api/{entity}s/query` endpoint; the extension methods
(`ApplyGridCompositeFilter`, `ApplyGridQuerySorts`) translate them into
LINQ expression trees that EF Core converts to SQL.

## Audience

Teams adopting the Dabbler `dotnet new dabbler-crud-service` template, and
hand-authored controllers that need the same query contract.

## Prerequisites

- .NET 10
- `Microsoft.EntityFrameworkCore` 10.x (the extensions return
  `IQueryable<T>` shapes EF Core can translate to SQL).

## Operators

`Equals`, `Contains`, `StartsWith`, `EndsWith`, `GreaterThan`, `LessThan`,
`GreaterThanOrEqual`, `LessThanOrEqual`, `IsEmpty`, `IsNotEmpty`. `In` is
deferred (silently skipped); unknown operators are silently skipped.
Operator strings match the constants in `Dabbler.Components.Data.FilterOperators`
so no translation layer is needed between the UI filter panel and the
server-side extension method.
