namespace Dabbler.Security.Configuration;

/// <summary>
/// Defines which roles are authorized to use the application's role-switching functionality.
/// </summary>
public sealed class RoleSwitchingPermission
{
    /// <summary>
    /// Roles that are permitted to use the role switcher (DabblerRoleSwitcher component).
    /// Typically only SystemAdministrator, but configurable.
    /// </summary>
    public List<string> AllowedRoles { get; set; } = [];
}
