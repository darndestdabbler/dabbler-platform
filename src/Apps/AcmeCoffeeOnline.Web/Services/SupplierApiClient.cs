using System.Net.Http.Json;
using Dabbler.Api.Querying;
using AcmeCoffeeOnline.Shared.DTOs;

namespace AcmeCoffeeOnline.Web.Data;

/// <summary>
/// Typed HTTP client for the Suppliers REST endpoints emitted by the
/// dabbler-crud-service template. Wraps the QBE grid-query POST + the
/// standard CRUD verbs (GET / POST / PUT / DELETE).
///
/// Register via AcmeCoffeeOnline.Web.Data.SupplierScaffoldingExtensions.AddSupplierScaffolding
/// in Program.cs (one call wires both this client and the GridDataProvider below).
/// </summary>
public class SupplierApiClient
{
    private readonly HttpClient _http;

    public SupplierApiClient(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Grid query — POST (not GET) because nested filter graphs do not round-trip
    /// through query-string encoding. Returns total count + items for the requested page.
    /// </summary>
    public async Task<GridQueryResponse<SupplierDto>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("api/suppliers/query", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GridQueryResponse<SupplierDto>>(cancellationToken)
            ?? new GridQueryResponse<SupplierDto>();
    }

    public async Task<SupplierDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<SupplierDto>($"api/suppliers/{id}", cancellationToken);
    }

    public async Task<SupplierDto?> CreateAsync(SupplierDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("api/suppliers", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SupplierDto>(cancellationToken);
    }

    public async Task<SupplierDto?> UpdateAsync(int id, SupplierDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _http.PutAsJsonAsync($"api/suppliers/{id}", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SupplierDto>(cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"api/suppliers/{id}", cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
