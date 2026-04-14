using System.Security.Claims;
using Dabbler.Security.Authorization.Requirements;
using Dabbler.Security.Configuration;
using Dabbler.Security.Options;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using FieldAccessLevel = Dabbler.Security.Configuration.FieldAccessLevel;

namespace Dabbler.Security.Authorization.Handlers;

/// <summary>
/// Handles authorization for entity fields based on a <see cref="FieldPermissionRequirement"/>.
/// </summary>
/// <remarks>
/// This handler determines if a user can view a specific field. It succeeds if the resolved
/// access level for the user is not <see cref="FieldAccessLevel.Hidden"/>.
/// The resolved access level is the most permissive level (<see cref="FieldAccessLevel.Editable"/> &gt; <see cref="FieldAccessLevel.ReadOnly"/> &gt; <see cref="FieldAccessLevel.Hidden"/>)
/// granted by any of the user's effective roles.
/// If a field is not explicitly configured, access is determined by the <see cref="UnregisteredResourcePolicy"/>.
/// </remarks>
public class FieldAuthorizationHandler : AuthorizationHandler<FieldPermissionRequirement>
{
    private readonly SecurityConfigurationManager _manager;
    private readonly UnregisteredResourcePolicy _unregisteredPolicy;
    private readonly IRoleSwitcher _roleSwitcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldAuthorizationHandler"/> class.
    /// </summary>
    /// <param name="manager">The security configuration manager.</param>
    /// <param name="options">The Dabbler security options.</param>
    /// <param name="roleSwitcher">The service for determining a user's effective roles.</param>
    public FieldAuthorizationHandler(
        SecurityConfigurationManager manager,
        IOptions<DabblerSecurityOptions> options,
        IRoleSwitcher roleSwitcher)
    {
        _manager = manager;
        _unregisteredPolicy = options.Value.UnregisteredResourcePolicy;
        _roleSwitcher = roleSwitcher;
    }

    /// <summary>
    /// Handles the authorization requirement for an entity field.
    /// </summary>
    /// <param name="context">The authorization handler context.</param>
    /// <param name="requirement">The field permission requirement to evaluate.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous handle operation.</returns>
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        FieldPermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        await _manager.EnsureLoadedAsync();
        var config = _manager.Current;

        var permission = config.FieldPermissions
            .FirstOrDefault(f =>
                string.Equals(f.EntityKey, requirement.EntityKey, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(f.FieldKey, requirement.FieldKey, StringComparison.OrdinalIgnoreCase));

        if (permission == null)
        {
            if (_unregisteredPolicy == UnregisteredResourcePolicy.FailOpen)
                context.Succeed(requirement);
            return;
        }

        var userRoles = GetEffectiveRoles(context.User, config);

        // Resolve the most permissive access level across all of the user's roles.
        // Editable > ReadOnly > Hidden. Succeed unless every matching role says Hidden.
        var resolvedLevel = ResolveAccessLevel(permission, userRoles);

        if (resolvedLevel != FieldAccessLevel.Hidden)
            context.Succeed(requirement);
    }

    private static FieldAccessLevel ResolveAccessLevel(
        FieldPermission permission,
        IReadOnlySet<string> userRoles)
    {
        var matchingLevels = permission.RolePermissions
            .Where(rp => userRoles.Contains(rp.RoleName))
            .Select(rp => rp.AccessLevel)
            .ToList();

        if (matchingLevels.Count == 0)
            return FieldAccessLevel.Hidden;

        // Return the most permissive level (lowest enum value: Editable=0, ReadOnly=1, Hidden=2)
        return (FieldAccessLevel)matchingLevels.Min(l => (int)l);
    }

    private IReadOnlySet<string> GetEffectiveRoles(ClaimsPrincipal user, SecurityConfiguration config)
    {
        var directRoles = _roleSwitcher.EffectiveRoles(user);
        return RoleInheritanceHelper.ExpandRoles(directRoles, config.Roles);
    }
}
