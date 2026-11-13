using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Web.Services;
using Dabbler.Components.Data;

namespace AcmeCoffeeOnline.Web.Data;

/// <summary>
/// Provides order items for a specific order. Fetches the full order and returns its items.
/// </summary>
public class OrderItemDataProvider : IGridDataProvider<OrderItemDto>
{
    private readonly OrderApiClient _client;

    public OrderItemDataProvider(OrderApiClient client)
    {
        _client = client;
    }

    /// <summary>
    /// The order ID whose items to provide. Must be set before calling FetchAsync.
    /// </summary>
    public int OrderId { get; set; }

    public async Task<GridDataResponse<OrderItemDto>> FetchAsync(
        GridDataRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _client.GetByIdAsync(OrderId, cancellationToken);
        if (order is null)
        {
            return new GridDataResponse<OrderItemDto>
            {
                Items = [],
                TotalCount = 0
            };
        }

        var items = order.OrderItems;
        var totalCount = items.Count;

        // Apply client-side paging since the API returns all items with the order
        var skip = request.PageIndex * request.PageSize;
        var pageItems = items.Skip(skip).Take(request.PageSize).ToList();

        return new GridDataResponse<OrderItemDto>
        {
            Items = pageItems,
            TotalCount = totalCount
        };
    }
}
