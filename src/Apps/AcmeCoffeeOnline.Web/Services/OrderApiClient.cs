using System.Net.Http.Json;
using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Shared.Enums;

namespace AcmeCoffeeOnline.Web.Services;

public class OrderApiClient
{
    private readonly HttpClient _http;

    public OrderApiClient(HttpClient http)
    {
        _http = http;
    }

    // ── Orders ──────────────────────────────────────────────────────────

    public async Task<PagedResponse<OrderDto>> GetAllAsync(
        PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = BuildPagedQuery(request);
        var result = await _http.GetFromJsonAsync<PagedResponse<OrderDto>>(
            $"api/orders{query}", cancellationToken);
        return result ?? new PagedResponse<OrderDto>();
    }

    public async Task<OrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<OrderDto>(
            $"api/orders/{id}", cancellationToken);
    }

    public async Task<PagedResponse<OrderDto>> GetByCustomerIdAsync(
        int customerId, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = BuildPagedQuery(request);
        var result = await _http.GetFromJsonAsync<PagedResponse<OrderDto>>(
            $"api/orders/by-customer/{customerId}{query}", cancellationToken);
        return result ?? new PagedResponse<OrderDto>();
    }

    public async Task<OrderDto?> CreateAsync(OrderDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("api/orders", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderDto>(cancellationToken);
    }

    public async Task<OrderDto?> UpdateAsync(int id, OrderDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _http.PutAsJsonAsync($"api/orders/{id}", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderDto>(cancellationToken);
    }

    public async Task UpdateStatusAsync(
        int id, OrderStatus newStatus, CancellationToken cancellationToken = default)
    {
        var body = new OrderStatusUpdateRequest { NewStatus = newStatus };
        var response = await _http.PatchAsJsonAsync($"api/orders/{id}/status", body, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    // ── Order Items ─────────────────────────────────────────────────────

    public async Task<OrderItemDto?> AddItemAsync(
        int orderId, OrderItemDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync($"api/orders/{orderId}/items", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderItemDto>(cancellationToken);
    }

    public async Task RemoveItemAsync(
        int orderId, int itemId, CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync(
            $"api/orders/{orderId}/items/{itemId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    // ── Order Notes ─────────────────────────────────────────────────────

    public async Task<List<OrderNoteDto>> GetNotesByOrderIdAsync(
        int orderId, CancellationToken cancellationToken = default)
    {
        var result = await _http.GetFromJsonAsync<List<OrderNoteDto>>(
            $"api/orders/{orderId}/notes", cancellationToken);
        return result ?? [];
    }

    public async Task<OrderNoteDto?> AddNoteAsync(
        int orderId, OrderNoteDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync($"api/orders/{orderId}/notes", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderNoteDto>(cancellationToken);
    }

    public async Task<OrderNoteDto?> UpdateNoteAsync(
        int orderId, int noteId, OrderNoteDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/orders/{orderId}/notes/{noteId}", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderNoteDto>(cancellationToken);
    }

    public async Task DeleteNoteAsync(
        int orderId, int noteId, CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync(
            $"api/orders/{orderId}/notes/{noteId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private static string BuildPagedQuery(PagedRequest request)
    {
        var parts = new List<string>
        {
            $"page={request.Page}",
            $"pageSize={request.PageSize}"
        };

        if (!string.IsNullOrEmpty(request.SortBy))
            parts.Add($"sortBy={Uri.EscapeDataString(request.SortBy)}");

        if (request.SortDescending)
            parts.Add("sortDescending=true");

        if (!string.IsNullOrEmpty(request.SearchTerm))
            parts.Add($"searchTerm={Uri.EscapeDataString(request.SearchTerm)}");

        return "?" + string.Join("&", parts);
    }
}
