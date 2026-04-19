using Dabbler.Security.Authorization.Requirements;
using Dabbler.Security.Configuration;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Authorization;

namespace Dabbler.Security.Authorization.Handlers;

/// <summary>
/// API endpoint authorization under the restriction-based model.
/// </summary>
/// <remarks>
/// Consults baked-in role privileges first; any baked-in role is granted
/// endpoint access. The new model does not express endpoint-level
/// restrictions for user-type roles — user-type roles are
/// default-permissive at the endpoint layer. Data-level narrowing happens
/// through column and row restrictions; admin-scoped action gating happens
/// through baked-in role privileges. A future session set may add
/// request-method gating (for example, blocking writes for
/// <see cref="CrossOrgMonitorPrivileges"/>). Role inheritance is never
/// consulted.
/// </remarks>
public class EndpointAuthorizationHandler : AuthorizationHandler<EndpointPermissionRequirement>
{
    private readonly SecurityConfigurationManager _manager;
    private readonly IRoleSwitcher _roleSwitcher;
    private readonly IBakedInRoleProvider _bakedInRoleProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointAuthorizationHandler"/> class.
    /// </summary>
    /// <param name="manager">The security configuration manager.</param>
    /// <param name="roleSwitcher">The scoped role switcher that yields the user's active role(s).</param>
    /// <param name="bakedInRoleProvider">The baked-in role provider consulted before any JSON-configured restriction lookup.</param>
    public EndpointAuthorizationHandler(
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
        EndpointPermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        await _manager.EnsureLoadedAsync();
        var config = _manager.Current;

        foreach (var roleName in _roleSwitcher.EffectiveRoles(context.User))
        {
            if (IsAllowed(roleName, config))
            {
                context.Succeed(requirement);
                return;
            }
        }
    }

    private bool IsAllowed(string roleName, SecurityConfiguration config)
    {
        if (_bakedInRoleProvider.TryGetBakedInRole(roleName) is not null)
            return true;

        // User-type roles are default-permissive at the endpoint layer in the new
        // model. Data-level narrowing is handled by column and row restrictions,
        // and admin-scoped action gating is handled by baked-in role privileges;
        // there is no user-type endpoint restriction type to consult here.
        _ = config;
        return true;
    }
}
