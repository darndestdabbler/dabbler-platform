namespace Dabbler.Security.Configuration;

/// <summary>
/// Defines a user-type role. Each user-type role is default-permissive — its access
/// is narrowed only by the three restriction lists (<see cref="ColumnRestrictions"/>,
/// <see cref="RowRestrictions"/>, <see cref="UiRestrictions"/>) plus any
/// <see cref="MaskExemption"/> rows that target it.
/// </summary>
/// <remarks>
/// Baked-in roles (SystemAdmin, CrossOrgUserAdmin, CrossOrgMonitor, UserAdmin,
/// User) are not represented by <see cref="RoleDefinition"/> instances in
/// <c>SecurityConfiguration.UserTypeRoles</c>. They live as code constants with
/// compiled privilege rules; only their <c>DisplayAlias</c> is per-deployment
/// configurable, via <c>SecurityConfiguration.BakedInRoleAliasOverrides</c>.
/// </remarks>
public sealed class RoleDefinition
{
    /// <summary>
    /// The canonical, immutable name of the role. This name is used in claims,
    /// audit records, and handler logic and must never change once deployed.
    /// Per-deployment rebranding goes through <see cref="DisplayAlias"/>.
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// Optional per-deployment display label shown in the dashboard, role
    /// switcher, and audit UI. When null, consumers fall back to
    /// <see cref="RoleName"/>. Baked-in roles use
    /// <c>BakedInRoleDefinition.DefaultDisplayAlias</c> as the default and
    /// honour any override in <c>SecurityConfiguration.BakedInRoleAliasOverrides</c>.
    /// </summary>
    public string? DisplayAlias { get; set; }

    /// <summary>
    /// A human-readable description of the role's purpose or typical users.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Column-level restrictions for this role. Absence means the role can see
    /// and edit every column on every entity.
    /// </summary>
    public List<ColumnRestriction> ColumnRestrictions { get; set; } = [];

    /// <summary>
    /// Row-level restrictions for this role. Absence means the role can see
    /// every record of every entity (modulo the global organization filter
    /// delivered in Session Set C).
    /// </summary>
    public List<RowRestriction> RowRestrictions { get; set; } = [];

    /// <summary>
    /// Page- and control-level UI restrictions for this role. Absence means the
    /// role can see and interact with every page and every control.
    /// </summary>
    public List<UiRestriction> UiRestrictions { get; set; } = [];

    /// <summary>
    /// Legacy inheritance pointer. Role inheritance is removed from the
    /// authorization model — a user assigned to multiple roles must explicitly
    /// switch. Retained on the type only so the legacy EF store and Blazor
    /// admin components continue to compile between Session 2 and Session 3 of
    /// the <c>rearchitect-role-administration-foundations</c> set; no handler
    /// consults it at runtime.
    /// </summary>
    [Obsolete("Role inheritance is removed; handlers no longer consult InheritsFrom. Scheduled for removal in Session 3 of rearchitect-role-administration-foundations.")]
    public string? InheritsFrom { get; set; }
}
