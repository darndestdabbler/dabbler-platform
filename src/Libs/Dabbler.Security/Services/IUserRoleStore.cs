namespace Dabbler.Security.Services;

/// <summary>
/// Abstracts persistence of role assignments keyed by a string user ID.
/// Decoupled from ClaimsPrincipal so it works with both magic-link users
/// (internal int-keyed table) and external IdP users (string subject claim).
/// </summary>
public interface IUserRoleStore
{
    /// <summary>
    /// Gets the roles assigned to a specified user.
    /// </summary>
    /// <param name="userId">The ID of the user whose roles should be retrieved.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation, containing a read-only list of role names.</returns>
    Task<IReadOnlyList<string>> GetRolesAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Sets the roles for a specified user, replacing any existing roles.
    /// </summary>
    /// <param name="userId">The ID of the user whose roles should be set.</param>
    /// <param name="roles">A collection of role names to assign to the user.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetRolesAsync(string userId, IEnumerable<string> roles, CancellationToken ct = default);

    /// <summary>
    /// Adds a role to a specified user.
    /// </summary>
    /// <param name="userId">The ID of the user to add the role to.</param>
    /// <param name="roleName">The name of the role to add.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddRoleAsync(string userId, string roleName, CancellationToken ct = default);

    /// <summary>
    /// Removes a role from a specified user.
    /// </summary>
    /// <param name="userId">The ID of the user to remove the role from.</param>
    /// <param name="roleName">The name of the role to remove.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveRoleAsync(string userId, string roleName, CancellationToken ct = default);

    /// <summary>
    /// Returns all user ID to role list mappings. Intended for the admin UI.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing a read-only dictionary
    /// where keys are user IDs and values are lists of assigned role names.
    /// </returns>
    Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetAllAsync(CancellationToken ct = default);
}
