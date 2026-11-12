using Microsoft.AspNetCore.Authorization;

namespace Dabbler.Security.Authorization.Requirements;

/// <summary>
/// Represents the requirement that a user must have permission to interact with a specific UI control on a page.
/// </summary>
public sealed class ControlPermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the unique key identifying the page to which the control belongs.
    /// </summary>
    public string PageKey { get; }

    /// <summary>
    /// Gets the unique key identifying the control within the page.
    /// </summary>
    public string ControlKey { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ControlPermissionRequirement"/> class.
    /// </summary>
    /// <param name="pageKey">The unique key of the page containing the control.</param>
    /// <param name="controlKey">The unique key of the control.</param>
    public ControlPermissionRequirement(string pageKey, string controlKey)
    {
        PageKey = pageKey;
        ControlKey = controlKey;
    }
}
