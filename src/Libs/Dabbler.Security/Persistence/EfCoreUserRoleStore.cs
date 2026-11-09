using Dabbler.Security.Data;
using Dabbler.Security.Entities;
using Dabbler.Security.Services;
using Microsoft.EntityFrameworkCore;

namespace Dabbler.Security.Persistence;

/// <summary>
/// Persists user-role assignments using the <see cref="AppUserRoleAssignment"/> table.
/// Uses <see cref="IDbContextFactory{TContext}"/> so each operation gets a short-lived
/// DbContext — safe for singleton registration.
/// </summary>
public sealed class EfCoreUserRoleStore : IUserRoleStore
{
    private readonly IDbContextFactory<DabblerSecurityDbContext> _dbFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreUserRoleStore"/> class.
    /// </summary>
    /// <param name="dbFactory">The DbContext factory for creating <see cref="DabblerSecurityDbContext"/> instances.</param>
    public EfCoreUserRoleStore(IDbContextFactory<DabblerSecurityDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetRolesAsync(string userId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.AppUserRoleAssignments
            .Where(a => a.UserId == userId)
            .Select(a => a.RoleName)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task SetRolesAsync(string userId, IEnumerable<string> roles, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var existing = await db.AppUserRoleAssignments
            .Where(a => a.UserId == userId)
            .ToListAsync(ct);

        db.AppUserRoleAssignments.RemoveRange(existing);

        foreach (var roleName in roles)
        {
            db.AppUserRoleAssignments.Add(new AppUserRoleAssignment
            {
                UserId = userId,
                RoleName = roleName
            });
        }

        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task AddRoleAsync(string userId, string roleName, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var exists = await db.AppUserRoleAssignments
            .AnyAsync(a => a.UserId == userId && a.RoleName == roleName, ct);

        if (!exists)
        {
            db.AppUserRoleAssignments.Add(new AppUserRoleAssignment
            {
                UserId = userId,
                RoleName = roleName
            });
            await db.SaveChangesAsync(ct);
        }
    }

    /// <inheritdoc />
    public async Task RemoveRoleAsync(string userId, string roleName, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var assignment = await db.AppUserRoleAssignments
            .FirstOrDefaultAsync(a => a.UserId == userId && a.RoleName == roleName, ct);

        if (assignment is not null)
        {
            db.AppUserRoleAssignments.Remove(assignment);
            await db.SaveChangesAsync(ct);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var all = await db.AppUserRoleAssignments.ToListAsync(ct);

        return all
            .GroupBy(a => a.UserId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g.Select(a => a.RoleName).ToList(),
                StringComparer.OrdinalIgnoreCase);
    }
}
