namespace AcmeCoffeeOnline.Shared.DTOs;

/// <summary>
/// Transport DTO for server-side grid queries — carries sort descriptors, a composite QBE
/// filter graph, and paging parameters. Consumed by scaffolded controller POST /query endpoints
/// and produced by scaffolded GridDataProviders that bridge <c>GridDataRequest</c> to the API.
/// </summary>
public class GridQueryRequest
{
    public int PageIndex { get; init; }
    public int PageSize { get; init; } = 25;
    public IReadOnlyList<GridQuerySort> Sorts { get; init; } = [];
    public GridCompositeFilter? Filter { get; init; }
}

/// <summary>Sort descriptor for a single property, conveying direction.</summary>
public record GridQuerySort(string PropertyName, bool Descending = false);

/// <summary>
/// A single leaf filter condition: property + operator string + value.
/// Operator strings match <c>Dabbler.Components.Data.FilterOperators</c> constants.
/// </summary>
public record GridQueryFilter(string PropertyName, string Operator, object? Value);

/// <summary>
/// Composite filter with AND/OR logic, leaf conditions, and nested child composites.
/// </summary>
public class GridCompositeFilter
{
    public GridFilterLogic Logic { get; init; } = GridFilterLogic.And;
    public IReadOnlyList<GridQueryFilter> Filters { get; init; } = [];
    public IReadOnlyList<GridCompositeFilter> Children { get; init; } = [];
}

/// <summary>Logical operator that combines conditions in a <see cref="GridCompositeFilter"/>.</summary>
public enum GridFilterLogic { And, Or }
