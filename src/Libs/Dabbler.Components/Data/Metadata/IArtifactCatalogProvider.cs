// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data.Metadata;

/// <summary>
/// Data provider contract for querying the artifact catalog.
/// Returns available artifacts by type with search and security filtering.
/// Used by the top menu bar dropdowns and the catalog search pages.
/// </summary>
public interface IArtifactCatalogProvider
{
    /// <summary>
    /// Returns all artifacts of a given type that the specified user has permission to access.
    /// </summary>
    /// <param name="artifactType">The artifact type to query.</param>
    /// <param name="userId">The user whose permissions determine visibility.</param>
    /// <returns>Catalog entries the user can access, ordered by name.</returns>
    Task<IReadOnlyList<ArtifactCatalogEntry>> GetArtifactsAsync(
        ArtifactType artifactType,
        string userId);

    /// <summary>
    /// Searches artifacts by name or description, filtered by type and user permissions.
    /// </summary>
    /// <param name="artifactType">The artifact type to search within.</param>
    /// <param name="userId">The user whose permissions determine visibility.</param>
    /// <param name="searchText">Text to match against artifact name, description, and tags.</param>
    /// <returns>Matching catalog entries, ordered by relevance.</returns>
    Task<IReadOnlyList<ArtifactCatalogEntry>> SearchArtifactsAsync(
        ArtifactType artifactType,
        string userId,
        string searchText);
}
