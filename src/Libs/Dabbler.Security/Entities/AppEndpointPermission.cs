namespace Dabbler.Security.Entities;

/// <summary>
/// Grants a role access to a registered API endpoint.
/// EndpointKey matches a key registered via options.RegisterEndpoints().
/// </summary>
/// <remarks>
/// These records are critical for controlling access to API resources.
/// Unauthorized modification or misconfiguration can lead to security vulnerabilities,
/// including unauthorized data access or manipulation.
/// </remarks>
public class AppEndpointPermission
{
    /// <summary>
    /// The unique identifier for this endpoint permission record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The foreign key identifying the associated application role.
    /// This links the permission to a specific <see cref="AppRole"/>.
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// Navigation property to the associated <see cref="AppRole"/> entity.
    /// </summary>
    public AppRole Role { get; set; } = null!;

    /// <summary>
    /// The unique key or identifier of the API endpoint this permission applies to.
    /// E.g. "POST /api/orders" or "OrdersController.Create".
    /// This key should match an endpoint registered via the application's configuration.
    /// </summary>
    public string EndpointKey { get; set; } = string.Empty;

    /// <summary>
    /// A boolean value indicating whether the specified role is granted (<see langword="true"/>)
    /// or explicitly denied (<see langword="false"/>) access to the endpoint. Deny takes
    /// precedence over allow when a user holds both.
    /// </summary>
    public bool Allowed { get; set; } = true;
}
