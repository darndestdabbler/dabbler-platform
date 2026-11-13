namespace Dabbler.Security.Authorization;

/// <summary>
/// DI contract that surfaces the baked-in roles for this deployment.
///
/// The platform always supplies the five canonical roles
/// (<see cref="BakedInRoles.SystemAdmin"/>,
/// <see cref="BakedInRoles.CrossOrgUserAdmin"/>,
/// <see cref="BakedInRoles.CrossOrgMonitor"/>,
/// <see cref="BakedInRoles.UserAdmin"/>,
/// <see cref="BakedInRoles.User"/>) through
/// <c>PlatformBakedInRoleProvider</c>. An application may replace or
/// decorate this service to register additional <b>non-user-type</b>
/// baked-in roles with their own compiled privilege profiles. User-type
/// roles are JSON-configured and are not surfaced through this contract.
/// </summary>
public interface IBakedInRoleProvider
{
    /// <summary>
    /// Returns the complete set of baked-in roles the handlers should
    /// consult, in privilege-descending order for easy inspection.
    /// </summary>
    IReadOnlyList<BakedInRoleDefinition> GetBakedInRoles();

    /// <summary>
    /// Returns the baked-in role with the given canonical name, or
    /// <c>null</c> when the name is not one of the registered baked-in
    /// roles. Comparison is case-insensitive.
    /// </summary>
    BakedInRoleDefinition? TryGetBakedInRole(string canonicalName);
}
