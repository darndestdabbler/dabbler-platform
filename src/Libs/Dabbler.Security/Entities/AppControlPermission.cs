using Dabbler.Security.Configuration;

namespace Dabbler.Security.Entities;

/// <summary>
/// Configures the visible/enabled state of a registered UI control for a given role.
/// PageKey and ControlKey match keys registered via options.RegisterControls().
/// </summary>
/// <remarks>
/// While primarily affecting user experience, improper control state configuration
/// could potentially expose sensitive UI elements or functionality.
/// Ensure these permissions are managed to maintain data privacy and system integrity.
/// </remarks>
public class AppControlPermission
{
    /// <summary>
    /// The unique identifier for this UI control permission record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The foreign key identifying the associated application role.
    /// This links the permission to a specific <see cref="AppRole"/>.
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// Navigation property to the associated <see cref="AppRole"/> entity.
    /// </summary>
    public AppRole Role { get; set; } = null!;

    /// <summary>
    /// The unique identifier of the page where the UI control resides.
    /// This key should match a registered page within the application's configuration.
    /// </summary>
    public string PageKey { get; set; } = string.Empty;

    /// <summary>
    /// The unique identifier of the specific UI control within the page.
    /// This key should match a registered control within the application's configuration.
    /// </summary>
    public string ControlKey { get; set; } = string.Empty;

    /// <summary>
    /// An enumeration value indicating the desired state (e.g., <see cref="ControlState.Enabled"/>,
    /// <see cref="ControlState.Disabled"/>, <see cref="ControlState.Hidden"/>) of the control for the role.
    /// </summary>
    public ControlState State { get; set; } = ControlState.Enabled;
}
