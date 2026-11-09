namespace Dabbler.Security.Configuration;

/// <summary>
/// Defines CRUD (Create, Read, Update, Delete) permissions for a specific entity.
/// </summary>
/// <remarks>
/// Legacy grant-based type. User-type roles are now default-permissive;
/// narrowing access happens through <see cref="ColumnRestriction"/>
/// (per-column view-only / hidden) rather than per-entity CRUD grants.
/// Scheduled for removal in Session 3 of
/// <c>rearchitect-role-administration-foundations</c>.
/// </remarks>
[Obsolete("Replaced by default-permissive CRUD plus ColumnRestriction. Scheduled for removal in Session 3 of rearchitect-role-administration-foundations.")]
public sealed class EntityPermission
{
    /// <summary>The entity key as registered via options.RegisterEntities().</summary>
    public string EntityKey { get; set; } = string.Empty;

    /// <summary>
    /// A collection of role-specific permissions detailing the allowed CRUD operations on this entity.
    /// </summary>
    public List<EntityRolePermission> RolePermissions { get; set; } = [];
}

/// <summary>
/// Specifies the CRUD operations a particular role is permitted to perform on an entity.
/// </summary>
public sealed class EntityRolePermission
{
    /// <summary>
    /// The name of the role to which these permissions apply.
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// The CRUD operations this role may perform. Valid values include: "Create", "Read", "Update", "Delete".
    /// </summary>
    public List<string> AllowedOperations { get; set; } = [];
}
