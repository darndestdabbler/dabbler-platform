using System.Net;
using System.Reflection;
using System.Text.Json;
using AcmeCoffeeOnline.Web.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AcmeCoffeeOnline.Web.Tests;

public sealed class WebHealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public WebHealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_ReturnsHealthyPayloadAsync()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");
        await using var contentStream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(contentStream);
        var root = document.RootElement;
        var expectedEnvironment = _factory.Services.GetRequiredService<Microsoft.Extensions.Hosting.IHostEnvironment>().EnvironmentName;
        var expectedDeliveryMode = _factory.Services.GetRequiredService<IOptions<AppAuthenticationOptions>>().Value.DeliveryMode;
        var expectedVersion = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(Program).Assembly.GetName().Version?.ToString();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("web", root.GetProperty("service").GetString());
        Assert.Equal("healthy", root.GetProperty("status").GetString());
        Assert.Equal(expectedEnvironment, root.GetProperty("environment").GetString());
        Assert.Equal(expectedVersion, root.GetProperty("version").GetString());
        Assert.Equal(expectedDeliveryMode, root.GetProperty("deliveryMode").GetString());
    }
}
