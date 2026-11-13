namespace Dabbler.Api.Querying;

/// <summary>
/// Transport DTO for server-side grid queries — carries sort descriptors, a composite QBE
/// filter graph, and paging parameters. Consumed by scaffolded controller POST /query endpoints
/// and produced by scaffolded GridDataProviders that bridge <c>GridDataRequest</c> to the API.
/// </summary>
public class GridQueryRequest
{
    /// <summary>Zero-based index of the requested page.</summary>
    public int PageIndex { get; init; }

    /// <summary>Number of rows per page. Defaults to 25.</summary>
    public int PageSize { get; init; } = 25;

    /// <summary>Sort descriptors, applied in order. Empty by default.</summary>
    public IReadOnlyList<GridQuerySort> Sorts { get; init; } = [];

    /// <summary>Root of the filter graph, or <c>null</c> for no filtering.</summary>
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
    /// <summary>How <see cref="Filters"/> and <see cref="Children"/> are combined. Defaults to <see cref="GridFilterLogic.And"/>.</summary>
    public GridFilterLogic Logic { get; init; } = GridFilterLogic.And;

    /// <summary>Leaf conditions of this composite. Empty by default.</summary>
    public IReadOnlyList<GridQueryFilter> Filters { get; init; } = [];

    /// <summary>Nested composites, each combined with the leaf conditions under <see cref="Logic"/>. Empty by default.</summary>
    public IReadOnlyList<GridCompositeFilter> Children { get; init; } = [];
}

/// <summary>Logical operator that combines conditions in a <see cref="GridCompositeFilter"/>.</summary>
public enum GridFilterLogic
{
    /// <summary>Every condition must match.</summary>
    And,

    /// <summary>At least one condition must match.</summary>
    Or,
}

/// <summary>
/// Paged response shape emitted by scaffolded controller POST /query endpoints. Pairs with
/// <see cref="GridQueryRequest"/>: <c>PageIndex</c> is zero-based, mirroring the request;
/// <c>TotalCount</c> is the unpaged row count.
/// </summary>
public class GridQueryResponse<T>
{
    /// <summary>Rows of the returned page.</summary>
    public List<T> Items { get; set; } = [];

    /// <summary>Number of rows matching the filter, before paging.</summary>
    public int TotalCount { get; set; }

    /// <summary>Zero-based index of the returned page.</summary>
    public int PageIndex { get; set; }

    /// <summary>Number of rows per page.</summary>
    public int PageSize { get; set; }

    /// <summary>Number of pages for <see cref="TotalCount"/> at <see cref="PageSize"/>; zero when <see cref="PageSize"/> is not positive.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary><c>true</c> when <see cref="PageIndex"/> is past the first page.</summary>
    public bool HasPreviousPage => PageIndex > 0;

    /// <summary><c>true</c> when a page follows <see cref="PageIndex"/>.</summary>
    public bool HasNextPage => PageIndex + 1 < TotalPages;
}
