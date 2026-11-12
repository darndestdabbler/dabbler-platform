using System.Text.Json;
using Dabbler.Security.Abstractions;
using Microsoft.Extensions.Caching.Distributed;

namespace Dabbler.Security.Services;

/// <summary>
/// Rate limit store backed by <see cref="IDistributedCache"/>.
/// Uses a simple counter with TTL per window. Suitable for multi-server deployments
/// when backed by Redis, SQL Server, or another distributed cache provider.
/// </summary>
public class DistributedRateLimitStore : IRateLimitStore
{
    private readonly IDistributedCache _cache;
    private const string Prefix = "ratelimit:";
    private const string LockPrefix = "ratelimit:lock:";

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedRateLimitStore"/> class.
    /// </summary>
    /// <param name="cache">The distributed cache instance to use for storage.</param>
    public DistributedRateLimitStore(IDistributedCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<int> RecordAttemptAsync(string key, TimeSpan window)
    {
        var cacheKey = Prefix + key;
        var now = DateTime.UtcNow;

        // Read existing attempts
        var data = await _cache.GetStringAsync(cacheKey);
        var attempts = data != null
            ? JsonSerializer.Deserialize<List<long>>(data) ?? []
            : new List<long>();

        // Purge expired and add new
        var cutoff = now.Ticks - window.Ticks;
        attempts.RemoveAll(t => t < cutoff);
        attempts.Add(now.Ticks);

        // Store with TTL equal to the window
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(attempts),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = window });

        return attempts.Count;
    }

    /// <inheritdoc />
    public async Task<DateTime?> GetLockExpiryAsync(string key)
    {
        var data = await _cache.GetStringAsync(LockPrefix + key);
        if (data == null) return null;

        var lockExpiry = new DateTime(long.Parse(data), DateTimeKind.Utc);
        if (lockExpiry <= DateTime.UtcNow)
        {
            await _cache.RemoveAsync(LockPrefix + key);
            await _cache.RemoveAsync(Prefix + key);
            return null;
        }

        return lockExpiry;
    }

    /// <inheritdoc />
    public async Task SetLockAsync(string key, DateTime until)
    {
        var ttl = until - DateTime.UtcNow;
        if (ttl <= TimeSpan.Zero) return;

        await _cache.SetStringAsync(LockPrefix + key, until.Ticks.ToString(),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });
    }

    /// <inheritdoc />
    public async Task ResetAsync(string key)
    {
        await _cache.RemoveAsync(Prefix + key);
        await _cache.RemoveAsync(LockPrefix + key);
    }
}
