namespace Dabbler.Security.Configuration;

/// <summary>
/// Restricts a user-type role's visibility or editability of a specific column on a
/// specific entity. Absence of a <see cref="ColumnRestriction"/> means the role has
/// full view-and-edit access (user-type roles are default-permissive); presence
/// narrows that access by <see cref="RestrictionKind"/>.
/// </summary>
/// <remarks>
/// <para>
/// Sensitive-field masking is modelled separately from <see cref="ColumnRestriction"/> —
/// see <see cref="DefaultMaskRule"/> and <see cref="MaskExemption"/>. A column can be
/// both masked (at the platform level) and further view-only restricted for a given role.
/// </para>
/// <para>
/// <see cref="ColumnRestriction"/> applies only to user-type roles. Baked-in roles
/// (SystemAdmin, CrossOrgUserAdmin, CrossOrgMonitor, UserAdmin, User) have compiled-in
/// privilege rules and do not consult this list.
/// </para>
/// </remarks>
public sealed class ColumnRestriction
{
    /// <summary>
    /// The entity (table) the restricted column belongs to.
    /// </summary>
    public string EntityKey { get; set; } = string.Empty;

    /// <summary>
    /// The column (field) on the entity that is restricted.
    /// </summary>
    public string ColumnKey { get; set; } = string.Empty;

    /// <summary>
    /// How the column is restricted for this role.
    /// </summary>
    public ColumnRestrictionKind RestrictionKind { get; set; } = ColumnRestrictionKind.ViewOnly;
}

/// <summary>
/// The binary shape of a <see cref="ColumnRestriction"/>. "Masked" is deliberately
/// not included here — masking is a platform-wide concern expressed via
/// <see cref="DefaultMaskRule"/> plus per-role <see cref="MaskExemption"/>.
/// </summary>
public enum ColumnRestrictionKind
{
    /// <summary>
    /// The role can see the column but cannot edit it. Data-bound controls render
    /// disabled rather than hidden.
    /// </summary>
    ViewOnly = 0,

    /// <summary>
    /// The role cannot see and cannot edit the column. Data-bound controls are
    /// hidden (and disabled) entirely.
    /// </summary>
    Hidden = 1,
}
