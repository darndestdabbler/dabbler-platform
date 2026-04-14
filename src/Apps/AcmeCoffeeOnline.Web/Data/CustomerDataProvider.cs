using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Web.Services;
using Dabbler.Components.Data;

namespace AcmeCoffeeOnline.Web.Data;

public class CustomerDataProvider : IGridDataProvider<CustomerDto>
{
    private readonly CustomerApiClient _client;

    public CustomerDataProvider(CustomerApiClient client)
    {
        _client = client;
    }

    public async Task<GridDataResponse<CustomerDto>> FetchAsync(
        GridDataRequest request, CancellationToken cancellationToken = default)
    {
        var pagedRequest = MapToPagedRequest(request);
        var response = await _client.GetAllAsync(pagedRequest, cancellationToken);

        return new GridDataResponse<CustomerDto>
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
