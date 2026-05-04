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
        // Prefer the CompositeFilter shape — that's what FilterPanel emits
        // today. The legacy flat-list `Filters` is kept for backward
        // compatibility with callers that haven't migrated, but it is
        // empty whenever CompositeFilter is set.
        //
        // Note: this is a lossy translation. The API supports a single
        // text searchTerm that matches against several columns (FirstName,
        // LastName, Email, City, State); the FilterPanel emits structured
        // per-column filters (e.g., LastName contains "Smith"). For a
        // single text-contains filter this works because the API's
        // searchTerm semantics are a superset, but a structured-filter
        // API would let callers say "LastName contains Smith AND City
        // equals Tucson" without conflation. That's a future enhancement;
        // this fix unblocks the FilterPanel integration today.
        if (request.CompositeFilter is not null)
        {
            foreach (var filter in request.CompositeFilter.Filters)
            {
                if (filter.Operator == "contains" && filter.Value is string value)
                    return value;
            }
        }

        foreach (var filter in request.Filters)
        {
            if (filter.Operator == "contains" && filter.Value is string value)
                return value;
        }
        return null;
    }
}
