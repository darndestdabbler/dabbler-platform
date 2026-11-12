using Dabbler.Security.Abstractions;
using Dabbler.Security.Data;
using Dabbler.Security.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dabbler.Security.Services;

/// <summary>
/// Implements <see cref="IUserService"/> to manage user accounts.
/// </summary>
public class UserService : IUserService
{
    private readonly DabblerSecurityDbContext _db;
    private readonly ILogger<UserService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserService"/> class.
    /// </summary>
    /// <param name="db">The database context for security data.</param>
    /// <param name="logger">The logger for this service.</param>
    public UserService(DabblerSecurityDbContext db, ILogger<UserService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a user by their unique identifier, including their roles.
    /// This method does not track the returned entity.
    /// </summary>
    /// <param name="userId">The ID of the user to retrieve.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the <see cref="AppUser"/> if found; otherwise, <c>null</c>.
    /// </returns>
    public async Task<AppUser?> GetByIdCachedAsync(int userId)
    {
        return await _db.AppUsers
            .AsNoTracking()
            .Include(u => u.Roles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);
    }

    /// <summary>
    /// Retrieves a user by their email address, including their roles.
    /// This method performs a case-insensitive search and does not track the returned entity.
    /// </summary>
    /// <param name="email">The email address of the user to retrieve.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the <see cref="AppUser"/> if found; otherwise, <c>null</c>.
    /// </returns>
    public async Task<AppUser?> GetByEmailAsync(string email)
    {
        var normalizedEmail = email.ToLowerInvariant();
        return await _db.AppUsers
            .AsNoTracking()
            .Include(u => u.Roles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.DeletedAt == null);
    }

    /// <summary>
    /// Retrieves a list of all non-deleted users, including their roles and external logins.
    /// This method does not track the returned entities.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a read-only list of all <see cref="AppUser"/>s.
    /// </returns>
    public async Task<IReadOnlyList<AppUser>> GetAllAsync()
    {
        return await _db.AppUsers
            .AsNoTracking()
            .Where(u => u.DeletedAt == null)
            .Include(u => u.Roles)
            .ThenInclude(ur => ur.Role)
            .Include(u => u.ExternalLogins)
            .ToListAsync();
    }

    /// <summary>
    /// Creates a new user with the specified details.
    /// </summary>
    /// <param name="email">The user's email address. It will be normalized to lowercase.</param>
    /// <param name="displayName">The user's display name.</param>
    /// <param name="createdByUserId">The ID of the user performing the creation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the newly created <see cref="AppUser"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if a user with the specified email already exists.</exception>
    public async Task<AppUser> CreateAsync(string email, string displayName, int createdByUserId)
    {
        var normalizedEmail = email.ToLowerInvariant();
        
        if (await _db.AppUsers.AnyAsync(u => u.Email == normalizedEmail && u.DeletedAt == null))
        {
            throw new InvalidOperationException($"User with email {email} already exists");
        }

        var user = new AppUser
        {
            Email = normalizedEmail,
            DisplayName = displayName,
            IsActive = true,
            CreatedBy = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User created: {Email} by user {CreatedBy}", 
            MaskEmail(email), createdByUserId);

        return user;
    }

    /// <summary>
    /// Updates an existing user's display name and active status.
    /// </summary>
    /// <param name="userId">The ID of the user to update.</param>
    /// <param name="displayName">The new display name for the user.</param>
    /// <param name="isActive">The new active status for the user.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the user with the specified ID is not found.</exception>
    public async Task UpdateAsync(int userId, string displayName, bool isActive)
    {
        var user = await _db.AppUsers.FindAsync(userId);
        if (user == null || user.DeletedAt != null)
        {
            throw new InvalidOperationException($"User {userId} not found");
        }

        user.DisplayName = displayName;
        user.IsActive = isActive;

        await _db.SaveChangesAsync();

        _logger.LogInformation("User updated: {UserId}", userId);
    }

    /// <summary>
    /// Deletes a user. This operation first marks the user as deleted to create a temporal history record,
    /// then performs a hard delete from the database.
    /// </summary>
    /// <param name="userId">The ID of the user to delete.</param>
    /// <param name="deletedByUserId">The ID of the user performing the deletion.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the user with the specified ID is not found.</exception>
    public async Task DeleteAsync(int userId, int deletedByUserId)
    {
        var user = await _db.AppUsers.FindAsync(userId);
        if (user == null || user.DeletedAt != null)
        {
            throw new InvalidOperationException($"User {userId} not found");
        }

        // Step 1: Stamp the deletion — this creates a temporal history entry
        // that records who deleted and when
        user.DeletedAt = DateTime.UtcNow;
        user.DeletedBy = deletedByUserId;
        await _db.SaveChangesAsync();

        // Step 2: Hard delete the row
        _db.AppUsers.Remove(user);
        await _db.SaveChangesAsync();

        _logger.LogWarning("User deleted: {UserId} by user {DeletedBy}", 
            userId, deletedByUserId);
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
