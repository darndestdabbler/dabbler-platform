using Dabbler.Components.Data.Metadata;

namespace Dabbler.Platform.Stubs;

/// <summary>
/// In-memory usage-tracking provider for stub-backed menu flows.
/// </summary>
public sealed class StubUsageTrackingProvider : IUsageTrackingProvider
{
    private readonly List<ArtifactUsageRecord> _records;
    private readonly Lock _gate = new();

    /// <summary>
    /// Initializes a new provider with seed usage records.
    /// </summary>
    public StubUsageTrackingProvider(IEnumerable<ArtifactUsageRecord>? records = null)
    {
        _records = records?.Select(Clone).ToList() ?? [];
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ArtifactUsageRecord>> GetPinnedAsync(string userId, ArtifactType artifactType)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<ArtifactUsageRecord>>(
                _records
                    .Where(record => record.UserId == userId
                        && record.ArtifactType == artifactType
                        && record.IsPinned)
                    .OrderByDescending(record => record.LastAccessedAt)
                    .Select(Clone)
                    .ToArray());
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ArtifactUsageRecord>> GetRecentAsync(string userId, ArtifactType artifactType, int maxItems = 7)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<ArtifactUsageRecord>>(
                _records
                    .Where(record => record.UserId == userId && record.ArtifactType == artifactType)
                    .OrderByDescending(record => record.LastAccessedAt)
                    .Take(maxItems)
                    .Select(Clone)
                    .ToArray());
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ArtifactUsageRecord>> GetFrequentAsync(string userId, ArtifactType artifactType, int maxItems = 7)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<ArtifactUsageRecord>>(
                _records
                    .Where(record => record.UserId == userId && record.ArtifactType == artifactType)
                    .OrderByDescending(record => record.AccessCount)
                    .ThenByDescending(record => record.LastAccessedAt)
                    .Take(maxItems)
                    .Select(Clone)
                    .ToArray());
        }
    }

    /// <inheritdoc />
    public Task TrackAccessAsync(string userId, ArtifactType artifactType, string artifactId)
    {
        lock (_gate)
        {
            var record = FindOrCreate(userId, artifactType, artifactId);
            record.AccessCount += 1;
            record.LastAccessedAt = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetPinnedAsync(string userId, ArtifactType artifactType, string artifactId, bool isPinned)
    {
        lock (_gate)
        {
            var record = FindOrCreate(userId, artifactType, artifactId);
            record.IsPinned = isPinned;
        }

        return Task.CompletedTask;
    }

    private ArtifactUsageRecord FindOrCreate(string userId, ArtifactType artifactType, string artifactId)
    {
        var existing = _records.FirstOrDefault(record =>
            record.UserId == userId
            && record.ArtifactType == artifactType
            && record.ArtifactId == artifactId);

        if (existing is not null)
        {
            return existing;
        }

        var created = new ArtifactUsageRecord
        {
            UserId = userId,
            ArtifactType = artifactType,
            ArtifactId = artifactId,
            LastAccessedAt = DateTime.UtcNow,
            AccessCount = 0,
            IsPinned = false
        };

        _records.Add(created);
        return created;
    }

    private static ArtifactUsageRecord Clone(ArtifactUsageRecord record) =>
        new()
        {
            UserId = record.UserId,
            ArtifactType = record.ArtifactType,
            ArtifactId = record.ArtifactId,
            LastAccessedAt = record.LastAccessedAt,
            AccessCount = record.AccessCount,
            IsPinned = record.IsPinned
        };
}
