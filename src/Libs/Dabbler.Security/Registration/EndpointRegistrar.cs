namespace Dabbler.Security.Registration;

/// <summary>
/// Fluent builder used inside <c>DabblerSecurityOptions.RegisterEndpoints()</c>
/// to declare the API endpoints the host application wants to protect.
/// </summary>
public sealed class EndpointRegistrar
{
    private readonly List<RegisteredEndpoint> _endpoints = [];

    /// <summary>
    /// Registers an API endpoint to be protected.
    /// </summary>
    /// <param name="key">A unique key for the endpoint.</param>
    /// <param name="displayName">The name of the endpoint to display in the admin UI.</param>
    /// <param name="httpMethod">An optional hint for the HTTP method (e.g., GET, POST).</param>
    /// <param name="route">An optional hint for the route template.</param>
    /// <returns>The same <see cref="EndpointRegistrar"/> instance for chaining.</returns>
    public EndpointRegistrar Add(
        string key,
        string displayName,
        string? httpMethod = null,
        string? route = null)
    {
        _endpoints.Add(new RegisteredEndpoint
        {
            Key = key,
            DisplayName = displayName,
            HttpMethod = httpMethod,
            Route = route
        });
        return this;
    }

    internal IReadOnlyList<RegisteredEndpoint> Build() => _endpoints.AsReadOnly();
}
