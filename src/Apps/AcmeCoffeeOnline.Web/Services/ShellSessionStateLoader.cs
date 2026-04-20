using System.Security.Claims;
using AcmeCoffeeOnline.Shared.DTOs;
using Dabbler.Security.Authorization;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace AcmeCoffeeOnline.Web.Services;

public sealed record ShellSessionState(
    string UserId,
    string UserName,
    IReadOnlyList<string> UserRoles,
    bool IsAdmin,
    bool IsSystemAdmin,
    bool CanReachSecurityDashboard,
    string? ActiveOrganizationKey,
    string? ActiveOrganizationName,
    IReadOnlyList<OrganizationOptionDto> OrganizationOptions);

public sealed class ShellSessionStateLoader
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly OrganizationOptionsApiClient _organizationOptionsApiClient;

    public ShellSessionStateLoader(
        AuthenticationStateProvider authenticationStateProvider,
        OrganizationOptionsApiClient organizationOptionsApiClient)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _organizationOptionsApiClient = organizationOptionsApiClient;
    }

    public async Task<ShellSessionState> LoadAsync(CancellationToken cancellationToken = default)
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        var userRoles = user.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(roleName => roleName)
            .ToArray();

        var rolesSet = new HashSet<string>(userRoles, StringComparer.OrdinalIgnoreCase);
        var isSystemAdmin = rolesSet.Contains("SystemAdmin");

        IReadOnlyList<OrganizationOptionDto> organizationOptions = [];
        if (user.Identity?.IsAuthenticated == true)
        {
            organizationOptions = await _organizationOptionsApiClient.GetOptionsAsync(cancellationToken);
        }

        // Show admin nav if the user has BackofficeUserAdmin in any org membership,
        // not just the currently active one. They still need to switch to that org
        // to use admin features, but the nav group should be discoverable.
        var isAdmin = isSystemAdmin
            || rolesSet.Contains("BackofficeUserAdmin")
            || organizationOptions.Any(o => o.RoleNames.Contains("BackofficeUserAdmin", StringComparer.OrdinalIgnoreCase));

        var canReachSecurityDashboard = BakedInRoles.All.Any(bakedIn =>
            rolesSet.Contains(bakedIn, StringComparer.OrdinalIgnoreCase));

        return new ShellSessionState(
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
            user.FindFirst(ClaimTypes.Name)?.Value
                ?? user.FindFirst(ClaimTypes.Email)?.Value
                ?? "User",
            userRoles,
            isAdmin,
            isSystemAdmin,
            canReachSecurityDashboard,
            user.FindFirstValue(UserSessionContextConstants.ActiveOrganizationKeyClaimType),
            user.FindFirstValue(UserSessionContextConstants.ActiveOrganizationNameClaimType),
            organizationOptions);
    }
}
