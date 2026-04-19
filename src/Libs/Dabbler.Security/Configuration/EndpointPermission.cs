namespace Dabbler.Security.Configuration;

/// <summary>
/// Represents a security permission rule for a specific application endpoint.
/// </summary>
/// <remarks>
/// Legacy grant-based type. Replaced by baked-in role privileges (for the
/// five canonical roles) plus <see cref="UiRestriction"/> on each user-type
/// role. Scheduled for removal in Session 3 of
/// <c>rearchitect-role-administration-foundations</c>.
/// </remarks>
[Obsolete("Replaced by baked-in role privileges plus UiRestriction. Scheduled for removal in Session 3 of rearchitect-role-administration-foundations.")]
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
