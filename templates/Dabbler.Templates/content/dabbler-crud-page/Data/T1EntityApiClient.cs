using System.Net.Http.Json;
using Dabbler.Api.Querying;
using T5DtoNs;

namespace T3WebNs.Data;

/// <summary>
/// Typed HTTP client for the T2EntityPlural REST endpoints emitted by the
/// dabbler-crud-service template. Wraps the QBE grid-query POST + the
/// standard CRUD verbs (GET / POST / PUT / DELETE).
///
/// Register via T3WebNs.Data.T1EntityScaffoldingExtensions.AddT1EntityScaffolding
/// in Program.cs (one call wires both this client and the GridDataProvider below).
/// </summary>
public class T1EntityApiClient
{
    private readonly HttpClient _http;

    public T1EntityApiClient(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Grid query — POST (not GET) because nested filter graphs do not round-trip
    /// through query-string encoding. Returns total count + items for the requested page.
    /// </summary>
    public async Task<GridQueryResponse<T1EntityDto>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("api/T9RouteName/query", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GridQueryResponse<T1EntityDto>>(cancellationToken)
            ?? new GridQueryResponse<T1EntityDto>();
    }

    public async Task<T1EntityDto?> GetByIdAsync(T7PkType id, CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<T1EntityDto>($"api/T9RouteName/{id}", cancellationToken);
    }

    public async Task<T1EntityDto?> CreateAsync(T1EntityDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("api/T9RouteName", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T1EntityDto>(cancellationToken);
    }

    public async Task<T1EntityDto?> UpdateAsync(T7PkType id, T1EntityDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _http.PutAsJsonAsync($"api/T9RouteName/{id}", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T1EntityDto>(cancellationToken);
    }

    public async Task<bool> DeleteAsync(T7PkType id, CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"api/T9RouteName/{id}", cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
