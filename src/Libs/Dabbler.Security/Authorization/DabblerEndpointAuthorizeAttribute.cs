using Microsoft.AspNetCore.Authorization;

namespace Dabbler.Security.Authorization;

/// <summary>
/// Applies endpoint-level authorization using the runtime security configuration.
/// The policy name <c>DabblerEndpoint:{endpointKey}</c> is resolved by
/// <see cref="DabblerAuthorizationPolicyProvider"/> to an <see cref="Requirements.EndpointPermissionRequirement"/>.
/// </summary>
/// <remarks>
/// This attribute simplifies applying custom, database-driven authorization rules to API endpoints
/// by abstracting the policy creation logic.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class DabblerEndpointAuthorizeAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DabblerEndpointAuthorizeAttribute"/> class
    /// with the specified endpoint key.
    /// </summary>
    /// <param name="endpointKey">The unique key identifying the endpoint to authorize access for.</param>
    public DabblerEndpointAuthorizeAttribute(string endpointKey)
        : base($"DabblerEndpoint:{endpointKey}")
    {
    }
}
