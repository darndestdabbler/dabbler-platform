using Dabbler.Security.Data;
using Dabbler.Security.Entities;
using Dabbler.Security.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AcmeCoffeeOnline.Api.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AcmeCoffeeDbContext>();
        var securityContext = scope.ServiceProvider.GetRequiredService<DabblerSecurityDbContext>();
        var securityOptions = scope.ServiceProvider.GetRequiredService<IOptions<DabblerSecurityOptions>>().Value;

        await context.Database.EnsureCreatedAsync();
        await EnsureAcmeCoffeeSchemaAsync(context);
        await securityContext.Database.MigrateAsync();

        if (!await context.Products.AnyAsync())
        {
            // Phase 1: Products and Customers (no FK dependencies)
            var products = SeedData.GetProducts();
            var customers = SeedData.GetCustomers();
            context.Products.AddRange(products);
            context.Customers.AddRange(customers);
            await context.SaveChangesAsync();

            // Phase 2: Orders with items and notes (needs product/customer IDs)
            var orders = SeedData.GetOrders(customers, products);
            context.Orders.AddRange(orders);
            await context.SaveChangesAsync();
        }

        if (!await securityContext.AppRoles.AnyAsync())
        {
            var customers = await context.Customers
                .AsNoTracking()
                .OrderBy(customer => customer.Id)
                .ToListAsync();

            var organizations = SeedData.GetOrganizations(
                customers,
                securityOptions.DefaultOrganizationKey,
                securityOptions.DefaultOrganizationName);
            securityContext.AppOrganizations.AddRange(organizations);
            await securityContext.SaveChangesAsync();

            // Phase 3: Security roles
            var roles = SeedData.GetSecurityRoles();
            securityContext.AppRoles.AddRange(roles);
            await securityContext.SaveChangesAsync();

            // Phase 4: Security users
            var users = SeedData.GetSecurityUsers(customers);
            securityContext.AppUsers.AddRange(users);
            await securityContext.SaveChangesAsync();

            // Phase 5: Organization memberships and role assignments.
            var memberships = SeedData.GetOrganizationMemberships(users, organizations, customers);
            securityContext.AppUserOrganizationMemberships.AddRange(memberships);
            await securityContext.SaveChangesAsync();

            var assignments = SeedData.GetUserRoleAssignments(users, roles, memberships);
            securityContext.AppUserRoles.AddRange(assignments);
            await securityContext.SaveChangesAsync();

            // Phase 6: External login links (DevIdp OID → AppUser)
            if (!await securityContext.AppExternalLogins.AnyAsync())
            {
                var externalLogins = SeedData.GetExternalLogins(users);
                securityContext.AppExternalLogins.AddRange(externalLogins);
                await securityContext.SaveChangesAsync();
            }
        }
        else
        {
            await BackfillSecuritySeedDataAsync(context, securityContext, securityOptions);
        }

        // Phase 7: Runtime security configuration (roles, permissions, row filters)
        await SecuritySeedData.SeedAsync(services);
    }

    // Applies schema changes that EnsureCreated misses on existing databases.
    // Each block is idempotent and guarded by a column-existence check.
    private static async Task EnsureAcmeCoffeeSchemaAsync(AcmeCoffeeDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (
                SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID(N'[dbo].[Customers]')
                  AND name = N'SocialSecurityNumber')
            BEGIN
                ALTER TABLE [dbo].[Customers]
                    ADD [SocialSecurityNumber] nvarchar(11) NULL;
            END");

        // Backfill the well-known test SSN that SeedData.GetCustomers sets
        // for customer 0. The seeder only fires on initial seed; for
        // pre-existing dev/CI DBs the ALTER above adds the column with
        // NULLs and never replays the seed expression, so the masking
        // UAT's "real value under exemption" path renders empty. Apply
        // the backfill conditionally so a deliberately-cleared value
        // doesn't get reset on every startup.
        await context.Database.ExecuteSqlRawAsync(@"
            UPDATE [dbo].[Customers]
            SET [SocialSecurityNumber] = N'123-45-6789'
            WHERE [Id] = 1
              AND [SocialSecurityNumber] IS NULL;");
    }

    private static async Task BackfillSecuritySeedDataAsync(
        AcmeCoffeeDbContext context,
        DabblerSecurityDbContext securityContext,
        DabblerSecurityOptions securityOptions)
    {
        var customers = await context.Customers
            .AsNoTracking()
            .OrderBy(customer => customer.Id)
            .ToListAsync();

        var desiredOrganizations = SeedData.GetOrganizations(
            customers,
            securityOptions.DefaultOrganizationKey,
            securityOptions.DefaultOrganizationName);
        await EnsureOrganizationsAsync(securityContext, desiredOrganizations);

        var desiredRoles = SeedData.GetSecurityRoles();
        await EnsureRolesAsync(securityContext, desiredRoles);

        var desiredUsers = SeedData.GetSecurityUsers(customers);
        await EnsureUsersAsync(securityContext, desiredUsers);

        var actualUsers = await securityContext.AppUsers
            .AsNoTracking()
            .ToListAsync();
        var actualOrganizations = await securityContext.AppOrganizations
            .AsNoTracking()
            .ToListAsync();
        var actualRoles = await securityContext.AppRoles
            .AsNoTracking()
            .ToListAsync();

        var desiredMemberships = SeedData.GetOrganizationMemberships(actualUsers, actualOrganizations, customers);
        await EnsureMembershipsAsync(securityContext, desiredMemberships);

        var actualMemberships = await securityContext.AppUserOrganizationMemberships
            .AsNoTracking()
            .ToListAsync();
        var desiredAssignments = SeedData.GetUserRoleAssignments(actualUsers, actualRoles, actualMemberships);
        await EnsureUserRolesAsync(securityContext, desiredAssignments);

        var desiredExternalLogins = SeedData.GetExternalLogins(actualUsers);
        await EnsureExternalLoginsAsync(securityContext, desiredExternalLogins);
    }

    private static async Task EnsureOrganizationsAsync(
        DabblerSecurityDbContext securityContext,
        IReadOnlyList<AppOrganization> desiredOrganizations)
    {
        var existingOrganizations = await securityContext.AppOrganizations.ToListAsync();
        var changed = false;

        foreach (var desiredOrganization in desiredOrganizations)
        {
            var existingOrganization = existingOrganizations.FirstOrDefault(organization =>
                string.Equals(organization.Key, desiredOrganization.Key, StringComparison.OrdinalIgnoreCase));

            if (existingOrganization is null)
            {
                securityContext.AppOrganizations.Add(new AppOrganization
                {
                    Key = desiredOrganization.Key,
                    Name = desiredOrganization.Name,
                    IsInternal = desiredOrganization.IsInternal,
                    IsActive = desiredOrganization.IsActive,
                    CreatedAt = desiredOrganization.CreatedAt
                });
                changed = true;
                continue;
            }

            if (!string.Equals(existingOrganization.Name, desiredOrganization.Name, StringComparison.Ordinal)
                || existingOrganization.IsInternal != desiredOrganization.IsInternal
                || existingOrganization.IsActive != desiredOrganization.IsActive)
            {
                existingOrganization.Name = desiredOrganization.Name;
                existingOrganization.IsInternal = desiredOrganization.IsInternal;
                existingOrganization.IsActive = desiredOrganization.IsActive;
                changed = true;
            }
        }

        if (changed)
        {
            await securityContext.SaveChangesAsync();
        }
    }

    private static async Task EnsureRolesAsync(
        DabblerSecurityDbContext securityContext,
        IReadOnlyList<AppRole> desiredRoles)
    {
        var existingRoles = await securityContext.AppRoles.ToListAsync();
        var changed = false;

        foreach (var desiredRole in desiredRoles)
        {
            var existingRole = existingRoles.FirstOrDefault(role =>
                string.Equals(role.Name, desiredRole.Name, StringComparison.OrdinalIgnoreCase));

            if (existingRole is null)
            {
                securityContext.AppRoles.Add(new AppRole
                {
                    Name = desiredRole.Name,
                    Description = desiredRole.Description,
                    IsSystemRole = desiredRole.IsSystemRole,
                    InheritsFromRoleName = desiredRole.InheritsFromRoleName,
                    CreatedAt = desiredRole.CreatedAt
                });
                changed = true;
                continue;
            }

            if (!string.Equals(existingRole.Description, desiredRole.Description, StringComparison.Ordinal)
                || existingRole.IsSystemRole != desiredRole.IsSystemRole
                || !string.Equals(existingRole.InheritsFromRoleName, desiredRole.InheritsFromRoleName, StringComparison.Ordinal)
                || existingRole.DeletedAt is not null)
            {
                existingRole.Description = desiredRole.Description;
                existingRole.IsSystemRole = desiredRole.IsSystemRole;
                existingRole.InheritsFromRoleName = desiredRole.InheritsFromRoleName;
                existingRole.DeletedAt = null;
                existingRole.DeletedBy = null;
                changed = true;
            }
        }

        if (changed)
        {
            await securityContext.SaveChangesAsync();
        }
    }

    private static async Task EnsureUsersAsync(
        DabblerSecurityDbContext securityContext,
        IReadOnlyList<AppUser> desiredUsers)
    {
        var existingUsers = await securityContext.AppUsers.ToListAsync();
        var changed = false;

        foreach (var desiredUser in desiredUsers)
        {
            var existingUser = existingUsers.FirstOrDefault(user =>
                string.Equals(user.Email, desiredUser.Email, StringComparison.OrdinalIgnoreCase));

            if (existingUser is null)
            {
                securityContext.AppUsers.Add(new AppUser
                {
                    Email = desiredUser.Email,
                    DisplayName = desiredUser.DisplayName,
                    IsActive = desiredUser.IsActive,
                    CreatedAt = desiredUser.CreatedAt
                });
                changed = true;
                continue;
            }

            if (!string.Equals(existingUser.DisplayName, desiredUser.DisplayName, StringComparison.Ordinal)
                || existingUser.IsActive != desiredUser.IsActive
                || existingUser.DeletedAt is not null)
            {
                existingUser.DisplayName = desiredUser.DisplayName;
                existingUser.IsActive = desiredUser.IsActive;
                existingUser.DeletedAt = null;
                existingUser.DeletedBy = null;
                changed = true;
            }
        }

        if (changed)
        {
            await securityContext.SaveChangesAsync();
        }
    }

    private static async Task EnsureMembershipsAsync(
        DabblerSecurityDbContext securityContext,
        IReadOnlyList<AppUserOrganizationMembership> desiredMemberships)
    {
        var existingMemberships = await securityContext.AppUserOrganizationMemberships.ToListAsync();
        var changed = false;

        foreach (var desiredMembership in desiredMemberships)
        {
            var existingMembership = existingMemberships.FirstOrDefault(membership =>
                membership.UserId == desiredMembership.UserId
                && membership.OrganizationId == desiredMembership.OrganizationId);

            if (existingMembership is null)
            {
                securityContext.AppUserOrganizationMemberships.Add(new AppUserOrganizationMembership
                {
                    UserId = desiredMembership.UserId,
                    OrganizationId = desiredMembership.OrganizationId,
                    IsDefault = desiredMembership.IsDefault,
                    IsActive = desiredMembership.IsActive,
                    JoinedAt = desiredMembership.JoinedAt,
                    JoinedBy = desiredMembership.JoinedBy
                });
                changed = true;
                continue;
            }

            if (existingMembership.IsDefault != desiredMembership.IsDefault
                || existingMembership.IsActive != desiredMembership.IsActive
                || existingMembership.DeletedAt is not null)
            {
                existingMembership.IsDefault = desiredMembership.IsDefault;
                existingMembership.IsActive = desiredMembership.IsActive;
                existingMembership.DeletedAt = null;
                existingMembership.DeletedBy = null;
                changed = true;
            }
        }

        if (changed)
        {
            await securityContext.SaveChangesAsync();
        }
    }

    private static async Task EnsureUserRolesAsync(
        DabblerSecurityDbContext securityContext,
        IReadOnlyList<AppUserRole> desiredAssignments)
    {
        var existingAssignments = await securityContext.AppUserRoles.ToListAsync();
        var changed = false;

        foreach (var desiredAssignment in desiredAssignments)
        {
            var existingAssignment = existingAssignments.FirstOrDefault(assignment =>
                assignment.UserId == desiredAssignment.UserId
                && assignment.RoleId == desiredAssignment.RoleId
                && assignment.OrganizationMembershipId == desiredAssignment.OrganizationMembershipId);

            if (existingAssignment is null)
            {
                securityContext.AppUserRoles.Add(new AppUserRole
                {
                    UserId = desiredAssignment.UserId,
                    RoleId = desiredAssignment.RoleId,
                    OrganizationMembershipId = desiredAssignment.OrganizationMembershipId,
                    AssignedAt = desiredAssignment.AssignedAt,
                    AssignedBy = desiredAssignment.AssignedBy
                });
                changed = true;
                continue;
            }

            if (existingAssignment.DeletedAt is not null)
            {
                existingAssignment.DeletedAt = null;
                existingAssignment.DeletedBy = null;
                existingAssignment.AssignedBy = desiredAssignment.AssignedBy;
                changed = true;
            }
        }

        if (changed)
        {
            await securityContext.SaveChangesAsync();
        }
    }

    private static async Task EnsureExternalLoginsAsync(
        DabblerSecurityDbContext securityContext,
        IReadOnlyList<AppExternalLogin> desiredExternalLogins)
    {
        var existingExternalLogins = await securityContext.AppExternalLogins.ToListAsync();
        var changed = false;

        foreach (var desiredExternalLogin in desiredExternalLogins)
        {
            var existingExternalLogin = existingExternalLogins.FirstOrDefault(login =>
                string.Equals(login.Provider, desiredExternalLogin.Provider, StringComparison.OrdinalIgnoreCase)
                && string.Equals(login.ProviderSubjectId, desiredExternalLogin.ProviderSubjectId, StringComparison.Ordinal));

            if (existingExternalLogin is null)
            {
                securityContext.AppExternalLogins.Add(new AppExternalLogin
                {
                    UserId = desiredExternalLogin.UserId,
                    Provider = desiredExternalLogin.Provider,
                    ProviderSubjectId = desiredExternalLogin.ProviderSubjectId,
                    ProviderEmail = desiredExternalLogin.ProviderEmail,
                    LinkedAt = desiredExternalLogin.LinkedAt
                });
                changed = true;
                continue;
            }

            if (existingExternalLogin.UserId != desiredExternalLogin.UserId
                || !string.Equals(existingExternalLogin.ProviderEmail, desiredExternalLogin.ProviderEmail, StringComparison.OrdinalIgnoreCase))
            {
                existingExternalLogin.UserId = desiredExternalLogin.UserId;
                existingExternalLogin.ProviderEmail = desiredExternalLogin.ProviderEmail;
                changed = true;
            }
        }

        if (changed)
        {
            await securityContext.SaveChangesAsync();
        }
    }
}
