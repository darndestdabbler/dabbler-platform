using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Web.Services;
using Dabbler.Components.Data;

namespace AcmeCoffeeOnline.Web.Data;

public class ProductDataProvider : IGridDataProvider<ProductDto>
{
    private readonly ProductApiClient _client;

    public ProductDataProvider(ProductApiClient client)
    {
        _client = client;
    }

    public async Task<GridDataResponse<ProductDto>> FetchAsync(
        GridDataRequest request, CancellationToken cancellationToken = default)
    {
        var pagedRequest = MapToPagedRequest(request);
        var response = await _client.GetAllAsync(pagedRequest, cancellationToken);

        return new GridDataResponse<ProductDto>
        {
            Items = response.Items,
            TotalCount = response.TotalCount
        };
    }

    private static PagedRequest MapToPagedRequest(GridDataRequest request)
    {
        var paged = new PagedRequest
        {
            Page = request.PageIndex + 1,
            PageSize = request.PageSize
        };

        if (request.Sorts.Count > 0)
        {
            paged.SortBy = request.Sorts[0].PropertyName;
            paged.SortDescending = request.Sorts[0].Descending;
        }

        var searchFilter = FindSearchFilter(request);
        if (searchFilter is not null)
            paged.SearchTerm = searchFilter;

        return paged;
    }

    private static string? FindSearchFilter(GridDataRequest request)
    {
        foreach (var filter in request.Filters)
        {
            if (filter.Operator == FilterOperators.Contains && filter.Value is string value)
                return value;
        }
        return null;
    }
}
