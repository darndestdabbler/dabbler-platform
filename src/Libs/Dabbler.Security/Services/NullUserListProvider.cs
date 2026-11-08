namespace Dabbler.Security.Services;

/// <summary>
/// Safety-default <see cref="IUserListProvider"/> that returns an empty user list.
/// Registered automatically so DI doesn't throw when the host hasn't wired up a
/// real identity source. Replace with a real implementation to show users in the admin UI.
/// </summary>
internal sealed class NullUserListProvider : IUserListProvider
{
    /// <summary>
    /// Gets an empty list of users.
    /// </summary>
    /// <param name="ct">A cancellation token (not used).</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an empty read-only list of <see cref="UserSummary"/>.</returns>
    public Task<IReadOnlyList<UserSummary>> GetUsersAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<UserSummary>>(Array.Empty<UserSummary>());
}
