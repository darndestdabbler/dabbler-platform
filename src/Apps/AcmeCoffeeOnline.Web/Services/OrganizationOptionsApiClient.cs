using System.Net.Http.Json;
using AcmeCoffeeOnline.Shared.DTOs;

namespace AcmeCoffeeOnline.Web.Services;

public sealed class OrganizationOptionsApiClient
{
    private readonly HttpClient _httpClient;

    public OrganizationOptionsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<OrganizationOptionDto>> GetOptionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var options = await _httpClient.GetFromJsonAsync<IReadOnlyList<OrganizationOptionDto>>(
                "api/session/organization-options",
                cancellationToken);

            return options ?? [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return [];
        }
    }
}
