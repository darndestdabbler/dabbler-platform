namespace Dabbler.Security.Configuration;

/// <summary>
/// Grants a specific role the ability to see the unmasked value of a column that
/// is otherwise default-masked by a <see cref="DefaultMaskRule"/>. Exemptions may
/// target either a user-type role or a baked-in role.
/// </summary>
/// <remarks>
/// <para>
/// Exemptions live in a <b>top-level</b> <c>MaskExemptions</c> list on
/// <see cref="SecurityConfiguration"/> rather than on a per-role collection so
/// they can target baked-in roles (whose compiled privileges otherwise live
/// entirely in code).
/// </para>
/// <para>
/// <b>Separation-of-duties authority rules</b> (enforced by the exemption-granting
/// service wired in Session Set D):
/// <list type="bullet">
///   <item>Exemptions targeting user-type roles or UserAdmin: may be granted by
///     SystemAdmin or CrossOrgUserAdmin, with or without <see cref="ExpiresAt"/>.</item>
///   <item>Exemptions targeting SystemAdmin or CrossOrgMonitor: may be granted
///     only by CrossOrgUserAdmin and <b>must</b> have a non-null
///     <see cref="ExpiresAt"/>.</item>
///   <item>Exemptions targeting CrossOrgUserAdmin: self-granted only (same user)
///     and <b>must</b> have a non-null <see cref="ExpiresAt"/>.</item>
/// </list>
/// </para>
/// </remarks>
public sealed class MaskExemption
{
    /// <summary>
    /// The entity (table) the exempted column belongs to. Must match a
    /// <see cref="DefaultMaskRule"/> — you can only exempt a column that is
    /// already declared sensitive.
    /// </summary>
    public string EntityKey { get; set; } = string.Empty;

    /// <summary>
    /// The exempted column. Must match a <see cref="DefaultMaskRule"/>.
    /// </summary>
    public string ColumnKey { get; set; } = string.Empty;

    /// <summary>
    /// The canonical name of the role being exempted — may be a user-type role
    /// name or a baked-in role name.
    /// </summary>
    public string TargetRoleName { get; set; } = string.Empty;

    /// <summary>
    /// Identifier (typically username or stable user key) of the SystemAdmin or
    /// CrossOrgUserAdmin who granted the exemption. Rendered in the audit UI
    /// alongside the exempted row.
    /// </summary>
    public string GrantedBy { get; set; } = string.Empty;

    /// <summary>
    /// When the exemption was granted.
    /// </summary>
    public DateTimeOffset GrantedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Optional expiration. Null = permanent (only valid for exemptions targeting
    /// user-type roles or UserAdmin). Non-null = time-bound; required for
    /// exemptions targeting SystemAdmin, CrossOrgMonitor, or CrossOrgUserAdmin.
    /// Expired exemptions are treated as non-existent at resolution time — no
    /// background worker is required; the mask service simply compares
    /// <see cref="ExpiresAt"/> to "now" when evaluating.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }
}
