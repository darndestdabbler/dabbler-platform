using Dabbler.Security.Data;
using Dabbler.Security.Services;
using Microsoft.EntityFrameworkCore;

namespace AcmeCoffeeOnline.Web.Services;

/// <summary>
/// Implements IUserListProvider by reading users from the Dabbler EF Core store.
/// Required so the DabblerUserRoleManager admin component can display and search
/// users without coupling the library to a specific identity store.
/// </summary>
public sealed class EfUserListProvider(DabblerSecurityDbContext db) : IUserListProvider
{
    public async Task<IReadOnlyList<UserSummary>> GetUsersAsync(CancellationToken ct = default)
    {
        var users = await db.AppUsers
            .Where(u => u.IsActive)
            .OrderBy(u => u.DisplayName)
            .Select(u => new UserSummary(u.Id.ToString(), u.DisplayName, u.Email))
            .ToListAsync(ct);

        return users;
    }
}
