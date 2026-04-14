using AcmeCoffeeOnline.Web.Tests.Infrastructure;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace AcmeCoffeeOnline.Web.Tests;

public sealed class ObservabilitySmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ObservabilitySmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DevEmailSender_EmitsStructuredMagicLinkCaptureLogAsync()
    {
        var collector = new TestLogCollector();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(collector.CreateProvider()));
        var logger = loggerFactory.CreateLogger<DevEmailSender>();
        var sender = new DevEmailSender(logger);

        await sender.SendMagicLinkAsync("customer@acme.test", "123456", "http://localhost/login?token=abc");

        var entry = Assert.Single(collector.Entries);
        Assert.Equal(7100, entry.EventId.Id);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("MagicLink", entry.State["EmailType"]);
        Assert.Equal("c***r@acme.test", entry.State["MaskedEmail"]);
        Assert.Equal(true, entry.State["HasCode"]);
        Assert.Equal(true, entry.State["HasLink"]);
    }

    [Fact]
    public async Task SeededSessionEndpoint_EmitsStructuredObservabilityLogAsync()
    {
        var collector = new TestLogCollector();
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddProvider(collector.CreateProvider());
            });
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        using var signInRequest = new HttpRequestMessage(
            HttpMethod.Get,
            "/test-auth/seeded-session?roles=BackofficeUser&displayName=Observability%20User");
        signInRequest.Headers.Add("X-Test-Auth-Secret", "acme-local-seeded-session");

        using var signInResponse = await client.SendAsync(signInRequest);

        Assert.True(signInResponse.IsSuccessStatusCode);

        var entry = Assert.Single(collector.Entries, log => log.EventId.Id == 4101);
        Assert.Equal("s***r@acme.test", entry.State["MaskedEmail"]);
        Assert.Equal("seeded-user", entry.State["ResolvedUserId"]);
        Assert.Equal("BackofficeUser", entry.State["Roles"]);
        Assert.Equal(false, entry.State["MatchedExistingUser"]);
    }

    [Fact]
    public async Task MagicLinkDiagnosticsEndpoint_EmitsStructuredObservabilityLogAsync()
    {
        var collector = new TestLogCollector();
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddProvider(collector.CreateProvider());
            });
        });

        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/test-auth/magic-links?email=customer%40acme.test&clear=true");
        request.Headers.Add("X-Test-Auth-Secret", "acme-local-seeded-session");

        using var response = await client.SendAsync(request);

        Assert.True(response.IsSuccessStatusCode);

        var entry = Assert.Single(collector.Entries, log => log.EventId.Id == 4100);
        Assert.Equal("c***r@acme.test", entry.State["EmailFilter"]);
        Assert.Equal(true, entry.State["ClearInbox"]);
        Assert.Equal(0, entry.State["CapturedCount"]);
    }
}
