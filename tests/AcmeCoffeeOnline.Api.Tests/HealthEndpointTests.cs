using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AcmeCoffeeOnline.Api.Tests;

public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_ReturnsHealthyPayloadAsync()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"service\":\"api\"", content);
        Assert.Contains("\"status\":\"healthy\"", content);
    }
}
