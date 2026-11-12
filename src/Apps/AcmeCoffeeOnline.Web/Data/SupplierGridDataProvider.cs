using Dabbler.Api.Querying;
using Dabbler.Components.Data;
using AcmeCoffeeOnline.Shared.DTOs;

namespace AcmeCoffeeOnline.Web.Data;

/// <summary>
/// Adapter from <see cref="IGridDataProvider{T}"/> (the contract MudDataGrid consumes
/// via <c>ServerData</c>) to <see cref="SupplierApiClient.QueryAsync"/>. Translates
/// <c>GridDataRequest</c> (Dabbler.Components transport) to <c>GridQueryRequest</c>
/// (Dabbler.Api.Querying transport) field for field: the filter panels emit the
/// <see cref="FilterOperators"/> constants, which are the operator strings the server-side
/// <c>QueryableExtensions.ApplyGridCompositeFilter</c> matches, so operators pass through unchanged.
/// </summary>
public class SupplierGridDataProvider : IGridDataProvider<SupplierDto>
{
    private readonly SupplierApiClient _client;

    public SupplierGridDataProvider(SupplierApiClient client)
    {
        _client = client;
    }

    public async Task<GridDataResponse<SupplierDto>> FetchAsync(
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

        return new GridDataResponse<SupplierDto>
        {
            Items = response.Items,
            TotalCount = response.TotalCount,
        };
    }

    private static GridCompositeFilter? MapCompositeFilter(CompositeFilterDescriptor? source)
    {
        if (source is null) return null;

        var leaves = source.Filters
            .Select(f => new GridQueryFilter(f.PropertyName, f.Operator, f.Value))
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
                .Select(f => new GridQueryFilter(f.PropertyName, f.Operator, f.Value))
                .ToList(),
        };
    }
}
