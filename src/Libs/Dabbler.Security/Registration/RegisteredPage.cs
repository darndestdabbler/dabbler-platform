namespace Dabbler.Security.Registration;

/// <summary>
/// Describes a page that has been registered with the resource registry so the admin UI
/// can render permission matrices for it.
/// </summary>
public sealed class RegisteredPage
{
    /// <summary>
    /// Gets the unique key for the page.
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// Gets the name of the page for display in the admin UI.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Gets an optional description of the page for display in the admin UI.
    /// </summary>
    public string? Description { get; init; }
}
