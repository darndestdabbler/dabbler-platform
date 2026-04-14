using System.Security.Claims;
using Dabbler.Security.Authorization.Requirements;
using Dabbler.Security.Configuration;
using Dabbler.Security.Options;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Dabbler.Security.Authorization.Handlers;

/// <summary>
/// Handles authorization for API endpoints based on an <see cref="EndpointPermissionRequirement"/>.
/// </summary>
/// <remarks>
/// This handler checks if a user, based on their effective roles (including inherited roles),
/// is allowed to access a specific API endpoint.
/// Access is determined by consulting the loaded <see cref="SecurityConfiguration"/>.
/// If an endpoint is not explicitly configured, access is determined by the <see cref="UnregisteredResourcePolicy"/>.
/// </remarks>
public class EndpointAuthorizationHandler : AuthorizationHandler<EndpointPermissionRequirement>
{
    private readonly SecurityConfigurationManager _manager;
    private readonly UnregisteredResourcePolicy _unregisteredPolicy;
    private readonly IRoleSwitcher _roleSwitcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointAuthorizationHandler"/> class.
    /// </summary>
    /// <param name="manager">The security configuration manager.</param>
    /// <param name="options">The Dabbler security options.</param>
    /// <param name="roleSwitcher">The service for determining a user's effective roles.</param>
    public EndpointAuthorizationHandler(
        SecurityConfigurationManager manager,
        IOptions<DabblerSecurityOptions> options,
        IRoleSwitcher roleSwitcher)
    {
        _manager = manager;
        _unregisteredPolicy = options.Value.UnregisteredResourcePolicy;
        _roleSwitcher = roleSwitcher;
    }

    /// <summary>
    /// Handles the authorization requirement for an API endpoint.
    /// </summary>
    /// <param name="context">The authorization handler context.</param>
    /// <param name="requirement">The endpoint permission requirement to evaluate.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous handle operation.</returns>
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        EndpointPermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        await _manager.EnsureLoadedAsync();
        var config = _manager.Current;

        var permission = config.EndpointPermissions
            .FirstOrDefault(e => string.Equals(e.EndpointKey, requirement.EndpointKey, StringComparison.OrdinalIgnoreCase));

        if (permission == null)
        {
            if (_unregisteredPolicy == UnregisteredResourcePolicy.FailOpen)
                context.Succeed(requirement);
            return;
        }

        var userRoles = GetEffectiveRoles(context.User, config);

        if (permission.AllowedRoles.Any(r => userRoles.Contains(r)))
            context.Succeed(requirement);
    }

    private IReadOnlySet<string> GetEffectiveRoles(ClaimsPrincipal user, SecurityConfiguration config)
    {
        var directRoles = _roleSwitcher.EffectiveRoles(user);
        return RoleInheritanceHelper.ExpandRoles(directRoles, config.Roles);
    }
}
