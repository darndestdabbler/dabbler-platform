namespace Dabbler.Security.Entities;

/// <summary>
/// Defines a field-level permission granted to an <see cref="AppRole"/>.
/// This specifies fine-grained access control for individual fields of an entity (e.g., "Read" for Price, "Hidden" for SSN).
/// These records are critical for granular authorization and data security.
/// </summary>
public class AppFieldPermission
{
    /// <summary>
    /// Gets or sets the unique identifier for the field permission record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the associated <see cref="AppRole"/>.
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// Gets or sets the name of the entity (e.g., "Product", "User", "Order") containing the field.
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the specific field (e.g., "Price", "Description", "SSN") to which this permission applies.
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the access level for the specified field (e.g., "Read", "Write", "Hidden").
    /// This string should correspond to defined access levels within the application's authorization policy.
    /// Incorrectly configured field access levels can expose sensitive data or prevent legitimate data modification.
    /// </summary>
    public string AccessLevel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the navigation property for the associated <see cref="AppRole"/>.
    /// </summary>
    public AppRole? Role { get; set; }
}
