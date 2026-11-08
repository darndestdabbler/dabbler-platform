// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data.Metadata;

/// <summary>
/// Data provider contract for artifact usage tracking.
/// Supplies pinned, recent, and frequent artifact lists for the top menu bar dropdowns,
/// and records access events when artifacts are opened.
/// </summary>
public interface IUsageTrackingProvider
{
    /// <summary>
    /// Returns artifacts the user has pinned for a given artifact type, ordered by pin date.
    /// </summary>
    /// <param name="userId">The user whose pins to retrieve.</param>
    /// <param name="artifactType">The artifact type to filter by.</param>
    /// <returns>Pinned usage records for the menu dropdown's pinned section.</returns>
    Task<IReadOnlyList<ArtifactUsageRecord>> GetPinnedAsync(
        string userId,
        ArtifactType artifactType);

    /// <summary>
    /// Returns the user's most recently accessed artifacts for a given type,
    /// ordered by <see cref="ArtifactUsageRecord.LastAccessedAt"/> descending.
    /// </summary>
    /// <param name="userId">The user whose recent items to retrieve.</param>
    /// <param name="artifactType">The artifact type to filter by.</param>
    /// <param name="maxItems">Maximum number of items to return (default 7).</param>
    /// <returns>Recently accessed usage records.</returns>
    Task<IReadOnlyList<ArtifactUsageRecord>> GetRecentAsync(
        string userId,
        ArtifactType artifactType,
        int maxItems = 7);

    /// <summary>
    /// Returns the user's most frequently accessed artifacts for a given type,
    /// blending recency and frequency for ranking.
    /// </summary>
    /// <param name="userId">The user whose frequent items to retrieve.</param>
    /// <param name="artifactType">The artifact type to filter by.</param>
    /// <param name="maxItems">Maximum number of items to return (default 7).</param>
    /// <returns>Frequently accessed usage records.</returns>
    Task<IReadOnlyList<ArtifactUsageRecord>> GetFrequentAsync(
        string userId,
        ArtifactType artifactType,
        int maxItems = 7);

    /// <summary>
    /// Records an access event for an artifact, updating
    /// <see cref="ArtifactUsageRecord.LastAccessedAt"/> and incrementing
    /// <see cref="ArtifactUsageRecord.AccessCount"/>.
    /// </summary>
    /// <param name="userId">The user who accessed the artifact.</param>
    /// <param name="artifactType">The type of the accessed artifact.</param>
    /// <param name="artifactId">The identifier of the accessed artifact.</param>
    Task TrackAccessAsync(
        string userId,
        ArtifactType artifactType,
        string artifactId);

    /// <summary>
    /// Sets or clears the pinned state for an artifact.
    /// </summary>
    /// <param name="userId">The user whose pin state to update.</param>
    /// <param name="artifactType">The type of the artifact.</param>
    /// <param name="artifactId">The identifier of the artifact.</param>
    /// <param name="isPinned">True to pin, false to unpin.</param>
    Task SetPinnedAsync(
        string userId,
        ArtifactType artifactType,
        string artifactId,
        bool isPinned);
}
