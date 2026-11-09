using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Dabbler.Security.Middleware;

/// <summary>
/// Middleware to resolve an application user based on special HTTP headers,
/// typically used for trusted, internal service-to-service communication.
/// It checks for <c>X-Internal-Api-Key</c> and <c>X-App-User-Id</c> headers.
/// </summary>
public class InternalUserMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _expectedKey;
    private readonly ILogger<InternalUserMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InternalUserMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="expectedKey">The secret key that must be present in the <c>X-Internal-Api-Key</c> header.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="expectedKey"/> is null or empty.</exception>
    public InternalUserMiddleware(
        RequestDelegate next,
        string expectedKey,
        ILogger<InternalUserMiddleware> logger)
    {
        if (string.IsNullOrWhiteSpace(expectedKey))
            throw new ArgumentException("Internal API key must not be null or empty.", nameof(expectedKey));

        _next = next;
        _expectedKey = expectedKey;
        _logger = logger;
    }

    /// <summary>
    /// Processes a request to identify and load an internal user.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
    /// <param name="sessionContextService">The service used to resolve active-organization session state.</param>
    /// <returns>A <see cref="Task"/> that represents the execution of this middleware.</returns>
    /// <remarks>
    /// If the <c>X-Internal-Api-Key</c> header matches the expected key and a valid user ID is
    /// provided in the <c>X-App-User-Id</c> header, the corresponding user and their roles are
    /// loaded and stored in the <see cref="HttpContext.Items"/> collection.
    /// </remarks>
    public async Task InvokeAsync(HttpContext context, IUserSessionContextService sessionContextService)
    {
        if (context.Request.Headers.TryGetValue("X-Internal-Api-Key", out var key)
            && ConstantTimeEquals(key.ToString(), _expectedKey)
            && context.Request.Headers.TryGetValue("X-App-User-Id", out var userIdHeader)
            && int.TryParse(userIdHeader, out var userId))
        {
            var requestedOrganizationId = TryParseActiveOrganizationIdHeader(context);
            var sessionContext = await sessionContextService.BuildAsync(userId, requestedOrganizationId);
            if (sessionContext != null)
            {
                context.Items["AppUserId"] = userId;
                context.Items["AppUserRoles"] = sessionContext.EffectiveRoles.ToList();

                if (sessionContext.ActiveOrganizationId.HasValue)
                {
                    context.Items["ActiveOrganizationId"] = sessionContext.ActiveOrganizationId.Value;
                }

                if (sessionContext.ActiveCustomerId.HasValue)
                {
                    context.Items["AppUserCustomerId"] = sessionContext.ActiveCustomerId.Value;
                }

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, sessionContext.UserId.ToString()),
                    new(ClaimTypes.Email, sessionContext.Email),
                    new(ClaimTypes.Name, sessionContext.DisplayName)
                };

                foreach (var roleName in sessionContext.EffectiveRoles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, roleName));
                }

                if (sessionContext.ActiveOrganizationId.HasValue)
                {
                    claims.Add(new Claim(
                        UserSessionContextConstants.ActiveOrganizationIdClaimType,
                        sessionContext.ActiveOrganizationId.Value.ToString()));
                }

                if (sessionContext.ActiveOrganizationMembershipId.HasValue)
                {
                    claims.Add(new Claim(
                        UserSessionContextConstants.ActiveOrganizationMembershipIdClaimType,
                        sessionContext.ActiveOrganizationMembershipId.Value.ToString()));
                }

                if (!string.IsNullOrWhiteSpace(sessionContext.ActiveOrganizationKey))
                {
                    claims.Add(new Claim(
                        UserSessionContextConstants.ActiveOrganizationKeyClaimType,
                        sessionContext.ActiveOrganizationKey));
                }

                if (!string.IsNullOrWhiteSpace(sessionContext.ActiveOrganizationName))
                {
                    claims.Add(new Claim(
                        UserSessionContextConstants.ActiveOrganizationNameClaimType,
                        sessionContext.ActiveOrganizationName));
                }

                if (sessionContext.ActiveCustomerId.HasValue)
                {
                    claims.Add(new Claim("CustomerId", sessionContext.ActiveCustomerId.Value.ToString()));
                }

                var identity = new ClaimsIdentity(claims, "InternalApi");
                context.User = new ClaimsPrincipal(identity);

                _logger.LogDebug("Internal user resolved: {UserId}", userId);
            }
        }

        await _next(context);
    }

    private static bool ConstantTimeEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);

        if (aBytes.Length != bBytes.Length)
        {
            // Still perform a comparison to avoid leaking length information via timing
            CryptographicOperations.FixedTimeEquals(aBytes, aBytes);
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }

    private static int? TryParseActiveOrganizationIdHeader(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(
                UserSessionContextConstants.ActiveOrganizationIdHeaderName,
                out var activeOrganizationHeader))
        {
            return null;
        }

        return int.TryParse(activeOrganizationHeader, out var activeOrganizationId)
            ? activeOrganizationId
            : null;
    }
}
