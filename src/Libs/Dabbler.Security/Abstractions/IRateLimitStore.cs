namespace Dabbler.Security.Abstractions;

/// <summary>
/// Abstraction for rate limit state storage. The default in-memory implementation
/// is suitable for single-server deployments. Use a distributed implementation
/// (backed by IDistributedCache) for multi-server environments.
/// </summary>
public interface IRateLimitStore
{
    /// <summary>
    /// Records an attempt for the given key and returns the total number of
    /// attempts within the sliding window. Expired attempts are purged.
    /// </summary>
    /// <param name="key">The unique identifier for the rate limit (e.g., IP address, user ID).</param>
    /// <param name="window">The sliding time window for counting attempts.</param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous operation.
    /// The task result contains the total number of attempts recorded within the specified window
    /// after recording the current attempt.
    /// </returns>
    Task<int> RecordAttemptAsync(string key, TimeSpan window);

    /// <summary>
    /// Returns the lock expiry time if the key is currently locked, or null.
    /// </summary>
    /// <param name="key">The unique identifier for the rate limit.</param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous operation.
    /// The task result contains a <see cref="DateTime"/> representing the UTC expiry time of the lock,
    /// or <c>null</c> if the key is not currently locked.
    /// </returns>
    Task<DateTime?> GetLockExpiryAsync(string key);

    /// <summary>
    /// Locks the key until the specified UTC time. While locked, further attempts might be rejected
    /// or handled differently depending on the rate limiting policy.
    /// </summary>
    /// <param name="key">The unique identifier for the rate limit.</param>
    /// <param name="until">The UTC time until which the key should remain locked.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    Task SetLockAsync(string key, DateTime until);

    /// <summary>
    /// Clears all recorded attempts and any active lock for the specified key.
    /// </summary>
    /// <param name="key">The unique identifier for the rate limit.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    Task ResetAsync(string key);
}
