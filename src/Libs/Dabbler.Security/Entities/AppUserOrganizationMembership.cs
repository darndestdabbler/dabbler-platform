namespace Dabbler.Security.Entities;

/// <summary>
/// Represents a user's membership in a specific organization.
/// Organization-scoped role assignments attach to this membership.
/// </summary>
public class AppUserOrganizationMembership
{
    /// <summary>
    /// Gets or sets the surrogate primary key for the membership.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the user that belongs to the organization.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the organization that the user belongs to.
    /// </summary>
    public int OrganizationId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the user's default organization.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this membership is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets when the membership was established.
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the identifier of the user who established the membership.
    /// </summary>
    public int? JoinedBy { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this membership was soft-deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who soft-deleted this membership.
    /// </summary>
    public int? DeletedBy { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the associated user.
    /// </summary>
    public AppUser? User { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the associated organization.
    /// </summary>
    public AppOrganization? Organization { get; set; }

    /// <summary>
    /// Gets or sets the organization-scoped role assignments attached to this membership.
    /// </summary>
    public ICollection<AppUserRole> Roles { get; set; } = new List<AppUserRole>();
}
