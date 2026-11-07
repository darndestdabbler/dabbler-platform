using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Web.Services;
using Dabbler.Components.Data;

namespace AcmeCoffeeOnline.Web.Data;

public class OrderDataProvider : IGridDataProvider<OrderDto>
{
    private readonly OrderApiClient _client;

    public OrderDataProvider(OrderApiClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Optional customer ID filter. When set, only orders for that customer are returned.
    /// </summary>
    public int? CustomerId { get; set; }

    public async Task<GridDataResponse<OrderDto>> FetchAsync(
        GridDataRequest request, CancellationToken cancellationToken = default)
    {
        var pagedRequest = MapToPagedRequest(request);

        var response = CustomerId.HasValue
            ? await _client.GetByCustomerIdAsync(CustomerId.Value, pagedRequest, cancellationToken)
            : await _client.GetAllAsync(pagedRequest, cancellationToken);

        return new GridDataResponse<OrderDto>
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
            if (filter.Operator == "contains" && filter.Value is string value)
                return value;
        }
        return null;
    }
}
