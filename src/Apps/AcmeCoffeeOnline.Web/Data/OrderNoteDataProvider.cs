using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Web.Services;
using Dabbler.Components.Data;

namespace AcmeCoffeeOnline.Web.Data;

/// <summary>
/// Provides order notes for a specific order via the notes API endpoint.
/// </summary>
public class OrderNoteDataProvider : IGridDataProvider<OrderNoteDto>
{
    private readonly OrderApiClient _client;

    public OrderNoteDataProvider(OrderApiClient client)
    {
        _client = client;
    }

    /// <summary>
    /// The order ID whose notes to provide. Must be set before calling FetchAsync.
    /// </summary>
    public int OrderId { get; set; }

    public async Task<GridDataResponse<OrderNoteDto>> FetchAsync(
        GridDataRequest request, CancellationToken cancellationToken = default)
    {
        var notes = await _client.GetNotesByOrderIdAsync(OrderId, cancellationToken);
        var totalCount = notes.Count;

        // Apply client-side paging since the API returns all notes for the order
        var skip = request.PageIndex * request.PageSize;
        var pageItems = notes.Skip(skip).Take(request.PageSize).ToList();

        return new GridDataResponse<OrderNoteDto>
        {
            Items = pageItems,
            TotalCount = totalCount
        };
    }
}
