using Dabbler.Api.Querying;
using Dabbler.Components.Data;
using T5DtoNs;

namespace T3WebNs.Data;

/// <summary>
/// Adapter from <see cref="IGridDataProvider{T}"/> (the contract MudDataGrid consumes
/// via <c>ServerData</c>) to <see cref="T1EntityApiClient.QueryAsync"/>. Translates
/// <c>GridDataRequest</c> (Dabbler.Components transport) to <c>GridQueryRequest</c>
/// (Dabbler.Api.Querying transport) and remaps operator strings from the lowercase
/// shorthand the DeclarativeFilterPanel emits ("contains", "eq", "gte", "lte") to the
/// PascalCase canon the server-side <c>QueryableExtensions.ApplyGridCompositeFilter</c>
/// recognises ("Contains", "Equals", "GreaterThanOrEqual", "LessThanOrEqual").
/// </summary>
public class T1EntityGridDataProvider : IGridDataProvider<T1EntityDto>
{
    private readonly T1EntityApiClient _client;

    public T1EntityGridDataProvider(T1EntityApiClient client)
    {
        _client = client;
    }

    public async Task<GridDataResponse<T1EntityDto>> FetchAsync(
        GridDataRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = new GridQueryRequest
        {
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
            Sorts = request.Sorts
                .Select(s => new GridQuerySort(s.PropertyName, s.Descending))
                .ToList(),
            Filter = MapCompositeFilter(request.CompositeFilter)
                  ?? MapFlatFilters(request.Filters),
        };

        var response = await _client.QueryAsync(query, cancellationToken);

        return new GridDataResponse<T1EntityDto>
        {
            Items = response.Items,
            TotalCount = response.TotalCount,
        };
    }

    private static GridCompositeFilter? MapCompositeFilter(CompositeFilterDescriptor? source)
    {
        if (source is null) return null;

        var leaves = source.Filters
            .Select(f => new GridQueryFilter(f.PropertyName, NormaliseOperator(f.Operator), f.Value))
            .ToList();

        var children = source.Children
            .Select(MapCompositeFilter)
            .Where(c => c is not null)
            .Select(c => c!)
            .ToList();

        if (leaves.Count == 0 && children.Count == 0) return null;

        return new GridCompositeFilter
        {
            Logic = source.Logic == CompositeFilterLogic.Or ? GridFilterLogic.Or : GridFilterLogic.And,
            Filters = leaves,
            Children = children,
        };
    }

    private static GridCompositeFilter? MapFlatFilters(IReadOnlyList<FilterDescriptor> filters)
    {
        if (filters.Count == 0) return null;

        return new GridCompositeFilter
        {
            Logic = GridFilterLogic.And,
            Filters = filters
                .Select(f => new GridQueryFilter(f.PropertyName, NormaliseOperator(f.Operator), f.Value))
                .ToList(),
        };
    }

    /// <summary>
    /// Aligns the lowercase shorthand the DeclarativeFilterPanel emits with the
    /// PascalCase constants in <see cref="FilterOperators"/> that
    /// <see cref="QueryableExtensions.ApplyGridCompositeFilter"/> matches against.
    /// Unknown strings pass through unchanged so non-default operators authored by hand
    /// at the call site are still preserved.
    /// </summary>
    private static string NormaliseOperator(string op) => op switch
    {
        "contains" => FilterOperators.Contains,
        "startswith" => FilterOperators.StartsWith,
        "endswith" => FilterOperators.EndsWith,
        "eq" => FilterOperators.Equals,
        "gt" => FilterOperators.GreaterThan,
        "lt" => FilterOperators.LessThan,
        "gte" => FilterOperators.GreaterThanOrEqual,
        "lte" => FilterOperators.LessThanOrEqual,
        "isempty" => FilterOperators.IsEmpty,
        "isnotempty" => FilterOperators.IsNotEmpty,
        _ => op,
    };
}
