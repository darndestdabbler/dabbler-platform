using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;

namespace AcmeCoffeeOnline.Web.Tests;

public sealed class WebGuardrailTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public WebGuardrailTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HomePage_RendersInsideMainShellAndIncludesDabblerAssetAsync()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Development"));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        using var signInRequest = new HttpRequestMessage(
            HttpMethod.Get,
            "/test-auth/seeded-session?roles=BackofficeUser&displayName=Guardrail%20Home%20User");
        signInRequest.Headers.Add("X-Test-Auth-Secret", "acme-local-seeded-session");

        using var signInResponse = await client.SendAsync(signInRequest);
        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);

        var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"main-shell\"", content);
        Assert.Contains("AcmeCoffeeOnline", content);
        Assert.Contains("data-dabbler-asset=\"dabbler-shell.css\"", content);
    }

    [Fact]
    public async Task LoginPage_RendersWithAuthLayoutWithoutMainShellAsync()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/login");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"auth-shell\"", content);
        Assert.Contains("data-testid=\"login-page\"", content);
        Assert.DoesNotContain("data-testid=\"main-shell\"", content);
    }

    [Fact]
    public async Task SeededSessionEndpoint_RequiresSecretAndUnlocksConfiguredPolicyAsync()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Development"));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var unauthorizedResponse = await client.GetAsync("/auth-check");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedResponse.StatusCode);

        var forbiddenResponse = await client.GetAsync("/test-auth/seeded-session?roles=BackofficeUser");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        using var signInRequest = new HttpRequestMessage(
            HttpMethod.Get,
            "/test-auth/seeded-session?roles=BackofficeUser&displayName=Guardrail%20Test%20User");
        signInRequest.Headers.Add("X-Test-Auth-Secret", "acme-local-seeded-session");

        using var signInResponse = await client.SendAsync(signInRequest);
        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);

        var authorizedResponse = await client.GetAsync("/auth-check");
        var authorizedContent = await authorizedResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, authorizedResponse.StatusCode);
        Assert.Contains("\"BackofficeUser\"", authorizedContent);
        Assert.Contains("Guardrail Test User", authorizedContent);
    }
}
