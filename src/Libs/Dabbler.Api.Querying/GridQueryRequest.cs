namespace Dabbler.Api.Querying;

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

/// <summary>
/// Paged response shape emitted by scaffolded controller POST /query endpoints. Pairs with
/// <see cref="GridQueryRequest"/>: <c>PageIndex</c> is zero-based, mirroring the request;
/// <c>TotalCount</c> is the unpaged row count.
/// </summary>
public class GridQueryResponse<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageIndex > 0;
    public bool HasNextPage => PageIndex + 1 < TotalPages;
}
