using Dabbler.Security.Authorization.Requirements;
using Dabbler.Security.Configuration;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Authorization;

namespace Dabbler.Security.Authorization.Handlers;

/// <summary>
/// Page authorization under the restriction-based model introduced by the
/// <c>rearchitect-role-administration-foundations</c> session set.
/// </summary>
/// <remarks>
/// <para>
/// Baked-in roles (SystemAdmin / CrossOrgUserAdmin / CrossOrgMonitor /
/// UserAdmin / User) are granted page access unconditionally. The baked-in
/// <see cref="BakedInRolePrivileges"/> profile governs what those roles may
/// *do* on data, not which pages they can reach.
/// </para>
/// <para>
/// User-type (app-defined) roles are default-permissive. Access is denied
/// only when the role carries a <see cref="UiRestriction"/> whose
/// <see cref="UiRestriction.PageKey"/> matches the requirement,
/// <see cref="UiRestriction.ControlKey"/> equals the entire-page wildcard
/// (<c>"*"</c>), and <see cref="UiRestriction.Kind"/> is
/// <see cref="UiRestrictionKind.Hidden"/>. A
/// <see cref="UiRestrictionKind.Disabled"/> entry still succeeds — the page
/// renders, but its controls render in a non-interactive state.
/// </para>
/// <para>
/// Role inheritance is not consulted; a user assigned to multiple roles must
/// switch explicitly to operate under each one. See
/// <c>docs/planning/project-guidance.md</c> Principles → Security And
/// Organization Model.
/// </para>
/// </remarks>
public class PageAuthorizationHandler : AuthorizationHandler<PagePermissionRequirement>
{
    private const string WildcardControlKey = "*";

    private readonly SecurityConfigurationManager _manager;
    private readonly IRoleSwitcher _roleSwitcher;
    private readonly IBakedInRoleProvider _bakedInRoleProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PageAuthorizationHandler"/> class.
    /// </summary>
    /// <param name="manager">The security configuration manager.</param>
    /// <param name="roleSwitcher">The scoped role switcher that yields the user's active role(s).</param>
    /// <param name="bakedInRoleProvider">The baked-in role provider consulted before any JSON-configured restriction lookup.</param>
    public PageAuthorizationHandler(
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
        PagePermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        await _manager.EnsureLoadedAsync();
        var config = _manager.Current;

        foreach (var roleName in _roleSwitcher.EffectiveRoles(context.User))
        {
            if (IsAllowed(roleName, requirement.PageKey, config))
            {
                context.Succeed(requirement);
                return;
            }
        }
    }

    private bool IsAllowed(string roleName, string pageKey, SecurityConfiguration config)
    {
        if (_bakedInRoleProvider.TryGetBakedInRole(roleName) is not null)
            return true;

        // User-type roles are default-permissive. A role that does not appear in
        // UserTypeRoles has no explicit restrictions and therefore succeeds.
        var userTypeRole = config.UserTypeRoles.FirstOrDefault(r =>
            string.Equals(r.RoleName, roleName, StringComparison.OrdinalIgnoreCase));
        if (userTypeRole is null)
            return true;

        var entirePageRestriction = userTypeRole.UiRestrictions.FirstOrDefault(r =>
            string.Equals(r.PageKey, pageKey, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.ControlKey, WildcardControlKey, StringComparison.Ordinal));

        return entirePageRestriction?.Kind != UiRestrictionKind.Hidden;
    }
}
