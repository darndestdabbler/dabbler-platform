using Dabbler.Security.Configuration;

namespace Dabbler.Security.Abstractions;

/// <summary>
/// Persistence abstraction for the SecurityConfiguration aggregate.
/// Two implementations ship out of the box: JsonFileSecurityStore (default) and EfCoreSecurityStore.
/// </summary>
public interface ISecurityConfigurationStore
{
    /// <summary>
    /// Loads the entire <see cref="SecurityConfiguration"/> aggregate from the store.
    /// </summary>
    /// <param name="ct">An optional <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous operation.
    /// The task result contains the loaded <see cref="SecurityConfiguration"/> object.
    /// </returns>
    Task<SecurityConfiguration> LoadAsync(CancellationToken ct = default);

    /// <summary>
    /// Saves the entire <see cref="SecurityConfiguration"/> aggregate to the store.
    /// </summary>
    /// <param name="configuration">The <see cref="SecurityConfiguration"/> object to save.</param>
    /// <param name="ct">An optional <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    Task SaveAsync(SecurityConfiguration configuration, CancellationToken ct = default);
}
