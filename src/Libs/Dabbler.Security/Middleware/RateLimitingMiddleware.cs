using System.Text;
using System.Text.Json;
using Dabbler.Security.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Dabbler.Security.Middleware;

/// <summary>
/// Middleware that applies rate limiting to specific authentication-related endpoints to
/// prevent brute-force attacks and resource abuse.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly IRateLimitStore _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="store">The store used to track rate limit attempts.</param>
    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IRateLimitStore store)
    {
        _next = next;
        _logger = logger;
        _store = store;
    }

    /// <summary>
    /// Processes a request to apply rate limiting rules.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
    /// <returns>A <see cref="Task"/> that represents the execution of this middleware.</returns>
    /// <remarks>
    /// This middleware inspects the request path and method to apply different rate limiting
    /// strategies for magic link issuance, token verification, and TOTP verification. If a
    /// limit is exceeded, it returns an HTTP 429 Too Many Requests response and short-circuits
    /// the pipeline.
    /// </remarks>
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();

        if (path?.Contains("/auth/send-magic-link") == true && context.Request.Method == "POST")
        {
            if (!await CheckMagicLinkLimitAsync(context))
                return;
        }
        else if (path?.Contains("/auth/verify-totp") == true && context.Request.Method == "POST")
        {
            // Must be checked before /auth/verify — the longer path is a substring of the shorter.
            if (!await CheckTotpLimitAsync(context))
                return;
        }
        else if (path?.Contains("/auth/verify") == true && context.Request.Method == "POST")
        {
            if (!await CheckVerifyLimitAsync(context))
                return;
        }

        await _next(context);
    }

    private async Task<bool> CheckMagicLinkLimitAsync(HttpContext context)
    {
        var email = await ReadStringFieldAsync(context, "email");
        if (string.IsNullOrEmpty(email))
            return true; // Let the endpoint handle validation

        var key = $"magiclink:{email}";
        var window = TimeSpan.FromMinutes(15);
        const int maxAttempts = 5;

        var count = await _store.RecordAttemptAsync(key, window);

        if (count > maxAttempts)
        {
            _logger.LogWarning("Rate limit exceeded for magic link: {Email}", MaskEmail(email));
            context.Response.StatusCode = 429;
            context.Response.Headers["Retry-After"] = "900"; // 15 minutes
            return false;
        }

        return true;
    }

    private async Task<bool> CheckVerifyLimitAsync(HttpContext context)
    {
        // SEC-3: Key by IP address (not token) to prevent bypass via token variation
        var ip = context.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrEmpty(ip))
            return true;

        var key = $"verify:ip:{ip}";
        var window = TimeSpan.FromMinutes(10);
        const int maxAttempts = 5;

        var count = await _store.RecordAttemptAsync(key, window);

        if (count > maxAttempts)
        {
            _logger.LogWarning("Rate limit exceeded for token verification from IP {IP}", ip);
            context.Response.StatusCode = 429;
            return false;
        }

        return true;
    }

    private async Task<bool> CheckTotpLimitAsync(HttpContext context)
    {
        // Prefer user ID if available (set by InternalUserMiddleware or auth flow),
        // fall back to IP address to ensure TOTP rate limiting is always active.
        string rateLimitKey;
        if (context.Items.TryGetValue("AppUserId", out var userIdObj) && userIdObj is int userId)
        {
            rateLimitKey = $"totp:user:{userId}";
        }
        else
        {
            var ip = context.Connection.RemoteIpAddress?.ToString();
            if (ip is null)
                return true;
            rateLimitKey = $"totp:ip:{ip}";
        }

        var window = TimeSpan.FromMinutes(30);
        const int maxAttempts = 5;

        // Check lockout
        var lockExpiry = await _store.GetLockExpiryAsync(rateLimitKey);
        if (lockExpiry.HasValue)
        {
            _logger.LogWarning("TOTP rate limit - locked for {Key}", rateLimitKey);
            context.Response.StatusCode = 429;
            context.Response.Headers["Retry-After"] =
                ((int)(lockExpiry.Value - DateTime.UtcNow).TotalSeconds).ToString();
            return false;
        }

        var count = await _store.RecordAttemptAsync(rateLimitKey, window);

        if (count >= maxAttempts)
        {
            await _store.SetLockAsync(rateLimitKey, DateTime.UtcNow.Add(window));
            _logger.LogWarning("TOTP rate limit exceeded - locked for {Key}", rateLimitKey);
            context.Response.StatusCode = 429;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Reads the request body as JSON and returns the value of <paramref name="fieldName"/> as a string.
    /// Rewinds the body stream so downstream middleware can read it again.
    /// Limits the read to 8 KB to prevent DoS via oversized request bodies.
    /// </summary>
    private static async Task<string?> ReadStringFieldAsync(HttpContext context, string fieldName)
    {
        const int MaxBodySize = 8192;

        try
        {
            context.Request.EnableBuffering();

            var buffer = new byte[MaxBodySize];
            var bytesRead = await context.Request.Body.ReadAsync(buffer.AsMemory(0, MaxBodySize));
            context.Request.Body.Position = 0;

            if (bytesRead == 0)
                return null;

            var body = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty(fieldName, out var prop))
                return prop.GetString()?.ToLowerInvariant();
        }
        catch
        {
            // Ignore parsing errors — let the endpoint deal with malformed JSON
            if (context.Request.Body.CanSeek)
                context.Request.Body.Position = 0;
        }

        return null;
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return "***";
        var parts = email.Split('@');
        if (parts[0].Length == 0)
            return $"***@{parts[1]}";
        return $"{parts[0][0]}***@{parts[1]}";
    }
}
