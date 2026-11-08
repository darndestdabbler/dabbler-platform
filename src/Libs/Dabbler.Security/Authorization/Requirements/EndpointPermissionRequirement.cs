using Microsoft.AspNetCore.Authorization;

namespace Dabbler.Security.Authorization.Requirements;

/// <summary>
/// Represents the requirement that a user must have permission to access a specific API endpoint.
/// </summary>
public sealed class EndpointPermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the unique key identifying the endpoint.
    /// </summary>
    public string EndpointKey { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointPermissionRequirement"/> class.
    /// </summary>
    /// <param name="endpointKey">The unique key of the endpoint.</param>
    public EndpointPermissionRequirement(string endpointKey)
    {
        EndpointKey = endpointKey;
    }
}
