using System.Security.Claims;
using Dabbler.Security.Authorization.Requirements;
using Dabbler.Security.Configuration;
using Dabbler.Security.Options;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Dabbler.Security.Authorization.Handlers;

/// <summary>
/// Handles authorization for UI controls based on a <see cref="ControlPermissionRequirement"/>.
/// </summary>
/// <remarks>
/// This handler determines if a user can view a specific UI control on a page. It succeeds if the resolved
/// state for the user is not <see cref="ControlState.Hidden"/>.
/// The resolved state is the most permissive state (<see cref="ControlState.Enabled"/> &gt; <see cref="ControlState.Disabled"/> &gt; <see cref="ControlState.Hidden"/>)
/// granted by any of the user's effective roles.
/// If a control is not explicitly configured, access is determined by the <see cref="UnregisteredResourcePolicy"/>.
/// </remarks>
public class ControlAuthorizationHandler : AuthorizationHandler<ControlPermissionRequirement>
{
    private readonly SecurityConfigurationManager _manager;
    private readonly UnregisteredResourcePolicy _unregisteredPolicy;
    private readonly IRoleSwitcher _roleSwitcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="ControlAuthorizationHandler"/> class.
    /// </summary>
    /// <param name="manager">The security configuration manager.</param>
    /// <param name="options">The Dabbler security options.</param>
    /// <param name="roleSwitcher">The service for determining a user's effective roles.</param>
    public ControlAuthorizationHandler(
        SecurityConfigurationManager manager,
        IOptions<DabblerSecurityOptions> options,
        IRoleSwitcher roleSwitcher)
    {
        _manager = manager;
        _unregisteredPolicy = options.Value.UnregisteredResourcePolicy;
        _roleSwitcher = roleSwitcher;
    }

    /// <summary>
    /// Handles the authorization requirement for a UI control.
    /// </summary>
    /// <param name="context">The authorization handler context.</param>
    /// <param name="requirement">The control permission requirement to evaluate.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous handle operation.</returns>
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ControlPermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        await _manager.EnsureLoadedAsync();
        var config = _manager.Current;

        var permission = config.ControlPermissions
            .FirstOrDefault(c =>
                string.Equals(c.PageKey, requirement.PageKey, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(c.ControlKey, requirement.ControlKey, StringComparison.OrdinalIgnoreCase));

        if (permission == null)
        {
            if (_unregisteredPolicy == UnregisteredResourcePolicy.FailOpen)
                context.Succeed(requirement);
            return;
        }

        var userRoles = GetEffectiveRoles(context.User, config);

        var resolvedState = ResolveControlState(permission, userRoles);

        // Succeed (visible) unless the resolved state is Hidden
        if (resolvedState != ControlState.Hidden)
            context.Succeed(requirement);
    }

    private static ControlState ResolveControlState(
        ControlPermission permission,
        IReadOnlySet<string> userRoles)
    {
        var matchingStates = permission.RolePermissions
            .Where(rp => userRoles.Contains(rp.RoleName))
            .Select(rp => rp.State)
            .ToList();

        if (matchingStates.Count == 0)
            return ControlState.Hidden;

        // Return the most permissive state (lowest enum value: Enabled=0, Disabled=1, Hidden=2)
        return (ControlState)matchingStates.Min(s => (int)s);
    }

    private IReadOnlySet<string> GetEffectiveRoles(ClaimsPrincipal user, SecurityConfiguration config)
    {
        var directRoles = _roleSwitcher.EffectiveRoles(user);
        return RoleInheritanceHelper.ExpandRoles(directRoles, config.Roles);
    }
}
