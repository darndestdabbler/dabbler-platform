using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AcmeCoffeeOnline.Web.Configuration;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AcmeCoffeeOnline.Web.Tests.Components.DevTools;

/// <summary>
/// Guards the endpoint contracts the Dev Tools UI consumes via
/// <c>TestAuthClient</c>. If someone changes the JSON shape of
/// <c>/test-auth/session-info</c> or <c>/test-auth/magic-links</c>, these tests
/// should fail before the UI breaks silently.
/// </summary>
public sealed class DevToolsEndpointContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DevToolsEndpointContractTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SessionInfoEndpoint_ReturnsExpectedShapeAsync()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Development"));
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/test-auth/session-info");
        request.Headers.Add("X-Test-Auth-Secret", ResolveSecret(factory));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var root = document.RootElement;

        // Keys the UI's TestAuthClient mirrors.
        Assert.True(root.TryGetProperty("isAuthenticated", out _));
        Assert.True(root.TryGetProperty("roles", out var rolesElement));
        Assert.Equal(JsonValueKind.Array, rolesElement.ValueKind);
        Assert.True(root.TryGetProperty("availableOrganizations", out var orgsElement));
        Assert.Equal(JsonValueKind.Array, orgsElement.ValueKind);
        Assert.True(root.TryGetProperty("userId", out _));
        Assert.True(root.TryGetProperty("email", out _));
        Assert.True(root.TryGetProperty("activeOrganizationName", out _));
    }

    [Fact]
    public async Task SessionInfoEndpoint_RejectsMissingSecretAsync()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Development"));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/test-auth/session-info");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MagicLinksEndpoint_ReturnsCapturedMessagesAsync()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Development"));

        var inbox = factory.Services.GetRequiredService<DevEmailInbox>();
        inbox.Clear();
        inbox.Add(new DevEmailMessage(
            EmailType: "MagicLink",
            Email: "contract@example.com",
            Code: "123456",
            LinkUrl: "https://app.local/login?token=xyz",
            QrCodeDataUri: null,
            CreatedAtUtc: DateTimeOffset.UtcNow));

        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Get, "/test-auth/magic-links?email=contract%40example.com");
        request.Headers.Add("X-Test-Auth-Secret", ResolveSecret(factory));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var messages = await response.Content.ReadFromJsonAsync<List<DevEmailMessage>>();
        Assert.NotNull(messages);
        var message = Assert.Single(messages);
        Assert.Equal("contract@example.com", message.Email);
        Assert.Equal("MagicLink", message.EmailType);
        Assert.Equal("123456", message.Code);
        Assert.Equal("https://app.local/login?token=xyz", message.LinkUrl);

        inbox.Clear();
    }

    [Fact]
    public async Task MagicLinksEndpoint_ClearFlag_EmptiesInboxAsync()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Development"));

        var inbox = factory.Services.GetRequiredService<DevEmailInbox>();
        inbox.Clear();
        inbox.Add(new DevEmailMessage(
            "MagicLink", "clear-me@example.com", "777",
            "https://app.local/login?token=cc", null, DateTimeOffset.UtcNow));

        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Get, "/test-auth/magic-links?clear=true");
        request.Headers.Add("X-Test-Auth-Secret", ResolveSecret(factory));

        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.DoesNotContain(inbox.GetAll(), message => message.EmailType == "MagicLink");
    }

    [Fact]
    public async Task MagicLinksEndpoint_RejectsMissingSecretAsync()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Development"));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/test-auth/magic-links");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static string ResolveSecret(WebApplicationFactory<Program> factory)
    {
        return factory.Services
            .GetRequiredService<IOptions<AppAuthenticationOptions>>()
            .Value
            .SeededSession
            .SharedSecret;
    }
}
