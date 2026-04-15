using System.Security.Claims;
using AcmeCoffeeOnline.Shared.DTOs;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace AcmeCoffeeOnline.Web.Services;

public sealed record ShellSessionState(
    string UserId,
    string UserName,
    IReadOnlyList<string> UserRoles,
    bool IsAdmin,
    bool IsSystemAdmin,
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
        var isAdmin = isSystemAdmin || rolesSet.Contains("BackofficeUserAdmin");

        IReadOnlyList<OrganizationOptionDto> organizationOptions = [];
        if (user.Identity?.IsAuthenticated == true)
        {
            organizationOptions = await _organizationOptionsApiClient.GetOptionsAsync(cancellationToken);
        }

        return new ShellSessionState(
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
            user.FindFirst(ClaimTypes.Name)?.Value
                ?? user.FindFirst(ClaimTypes.Email)?.Value
                ?? "User",
            userRoles,
            isAdmin,
            isSystemAdmin,
            user.FindFirstValue(UserSessionContextConstants.ActiveOrganizationKeyClaimType),
            user.FindFirstValue(UserSessionContextConstants.ActiveOrganizationNameClaimType),
            organizationOptions);
    }
}
