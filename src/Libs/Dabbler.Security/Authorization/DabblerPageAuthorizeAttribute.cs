using Microsoft.AspNetCore.Authorization;

namespace Dabbler.Security.Authorization;

/// <summary>
/// Applies page-level authorization using the runtime security configuration.
/// The policy name <c>DabblerPage:{pageKey}</c> is resolved by
/// <see cref="DabblerAuthorizationPolicyProvider"/> to a <see cref="Requirements.PagePermissionRequirement"/>.
/// </summary>
/// <remarks>
/// This attribute simplifies applying custom, database-driven authorization rules to pages
/// by abstracting the policy creation logic.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class DabblerPageAuthorizeAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DabblerPageAuthorizeAttribute"/> class
    /// with the specified page key.
    /// </summary>
    /// <param name="pageKey">The unique key identifying the page to authorize access for.</param>
    public DabblerPageAuthorizeAttribute(string pageKey)
        : base($"DabblerPage:{pageKey}")
    {
    }
}
