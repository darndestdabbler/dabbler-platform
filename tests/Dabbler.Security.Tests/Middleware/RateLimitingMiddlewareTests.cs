using System.Net;
using System.Text;
using Dabbler.Security.Middleware;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dabbler.Security.Tests.Middleware;

public class RateLimitingMiddlewareTests
{
    private static RateLimitingMiddleware CreateMiddleware(RequestDelegate? next = null)
    {
        next ??= _ => Task.CompletedTask;
        return new RateLimitingMiddleware(
            next,
            NullLogger<RateLimitingMiddleware>.Instance,
            new InMemoryRateLimitStore());
    }

    private static DefaultHttpContext CreateContext(string path, string method, string jsonBody,
        string? ipAddress = "127.0.0.1")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;
        var bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
        context.Request.Body = new MemoryStream(bodyBytes);
        context.Request.ContentType = "application/json";
        if (ipAddress != null)
            context.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);
        return context;
    }

    // ── Magic Link Rate Limiting ───────────────────────────────────────────

    [Fact]
    public async Task MagicLink_FirstFiveRequests_PassThrough()
    {
        var middleware = CreateMiddleware();

        for (int i = 0; i < 5; i++)
        {
            var ctx = CreateContext("/auth/send-magic-link", "POST",
                """{"email":"user@example.com","baseVerifyUrl":"https://app/login"}""");
            await middleware.InvokeAsync(ctx);
            ctx.Response.StatusCode.Should().NotBe(429, $"request {i + 1} should pass");
        }
    }

    [Fact]
    public async Task MagicLink_SixthRequest_Returns429()
    {
        var middleware = CreateMiddleware();
        const string email = "throttled@example.com";
        var body = $$$"""{"email":"{{{email}}}","baseVerifyUrl":"https://app/login"}""";

        for (int i = 0; i < 5; i++)
        {
            var ctx = CreateContext("/auth/send-magic-link", "POST", body);
            await middleware.InvokeAsync(ctx);
        }

        var blocked = CreateContext("/auth/send-magic-link", "POST", body);
        await middleware.InvokeAsync(blocked);

        blocked.Response.StatusCode.Should().Be(429);
        blocked.Response.Headers["Retry-After"].ToString().Should().Be("900");
    }

    [Fact]
    public async Task MagicLink_DifferentEmails_TrackedSeparately()
    {
        var middleware = CreateMiddleware();

        // Exhaust limit for email A
        for (int i = 0; i < 5; i++)
        {
            var ctx = CreateContext("/auth/send-magic-link", "POST",
                """{"email":"a@example.com","baseVerifyUrl":"https://app/login"}""");
            await middleware.InvokeAsync(ctx);
        }

        // Email B should still pass
        var ctxB = CreateContext("/auth/send-magic-link", "POST",
            """{"email":"b@example.com","baseVerifyUrl":"https://app/login"}""");
        await middleware.InvokeAsync(ctxB);

        ctxB.Response.StatusCode.Should().NotBe(429);
    }

    [Fact]
    public async Task MagicLink_NonPostRequest_PassesThrough()
    {
        var middleware = CreateMiddleware();
        var ctx = CreateContext("/auth/send-magic-link", "GET", string.Empty);
        await middleware.InvokeAsync(ctx);
        ctx.Response.StatusCode.Should().NotBe(429);
    }

    [Fact]
    public async Task MagicLink_MissingEmail_PassesThrough()
    {
        var middleware = CreateMiddleware();
        var ctx = CreateContext("/auth/send-magic-link", "POST", """{"baseVerifyUrl":"https://app"}""");
        await middleware.InvokeAsync(ctx);
        ctx.Response.StatusCode.Should().NotBe(429);
    }

    // ── Token Verify Rate Limiting (SEC-3: keyed by IP) ───────────────────

    [Fact]
    public async Task Verify_FiveRequestsSameIp_PassThrough()
    {
        var middleware = CreateMiddleware();

        for (int i = 0; i < 5; i++)
        {
            var ctx = CreateContext("/auth/verify", "POST",
                """{"token":"abc-def-ghi-123"}""", "10.0.0.1");
            await middleware.InvokeAsync(ctx);
            ctx.Response.StatusCode.Should().NotBe(429);
        }
    }

    [Fact]
    public async Task Verify_SixthRequestSameIp_Returns429()
    {
        var middleware = CreateMiddleware();

        for (int i = 0; i < 5; i++)
        {
            var ctx = CreateContext("/auth/verify", "POST",
                $$$"""{"token":"token-{{{i}}}"}""", "10.0.0.2");
            await middleware.InvokeAsync(ctx);
        }

        // 6th request from same IP — should be blocked regardless of token
        var blocked = CreateContext("/auth/verify", "POST",
            """{"token":"different-token"}""", "10.0.0.2");
        await middleware.InvokeAsync(blocked);

        blocked.Response.StatusCode.Should().Be(429);
    }

    [Fact]
    public async Task Verify_DifferentIps_TrackedSeparately()
    {
        var middleware = CreateMiddleware();

        // Exhaust limit from IP A
        for (int i = 0; i < 6; i++)
        {
            var ctx = CreateContext("/auth/verify", "POST",
                """{"token":"some-token"}""", "10.0.0.3");
            await middleware.InvokeAsync(ctx);
        }

        // IP B should still pass
        var ctxB = CreateContext("/auth/verify", "POST",
            """{"token":"some-token"}""", "10.0.0.4");
        await middleware.InvokeAsync(ctxB);

        ctxB.Response.StatusCode.Should().NotBe(429);
    }

    [Fact]
    public async Task Verify_VaryingTokensSameIp_StillCounted()
    {
        // SEC-3 regression: varying tokens should NOT bypass the limit
        var middleware = CreateMiddleware();

        for (int i = 0; i < 5; i++)
        {
            var ctx = CreateContext("/auth/verify", "POST",
                $$$"""{"token":"unique-token-{{{i}}}"}""", "10.0.0.5");
            await middleware.InvokeAsync(ctx);
        }

        var blocked = CreateContext("/auth/verify", "POST",
            """{"token":"yet-another-token"}""", "10.0.0.5");
        await middleware.InvokeAsync(blocked);

        blocked.Response.StatusCode.Should().Be(429);
    }

    // ── TOTP Rate Limiting ─────────────────────────────────────────────────

    [Fact]
    public async Task Totp_NoUserIdInContext_FallsBackToIp()
    {
        var middleware = CreateMiddleware();
        // AppUserId not set — should use IP-based key and pass through
        var ctx = CreateContext("/auth/verify-totp", "POST",
            """{"userId":1,"code":"123456"}""", "10.0.0.10");
        await middleware.InvokeAsync(ctx);
        ctx.Response.StatusCode.Should().NotBe(429);
    }

    [Fact]
    public async Task Totp_LockedUser_Returns429WithRetryAfterHeader()
    {
        var middleware = CreateMiddleware();
        const int userId = 42;

        // Build a locked state by exhausting attempts
        for (int i = 0; i < 5; i++)
        {
            var ctx = CreateContext("/auth/verify-totp", "POST",
                """{"userId":42,"code":"000000"}""", "10.0.0.11");
            ctx.Items["AppUserId"] = userId;
            await middleware.InvokeAsync(ctx);
        }

        // 6th request — account should now be locked
        var blocked = CreateContext("/auth/verify-totp", "POST",
            """{"userId":42,"code":"000000"}""", "10.0.0.11");
        blocked.Items["AppUserId"] = userId;
        await middleware.InvokeAsync(blocked);

        blocked.Response.StatusCode.Should().Be(429);
        blocked.Response.Headers.ContainsKey("Retry-After").Should().BeTrue();
    }

    // ── Unrelated Routes ──────────────────────────────────────────────────

    [Fact]
    public async Task UnrelatedRoute_AlwaysPassesThrough()
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = CreateContext("/api/widgets", "POST", """{"name":"Widget"}""");
        await middleware.InvokeAsync(ctx);
        nextCalled.Should().BeTrue();
    }
}
