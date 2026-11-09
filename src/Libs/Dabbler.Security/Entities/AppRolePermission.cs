namespace Dabbler.Security.Entities;

/// <summary>
/// Defines an entity-level permission granted to an <see cref="AppRole"/>.
/// This specifies what actions (e.g., View, Create, Edit, Delete) a role can perform on a given entity type.
/// These records are critical for the application's authorization decisions.
/// </summary>
public class AppRolePermission
{
    /// <summary>
    /// Gets or sets the unique identifier for the role permission record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the associated <see cref="AppRole"/>.
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// Gets or sets the name of the entity (e.g., "Product", "Order", "User") to which this permission applies.
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of permission (e.g., "View", "Create", "Edit", "Delete").
    /// This string should correspond to defined permission types within the application's authorization policy.
    /// Misconfigured permission types can lead to security vulnerabilities.
    /// </summary>
    public string PermissionType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the navigation property for the associated <see cref="AppRole"/>.
    /// </summary>
    public AppRole? Role { get; set; }
}
