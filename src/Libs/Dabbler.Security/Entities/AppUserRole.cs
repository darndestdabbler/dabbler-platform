namespace Dabbler.Security.Entities;

/// <summary>
/// Represents the assignment of a specific <see cref="AppRole"/> to an <see cref="AppUser"/>.
/// This entity forms the many-to-many relationship between users and roles, and contains assignment metadata.
/// </summary>
public class AppUserRole
{
    /// <summary>
    /// Gets or sets the foreign key to the associated <see cref="AppUser"/>.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the associated <see cref="AppRole"/>.
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the role was assigned to the user.
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the identifier of the user who performed the role assignment.
    /// This is important for auditing security-related actions.
    /// </summary>
    public int AssignedBy { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this role assignment was soft-deleted.
    /// A non-null value indicates the assignment is logically deleted.
    /// Deleting a role assignment directly impacts a user's permissions and access.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who soft-deleted this role assignment.
    /// </summary>
    public int? DeletedBy { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the associated <see cref="AppUser"/>.
    /// </summary>
    public AppUser? User { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the associated <see cref="AppRole"/>.
    /// </summary>
    public AppRole? Role { get; set; }
}
