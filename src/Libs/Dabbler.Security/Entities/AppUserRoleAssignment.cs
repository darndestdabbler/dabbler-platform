namespace Dabbler.Security.Entities;

/// <summary>
/// Stores role assignments for users, identifying users by a string UserId.
/// This entity is designed to be flexible, supporting both internal magic-link users
/// (where <see cref="UserId"/> typically corresponds to <c>AppUser.Id.ToString()</c>) and
/// external Identity Provider (IdP) users (where <see cref="UserId"/> is the IdP subject or name-identifier claim).
/// This is separate from <c>AppUserRole</c> which uses integer foreign keys for assignments to the internal <c>AppUser</c> table.
/// <para>
/// **Security Note:** Records in this table define a user's permissions and access rights.
/// Unauthorized modification or creation of these assignments can lead to privilege escalation.
/// </para>
/// </summary>
public class AppUserRoleAssignment
{
    /// <summary>
    /// The unique identifier for this role assignment record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The IdP-agnostic user identifier (e.g., a string subject claim from an external IdP,
    /// or the string representation of an internal <c>AppUser.Id</c>).
    /// <para>
    /// **Security Note:** This is a primary identifier for a user's permissions. It must be
    /// securely handled and validated to prevent impersonation or unauthorized role assignments.
    /// </para>
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The denormalized name of the role being assigned. This allows for flexibility,
    /// especially for scenarios like JSON stores where a direct foreign key to <c>AppRole</c>
    /// might not be feasible or desired.
    /// <para>
    /// **Security Note:** The <see cref="RoleName"/> defines the user's capabilities.
    /// Ensure that role names are managed securely and correspond to defined permissions.
    /// </para>
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// The UTC timestamp when this role assignment was made.
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The string userId of the user who performed this assignment.
    /// This can be used for auditing purposes.
    /// </summary>
    public string? AssignedBy { get; set; }
}
