namespace Dabbler.Security.Services;

/// <summary>
/// Host application implements this adapter so the library's admin UI can display
/// a user picker without coupling to a specific identity store.
/// </summary>
/// <remarks>
/// No default implementation is registered — the host app must provide one
/// if it intends to use any admin UI component that lists users.
/// </remarks>
public interface IUserListProvider
{
    /// <summary>
    /// Gets a list of all users for display purposes.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing a read-only list of user summaries.
    /// </returns>
    Task<IReadOnlyList<UserSummary>> GetUsersAsync(CancellationToken ct = default);
}

/// <summary>
/// Minimal user summary surfaced to the admin UI and role assignment store.
/// </summary>
/// <param name="UserId">The unique identifier for the user.</param>
/// <param name="DisplayName">The name to display for the user.</param>
/// <param name="Email">The user's email address.</param>
public sealed record UserSummary(string UserId, string DisplayName, string Email);
