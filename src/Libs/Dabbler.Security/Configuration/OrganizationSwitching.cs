namespace Dabbler.Security.Configuration;

/// <summary>
/// Per-deployment configuration for the organization-switching surface. The
/// baked-in SystemAdmin, CrossOrgUserAdmin, and CrossOrgMonitor roles can
/// always switch; other users are implicitly allowed to switch among the
/// organizations they are members of. This type exists to let a SystemAdmin
/// broaden or restrict the org-switcher UI beyond those defaults without
/// touching code.
/// </summary>
/// <remarks>
/// Replaces the legacy <c>RoleSwitchingPermission</c>. Role-switching itself
/// (changing the active role within an organization) is governed by the
/// baked-in role privilege rules and does not need a config entry.
/// </remarks>
public sealed class OrganizationSwitching
{
    /// <summary>
    /// Role canonical names that are additionally permitted to see the
    /// organization switcher UI. Baked-in roles with implicit cross-org
    /// privileges are always allowed regardless of this list.
    /// </summary>
    public List<string> AllowedRoles { get; set; } = [];
}
