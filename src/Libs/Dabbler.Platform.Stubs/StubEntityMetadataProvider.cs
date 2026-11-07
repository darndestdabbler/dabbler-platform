using Dabbler.Components.Data.Metadata;

namespace Dabbler.Platform.Stubs;

/// <summary>
/// In-memory metadata provider for stub-backed hosts.
/// </summary>
public sealed class StubEntityMetadataProvider : IEntityMetadataProvider
{
    private readonly IReadOnlyList<EntityMetadata> _entities;
    private readonly IReadOnlyDictionary<string, EntityMetadata> _byName;

    /// <summary>
    /// Initializes a new provider with the supplied entities.
    /// </summary>
    public StubEntityMetadataProvider(IEnumerable<EntityMetadata> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        _entities = entities.ToArray();
        _byName = _entities.ToDictionary(entity => entity.EntityName, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public EntityMetadata? GetEntityMetadata(string entityName) =>
        _byName.TryGetValue(entityName, out var metadata) ? metadata : null;

    /// <inheritdoc />
    public IReadOnlyList<EntityMetadata> GetAllEntities() => _entities;

    /// <inheritdoc />
    public IReadOnlyList<EntityMetadata> GetNavigableEntities() =>
        _entities
            .Where(entity => entity.ShowInNavigation)
            .OrderBy(entity => entity.NavigationOrder)
            .ThenBy(entity => entity.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
