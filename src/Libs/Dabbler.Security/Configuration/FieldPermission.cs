namespace Dabbler.Security.Configuration;

/// <summary>
/// Defines granular access levels for specific fields within an entity.
/// </summary>
/// <remarks>
/// Legacy grant-based type. Replaced by per-role
/// <see cref="ColumnRestriction"/> (view-only / hidden) plus platform-level
/// <see cref="DefaultMaskRule"/> and per-role <see cref="MaskExemption"/>.
/// </remarks>
[Obsolete("Replaced by ColumnRestriction plus DefaultMaskRule / MaskExemption.")]
public sealed class FieldPermission
{
    /// <summary>
    /// The entity key to which this field permission applies, as registered via options.RegisterEntities().
    /// </summary>
    public string EntityKey { get; set; } = string.Empty;
    
    /// <summary>
    /// The field key within the specified entity, as registered.
    /// </summary>
    public string FieldKey { get; set; } = string.Empty;
    
    /// <summary>
    /// A collection of role-specific field access levels (Editable, ReadOnly, Hidden).
    /// </summary>
    public List<FieldRolePermission> RolePermissions { get; set; } = [];
}

/// <summary>
/// Specifies the access level a particular role has for a specific field.
/// </summary>
public sealed class FieldRolePermission
{
    /// <summary>
    /// The name of the role to which this field access level applies.
    /// </summary>
    public string RoleName { get; set; } = string.Empty;
    
    /// <summary>
    /// The access level for the field for this role.
    /// </summary>
    public FieldAccessLevel AccessLevel { get; set; } = FieldAccessLevel.Editable;
}

/// <summary>
/// Canonical field access level enum for the Dabbler.Security configuration system.
/// Database records may contain the legacy value "ReadWrite" (equivalent to Editable)
/// for backward compatibility — both string values are accepted during deserialization.
/// </summary>
public enum FieldAccessLevel
{
    /// <summary>
    /// The field can be viewed and modified.
    /// </summary>
    Editable = 0,
    /// <summary>
    /// The field can be viewed but not modified.
    /// </summary>
    ReadOnly = 1,
    /// <summary>
    /// The field is completely hidden and not accessible.
    /// </summary>
    Hidden = 2
}
