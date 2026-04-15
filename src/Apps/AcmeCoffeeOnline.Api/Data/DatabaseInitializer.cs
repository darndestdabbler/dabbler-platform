using Dabbler.Security.Data;
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

        await context.Database.MigrateAsync();
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

        // Phase 7: Runtime security configuration (roles, permissions, row filters)
        await SecuritySeedData.SeedAsync(services);
    }
}
