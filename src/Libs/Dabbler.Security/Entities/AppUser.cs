namespace Dabbler.Security.Entities;

/// <summary>
/// Represents a user account within the application's security context.
/// Contains fundamental user information and links to associated security roles and external login providers.
/// </summary>
public class AppUser
{
    /// <summary>
    /// Gets or sets the unique identifier for the application user.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the user's email address.
    /// This is often used for login, notifications, and is considered Personally Identifiable Information (PII).
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's display name, intended for presentation in the UI.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the user account is currently active.
    /// Inactive accounts typically cannot log in or access resources.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the UTC timestamp when the user account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the identifier of the user who created this account.
    /// </summary>
    public int? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp of the user's last successful login.
    /// This can be used for security monitoring or activity tracking.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the user account was soft-deleted.
    /// A non-null value indicates the account is logically deleted but retained for audit or recovery.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who soft-deleted this account.
    /// </summary>
    public int? DeletedBy { get; set; }

    /// <summary>
    /// Gets or sets the collection of roles assigned to this user.
    /// This collection defines the user's permissions and access within the application.
    /// </summary>
    public ICollection<AppUserRole> Roles { get; set; } = new List<AppUserRole>();

    /// <summary>
    /// Gets or sets the collection of organization memberships for this user.
    /// </summary>
    public ICollection<AppUserOrganizationMembership> OrganizationMemberships { get; set; } = new List<AppUserOrganizationMembership>();

    /// <summary>
    /// Gets or sets the collection of external login providers (e.g., Google, Facebook) linked to this user account.
    /// </summary>
    public ICollection<AppExternalLogin> ExternalLogins { get; set; } = new List<AppExternalLogin>();
}
