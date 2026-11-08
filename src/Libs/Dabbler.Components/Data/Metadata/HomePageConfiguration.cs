// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data.Metadata;

/// <summary>
/// Configures the initial workspace tab shown on login.
/// Supports a global default and optional per-role overrides.
/// </summary>
public class HomePageConfiguration
{
    /// <summary>
    /// The default home page for all users. Null means empty workspace on login.
    /// Format: <c>"Dashboard:&lt;id&gt;"</c> or <c>"Form:&lt;entityName&gt;"</c>.
    /// </summary>
    public string? DefaultHomePage { get; set; }

    /// <summary>
    /// Optional per-role overrides for the home page.
    /// Key is the role name; value uses the same format as <see cref="DefaultHomePage"/>.
    /// </summary>
    /// <example>
    /// A sales team might land on a sales dashboard while a warehouse team lands on
    /// an inventory form: <c>{ "Sales": "Dashboard:sales-overview", "Warehouse": "Form:Inventory" }</c>.
    /// </example>
    public Dictionary<string, string> HomePagePerRole { get; set; } = new();

    /// <summary>
    /// Resolves the home page target for a given set of user roles.
    /// Checks role overrides first (first match wins), then falls back to
    /// <see cref="DefaultHomePage"/>.
    /// </summary>
    /// <param name="userRoles">The roles assigned to the current user.</param>
    /// <returns>The home page target string, or null if no home page is configured.</returns>
    public string? ResolveHomePage(IEnumerable<string> userRoles)
    {
        foreach (var role in userRoles)
        {
            if (HomePagePerRole.TryGetValue(role, out var target))
                return target;
        }

        return DefaultHomePage;
    }
}
