using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Dabbler.Platform.Stubs;

/// <summary>
/// Mutable authentication-state provider for stub-backed tests and demo hosts.
/// </summary>
public sealed class StubAuthenticationStateProvider : AuthenticationStateProvider
{
    private AuthenticationState _state;

    /// <summary>
    /// Initializes the provider with a user context.
    /// </summary>
    public StubAuthenticationStateProvider(StubUserContext userContext)
    {
        ArgumentNullException.ThrowIfNull(userContext);
        _state = new AuthenticationState(BuildPrincipal(userContext));
    }

    /// <inheritdoc />
    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_state);

    /// <summary>
    /// Replaces the current stub user context and notifies consumers.
    /// </summary>
    public void SetUser(StubUserContext userContext)
    {
        ArgumentNullException.ThrowIfNull(userContext);
        _state = new AuthenticationState(BuildPrincipal(userContext));
        NotifyAuthenticationStateChanged(Task.FromResult(_state));
    }

    private static ClaimsPrincipal BuildPrincipal(StubUserContext userContext)
    {
        if (!userContext.IsAuthenticated)
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userContext.UserId),
            new(ClaimTypes.Name, userContext.DisplayName),
            new(ClaimTypes.Email, userContext.Email)
        };

        claims.AddRange(userContext.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        if (!string.IsNullOrWhiteSpace(userContext.OrganizationId))
        {
            claims.Add(new Claim("OrganizationId", userContext.OrganizationId));
        }

        claims.AddRange(userContext.AdditionalClaims);

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Stub"));
    }
}
