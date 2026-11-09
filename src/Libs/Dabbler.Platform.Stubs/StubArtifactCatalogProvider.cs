using Dabbler.Components.Data.Metadata;

namespace Dabbler.Platform.Stubs;

/// <summary>
/// In-memory artifact catalog provider for compatibility shell flows.
/// </summary>
public sealed class StubArtifactCatalogProvider : IArtifactCatalogProvider
{
    private readonly IReadOnlyList<ArtifactCatalogEntry> _entries;

    /// <summary>
    /// Initializes a new provider with catalog entries.
    /// </summary>
    public StubArtifactCatalogProvider(IEnumerable<ArtifactCatalogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        _entries = entries.ToArray();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ArtifactCatalogEntry>> GetArtifactsAsync(ArtifactType artifactType, string userId)
    {
        return Task.FromResult<IReadOnlyList<ArtifactCatalogEntry>>(
            _entries.Where(entry => entry.ArtifactType == artifactType).ToArray());
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ArtifactCatalogEntry>> SearchArtifactsAsync(
        ArtifactType artifactType,
        string userId,
        string searchText)
    {
        searchText ??= string.Empty;

        return Task.FromResult<IReadOnlyList<ArtifactCatalogEntry>>(
            _entries
                .Where(entry => entry.ArtifactType == artifactType)
                .Where(entry =>
                    entry.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || (entry.Description?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
                    || entry.Tags.Any(tag => tag.Contains(searchText, StringComparison.OrdinalIgnoreCase)))
                .ToArray());
    }
}
