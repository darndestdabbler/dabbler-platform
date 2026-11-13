// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data.Metadata;

/// <summary>
/// Provides entity metadata at runtime. Implementations may load metadata from
/// configuration files, database analysis, or hardcoded definitions.
/// </summary>
public interface IEntityMetadataProvider
{
    /// <summary>
    /// Returns metadata for the specified entity, or null if the entity is not registered.
    /// </summary>
    /// <param name="entityName">The programmatic entity name.</param>
    EntityMetadata? GetEntityMetadata(string entityName);

    /// <summary>
    /// Returns metadata for all registered entities.
    /// </summary>
    IReadOnlyList<EntityMetadata> GetAllEntities();

    /// <summary>
    /// Returns entities where <see cref="EntityMetadata.ShowInNavigation"/> is true,
    /// ordered by <see cref="EntityMetadata.NavigationOrder"/>.
    /// </summary>
    IReadOnlyList<EntityMetadata> GetNavigableEntities();
}
