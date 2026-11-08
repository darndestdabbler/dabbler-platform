using System.Net;
using System.Net.Http.Json;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Entities;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dabbler.Security.Tests.Endpoints;

/// <summary>
/// Integration tests for the /auth/* minimal-API endpoints registered by
/// MapDabblerSecurityEndpoints(). Each test creates its own TestServer so
/// service mocks can be set up independently.
/// </summary>
public class AuthEndpointsTests
{
    // ── Helpers ────────────────────────────────────────────────────────────

    private static AppUser MakeUser(int id = 1, string email = "user@test.local", bool hasMfa = false)
    {
        var roles = new List<AppUserRole>();
        return new AppUser
        {
            Id = id,
            Email = email,
            DisplayName = "Test User",
            IsActive = true,
            Roles = roles
        };
    }

    private static HttpClient BuildClient(
        Action<IServiceCollection> configureServices)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();

        builder.Services.AddAuthentication("Cookies")
            .AddCookie(o => o.LoginPath = "/login");
        builder.Services.AddAuthorization();

        configureServices(builder.Services);

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapDabblerSecurityEndpoints();

        app.StartAsync().GetAwaiter().GetResult();
        return app.GetTestClient();
    }

    // ── POST /auth/send-magic-link ─────────────────────────────────────────

    [Fact]
    public async Task SendMagicLink_ValidRequest_Returns200()
    {
        var mockMl = new Mock<IMagicLinkService>();
        mockMl.Setup(s => s.IssueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new MagicLinkIssueResult(true));

        var client = BuildClient(services =>
        {
            services.AddSingleton(mockMl.Object);
            services.AddSingleton(new Mock<IUserService>().Object);
        });

        var response = await client.PostAsJsonAsync("/auth/send-magic-link", new
        {
            Email = "user@test.local",
            BaseVerifyUrl = "https://app/login"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── POST /auth/verify (by token) ───────────────────────────────────────

    [Fact]
    public async Task Verify_ValidToken_NoMfa_Returns200WithSuccessTrue()
    {
        var user = MakeUser();
        var mockMl = new Mock<IMagicLinkService>();
        mockMl.Setup(s => s.VerifyByTokenAsync("valid-token", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new MagicLinkVerifyResult(true, false, user.Id, user.Email));

        var mockUserSvc = new Mock<IUserService>();
        mockUserSvc.Setup(s => s.GetByIdCachedAsync(user.Id))
                   .ReturnsAsync(user);

        var client = BuildClient(services =>
        {
            services.AddSingleton(mockMl.Object);
            services.AddSingleton(mockUserSvc.Object);
        });

        var response = await client.PostAsJsonAsync("/auth/verify", new { Token = "valid-token" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<VerifyResponseDto>();
        body!.RequiresMfa.Should().BeFalse();
        body.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Verify_ValidToken_MfaRequired_Returns200WithRequiresMfaTrue()
    {
        var mockMl = new Mock<IMagicLinkService>();
        mockMl.Setup(s => s.VerifyByTokenAsync("mfa-token", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new MagicLinkVerifyResult(true, RequiresMfa: true, UserId: 7, Email: "mfa@test.local"));

        var client = BuildClient(services =>
        {
            services.AddSingleton(mockMl.Object);
            services.AddSingleton(new Mock<IUserService>().Object);
        });

        var response = await client.PostAsJsonAsync("/auth/verify", new { Token = "mfa-token" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<VerifyResponseDto>();
        body!.RequiresMfa.Should().BeTrue();
        body.UserId.Should().Be(7);
    }

    [Fact]
    public async Task Verify_InvalidToken_Returns400()
    {
        var mockMl = new Mock<IMagicLinkService>();
        mockMl.Setup(s => s.VerifyByTokenAsync("bad-token", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new MagicLinkVerifyResult(false, false, null, null, "Invalid or expired token"));

        var client = BuildClient(services =>
        {
            services.AddSingleton(mockMl.Object);
            services.AddSingleton(new Mock<IUserService>().Object);
        });

        var response = await client.PostAsJsonAsync("/auth/verify", new { Token = "bad-token" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Verify_NoTokenOrCode_Returns400()
    {
        var client = BuildClient(services =>
        {
            services.AddSingleton(new Mock<IMagicLinkService>().Object);
            services.AddSingleton(new Mock<IUserService>().Object);
        });

        var response = await client.PostAsJsonAsync("/auth/verify", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── POST /auth/verify (by code) ────────────────────────────────────────

    [Fact]
    public async Task Verify_ValidCode_Returns200()
    {
        var user = MakeUser();
        var mockMl = new Mock<IMagicLinkService>();
        mockMl.Setup(s => s.VerifyByCodeAsync("user@test.local", "123456", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new MagicLinkVerifyResult(true, false, user.Id, user.Email));

        var mockUserSvc = new Mock<IUserService>();
        mockUserSvc.Setup(s => s.GetByIdCachedAsync(user.Id)).ReturnsAsync(user);

        var client = BuildClient(services =>
        {
            services.AddSingleton(mockMl.Object);
            services.AddSingleton(mockUserSvc.Object);
        });

        var response = await client.PostAsJsonAsync("/auth/verify", new
        {
            Email = "user@test.local",
            Code = "123456"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── POST /auth/verify-totp ─────────────────────────────────────────────

    [Fact]
    public async Task VerifyTotp_ValidCode_Returns200()
    {
        var user = MakeUser();
        var mockMfa = new Mock<IMfaService>();
        mockMfa.Setup(s => s.ValidateTotpAsync(user.Id, "654321")).ReturnsAsync(true);

        var mockUserSvc = new Mock<IUserService>();
        mockUserSvc.Setup(s => s.GetByIdCachedAsync(user.Id)).ReturnsAsync(user);

        var client = BuildClient(services =>
        {
            services.AddSingleton(new Mock<IMagicLinkService>().Object);
            services.AddSingleton(mockUserSvc.Object);
            services.AddSingleton(mockMfa.Object);
        });

        var response = await client.PostAsJsonAsync("/auth/verify-totp", new
        {
            UserId = user.Id,
            Code = "654321"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task VerifyTotp_InvalidCode_Returns400()
    {
        var mockMfa = new Mock<IMfaService>();
        mockMfa.Setup(s => s.ValidateTotpAsync(1, "000000")).ReturnsAsync(false);

        var client = BuildClient(services =>
        {
            services.AddSingleton(new Mock<IMagicLinkService>().Object);
            services.AddSingleton(new Mock<IUserService>().Object);
            services.AddSingleton(mockMfa.Object);
        });

        var response = await client.PostAsJsonAsync("/auth/verify-totp", new { UserId = 1, Code = "000000" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── POST /auth/logout ──────────────────────────────────────────────────

    [Fact]
    public async Task Logout_Unauthenticated_Redirects()
    {
        var client = BuildClient(services =>
        {
            services.AddSingleton(new Mock<IMagicLinkService>().Object);
            services.AddSingleton(new Mock<IUserService>().Object);
        });

        // Not authenticated → RequireAuthorization() on /logout will redirect
        var response = await client.PostAsync("/auth/logout", null);

        // Cookie auth redirects to /login for unauthenticated calls
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Unauthorized);
    }

    // ── DTOs for response parsing ──────────────────────────────────────────

    private class VerifyResponseDto
    {
        public bool RequiresMfa { get; set; }
        public int? UserId { get; set; }
        public bool Success { get; set; }
    }
}

// Namespace-level extension to expose MapDabblerSecurityEndpoints on WebApplication
file static class WebAppExtensions
{
    public static WebApplication MapDabblerSecurityEndpoints(this WebApplication app)
    {
        Dabbler.Security.Extensions.EndpointRouteBuilderExtensions.MapDabblerSecurityEndpoints(app);
        return app;
    }
}
