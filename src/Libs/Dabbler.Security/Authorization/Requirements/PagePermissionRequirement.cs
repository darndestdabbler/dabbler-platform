using Microsoft.AspNetCore.Authorization;

namespace Dabbler.Security.Authorization.Requirements;

/// <summary>
/// Represents the requirement that a user must have permission to access a specific page.
/// </summary>
public sealed class PagePermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the unique key identifying the page.
    /// </summary>
    public string PageKey { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PagePermissionRequirement"/> class.
    /// </summary>
    /// <param name="pageKey">The unique key of the page.</param>
    public PagePermissionRequirement(string pageKey)
    {
        PageKey = pageKey;
    }
}
