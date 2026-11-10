namespace Dabbler.Security.Entities;

/// <summary>
/// Represents a security role within the application, defining a set of permissions.
/// Roles are fundamental to the authorization system, dictating what users can access or do.
/// </summary>
public class AppRole
{
    /// <summary>
    /// Gets or sets the unique identifier for the role.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique name of the role (e.g., "Administrator", "Editor", "Viewer").
    /// This name is critical for identifying and assigning permissions.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a human-readable description of the role's purpose and typical responsibilities.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this is a system-defined role.
    /// System roles typically cannot be modified or deleted through standard application interfaces.
    /// </summary>
    public bool IsSystemRole { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the role was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the UTC timestamp when the role was soft-deleted.
    /// A non-null value indicates the role is logically deleted but retained for audit or recovery.
    /// Soft-deleting roles has significant security implications as users assigned to it may lose permissions.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who soft-deleted this role.
    /// </summary>
    public int? DeletedBy { get; set; }

    /// <summary>
    /// Optional. When set, this role inherits all permissions of the named parent role.
    /// Stored as a role name (not FK) to allow flexible ordering of role definitions.
    /// This mechanism creates a hierarchy of permissions and is a critical part of the authorization logic.
    /// </summary>
    public string? InheritsFromRoleName { get; set; }

    /// <summary>
    /// Gets or sets the collection of <see cref="AppUserRole"/> entities associating users with this role.
    /// </summary>
    public ICollection<AppUserRole> Users { get; set; } = new List<AppUserRole>();
}
