using Dabbler.Security.Abstractions;
using Dabbler.Security.Data;
using Dabbler.Security.Models;
using Microsoft.EntityFrameworkCore;

namespace Dabbler.Security.Services;

/// <summary>
/// Implements <see cref="IAuditService"/> to provide access to entity change history
/// using SQL Server temporal tables.
/// </summary>
public class AuditService : IAuditService
{
    private readonly DabblerSecurityDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditService"/> class.
    /// </summary>
    /// <param name="db">The database context for security data.</param>
    public AuditService(DabblerSecurityDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Retrieves the history of role assignments for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user to query history for.</param>
    /// <param name="from">The optional start of the date range to query.</param>
    /// <param name="to">The optional end of the date range to query.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a list of <see cref="AuditEntry"/> records for the user's role changes.
    /// </returns>
    public async Task<List<AuditEntry>> GetUserHistoryAsync(int userId, DateTime? from, DateTime? to)
    {
        var query = _db.AppUserRoles
            .TemporalAll()
            .Where(r => r.UserId == userId);

        if (from.HasValue)
            query = query.Where(r => EF.Property<DateTime>(r, "ValidFrom") >= from.Value);
        if (to.HasValue)
            query = query.Where(r => EF.Property<DateTime>(r, "ValidFrom") <= to.Value);

        return await query
            .OrderByDescending(r => EF.Property<DateTime>(r, "ValidFrom"))
            .Select(r => new AuditEntry
            {
                Timestamp = EF.Property<DateTime>(r, "ValidFrom"),
                EntityType = "AppUserRole",
                EntityId = $"{r.UserId}-{r.RoleId}",
                ChangedBy = r.AssignedBy.ToString(),
                Operation = r.DeletedAt == null ? "Assigned" : "Removed",
                Details = $"Role: {r.RoleId}"
            })
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves the history of role assignments, with optional filters for user and role.
    /// </summary>
    /// <param name="userId">The optional user ID to filter by.</param>
    /// <param name="roleId">The optional role ID to filter by.</param>
    /// <param name="from">The optional start of the date range to query.</param>
    /// <param name="to">The optional end of the date range to query.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a list of <see cref="AuditEntry"/> records for role assignments matching the criteria.
    /// </returns>
    public async Task<List<AuditEntry>> GetRoleAssignmentHistoryAsync(int? userId, int? roleId, DateTime? from, DateTime? to)
    {
        var query = _db.AppUserRoles.TemporalAll().AsQueryable();

        if (userId.HasValue)
            query = query.Where(r => r.UserId == userId.Value);
        if (roleId.HasValue)
            query = query.Where(r => r.RoleId == roleId.Value);
        if (from.HasValue)
            query = query.Where(r => EF.Property<DateTime>(r, "ValidFrom") >= from.Value);
        if (to.HasValue)
            query = query.Where(r => EF.Property<DateTime>(r, "ValidFrom") <= to.Value);

        return await query
            .OrderByDescending(r => EF.Property<DateTime>(r, "ValidFrom"))
            .Select(r => new AuditEntry
            {
                Timestamp = EF.Property<DateTime>(r, "ValidFrom"),
                EntityType = "AppUserRole",
                EntityId = $"{r.UserId}-{r.RoleId}",
                ChangedBy = r.AssignedBy.ToString(),
                Operation = r.DeletedAt == null ? "Assigned" : "Removed",
                Details = $"User: {r.UserId}, Role: {r.RoleId}"
            })
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves a paginated list of all changes across multiple audited entities within a specified date range.
    /// </summary>
    /// <param name="from">The start of the date range to query.</param>
    /// <param name="to">The end of the date range to query.</param>
    /// <param name="page">The page number to retrieve.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a list of all <see cref="AuditEntry"/> records within the specified range and page.
    /// </returns>
    public async Task<List<AuditEntry>> GetAllChangesAsync(DateTime from, DateTime to, int page, int pageSize)
    {
        var allEntries = new List<AuditEntry>();

        var userRoleChanges = await _db.AppUserRoles
            .TemporalAll()
            .Where(r => EF.Property<DateTime>(r, "ValidFrom") >= from && EF.Property<DateTime>(r, "ValidFrom") <= to)
            .Select(r => new AuditEntry
            {
                Timestamp = EF.Property<DateTime>(r, "ValidFrom"),
                EntityType = "AppUserRole",
                EntityId = $"{r.UserId}-{r.RoleId}",
                ChangedBy = r.AssignedBy.ToString(),
                Operation = r.DeletedAt == null ? "Assigned" : "Removed",
                Details = $"User: {r.UserId}, Role: {r.RoleId}"
            })
            .ToListAsync();
        allEntries.AddRange(userRoleChanges);

        var userChanges = await _db.AppUsers
            .TemporalAll()
            .Where(u => EF.Property<DateTime>(u, "ValidFrom") >= from && EF.Property<DateTime>(u, "ValidFrom") <= to)
            .Select(u => new AuditEntry
            {
                Timestamp = EF.Property<DateTime>(u, "ValidFrom"),
                EntityType = "AppUser",
                EntityId = u.Id.ToString(),
                ChangedBy = u.DeletedAt == null ? u.CreatedBy.ToString() : u.DeletedBy.ToString(),
                Operation = u.DeletedAt == null ? "Created/Updated" : "Deleted",
                Details = $"Email: {u.Email}"
            })
            .ToListAsync();
        allEntries.AddRange(userChanges);

        var roleChanges = await _db.AppRoles
            .TemporalAll()
            .Where(r => EF.Property<DateTime>(r, "ValidFrom") >= from && EF.Property<DateTime>(r, "ValidFrom") <= to)
            .Select(r => new AuditEntry
            {
                Timestamp = EF.Property<DateTime>(r, "ValidFrom"),
                EntityType = "AppRole",
                EntityId = r.Id.ToString(),
                ChangedBy = r.DeletedBy.ToString(),
                Operation = r.DeletedAt == null ? "Created/Updated" : "Deleted",
                Details = $"Role: {r.Name}"
            })
            .ToListAsync();
        allEntries.AddRange(roleChanges);

        var mfaChanges = await _db.AppUserMfas
            .TemporalAll()
            .Where(m => EF.Property<DateTime>(m, "ValidFrom") >= from && EF.Property<DateTime>(m, "ValidFrom") <= to)
            .Select(m => new AuditEntry
            {
                Timestamp = EF.Property<DateTime>(m, "ValidFrom"),
                EntityType = "AppUserMfa",
                EntityId = m.UserId.ToString(),
                ChangedBy = "System",
                Operation = m.IsVerified ? "Enrolled/Verified" : "Initiated/Reset",
                Details = $"User: {m.UserId}"
            })
            .ToListAsync();
        allEntries.AddRange(mfaChanges);

        return allEntries
            .OrderByDescending(e => e.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }
}
