using Dabbler.Security.Entities;
using System.Collections.Generic;

namespace Dabbler.Security.Abstractions;

/// <summary>
/// Defines an interface for services responsible for managing user accounts and retrieving user information.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Retrieves a user by their ID, utilizing a cache if available.
    /// </summary>
    /// <param name="userId">The ID of the user to retrieve.</param>
    /// <returns>A task representing the asynchronous operation, yielding the <see cref="AppUser"/> if found, otherwise null.</returns>
    Task<AppUser?> GetByIdCachedAsync(int userId);

    /// <summary>
    /// Retrieves a user by their email address.
    /// </summary>
    /// <param name="email">The email address of the user to retrieve.</param>
    /// <returns>A task representing the asynchronous operation, yielding the <see cref="AppUser"/> if found, otherwise null.</returns>
    Task<AppUser?> GetByEmailAsync(string email);

    /// <summary>
    /// Retrieves a read-only list of all active application users.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, yielding an <see cref="IReadOnlyList{T}"/> of <see cref="AppUser"/> objects.</returns>
    Task<IReadOnlyList<AppUser>> GetAllAsync();

    /// <summary>
    /// Creates a new user account.
    /// </summary>
    /// <param name="email">The email address for the new user, which must be unique.</param>
    /// <param name="displayName">The display name for the new user.</param>
    /// <param name="createdByUserId">The ID of the user performing this creation.</param>
    /// <returns>A task representing the asynchronous operation, yielding the newly created <see cref="AppUser"/> object.</returns>
    Task<AppUser> CreateAsync(string email, string displayName, int createdByUserId);

    /// <summary>
    /// Updates an existing user's display name and active status.
    /// </summary>
    /// <param name="userId">The ID of the user to update.</param>
    /// <param name="displayName">The new display name for the user.</param>
    /// <param name="isActive">The new active status for the user.</param>
    /// <returns>A task representing the asynchronous update operation.</returns>
    Task UpdateAsync(int userId, string displayName, bool isActive);

    /// <summary>
    /// Deletes a user account, typically by marking it as inactive or soft-deleting.
    /// </summary>
    /// <param name="userId">The ID of the user to delete.</param>
    /// <param name="deletedByUserId">The ID of the user performing this deletion.</param>
    /// <returns>A task representing the asynchronous deletion operation.</returns>
    Task DeleteAsync(int userId, int deletedByUserId);
}
