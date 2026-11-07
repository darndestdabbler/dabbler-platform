namespace Dabbler.Security.Registration;

/// <summary>
/// Describes an API endpoint that has been registered with the resource registry.
/// </summary>
public sealed class RegisteredEndpoint
{
    /// <summary>
    /// Gets the unique key for the endpoint.
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// Gets the name of the endpoint for display in the admin UI.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Gets an optional HTTP method hint (e.g., GET, POST). Informational only.
    /// </summary>
    public string? HttpMethod { get; init; }

    /// <summary>
    /// Gets an optional route template hint. Informational only.
    /// </summary>
    public string? Route { get; init; }
}
