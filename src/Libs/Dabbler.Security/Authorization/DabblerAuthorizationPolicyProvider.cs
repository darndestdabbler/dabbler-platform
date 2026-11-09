using Dabbler.Security.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Dabbler.Security.Authorization;

/// <summary>
/// Provides authorization policies dynamically based on policy names.
/// This provider creates policies for Dabbler-specific authorization attributes like
/// <see cref="DabblerPageAuthorizeAttribute"/> and <see cref="DabblerEndpointAuthorizeAttribute"/>.
/// </summary>
public class DabblerAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DabblerAuthorizationPolicyProvider"/> class.
    /// </summary>
    /// <param name="options">The options used to configure authorization.</param>
    public DabblerAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets an authorization policy from the policy name.
    /// </summary>
    /// <param name="policyName">The name of the policy to retrieve.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous operation.
    /// The task result contains the <see cref="AuthorizationPolicy"/> if a policy is found;
    /// otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// This method intercepts policy names with the prefixes <c>DabblerPage:</c> and <c>DabblerEndpoint:</c>
    /// to dynamically construct policies with <see cref="PagePermissionRequirement"/> or
    /// <see cref="EndpointPermissionRequirement"/> respectively. For other policy names,
    /// it falls back to the behavior of the <see cref="DefaultAuthorizationPolicyProvider"/>.
    /// </remarks>
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var policy = await base.GetPolicyAsync(policyName);
        if (policy != null)
            return policy;

        const string PagePrefix = "DabblerPage:";
        const string EndpointPrefix = "DabblerEndpoint:";

        // Page authorization: "DabblerPage:{pageKey}"
        if (policyName.StartsWith(PagePrefix, StringComparison.OrdinalIgnoreCase))
        {
            var pageKey = policyName[PagePrefix.Length..];
            if (!string.IsNullOrWhiteSpace(pageKey))
            {
                var builder = new AuthorizationPolicyBuilder();
                builder.AddRequirements(new PagePermissionRequirement(pageKey));
                return builder.Build();
            }
        }
        // Endpoint authorization: "DabblerEndpoint:{endpointKey}"
        else if (policyName.StartsWith(EndpointPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var endpointKey = policyName[EndpointPrefix.Length..];
            if (!string.IsNullOrWhiteSpace(endpointKey))
            {
                var builder = new AuthorizationPolicyBuilder();
                builder.AddRequirements(new EndpointPermissionRequirement(endpointKey));
                return builder.Build();
            }
        }

        return null;
    }
}
