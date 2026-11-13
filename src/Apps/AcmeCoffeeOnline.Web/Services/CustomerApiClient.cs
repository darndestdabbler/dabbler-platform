using System.Net.Http.Json;
using AcmeCoffeeOnline.Shared.DTOs;

namespace AcmeCoffeeOnline.Web.Services;

public class CustomerApiClient
{
    private readonly HttpClient _http;

    public CustomerApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<PagedResponse<CustomerDto>> GetAllAsync(
        PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = BuildPagedQuery(request);
        var result = await _http.GetFromJsonAsync<PagedResponse<CustomerDto>>(
            $"api/customers{query}", cancellationToken);
        return result ?? new PagedResponse<CustomerDto>();
    }

    public async Task<CustomerDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<CustomerDto>(
            $"api/customers/{id}", cancellationToken);
    }

    public async Task<CustomerDto?> CreateAsync(CustomerDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("api/customers", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CustomerDto>(cancellationToken);
    }

    public async Task<CustomerDto?> UpdateAsync(int id, CustomerDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _http.PutAsJsonAsync($"api/customers/{id}", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CustomerDto>(cancellationToken);
    }

    public async Task<PagedResponse<CustomerDto>> SearchAsync(
        string searchTerm, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = BuildPagedQuery(request);
        query += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
        var result = await _http.GetFromJsonAsync<PagedResponse<CustomerDto>>(
            $"api/customers/search{query}", cancellationToken);
        return result ?? new PagedResponse<CustomerDto>();
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
