using Dabbler.Security.Abstractions;
using Dabbler.Security.Configuration;
using Dabbler.Security.Data;
using Dabbler.Security.Entities;
using Microsoft.EntityFrameworkCore;

namespace DabblerApp.Web.Data;

/// <summary>
/// Seeds admin@local.test with SystemAdmin role (A12) and sample data (A13) on first run.
/// Runs at startup via MigrateAsync + seeding before app.Run().
/// </summary>
public static class AppDbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        // ── Security DB: migrate + seed admin@local.test with SystemAdmin role ──
        var securityDb = sp.GetRequiredService<DabblerSecurityDbContext>();
        await securityDb.Database.MigrateAsync();

        if (!await securityDb.AppUsers.AnyAsync())
        {
            // 1. Seed the SystemAdmin baked-in role
            var systemAdminRole = new AppRole
            {
                Name = "SystemAdmin",
                Description = "Full-access baked-in system administrator role.",
                IsSystemRole = true,
                CreatedAt = DateTime.UtcNow
            };
            securityDb.AppRoles.Add(systemAdminRole);
            await securityDb.SaveChangesAsync();

            // 2. Seed the admin user
            var adminUser = new AppUser
            {
                Email = "admin@local.test",
                DisplayName = "Platform Admin",
                IsActive = true,
                IsProvisioned = true,
                CreatedAt = DateTime.UtcNow
            };
            securityDb.AppUsers.Add(adminUser);
            await securityDb.SaveChangesAsync();

            // 3. Grant SystemAdmin role to admin@local.test (OrganizationMembershipId = null = global)
            securityDb.AppUserRoles.Add(new AppUserRole
            {
                UserId = adminUser.Id,
                RoleId = systemAdminRole.Id,
                OrganizationMembershipId = null,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = adminUser.Id
            });
            await securityDb.SaveChangesAsync();
        }

        // ── Security config: seed empty configuration if absent ───────────────
        var store = sp.GetRequiredService<ISecurityConfigurationStore>();
        var existing = await store.LoadAsync();
        if (existing.UserTypeRoles.Count == 0)
        {
            await store.SaveAsync(new SecurityConfiguration
            {
                UserTypeRoles = [],
                OrganizationSwitching = new OrganizationSwitching { AllowedRoles = [] },
                DefaultMasks = [],
                MaskExemptions = [],
                BakedInRoleAliasOverrides = []
            });
        }

        // ── Sample data (A13 — in-memory EF Core) ─────────────────────────────
        var appDb = sp.GetRequiredService<AppDbContext>();
        if (!await appDb.SampleItems.AnyAsync())
        {
            appDb.SampleItems.AddRange(SampleItemSeed.GetItems());
            await appDb.SaveChangesAsync();
        }
    }
}
