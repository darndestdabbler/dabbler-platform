using System.Security.Claims;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Options;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dabbler.Security.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IEndpointRouteBuilder"/> to map Dabbler.Security endpoints.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the authentication-related HTTP endpoints for Dabbler.Security.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the routes to.</param>
    /// <returns>An <see cref="IEndpointRouteBuilder"/> that can be used to further customize the routes.</returns>
    /// <remarks>
    /// This method maps the following endpoints under the <c>/auth</c> group:
    /// <list type="bullet">
    /// <item><c>POST /send-magic-link</c>: Initiates the magic link login flow.</item>
    /// <item><c>POST /verify</c>: Verifies a magic link token or code.</item>
    /// <item><c>POST /verify-totp</c>: Verifies a Time-based One-Time Password (TOTP) for MFA.</item>
    /// <item><c>POST /logout</c>: Logs the current user out.</item>
    /// <item><c>GET /challenge/{scheme}</c>: Initiates an external authentication flow (OIDC/OAuth).</item>
    /// <item><c>GET /external-callback</c>: Handles the callback from an external identity provider.</item>
    /// </list>
    /// </remarks>
    public static IEndpointRouteBuilder MapDabblerSecurityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth").WithTags("Authentication");

        group.MapPost("/send-magic-link", async (
            [FromBody] SendMagicLinkRequest request,
            [FromServices] IMagicLinkService magicLinkService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            var result = await magicLinkService.IssueAsync(request.Email, request.BaseVerifyUrl, ipAddress, ct);
            return Results.Ok(result);
        });

        group.MapPost("/verify", async (
            [FromBody] VerifyMagicLinkRequest request,
            [FromServices] IMagicLinkService magicLinkService,
            [FromServices] IUserService userService,
            HttpContext context,
            CancellationToken ct) =>
        {
            MagicLinkVerifyResult result;
            if (!string.IsNullOrEmpty(request.Token))
            {
                result = await magicLinkService.VerifyByTokenAsync(request.Token, ct);
            }
            else if (!string.IsNullOrEmpty(request.Email) && !string.IsNullOrEmpty(request.Code))
            {
                result = await magicLinkService.VerifyByCodeAsync(request.Email, request.Code, ct);
            }
            else
            {
                return Results.BadRequest("Must provide either token or email+code");
            }

            if (!result.Success)
            {
                return Results.BadRequest(result.Reason);
            }

            if (result.RequiresMfa)
            {
                // Issue a temporary cookie or just return a flag indicating MFA is required
                // For simplicity in this design, we'll return the requirement and expect the client
                // to call /verify-totp next. In a real app, you might issue a partial-auth cookie.
                return Results.Ok(new { RequiresMfa = true, UserId = result.UserId });
            }

            if (result.UserId.HasValue)
            {
                var user = await userService.GetByIdCachedAsync(result.UserId.Value);
                if (user != null)
                {
                    await SignInUserAsync(
                        context,
                        context.RequestServices.GetService<IUserSessionContextService>(),
                        user,
                        ct);
                    return Results.Ok(new { RequiresMfa = false, Success = true });
                }
            }

            return Results.BadRequest("User not found");
        });

        group.MapPost("/verify-totp", async (
            [FromBody] VerifyTotpRequest request,
            [FromServices] IMfaService mfaService,
            [FromServices] IUserService userService,
            HttpContext context) =>
        {
            var isValid = await mfaService.ValidateTotpAsync(request.UserId, request.Code);
            if (!isValid)
            {
                return Results.BadRequest("Invalid TOTP code");
            }

            var user = await userService.GetByIdCachedAsync(request.UserId);
            if (user != null)
            {
                await SignInUserAsync(
                    context,
                    context.RequestServices.GetService<IUserSessionContextService>(),
                    user);
                return Results.Ok(new { Success = true });
            }

            return Results.BadRequest("User not found");
        });

        group.MapPost("/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok();
        }).RequireAuthorization();

        // --- External authentication (OIDC / OAuth) endpoints ---

        group.MapGet("/challenge/{scheme}", async (
            string scheme,
            [FromQuery] string? returnUrl,
            [FromServices] IOptions<DabblerSecurityOptions> optionsAccessor,
            HttpContext context) =>
        {
            var options = optionsAccessor.Value;
            var provider = options.ExternalProviders.Find(p => p.AuthenticationScheme == scheme);
            if (provider == null)
            {
                return Results.BadRequest($"Unknown authentication scheme: {scheme}");
            }

            // Sanitize returnUrl to prevent open-redirect attacks
            var safeReturnUrl = IsLocalUrl(returnUrl) ? returnUrl! : "/";

            var properties = new AuthenticationProperties
            {
                RedirectUri = $"/auth/external-callback?returnUrl={Uri.EscapeDataString(safeReturnUrl)}",
                Items = { ["scheme"] = scheme, ["provider"] = provider.ProviderName }
            };

            await context.ChallengeAsync(scheme, properties);
            return Results.Empty;
        });

        group.MapGet("/external-callback", async (
            [FromQuery] string? returnUrl,
            [FromServices] IExternalLoginService externalLoginService,
            [FromServices] IOptions<DabblerSecurityOptions> optionsAccessor,
            [FromServices] ILoggerFactory loggerFactory,
            HttpContext context) =>
        {
            var logger = loggerFactory.CreateLogger("Dabbler.Security.ExternalAuth");

            var authResult = await context.AuthenticateAsync(ExternalProviderConfig.ExternalAuthScheme);
            if (!authResult.Succeeded || authResult.Principal == null)
            {
                logger.LogWarning("External authentication failed: {Failure}",
                    authResult.Failure?.Message ?? "No principal");
                return Results.Redirect("/login?error=external-auth-failed");
            }

            // Determine which provider was used from the auth properties
            var scheme = authResult.Properties?.Items["scheme"];
            var providerName = authResult.Properties?.Items["provider"];
            if (string.IsNullOrEmpty(scheme) || string.IsNullOrEmpty(providerName))
            {
                logger.LogWarning("External callback missing scheme/provider in auth properties");
                return Results.Redirect("/login?error=external-auth-failed");
            }

            var options = optionsAccessor.Value;
            var providerConfig = options.ExternalProviders.Find(p => p.AuthenticationScheme == scheme);
            if (providerConfig == null)
            {
                logger.LogWarning("Provider config not found for scheme {Scheme}", scheme);
                return Results.Redirect("/login?error=external-auth-failed");
            }

            // Extract user info using provider-specific claim mapping
            ExternalClaimMapper.ExternalUserInfo userInfo;
            try
            {
                userInfo = ExternalClaimMapper.ExtractUserInfo(providerName, authResult.Principal);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Failed to extract user info from external claims");
                return Results.Redirect("/login?error=external-auth-failed");
            }

            // Resolve or provision the AppUser
            var user = await externalLoginService.FindByExternalLoginAsync(providerName, userInfo.SubjectId);

            if (user == null)
            {
                var autoProvision = providerConfig.OidcOptions?.AutoProvisionUsers
                                    ?? providerConfig.OAuthOptions?.AutoProvisionUsers
                                    ?? false;

                if (!autoProvision)
                {
                    logger.LogInformation("Auto-provisioning disabled; unknown user from {Provider} with subject {SubjectId}",
                        providerName, userInfo.SubjectId);
                    return Results.Redirect("/login?error=user-not-provisioned");
                }

                var defaultRoles = providerConfig.OidcOptions?.DefaultRoles
                                   ?? providerConfig.OAuthOptions?.DefaultRoles
                                   ?? [];

                try
                {
                    user = await externalLoginService.ProvisionAndLinkAsync(
                        providerName,
                        userInfo.SubjectId,
                        userInfo.Email,
                        userInfo.DisplayName,
                        defaultRoles);
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogWarning(ex, "Failed to provision user from {Provider}", providerName);
                    return Results.Redirect("/login?error=provisioning-failed");
                }
            }

            // Issue the application cookie (same mechanism as magic link flow)
            await SignInUserAsync(
                context,
                context.RequestServices.GetService<IUserSessionContextService>(),
                user);

            // Clean up the temporary external cookie
            await context.SignOutAsync(ExternalProviderConfig.ExternalAuthScheme);

            logger.LogInformation("External login successful: {Provider} user {UserId}",
                providerName, user.Id);

            var safeReturnUrl = IsLocalUrl(returnUrl) ? returnUrl! : "/";
            return Results.Redirect(safeReturnUrl);
        });

        return endpoints;
    }

    private static async Task SignInUserAsync(
        HttpContext context,
        IUserSessionContextService? sessionContextService,
        Entities.AppUser user,
        CancellationToken ct = default)
    {
        ClaimsPrincipal? principal = null;
        if (sessionContextService is not null)
        {
            principal = await sessionContextService.CreatePrincipalAsync(
                user.Id,
                CookieAuthenticationDefaults.AuthenticationScheme,
                requestedOrganizationId: null,
                overrideRoles: null,
                ct);
        }

        if (principal is null)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.DisplayName)
            };

            claims.AddRange(
                user.Roles
                    .Where(role => role.Role is not null)
                    .Select(role => new Claim(ClaimTypes.Role, role.Role!.Name)));

            principal = new ClaimsPrincipal(
                new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        }

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true
            });
    }

    /// <summary>
    /// Validates that a return URL is local to prevent open-redirect attacks.
    /// </summary>
    private static bool IsLocalUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return false;

        // Must start with / and not with // (protocol-relative) or /\ (backslash trick)
        return url.StartsWith('/') && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));
    }
}

/// <summary>
/// Represents the request body for sending a magic link.
/// </summary>
public class SendMagicLinkRequest
{
    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base URL for the verification link that will be sent to the user.
    /// The magic link service will append the token to this URL.
    /// </summary>
    public string BaseVerifyUrl { get; set; } = string.Empty;
}

/// <summary>
/// Represents the request body for verifying a magic link.
/// </summary>
public class VerifyMagicLinkRequest
{
    /// <summary>
    /// Gets or sets the full magic link token.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Gets or sets the user's email address. Used in conjunction with <see cref="Code"/>.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the short code. Used in conjunction with <see cref="Email"/>.
    /// </summary>
    public string? Code { get; set; }
}

/// <summary>
/// Represents the request body for verifying a TOTP code.
/// </summary>
public class VerifyTotpRequest
{
    /// <summary>
    /// Gets or sets the ID of the user attempting to verify.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the TOTP code from the user's authenticator app.
    /// </summary>
    public string Code { get; set; } = string.Empty;
}
