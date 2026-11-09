namespace Dabbler.Security.Configuration;

/// <summary>
/// Defines security permissions for a specific UI control on a given page.
/// </summary>
/// <remarks>
/// Legacy grant-based type. Replaced by per-role <see cref="UiRestriction"/>.
/// Scheduled for removal in Session 3 of
/// <c>rearchitect-role-administration-foundations</c>.
/// </remarks>
[Obsolete("Replaced by UiRestriction on each user-type RoleDefinition. Scheduled for removal in Session 3 of rearchitect-role-administration-foundations.")]
public sealed class ControlPermission
{
    /// <summary>The page this control lives on.</summary>
    public string PageKey { get; set; } = string.Empty;

    /// <summary>The control key as registered via options.RegisterControls().</summary>
    public string ControlKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a list of role-specific permissions for this control, dictating its state for each role.
    /// </summary>
    public List<ControlRolePermission> RolePermissions { get; set; } = [];
}

/// <summary>
/// Defines the desired state of a UI control for a specific user role.
/// </summary>
public sealed class ControlRolePermission
{
    /// <summary>
    /// Gets or sets the name of the role to which this permission applies.
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the state (<see cref="ControlState"/>) the control should adopt for the specified role.
    /// </summary>
    public ControlState State { get; set; } = ControlState.Enabled;
}

