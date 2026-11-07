using System.Net.Http.Json;
using AcmeCoffeeOnline.Shared.DTOs;

namespace AcmeCoffeeOnline.Web.Services;

public class ProductApiClient
{
    private readonly HttpClient _http;

    public ProductApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<PagedResponse<ProductDto>> GetAllAsync(
        PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = BuildPagedQuery(request);
        var result = await _http.GetFromJsonAsync<PagedResponse<ProductDto>>(
            $"api/products{query}", cancellationToken);
        return result ?? new PagedResponse<ProductDto>();
    }

    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<ProductDto>(
            $"api/products/{id}", cancellationToken);
    }

    public async Task<ProductDto?> CreateAsync(ProductDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("api/products", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken);
    }

    public async Task<ProductDto?> UpdateAsync(int id, ProductDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _http.PutAsJsonAsync($"api/products/{id}", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken);
    }

    public async Task ToggleAvailabilityAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _http.PatchAsync($"api/products/{id}/availability", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

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
