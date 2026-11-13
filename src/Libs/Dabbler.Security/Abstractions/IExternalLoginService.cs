using Dabbler.Security.Entities;

namespace Dabbler.Security.Abstractions;

/// <summary>
/// Defines an abstraction for managing external login providers (e.g., Google, Facebook) for application users.
/// </summary>
public interface IExternalLoginService
{
    /// <summary>
    /// Finds an application user associated with a specific external login.
    /// </summary>
    /// <param name="provider">The name of the external login provider (e.g., "Google", "Facebook").</param>
    /// <param name="subjectId">The unique identifier for the user from the external provider.</param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous operation.
    /// The task result contains the <see cref="AppUser"/> if found, otherwise <c>null</c>.
    /// </returns>
    Task<AppUser?> FindByExternalLoginAsync(string provider, string subjectId);

    /// <summary>
    /// Links an existing application user to a new external login.
    /// </summary>
    /// <param name="userId">The unique identifier of the application user.</param>
    /// <param name="provider">The name of the external login provider.</param>
    /// <param name="subjectId">The unique identifier for the user from the external provider.</param>
    /// <param name="email">The email address associated with the external login, if available.</param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous operation.
    /// The task result contains the newly created <see cref="AppExternalLogin"/> entity.
    /// </returns>
    Task<AppExternalLogin> LinkExternalLoginAsync(int userId, string provider, string subjectId, string? email);

    /// <summary>
    /// Provisions a new application user account, links it to an external login, and assigns default roles.
    /// This is typically used for first-time external logins where no existing user account is found.
    /// </summary>
    /// <param name="provider">The name of the external login provider.</param>
    /// <param name="subjectId">The unique identifier for the user from the external provider.</param>
    /// <param name="email">The email address associated with the external login, if available.</param>
    /// <param name="displayName">The display name for the new user account.</param>
    /// <param name="defaultRoles">A list of roles to assign to the newly provisioned user.</param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous operation.
    /// The task result contains the newly created <see cref="AppUser"/> entity.
    /// </returns>
    Task<AppUser> ProvisionAndLinkAsync(string provider, string subjectId, string? email, string displayName, IReadOnlyList<string> defaultRoles);

    /// <summary>
    /// Unlinks a specific external login from an application user.
    /// </summary>
    /// <param name="userId">The unique identifier of the application user.</param>
    /// <param name="provider">The name of the external login provider to unlink.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    Task UnlinkExternalLoginAsync(int userId, string provider);

    /// <summary>
    /// Retrieves a list of all external logins associated with a specific application user.
    /// </summary>
    /// <param name="userId">The unique identifier of the application user.</param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous operation.
    /// The task result contains a read-only list of <see cref="AppExternalLogin"/> entities.
    /// </returns>
    Task<IReadOnlyList<AppExternalLogin>> GetExternalLoginsForUserAsync(int userId);
}
