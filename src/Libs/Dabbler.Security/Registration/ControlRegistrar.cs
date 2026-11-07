namespace Dabbler.Security.Registration;

/// <summary>
/// Fluent builder used inside <c>DabblerSecurityOptions.RegisterControls()</c>
/// to declare the UI controls the host application wants to protect.
/// </summary>
public sealed class ControlRegistrar
{
    private readonly List<RegisteredControl> _controls = [];

    /// <summary>
    /// Registers a UI control to be protected.
    /// </summary>
    /// <param name="key">A unique key for the control within its page.</param>
    /// <param name="pageKey">The key of the page this control belongs to.</param>
    /// <param name="displayName">The name of the control to display in the admin UI.</param>
    /// <returns>The same <see cref="ControlRegistrar"/> instance for chaining.</returns>
    public ControlRegistrar Add(string key, string pageKey, string displayName)
    {
        _controls.Add(new RegisteredControl
        {
            Key = key,
            PageKey = pageKey,
            DisplayName = displayName
        });
        return this;
    }

    internal IReadOnlyList<RegisteredControl> Build() => _controls.AsReadOnly();
}
