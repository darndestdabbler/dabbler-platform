using System.Security.Claims;
using Dabbler.Security.Authorization.Requirements;
using Dabbler.Security.Configuration;
using Dabbler.Security.Options;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Dabbler.Security.Authorization.Handlers;

/// <summary>
/// Handles authorization for pages based on a <see cref="PagePermissionRequirement"/>.
/// </summary>
/// <remarks>
/// This handler checks if a user, based on their effective roles (including inherited roles),
/// is allowed to access a specific page.
/// Access is determined by consulting the loaded <see cref="SecurityConfiguration"/>.
/// If a page is not explicitly configured, access is determined by the <see cref="UnregisteredResourcePolicy"/>.
/// Denial rules take precedence over allow rules.
/// </remarks>
public class PageAuthorizationHandler : AuthorizationHandler<PagePermissionRequirement>
{
    private const string ScopedAdminRoute = "/admin/users";
    private const string ScopedAdminRoleName = "BackofficeUserAdmin";

    private readonly SecurityConfigurationManager _manager;
    private readonly UnregisteredResourcePolicy _unregisteredPolicy;
    private readonly IRoleSwitcher _roleSwitcher;
    private readonly IUserSessionContextService? _userSessionContextService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PageAuthorizationHandler"/> class.
    /// </summary>
    /// <param name="manager">The security configuration manager.</param>
    /// <param name="options">The Dabbler security options.</param>
    /// <param name="roleSwitcher">The service for determining a user's effective roles.</param>
    /// <param name="userSessionContextService">Optional session-context service used for scoped multi-organization page access checks.</param>
    public PageAuthorizationHandler(
        SecurityConfigurationManager manager,
        IOptions<DabblerSecurityOptions> options,
        IRoleSwitcher roleSwitcher,
        IUserSessionContextService? userSessionContextService = null)
    {
        _manager = manager;
        _unregisteredPolicy = options.Value.UnregisteredResourcePolicy;
        _roleSwitcher = roleSwitcher;
        _userSessionContextService = userSessionContextService;
    }

    /// <summary>
    /// Handles the authorization requirement for a page.
    /// </summary>
    /// <param name="context">The authorization handler context.</param>
    /// <param name="requirement">The page permission requirement to evaluate.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous handle operation.</returns>
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PagePermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        await _manager.EnsureLoadedAsync();
        var config = _manager.Current;

        var permission = config.PagePermissions
            .FirstOrDefault(p => string.Equals(p.PageKey, requirement.PageKey, StringComparison.OrdinalIgnoreCase));

        if (permission == null)
        {
            if (_unregisteredPolicy == UnregisteredResourcePolicy.FailOpen)
            {
                context.Succeed(requirement);
                return;
            }

            context.Fail(new AuthorizationFailureReason(
                this, $"Page '{requirement.PageKey}' is not registered and policy is FailClosed."));
            return;
        }

        var userRoles = GetEffectiveRoles(context.User, config);

        // Deny takes precedence
        if (permission.DeniedRoles.Any(r => userRoles.Contains(r)))
        {
            context.Fail(new AuthorizationFailureReason(
                this, $"User role is explicitly denied for page '{requirement.PageKey}'."));
            return;
        }

        if (permission.AllowedRoles.Any(r => userRoles.Contains(r)))
        {
            context.Succeed(requirement);
            return;
        }

        if (!string.Equals(requirement.PageKey, ScopedAdminRoute, StringComparison.OrdinalIgnoreCase))
        {
            context.Fail(new AuthorizationFailureReason(
                this, $"User has no allowed role for page '{requirement.PageKey}'."));
            return;
        }

        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            context.Fail(new AuthorizationFailureReason(
                this, "Unable to resolve user identifier for scoped admin page."));
            return;
        }

        if (_userSessionContextService is null)
        {
            context.Fail(new AuthorizationFailureReason(
                this, "Session context service is not available for scoped admin page."));
            return;
        }

        var sessionContext = await _userSessionContextService.BuildAsync(userId);
        if (sessionContext?.OrganizationMemberships.Any(organization =>
                organization.RoleNames.Contains(ScopedAdminRoleName, StringComparer.OrdinalIgnoreCase)) == true)
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail(new AuthorizationFailureReason(
            this, $"User lacks scoped admin role '{ScopedAdminRoleName}' in any organization."));
    }

    private IReadOnlySet<string> GetEffectiveRoles(ClaimsPrincipal user, SecurityConfiguration config)
    {
        var directRoles = _roleSwitcher.EffectiveRoles(user);
        return RoleInheritanceHelper.ExpandRoles(directRoles, config.Roles);
    }
}
