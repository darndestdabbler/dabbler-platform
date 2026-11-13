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
`GreaterThanOrEqual`, `LessThanOrEqual`, `IsEmpty`, `IsNotEmpty`, `In`.
Operator strings match the constants in `Dabbler.Components.Data.FilterOperators`
(case-sensitive), so no translation layer is needed between the UI filter panel
and the server-side extension method.

- `Contains`, `StartsWith` and `EndsWith` apply to `string` properties.
- `GreaterThan`, `LessThan`, `GreaterThanOrEqual` and `LessThanOrEqual` apply
  to types with an ordering: the numeric types, `DateOnly`, `TimeOnly`,
  `DateTime`, `DateTimeOffset`. They need a value.
- `IsEmpty` and `IsNotEmpty` apply to a property that can hold `null`; for a
  `string`, the empty string counts as empty.
- `Equals` with a `null` value applies to a property that can hold `null`.
- `In` applies when the value is an array (a CLR array, or a JSON array from a
  POST body): the property must equal one of its elements.

## Values

A filter value is converted to the property's type. It may already be of that
type, or be a `string` or a `JsonElement` (what System.Text.Json gives an
`object?` property of a POST body). Conversion uses the invariant culture:

- the numeric types and `bool`, from a number or its invariant text (`25.50`);
- `DateOnly`, `TimeOnly`, `DateTime` and `DateTimeOffset`, from ISO 8601 text
  (`2025-03-01`, `07:30:00`, `2025-03-01T09:00:00`, `2025-03-01T12:00:00+02:00`);
  a `DateOnly` also takes a date-time, and a `DateTimeOffset` without an offset
  is taken as UTC;
- `Guid`, from its text;
- an enum, from a member's name (case-insensitive) or number.

## Errors

Nothing is dropped. `ApplyGridCompositeFilter` and `ApplyGridQuerySorts` throw
`GridQueryException`, naming the property and the operator in its `Message`,
`PropertyName` and `Operator` (`null` for a sort), when:

- a filter or a sort names a property the type does not have;
- a filter names an operator that is not in the list above;
- the operator does not apply to the property's type;
- the value cannot be converted to the property's type.

`GridQueryException` derives from `ArgumentException`: it is the request that
is wrong, and an API answers it with `400 Bad Request` and a problem-details
body.

## If this library stops being maintained

This package is MIT-licensed, and its source is in
[`src/Libs/Dabbler.Api.Querying`](https://github.com/darndestdabbler/dabbler-platform/tree/master/src/Libs/Dabbler.Api.Querying).
If it stops being maintained, copy that source into your application,
replace the package reference with a project reference to your copy, and
own it from there. Keep the MIT license notice with the copied code.
