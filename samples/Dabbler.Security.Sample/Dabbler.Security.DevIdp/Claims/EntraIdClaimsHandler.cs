using System.Security.Claims;
using Dabbler.Security.DevIdp.Config;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OidcClaims = OpenIddict.Abstractions.OpenIddictConstants.Claims;
using OidcDestinations = OpenIddict.Abstractions.OpenIddictConstants.Destinations;
using OidcScopes = OpenIddict.Abstractions.OpenIddictConstants.Scopes;

namespace Dabbler.Security.DevIdp.Claims;

/// <summary>
/// Builds ClaimsIdentity instances that emit Entra ID-compatible claims
/// (oid, tid, roles, preferred_username, name, email) into both ID and access tokens.
/// </summary>
public class EntraIdClaimsHandler
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly DevIdpOptions _options;

    public EntraIdClaimsHandler(
        UserManager<IdentityUser> userManager,
        IOptions<DevIdpOptions> options)
    {
        _userManager = userManager;
        _options = options.Value;
    }

    public async Task<ClaimsIdentity> CreateClaimsIdentityAsync(
        IdentityUser user, IEnumerable<string> scopes)
    {
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: OidcClaims.Name,
            roleType: OidcClaims.Role);

        var userId = await _userManager.GetUserIdAsync(user);
        var email = await _userManager.GetEmailAsync(user);
        var roles = await _userManager.GetRolesAsync(user);
        var userClaims = await _userManager.GetClaimsAsync(user);
        var displayName = userClaims.FirstOrDefault(c => c.Type == "name")?.Value ?? email;

        // Standard OpenIddict claims
        identity.SetClaim(OidcClaims.Subject, userId);
        identity.SetClaim(OidcClaims.Name, displayName);
        identity.SetClaim(OidcClaims.Email, email);
        identity.SetClaim(OidcClaims.PreferredUsername, email);

        // Entra ID-compatible role claims — "roles" (plural) matches Entra ID convention
        foreach (var role in roles)
        {
            identity.AddClaim(new Claim("roles", role));
        }

        // Entra ID-compatible custom claims
        identity.AddClaim(new Claim("oid", userId));
        identity.AddClaim(new Claim("tid", _options.TenantId));

        // Set scopes and claim destinations
        identity.SetScopes(scopes);
        identity.SetDestinations(GetDestinations);

        return identity;
    }

    public static IEnumerable<string> GetDestinations(Claim claim)
    {
        switch (claim.Type)
        {
            case OidcClaims.Name or OidcClaims.PreferredUsername:
                yield return OidcDestinations.AccessToken;
                if (claim.Subject!.HasScope(OidcScopes.Profile))
                    yield return OidcDestinations.IdentityToken;
                yield break;

            case OidcClaims.Email:
                yield return OidcDestinations.AccessToken;
                if (claim.Subject!.HasScope(OidcScopes.Email))
                    yield return OidcDestinations.IdentityToken;
                yield break;

            case "roles":
                yield return OidcDestinations.AccessToken;
                if (claim.Subject!.HasScope("roles"))
                    yield return OidcDestinations.IdentityToken;
                yield break;

            // oid and tid always go to both tokens (required for Entra ID compatibility)
            case "oid" or "tid":
                yield return OidcDestinations.AccessToken;
                yield return OidcDestinations.IdentityToken;
                yield break;

            // Never expose the security stamp
            case "AspNet.Identity.SecurityStamp":
                yield break;

            default:
                yield return OidcDestinations.AccessToken;
                yield break;
        }
    }
}
