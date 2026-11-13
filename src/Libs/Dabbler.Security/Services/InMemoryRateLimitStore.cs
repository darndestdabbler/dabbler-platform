using System.Collections.Concurrent;
using Dabbler.Security.Abstractions;

namespace Dabbler.Security.Services;

/// <summary>
/// In-memory sliding-window rate limit store. State is lost on application restart.
/// Suitable for single-server deployments and development.
/// </summary>
public class InMemoryRateLimitStore : IRateLimitStore
{
    private readonly ConcurrentDictionary<string, RateLimitEntry> _entries = new();

    /// <inheritdoc />
    public Task<int> RecordAttemptAsync(string key, TimeSpan window)
    {
        var now = DateTime.UtcNow;
        var entry = _entries.GetOrAdd(key, _ => new RateLimitEntry());

        lock (entry)
        {
            entry.Attempts.RemoveAll(t => now - t > window);
            entry.Attempts.Add(now);
            return Task.FromResult(entry.Attempts.Count);
        }
    }

    /// <inheritdoc />
    public Task<DateTime?> GetLockExpiryAsync(string key)
    {
        if (!_entries.TryGetValue(key, out var entry))
            return Task.FromResult<DateTime?>(null);

        lock (entry)
        {
            if (entry.LockedUntil.HasValue && entry.LockedUntil.Value > DateTime.UtcNow)
                return Task.FromResult(entry.LockedUntil);

            // Lock expired — clear it
            if (entry.LockedUntil.HasValue)
            {
                entry.LockedUntil = null;
                entry.Attempts.Clear();
            }

            return Task.FromResult<DateTime?>(null);
        }
    }

    /// <inheritdoc />
    public Task SetLockAsync(string key, DateTime until)
    {
        var entry = _entries.GetOrAdd(key, _ => new RateLimitEntry());
        lock (entry)
        {
            entry.LockedUntil = until;
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ResetAsync(string key)
    {
        if (_entries.TryGetValue(key, out var entry))
        {
            lock (entry)
            {
                entry.Attempts.Clear();
                entry.LockedUntil = null;
            }
        }
        return Task.CompletedTask;
    }

    private class RateLimitEntry
    {
        public List<DateTime> Attempts { get; } = [];
        public DateTime? LockedUntil { get; set; }
    }
}
