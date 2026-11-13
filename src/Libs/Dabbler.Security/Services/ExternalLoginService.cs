using Dabbler.Security.Abstractions;
using Dabbler.Security.Data;
using Dabbler.Security.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dabbler.Security.Services;

/// <summary>
/// Manages external login providers for application users.
/// This service handles finding users by their external login, linking new providers to existing users,
/// and provisioning new users from external login information.
/// </summary>
public class ExternalLoginService : IExternalLoginService
{
    private readonly DabblerSecurityDbContext _db;
    private readonly ILogger<ExternalLoginService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalLoginService"/> class.
    /// </summary>
    /// <param name="db">The database context for security data.</param>
    /// <param name="logger">The logger for this service.</param>
    public ExternalLoginService(DabblerSecurityDbContext db, ILogger<ExternalLoginService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Finds a user by their external login provider and subject identifier.
    /// If a user is found, their <see cref="AppExternalLogin.LastUsedAt"/> timestamp is updated.
    /// </summary>
    /// <param name="provider">The name of the external login provider (e.g., "Google", "EntraId").</param>
    /// <param name="subjectId">The unique identifier for the user from the external provider.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the <see cref="AppUser"/>
    /// if found, active, and not deleted; otherwise, <c>null</c>.
    /// </returns>
    public async Task<AppUser?> FindByExternalLoginAsync(string provider, string subjectId)
    {
        // Single tracked query — allows LastUsedAt update without a second round-trip
        var login = await _db.AppExternalLogins
            .Include(x => x.User)
                .ThenInclude(u => u!.Roles)
                    .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(x => x.Provider == provider && x.ProviderSubjectId == subjectId);

        if (login?.User == null)
            return null;

        if (!login.User.IsActive || login.User.DeletedAt != null)
            return null;

        login.LastUsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return login.User;
    }

    /// <summary>
    /// Links an external login to an existing user account.
    /// </summary>
    /// <param name="userId">The ID of the user to link the external login to.</param>
    /// <param name="provider">The name of the external login provider.</param>
    /// <param name="subjectId">The unique identifier for the user from the external provider.</param>
    /// <param name="email">The email address associated with the external login, if available.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created <see cref="AppExternalLogin"/> record.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the specified user does not exist or has been deleted,
    /// or if the external login is already linked to another user.
    /// </exception>
    public async Task<AppExternalLogin> LinkExternalLoginAsync(int userId, string provider, string subjectId, string? email)
    {
        var user = await _db.AppUsers.FindAsync(userId);
        if (user == null || user.DeletedAt != null)
            throw new InvalidOperationException($"User {userId} not found");

        var login = new AppExternalLogin
        {
            UserId = userId,
            Provider = provider,
            ProviderSubjectId = subjectId,
            ProviderEmail = email,
            LinkedAt = DateTime.UtcNow
        };

        _db.AppExternalLogins.Add(login);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException(
                $"External login ({provider}, {subjectId}) is already linked to a user");
        }

        _logger.LogInformation("External login linked: provider={Provider} for user {UserId}",
            provider, userId);

        return login;
    }

    /// <summary>
    /// Creates a new user account based on external login information and links the external login to it.
    /// If an active user with the same email already exists, it links the external login to that user instead of creating a new one.
    /// </summary>
    /// <param name="provider">The name of the external login provider.</param>
    /// <param name="subjectId">The unique identifier for the user from the external provider.</param>
    /// <param name="email">The email address from the external provider. This is required for provisioning.</param>
    /// <param name="displayName">The display name from the external provider.</param>
    /// <param name="defaultRoles">A list of role names to assign to the user if a new account is created.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the newly provisioned or linked <see cref="AppUser"/>,
    /// including their assigned roles.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the email is null or empty, or if the external login is already linked to another user.
    /// </exception>
    public async Task<AppUser> ProvisionAndLinkAsync(
        string provider,
        string subjectId,
        string? email,
        string displayName,
        IReadOnlyList<string> defaultRoles)
    {
        var normalizedEmail = email?.ToLowerInvariant();

        if (string.IsNullOrEmpty(normalizedEmail))
            throw new InvalidOperationException("Email is required for user provisioning");

        // Check if a user with this email already exists — link to them instead of creating a duplicate
        var existingUser = await _db.AppUsers
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.DeletedAt == null);

        var user = existingUser ?? new AppUser
        {
            Email = normalizedEmail,
            DisplayName = displayName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        if (existingUser == null)
        {
            _db.AppUsers.Add(user);
            await _db.SaveChangesAsync();

            // Assign default roles
            foreach (var roleName in defaultRoles)
            {
                var role = await _db.AppRoles
                    .FirstOrDefaultAsync(r => r.Name == roleName && r.DeletedAt == null);
                if (role != null)
                {
                    _db.AppUserRoles.Add(new AppUserRole
                    {
                        UserId = user.Id,
                        RoleId = role.Id,
                        AssignedAt = DateTime.UtcNow,
                        AssignedBy = user.Id
                    });
                }
            }

            await _db.SaveChangesAsync();
        }

        var login = new AppExternalLogin
        {
            UserId = user.Id,
            Provider = provider,
            ProviderSubjectId = subjectId,
            ProviderEmail = email,
            LinkedAt = DateTime.UtcNow
        };

        _db.AppExternalLogins.Add(login);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException(
                $"External login ({provider}, {subjectId}) is already linked to a user");
        }

        _logger.LogInformation("User provisioned and linked: {Email} via {Provider}",
            MaskEmail(normalizedEmail), provider);

        // Reload user with roles so the caller has a complete entity
        var reloaded = await _db.AppUsers
            .Include(u => u.Roles)
                .ThenInclude(ur => ur.Role)
            .FirstAsync(u => u.Id == user.Id);

        return reloaded;
    }

    /// <summary>
    /// Removes the link between a user and an external login provider.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="provider">The name of the external login provider to unlink.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if no external login for the specified provider is found for the user.
    /// </exception>
    public async Task UnlinkExternalLoginAsync(int userId, string provider)
    {
        var login = await _db.AppExternalLogins
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Provider == provider);

        if (login == null)
            throw new InvalidOperationException($"No external login for provider {provider} found for user {userId}");

        _db.AppExternalLogins.Remove(login);
        await _db.SaveChangesAsync();

        _logger.LogInformation("External login unlinked: provider={Provider} for user {UserId}",
            provider, userId);
    }

    /// <summary>
    /// Retrieves all external logins linked to a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a read-only list
    /// of the user's <see cref="AppExternalLogin"/> records.
    /// </returns>
    public async Task<IReadOnlyList<AppExternalLogin>> GetExternalLoginsForUserAsync(int userId)
    {
        return await _db.AppExternalLogins
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToListAsync();
    }



    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return "***";

        var parts = email.Split('@');
        var local = parts[0];
        var domain = parts[1];

        var maskedLocal = local.Length > 2
            ? $"{local[0]}{new string('*', local.Length - 2)}{local[^1]}"
            : new string('*', local.Length);

        return $"{maskedLocal}@{domain}";
    }
}
