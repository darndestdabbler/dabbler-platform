namespace Dabbler.Security.Registration;

/// <summary>
/// Singleton that holds all resources registered by the host application at startup.
/// Read by the admin UI to populate permission matrix rows and columns.
/// Populated from <see cref="Dabbler.Security.Options.DabblerSecurityOptions"/> registration calls
/// during DI container construction.
/// </summary>
public sealed class ResourceRegistry
{
    /// <summary>
    /// Gets an empty resource registry instance.
    /// </summary>
    public static readonly ResourceRegistry Empty =
        new([], [], [], []);

    /// <summary>
    /// Gets the list of registered pages.
    /// </summary>
    public IReadOnlyList<RegisteredPage> Pages { get; }

    /// <summary>
    /// Gets the list of registered entities.
    /// </summary>
    public IReadOnlyList<RegisteredEntity> Entities { get; }

    /// <summary>
    /// Gets the list of registered API endpoints.
    /// </summary>
    public IReadOnlyList<RegisteredEndpoint> Endpoints { get; }

    /// <summary>
    /// Gets the list of registered UI controls.
    /// </summary>
    public IReadOnlyList<RegisteredControl> Controls { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceRegistry"/> class.
    /// </summary>
    /// <param name="pages">The list of registered pages.</param>
    /// <param name="entities">The list of registered entities.</param>
    /// <param name="endpoints">The list of registered API endpoints.</param>
    /// <param name="controls">The list of registered UI controls.</param>
    public ResourceRegistry(
        IReadOnlyList<RegisteredPage> pages,
        IReadOnlyList<RegisteredEntity> entities,
        IReadOnlyList<RegisteredEndpoint> endpoints,
        IReadOnlyList<RegisteredControl> controls)
    {
        Pages = pages;
        Entities = entities;
        Endpoints = endpoints;
        Controls = controls;
    }
}
