using System.Security.Claims;
using AcmeCoffeeOnline.Web.Configuration;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

namespace AcmeCoffeeOnline.Web.Services.DevTools;

/// <summary>
/// Exposes the same capabilities as the <c>/test-auth/*</c> endpoints to the
/// in-app Dev Tools menu, without the HTTP round-trip.
/// </summary>
/// <remarks>
/// The dev-tools-ui spec calls for a typed <c>HttpClient</c> wrapping the
/// loopback endpoints. In Blazor Server, a server-to-self HTTP call would need
/// the browser's auth cookie forwarded so <c>/test-auth/session-info</c> can
/// see the signed-in user, and <see cref="IHttpContextAccessor"/> returns null
/// during interactive circuit events. Calling the underlying services directly
/// avoids that plumbing while preserving the endpoint contracts for external
/// tooling. The endpoints remain available for PowerShell / CI use.
/// </remarks>
public sealed class TestAuthClient
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IUserSessionContextService _sessionContextService;
    private readonly DevEmailInbox _emailInbox;
    private readonly IOptions<AppAuthenticationOptions> _authOptions;

    public TestAuthClient(
        AuthenticationStateProvider authStateProvider,
        IUserSessionContextService sessionContextService,
        DevEmailInbox emailInbox,
        IOptions<AppAuthenticationOptions> authOptions)
    {
        _authStateProvider = authStateProvider;
        _sessionContextService = sessionContextService;
        _emailInbox = emailInbox;
        _authOptions = authOptions;
    }

    public bool IsEnabled => _authOptions.Value.SeededSession.Enabled;

    // Mirrors the response shape of the /test-auth/session-info endpoint in
    // Program.cs. DevToolsEndpointContractTests guards the endpoint's key set;
    // when adding a field here, add it to the endpoint and the contract test too.
    public async Task<SessionInfoSnapshot> GetSessionInfoAsync(CancellationToken ct = default)
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        var isAuthenticated = user.Identity?.IsAuthenticated ?? false;

        UserSessionContext? sessionContext = null;
        if (isAuthenticated
            && int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            var activeOrg = ParseActiveOrganizationId(user);
            sessionContext = await _sessionContextService.BuildAsync(userId, activeOrg, ct);
        }

        var memberships = sessionContext?.OrganizationMemberships
            .Select(organization => new SessionOrganizationSnapshot(
                organization.OrganizationId,
                organization.OrganizationKey,
                organization.OrganizationName,
                organization.OrganizationMembershipId,
                organization.IsDefault,
                organization.IsInternal,
                organization.RoleNames))
            .ToArray() ?? [];

        return new SessionInfoSnapshot(
            IsAuthenticated: isAuthenticated,
            UserId: user.FindFirstValue(ClaimTypes.NameIdentifier),
            Name: user.FindFirstValue(ClaimTypes.Name),
            Email: user.FindFirstValue(ClaimTypes.Email),
            CustomerId: user.FindFirstValue("CustomerId"),
            ActiveOrganizationId: user.FindFirstValue(UserSessionContextConstants.ActiveOrganizationIdClaimType),
            ActiveOrganizationKey: user.FindFirstValue(UserSessionContextConstants.ActiveOrganizationKeyClaimType),
            ActiveOrganizationName: user.FindFirstValue(UserSessionContextConstants.ActiveOrganizationNameClaimType),
            AvailableOrganizations: memberships,
            Roles: user.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray());
    }

    public IReadOnlyList<DevEmailMessage> GetMagicLinks(string? emailFilter = null)
    {
        return _emailInbox.GetAll()
            .Where(message => message.EmailType == "MagicLink")
            .Where(message => string.IsNullOrWhiteSpace(emailFilter)
                || string.Equals(message.Email, emailFilter, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(message => message.CreatedAtUtc)
            .ToArray();
    }

    public void ClearMagicLinks()
    {
        _emailInbox.Clear();
    }

    private static int? ParseActiveOrganizationId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(UserSessionContextConstants.ActiveOrganizationIdClaimType);
        return int.TryParse(claim, out var id) ? id : null;
    }
}

public sealed record SessionInfoSnapshot(
    bool IsAuthenticated,
    string? UserId,
    string? Name,
    string? Email,
    string? CustomerId,
    string? ActiveOrganizationId,
    string? ActiveOrganizationKey,
    string? ActiveOrganizationName,
    IReadOnlyList<SessionOrganizationSnapshot> AvailableOrganizations,
    IReadOnlyList<string> Roles);

public sealed record SessionOrganizationSnapshot(
    int OrganizationId,
    string OrganizationKey,
    string OrganizationName,
    int? OrganizationMembershipId,
    bool IsDefault,
    bool IsInternal,
    IReadOnlyList<string> Roles);
