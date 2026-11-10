namespace Dabbler.Security.Entities;

/// <summary>
/// Represents an organization context that a user may belong to, such as the
/// internal organization or a retail-business organization.
/// </summary>
public class AppOrganization
{
    /// <summary>
    /// Gets or sets the surrogate primary key for the organization.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the stable external key for the organization.
    /// This supports configuration and seed lookups without hard-coding numeric IDs.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name for the organization.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this is the configured internal organization.
    /// </summary>
    public bool IsInternal { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this organization is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the UTC timestamp when the organization was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the memberships associated with this organization.
    /// </summary>
    public ICollection<AppUserOrganizationMembership> Memberships { get; set; } = new List<AppUserOrganizationMembership>();
}
