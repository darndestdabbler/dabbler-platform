namespace Dabbler.Security.Registration;

/// <summary>
/// Describes a UI control that has been registered with the resource registry.
/// Controls are scoped to a page and can be configured to be enabled, disabled, or hidden per role.
/// </summary>
public sealed class RegisteredControl
{
    /// <summary>
    /// Gets the unique key for the control within its parent page.
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// Gets the key of the <see cref="RegisteredPage"/> this control belongs to.
    /// </summary>
    public string PageKey { get; init; } = string.Empty;

    /// <summary>
    /// Gets the name of the control for display in the admin UI.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;
}
