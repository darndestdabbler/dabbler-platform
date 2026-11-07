using Dabbler.Security.Authorization.Requirements;
using Dabbler.Security.Configuration;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Authorization;

namespace Dabbler.Security.Authorization.Handlers;

/// <summary>
/// UI control authorization under the restriction-based model.
/// </summary>
/// <remarks>
/// <para>
/// Baked-in roles always succeed. User-type roles consult the role's
/// <see cref="UiRestriction"/> list:
/// </para>
/// <list type="bullet">
///   <item>An entire-page <c>Hidden</c> restriction (<c>ControlKey == "*"</c>)
///     also hides every control on the page.</item>
///   <item>A per-control restriction with <see cref="UiRestrictionKind.Hidden"/>
///     fails authorization so the control is not rendered.</item>
///   <item>A per-control restriction with <see cref="UiRestrictionKind.Disabled"/>
///     succeeds; the control renders but the UI is responsible for the
///     non-interactive state.</item>
///   <item>Absence of a restriction succeeds (user-type roles are default-permissive).</item>
/// </list>
/// </remarks>
public class ControlAuthorizationHandler : AuthorizationHandler<ControlPermissionRequirement>
{
    private const string WildcardControlKey = "*";

    private readonly SecurityConfigurationManager _manager;
    private readonly IRoleSwitcher _roleSwitcher;
    private readonly IBakedInRoleProvider _bakedInRoleProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ControlAuthorizationHandler"/> class.
    /// </summary>
    /// <param name="manager">The security configuration manager.</param>
    /// <param name="roleSwitcher">The scoped role switcher that yields the user's active role(s).</param>
    /// <param name="bakedInRoleProvider">The baked-in role provider consulted before any JSON-configured restriction lookup.</param>
    public ControlAuthorizationHandler(
        SecurityConfigurationManager manager,
        IRoleSwitcher roleSwitcher,
        IBakedInRoleProvider bakedInRoleProvider)
    {
        _manager = manager;
        _roleSwitcher = roleSwitcher;
        _bakedInRoleProvider = bakedInRoleProvider;
    }

    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ControlPermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        await _manager.EnsureLoadedAsync();
        var config = _manager.Current;

        foreach (var roleName in _roleSwitcher.EffectiveRoles(context.User))
        {
            if (IsAllowed(roleName, requirement.PageKey, requirement.ControlKey, config))
            {
                context.Succeed(requirement);
                return;
            }
        }
    }

    private bool IsAllowed(string roleName, string pageKey, string controlKey, SecurityConfiguration config)
    {
        if (_bakedInRoleProvider.TryGetBakedInRole(roleName) is not null)
            return true;

        // User-type roles are default-permissive. A role absent from UserTypeRoles
        // has no restrictions configured and therefore succeeds.
        var userTypeRole = config.UserTypeRoles.FirstOrDefault(r =>
            string.Equals(r.RoleName, roleName, StringComparison.OrdinalIgnoreCase));
        if (userTypeRole is null)
            return true;

        var entirePageRestriction = userTypeRole.UiRestrictions.FirstOrDefault(r =>
            string.Equals(r.PageKey, pageKey, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.ControlKey, WildcardControlKey, StringComparison.Ordinal));
        if (entirePageRestriction?.Kind == UiRestrictionKind.Hidden)
            return false;

        var controlRestriction = userTypeRole.UiRestrictions.FirstOrDefault(r =>
            string.Equals(r.PageKey, pageKey, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.ControlKey, controlKey, StringComparison.OrdinalIgnoreCase));

        return controlRestriction?.Kind != UiRestrictionKind.Hidden;
    }
}
