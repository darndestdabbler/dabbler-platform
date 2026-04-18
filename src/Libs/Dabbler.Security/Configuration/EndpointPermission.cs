namespace Dabbler.Security.Configuration;

/// <summary>
/// Represents a security permission rule for a specific application endpoint.
/// </summary>
public sealed class EndpointPermission
{
    /// <summary>
    /// The endpoint key as registered via options.RegisterEndpoints().
    /// Typically "METHOD /path", e.g. "POST /api/orders".
    /// </summary>
    public string EndpointKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of roles that are permitted to access this endpoint.
    /// </summary>
    public List<string> AllowedRoles { get; set; } = [];

    /// <summary>
    /// Roles explicitly denied access to this endpoint. Deny takes precedence over allow
    /// when a user holds both an allowed and a denied role.
    /// </summary>
    public List<string> DeniedRoles { get; set; } = [];
}
