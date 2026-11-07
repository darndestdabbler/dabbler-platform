namespace Dabbler.Security.Registration;

/// <summary>
/// Fluent builder used inside <c>DabblerSecurityOptions.RegisterPages()</c>
/// to declare the pages the host application wants to protect.
/// </summary>
public sealed class PageRegistrar
{
    private readonly List<RegisteredPage> _pages = [];

    /// <summary>
    /// Registers a page to be protected.
    /// </summary>
    /// <param name="key">A unique key for the page.</param>
    /// <param name="displayName">The name of the page to display in the admin UI.</param>
    /// <param name="description">An optional description for the page, shown in the admin UI.</param>
    /// <returns>The same <see cref="PageRegistrar"/> instance for chaining.</returns>
    public PageRegistrar Add(string key, string displayName, string? description = null)
    {
        _pages.Add(new RegisteredPage { Key = key, DisplayName = displayName, Description = description });
        return this;
    }

    internal IReadOnlyList<RegisteredPage> Build() => _pages.AsReadOnly();
}
