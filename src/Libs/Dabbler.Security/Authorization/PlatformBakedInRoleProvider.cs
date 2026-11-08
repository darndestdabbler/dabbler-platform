namespace Dabbler.Security.Authorization;

/// <summary>
/// Default <see cref="IBakedInRoleProvider"/> that returns the five canonical
/// baked-in roles with their compiled privilege profiles. Applications that
/// need additional non-user-type baked-in roles should register a decorator
/// or alternative <see cref="IBakedInRoleProvider"/> in DI.
/// </summary>
public sealed class PlatformBakedInRoleProvider : IBakedInRoleProvider
{
    private static readonly IReadOnlyList<BakedInRoleDefinition> _roles =
    [
        new BakedInRoleDefinition(
            BakedInRoles.SystemAdmin,
            "System Administrator",
            "Has every privilege in the application. The only role that can create, edit, or delete roles and configure role restrictions.",
            SystemAdminPrivileges.Instance),

        new BakedInRoleDefinition(
            BakedInRoles.CrossOrgUserAdmin,
            "Cross-Organization User Administrator",
            "Acts as a UserAdmin across every organization and can provide operational assistance on other organizations' behalf. May self-assign the role to cover absences.",
            CrossOrgUserAdminPrivileges.Instance),

        new BakedInRoleDefinition(
            BakedInRoles.CrossOrgMonitor,
            "Cross-Organization Monitor",
            "Read-only visibility across all organizations, including cross-organization reporting. Cannot create, update, delete, or assign roles.",
            CrossOrgMonitorPrivileges.Instance),

        new BakedInRoleDefinition(
            BakedInRoles.UserAdmin,
            "User Administrator",
            "Delegated user administration within the user's assigned organizations. Can assign any role to a user in those organizations but cannot manage roles or organizations.",
            UserAdminPrivileges.Instance),

        new BakedInRoleDefinition(
            BakedInRoles.User,
            "User",
            "Default user-type role for transactional CRUD within the user's organization(s). Default-permissive; explicit Column, Row, and UI restrictions narrow what this role may see or do.",
            UserPrivileges.Instance),
    ];

    /// <inheritdoc />
    public IReadOnlyList<BakedInRoleDefinition> GetBakedInRoles() => _roles;

    /// <inheritdoc />
    public BakedInRoleDefinition? TryGetBakedInRole(string canonicalName)
    {
        if (string.IsNullOrEmpty(canonicalName))
            return null;

        foreach (var role in _roles)
        {
            if (string.Equals(role.Name, canonicalName, StringComparison.OrdinalIgnoreCase))
                return role;
        }

        return null;
    }
}
