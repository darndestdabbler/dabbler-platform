namespace Dabbler.Security.Configuration;

/// <summary>
/// Defines which roles are authorized to use the application's role-switching functionality.
/// </summary>
/// <remarks>
/// Legacy grant-based type. Role-switching privileges now come from compiled
/// baked-in role rules; organization-switching configuration moved to
/// <see cref="OrganizationSwitching"/>. Scheduled for removal in Session 3 of
/// <c>rearchitect-role-administration-foundations</c>.
/// </remarks>
[Obsolete("Replaced by OrganizationSwitching plus baked-in role privileges. Scheduled for removal in Session 3 of rearchitect-role-administration-foundations.")]
public sealed class RoleSwitchingPermission
{
    /// <summary>
    /// Roles that are permitted to use the role switcher (DabblerRoleSwitcher component).
    /// Typically only SystemAdministrator, but configurable.
    /// </summary>
    public List<string> AllowedRoles { get; set; } = [];
}
