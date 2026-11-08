namespace Dabbler.Security.Authorization;

/// <summary>
/// Canonical, immutable names for the five platform baked-in roles.
/// These string values appear in claims, audit records, and handler logic,
/// and must never change across releases. Per-deployment rebranding is
/// done through each role's DisplayAlias rather than by renaming a role.
/// </summary>
public static class BakedInRoles
{
    /// <summary>Canonical name of the all-privileges platform administrator baked-in role.</summary>
    public const string SystemAdmin       = "SystemAdmin";

    /// <summary>Canonical name of the cross-organization user-administration baked-in role.</summary>
    public const string CrossOrgUserAdmin = "CrossOrgUserAdmin";

    /// <summary>Canonical name of the cross-organization read-only monitor baked-in role.</summary>
    public const string CrossOrgMonitor   = "CrossOrgMonitor";

    /// <summary>Canonical name of the delegated user-administration baked-in role.</summary>
    public const string UserAdmin         = "UserAdmin";

    /// <summary>Canonical name of the default user-type baked-in role.</summary>
    public const string User              = "User";

    /// <summary>
    /// All five canonical baked-in role names, in privilege-descending order.
    /// </summary>
    public static IReadOnlyList<string> All { get; } =
    [
        SystemAdmin,
        CrossOrgUserAdmin,
        CrossOrgMonitor,
        UserAdmin,
        User,
    ];

    /// <summary>
    /// Returns <c>true</c> when <paramref name="roleName"/> matches one of the
    /// baked-in canonical names. Comparison is case-insensitive to match the
    /// existing platform convention for role-name comparisons.
    /// </summary>
    public static bool IsBakedIn(string? roleName) =>
        !string.IsNullOrEmpty(roleName)
        && All.Contains(roleName, StringComparer.OrdinalIgnoreCase);
}
