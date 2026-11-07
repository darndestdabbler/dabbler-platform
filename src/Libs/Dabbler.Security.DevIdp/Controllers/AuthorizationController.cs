using System.Security.Claims;
using Dabbler.Security.DevIdp.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OidcClaims = OpenIddict.Abstractions.OpenIddictConstants.Claims;
using OidcErrors = OpenIddict.Abstractions.OpenIddictConstants.Errors;

namespace Dabbler.Security.DevIdp.Controllers;

public class AuthorizationController : Controller
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly EntraIdClaimsHandler _claimsHandler;

    public AuthorizationController(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        EntraIdClaimsHandler claimsHandler)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _claimsHandler = claimsHandler;
    }

    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException(
                "The OpenID Connect request cannot be retrieved.");

        // Check if the user is already authenticated via the Identity cookie
        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);

        if (result is not { Succeeded: true })
        {
            // Not logged in — redirect to the login page, preserving the OIDC parameters
            return Challenge(
                new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase + Request.Path
                        + QueryString.Create(
                            Request.HasFormContentType
                                ? Request.Form.ToList()
                                : Request.Query.ToList())
                },
                IdentityConstants.ApplicationScheme);
        }

        var user = await _userManager.GetUserAsync(result.Principal!)
            ?? throw new InvalidOperationException("The user details cannot be retrieved.");

        // Build the claims identity with Entra ID-compatible claims
        // Consent is auto-granted (ConsentType.Implicit on all dev clients)
        var identity = await _claimsHandler.CreateClaimsIdentityAsync(
            user, request.GetScopes());

        return SignIn(
            new ClaimsPrincipal(identity),
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/token")]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException(
                "The OpenID Connect request cannot be retrieved.");

        if (request.IsAuthorizationCodeGrantType())
        {
            // OpenIddict already validated the authorization code and
            // deserialized the principal stored in it
            var result = await HttpContext.AuthenticateAsync(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var user = await _userManager.FindByIdAsync(
                result.Principal!.GetClaim(OidcClaims.Subject)!);

            if (user is null)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OidcErrors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The token is no longer valid."
                    }));
            }

            // Rebuild the identity with fresh claims from the database
            var identity = await _claimsHandler.CreateClaimsIdentityAsync(
                user, result.Principal!.GetScopes());

            return SignIn(
                new ClaimsPrincipal(identity),
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new InvalidOperationException("The specified grant type is not supported.");
    }
}
