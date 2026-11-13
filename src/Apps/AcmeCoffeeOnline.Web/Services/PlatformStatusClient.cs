using System.Net.Http.Json;

namespace AcmeCoffeeOnline.Web.Services;

public sealed class PlatformStatusClient(HttpClient httpClient)
{
    public async Task<PlatformStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetFromJsonAsync<PlatformStatusResponse>(
            "api/platform/status",
            cancellationToken);

        return response ?? new PlatformStatusResponse(
            Status: "unavailable",
            Stage: "missing-response",
            UtcTimestamp: DateTimeOffset.MinValue);
    }
}

public sealed record PlatformStatusResponse(
    string Status,
    string Stage,
    DateTimeOffset UtcTimestamp);
