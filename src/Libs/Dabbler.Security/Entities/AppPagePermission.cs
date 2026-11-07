namespace Dabbler.Security.Entities;

/// <summary>
/// Represents a specific permission granted to an application role for accessing a particular UI page.
/// These records define the core authorization rules for navigating within the application's user interface.
/// </summary>
/// <remarks>
/// Improper management of <see cref="AppPagePermission"/> records can lead to unauthorized page access
/// or privilege escalation. Ensure these permissions are managed securely and align with the principle
/// of least privilege.
/// </remarks>
public class AppPagePermission
{
    /// <summary>
    /// The unique identifier for this page permission record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The foreign key identifying the associated application role.
    /// This links the permission to a specific <see cref="AppRole"/>.
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// The unique route or identifier of the application page this permission applies to.
    /// This key should match a registered page within the application's routing configuration.
    /// </summary>
    public string PageRoute { get; set; } = string.Empty;

    /// <summary>
    /// A boolean value indicating whether the specified role is granted (<see langword="true"/>)
    /// or explicitly denied (<see langword="false"/>) access to the page.
    /// </summary>
    public bool Allowed { get; set; }

    /// <summary>
    /// Navigation property to the associated <see cref="AppRole"/> entity.
    /// </summary>
    public AppRole? Role { get; set; }
}
