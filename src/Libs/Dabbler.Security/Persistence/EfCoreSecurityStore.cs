using Dabbler.Security.Abstractions;
using Dabbler.Security.Configuration;
using Dabbler.Security.Data;
using Dabbler.Security.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dabbler.Security.Persistence;

/// <summary>
/// Persists SecurityConfiguration using the EF Core DabblerSecurityDbContext.
/// Maps between the existing permission tables (AppRolePermission, AppFieldPermission,
/// AppPagePermission) and the new runtime tables (AppEndpointPermission,
/// AppControlPermission, AppRowFilter).
/// </summary>
public sealed class EfCoreSecurityStore : ISecurityConfigurationStore
{
    private readonly IDbContextFactory<DabblerSecurityDbContext> _dbFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreSecurityStore"/> class.
    /// </summary>
    /// <param name="dbFactory">The DbContext factory for creating <see cref="DabblerSecurityDbContext"/> instances.</param>
    public EfCoreSecurityStore(IDbContextFactory<DabblerSecurityDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <inheritdoc />
    public async Task<SecurityConfiguration> LoadAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var roles = await db.AppRoles
            .Where(r => r.DeletedAt == null)
            .ToListAsync(ct);

        var entityPerms = await db.AppRolePermissions
            .Include(p => p.Role)
            .ToListAsync(ct);

        var fieldPerms = await db.AppFieldPermissions
            .Include(p => p.Role)
            .ToListAsync(ct);

        var pagePerms = await db.AppPagePermissions
            .Include(p => p.Role)
            .ToListAsync(ct);

        var endpointPerms = await db.AppEndpointPermissions
            .Include(p => p.Role)
            .ToListAsync(ct);

        var controlPerms = await db.AppControlPermissions
            .Include(p => p.Role)
            .ToListAsync(ct);

        var rowFilters = await db.AppRowFilters
            .Include(f => f.Role)
            .ToListAsync(ct);

        return new SecurityConfiguration
        {
            Roles = roles.Select(r => new RoleDefinition
            {
                RoleName = r.Name,
                Description = r.Description,
                InheritsFrom = r.InheritsFromRoleName
            }).ToList(),

            EntityPermissions = entityPerms
                .GroupBy(p => p.EntityName)
                .Select(g => new EntityPermission
                {
                    EntityKey = g.Key,
                    RolePermissions = g
                        .GroupBy(p => p.Role!.Name)
                        .Select(rg => new EntityRolePermission
                        {
                            RoleName = rg.Key,
                            AllowedOperations = rg.Select(p => p.PermissionType).ToList()
                        }).ToList()
                }).ToList(),

            FieldPermissions = fieldPerms
                .GroupBy(p => new { p.EntityName, p.FieldName })
                .Select(g => new FieldPermission
                {
                    EntityKey = g.Key.EntityName,
                    FieldKey = g.Key.FieldName,
                    RolePermissions = g.Select(p => new FieldRolePermission
                    {
                        RoleName = p.Role!.Name,
                        AccessLevel = MapFieldAccessLevel(p.AccessLevel)
                    }).ToList()
                }).ToList(),

            PagePermissions = pagePerms
                .GroupBy(p => p.PageRoute)
                .Select(g => new PagePermission
                {
                    PageKey = g.Key,
                    AllowedRoles = g.Where(p => p.Allowed).Select(p => p.Role!.Name).ToList(),
                    DeniedRoles = g.Where(p => !p.Allowed).Select(p => p.Role!.Name).ToList()
                }).ToList(),

            EndpointPermissions = endpointPerms
                .GroupBy(p => p.EndpointKey)
                .Select(g => new EndpointPermission
                {
                    EndpointKey = g.Key,
                    AllowedRoles = g.Select(p => p.Role!.Name).ToList()
                }).ToList(),

            ControlPermissions = controlPerms
                .GroupBy(p => new { p.PageKey, p.ControlKey })
                .Select(g => new ControlPermission
                {
                    PageKey = g.Key.PageKey,
                    ControlKey = g.Key.ControlKey,
                    RolePermissions = g.Select(p => new ControlRolePermission
                    {
                        RoleName = p.Role!.Name,
                        State = p.State
                    }).ToList()
                }).ToList(),

            RowFilters = rowFilters.Select(f => new RowFilter
            {
                EntityKey = f.EntityKey,
                RoleName = f.Role!.Name,
                FilterExpression = f.FilterExpression,
                Description = f.Description
            }).ToList()
        };
    }

    /// <inheritdoc />
    public async Task SaveAsync(SecurityConfiguration configuration, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Use an explicit transaction when the provider supports it (e.g., relational databases).
        // The in-memory provider does not support transactions, so we skip gracefully.
        var supportsTransactions = db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";
        var transaction = supportsTransactions ? await db.Database.BeginTransactionAsync(ct) : null;

        try
        {
            // Build a role name → id lookup (only active roles, case-insensitive)
            var rolesByName = await db.AppRoles
                .Where(r => r.DeletedAt == null)
                .ToDictionaryAsync(r => r.Name, StringComparer.OrdinalIgnoreCase, ct);

            await SyncEntityPermissionsAsync(db, configuration, rolesByName, ct);
            await SyncFieldPermissionsAsync(db, configuration, rolesByName, ct);
            await SyncPagePermissionsAsync(db, configuration, rolesByName, ct);
            await SyncEndpointPermissionsAsync(db, configuration, rolesByName, ct);
            await SyncControlPermissionsAsync(db, configuration, rolesByName, ct);
            await SyncRowFiltersAsync(db, configuration, rolesByName, ct);

            await db.SaveChangesAsync(ct);

            if (transaction is not null)
                await transaction.CommitAsync(ct);
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    // --- private sync helpers ---

    private static async Task SyncEntityPermissionsAsync(
        DabblerSecurityDbContext db,
        SecurityConfiguration config,
        Dictionary<string, AppRole> rolesByName,
        CancellationToken ct)
    {
        var existing = await db.AppRolePermissions.ToListAsync(ct);
        db.AppRolePermissions.RemoveRange(existing);

        foreach (var entityPerm in config.EntityPermissions)
        {
            foreach (var rp in entityPerm.RolePermissions)
            {
                if (!rolesByName.TryGetValue(rp.RoleName, out var role)) continue;
                foreach (var op in rp.AllowedOperations)
                {
                    db.AppRolePermissions.Add(new AppRolePermission
                    {
                        RoleId = role.Id,
                        EntityName = entityPerm.EntityKey,
                        PermissionType = op
                    });
                }
            }
        }
    }

    private static async Task SyncFieldPermissionsAsync(
        DabblerSecurityDbContext db,
        SecurityConfiguration config,
        Dictionary<string, AppRole> rolesByName,
        CancellationToken ct)
    {
        var existing = await db.AppFieldPermissions.ToListAsync(ct);
        db.AppFieldPermissions.RemoveRange(existing);

        foreach (var fieldPerm in config.FieldPermissions)
        {
            foreach (var rp in fieldPerm.RolePermissions)
            {
                if (!rolesByName.TryGetValue(rp.RoleName, out var role)) continue;
                db.AppFieldPermissions.Add(new AppFieldPermission
                {
                    RoleId = role.Id,
                    EntityName = fieldPerm.EntityKey,
                    FieldName = fieldPerm.FieldKey,
                    AccessLevel = MapFieldAccessLevelToString(rp.AccessLevel)
                });
            }
        }
    }

    private static async Task SyncPagePermissionsAsync(
        DabblerSecurityDbContext db,
        SecurityConfiguration config,
        Dictionary<string, AppRole> rolesByName,
        CancellationToken ct)
    {
        var existing = await db.AppPagePermissions.ToListAsync(ct);
        db.AppPagePermissions.RemoveRange(existing);

        foreach (var pagePerm in config.PagePermissions)
        {
            foreach (var roleName in pagePerm.AllowedRoles)
            {
                if (!rolesByName.TryGetValue(roleName, out var role)) continue;
                db.AppPagePermissions.Add(new AppPagePermission
                {
                    RoleId = role.Id,
                    PageRoute = pagePerm.PageKey,
                    Allowed = true
                });
            }
            foreach (var roleName in pagePerm.DeniedRoles)
            {
                if (!rolesByName.TryGetValue(roleName, out var role)) continue;
                db.AppPagePermissions.Add(new AppPagePermission
                {
                    RoleId = role.Id,
                    PageRoute = pagePerm.PageKey,
                    Allowed = false
                });
            }
        }
    }

    private static async Task SyncEndpointPermissionsAsync(
        DabblerSecurityDbContext db,
        SecurityConfiguration config,
        Dictionary<string, AppRole> rolesByName,
        CancellationToken ct)
    {
        var existing = await db.AppEndpointPermissions.ToListAsync(ct);
        db.AppEndpointPermissions.RemoveRange(existing);

        foreach (var ep in config.EndpointPermissions)
        {
            foreach (var roleName in ep.AllowedRoles)
            {
                if (!rolesByName.TryGetValue(roleName, out var role)) continue;
                db.AppEndpointPermissions.Add(new AppEndpointPermission
                {
                    RoleId = role.Id,
                    EndpointKey = ep.EndpointKey
                });
            }
        }
    }

    private static async Task SyncControlPermissionsAsync(
        DabblerSecurityDbContext db,
        SecurityConfiguration config,
        Dictionary<string, AppRole> rolesByName,
        CancellationToken ct)
    {
        var existing = await db.AppControlPermissions.ToListAsync(ct);
        db.AppControlPermissions.RemoveRange(existing);

        foreach (var cp in config.ControlPermissions)
        {
            foreach (var rp in cp.RolePermissions)
            {
                if (!rolesByName.TryGetValue(rp.RoleName, out var role)) continue;
                db.AppControlPermissions.Add(new AppControlPermission
                {
                    RoleId = role.Id,
                    PageKey = cp.PageKey,
                    ControlKey = cp.ControlKey,
                    State = rp.State
                });
            }
        }
    }

    private static async Task SyncRowFiltersAsync(
        DabblerSecurityDbContext db,
        SecurityConfiguration config,
        Dictionary<string, AppRole> rolesByName,
        CancellationToken ct)
    {
        var existing = await db.AppRowFilters.ToListAsync(ct);
        db.AppRowFilters.RemoveRange(existing);

        foreach (var rf in config.RowFilters)
        {
            if (!rolesByName.TryGetValue(rf.RoleName, out var role)) continue;
            db.AppRowFilters.Add(new AppRowFilter
            {
                RoleId = role.Id,
                EntityKey = rf.EntityKey,
                FilterExpression = rf.FilterExpression,
                Description = rf.Description
            });
        }
    }

    // --- mapping helpers ---

    private static FieldAccessLevel MapFieldAccessLevel(string accessLevel) =>
        accessLevel switch
        {
            "Editable" or "ReadWrite" => FieldAccessLevel.Editable,
            "ReadOnly" => FieldAccessLevel.ReadOnly,
            "Hidden" => FieldAccessLevel.Hidden,
            _ => FieldAccessLevel.Editable
        };

    private static string MapFieldAccessLevelToString(FieldAccessLevel level) =>
        level switch
        {
            FieldAccessLevel.Editable => "Editable",
            FieldAccessLevel.ReadOnly => "ReadOnly",
            FieldAccessLevel.Hidden => "Hidden",
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown FieldAccessLevel value.")
        };
}
