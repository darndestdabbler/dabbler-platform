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
    private readonly SecurityConfigurationManager _manager;
    private readonly UnregisteredResourcePolicy _unregisteredPolicy;
    private readonly IRoleSwitcher _roleSwitcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="PageAuthorizationHandler"/> class.
    /// </summary>
    /// <param name="manager">The security configuration manager.</param>
    /// <param name="options">The Dabbler security options.</param>
    /// <param name="roleSwitcher">The service for determining a user's effective roles.</param>
    public PageAuthorizationHandler(
        SecurityConfigurationManager manager,
        IOptions<DabblerSecurityOptions> options,
        IRoleSwitcher roleSwitcher)
    {
        _manager = manager;
        _unregisteredPolicy = options.Value.UnregisteredResourcePolicy;
        _roleSwitcher = roleSwitcher;
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
                context.Succeed(requirement);
            return;
        }

        var userRoles = GetEffectiveRoles(context.User, config);

        // Deny takes precedence
        if (permission.DeniedRoles.Any(r => userRoles.Contains(r)))
            return;

        if (permission.AllowedRoles.Any(r => userRoles.Contains(r)))
            context.Succeed(requirement);
    }

    private IReadOnlySet<string> GetEffectiveRoles(ClaimsPrincipal user, SecurityConfiguration config)
    {
        var directRoles = _roleSwitcher.EffectiveRoles(user);
        return RoleInheritanceHelper.ExpandRoles(directRoles, config.Roles);
    }
}
