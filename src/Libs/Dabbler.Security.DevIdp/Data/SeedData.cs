using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Dabbler.Security.DevIdp.Data;

public static class SeedData
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var applicationManager = services.GetRequiredService<IOpenIddictApplicationManager>();
        var scopeManager = services.GetRequiredService<IOpenIddictScopeManager>();

        await SeedRolesAsync(roleManager);
        await SeedUsersAsync(userManager);
        await SeedClientsAsync(applicationManager);
        await SeedScopesAsync(scopeManager);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = ["UserAdministrator", "Manager", "Editor", "Viewer",
            "SystemAdmin", "BackofficeUserAdmin", "BackofficeUser"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task SeedUsersAsync(UserManager<IdentityUser> userManager)
    {
        await SeedUserAsync(userManager,
            id: "11111111-1111-1111-1111-111111111111",
            email: "admin@contoso.local",
            displayName: "Admin User",
            password: "Pass123!",
            roles: ["UserAdministrator", "Manager"]);

        await SeedUserAsync(userManager,
            id: "22222222-2222-2222-2222-222222222222",
            email: "jane@contoso.local",
            displayName: "Jane Editor",
            password: "Pass123!",
            roles: ["Editor"]);

        await SeedUserAsync(userManager,
            id: "33333333-3333-3333-3333-333333333333",
            email: "viewer@contoso.local",
            displayName: "View Only",
            password: "Pass123!",
            roles: ["Viewer"]);

        // ── AcmeCoffeeOnline backoffice users ──
        await SeedUserAsync(userManager,
            id: "aaaa0000-0000-0000-0000-000000000001",
            email: "sysadmin@acmecoffee.local",
            displayName: "System Admin",
            password: "Pass123!",
            roles: ["SystemAdmin"]);

        await SeedUserAsync(userManager,
            id: "aaaa0000-0000-0000-0000-000000000002",
            email: "backoffice-admin@acmecoffee.local",
            displayName: "Kevin Brown",
            password: "Pass123!",
            roles: ["BackofficeUserAdmin"]);

        await SeedUserAsync(userManager,
            id: "aaaa0000-0000-0000-0000-000000000003",
            email: "backoffice1@acmecoffee.local",
            displayName: "Jane Doe",
            password: "Pass123!",
            roles: ["BackofficeUser"]);

        await SeedUserAsync(userManager,
            id: "aaaa0000-0000-0000-0000-000000000004",
            email: "backoffice2@acmecoffee.local",
            displayName: "Alex Smith",
            password: "Pass123!",
            roles: ["BackofficeUser"]);

        await SeedUserAsync(userManager,
            id: "aaaa0000-0000-0000-0000-000000000005",
            email: "backoffice3@acmecoffee.local",
            displayName: "Maria Wilson",
            password: "Pass123!",
            roles: ["BackofficeUser"]);
    }

    private static async Task SeedUserAsync(
        UserManager<IdentityUser> userManager,
        string id, string email, string displayName,
        string password, string[] roles)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var user = new IdentityUser
        {
            Id = id,
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create user {email}: " +
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // Store display name as a user claim (IdentityUser has no Name property)
        await userManager.AddClaimAsync(user, new Claim("name", displayName));

        await userManager.AddToRolesAsync(user, roles);
    }

    private static async Task SeedClientsAsync(IOpenIddictApplicationManager applicationManager)
    {
        await SeedClientAsync(applicationManager,
            clientId: "dabbler-security-sample",
            clientSecret: "sample-secret-dev-only",
            displayName: "Dabbler Security Sample",
            redirectUri: "http://localhost:5001/signin-EntraId");

        await SeedClientAsync(applicationManager,
            clientId: "dabbler-app-dev",
            clientSecret: "app-secret-dev-only",
            displayName: "Dabbler App Dev",
            redirectUri: "https://localhost:7001/signin-oidc");

        await SeedClientAsync(applicationManager,
            clientId: "dabbler-test-client",
            clientSecret: "test-secret",
            displayName: "Dabbler Test Client",
            redirectUri: "https://localhost:5001/signin-oidc-test");

        // AcmeCoffeeOnline Web app
        await SeedClientAsync(applicationManager,
            clientId: "acmecoffee-dev",
            clientSecret: "acmecoffee-secret-dev-only",
            displayName: "AcmeCoffee Online Dev",
            redirectUri: "https://localhost:7002/signin-EntraId");
    }

    private static async Task SeedClientAsync(
        IOpenIddictApplicationManager applicationManager,
        string clientId, string clientSecret,
        string displayName, string redirectUri)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            ClientType = ClientTypes.Confidential,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = displayName,
            RedirectUris = { new Uri(redirectUri) },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                Permissions.Scopes.Roles,
            }
        };

        var existing = await applicationManager.FindByClientIdAsync(clientId);
        if (existing is null)
        {
            await applicationManager.CreateAsync(descriptor);
            return;
        }

        await applicationManager.UpdateAsync(existing, descriptor);
    }

    private static async Task SeedScopesAsync(IOpenIddictScopeManager scopeManager)
    {
        if (await scopeManager.FindByNameAsync("roles") is not null)
            return;

        await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
        {
            Name = "roles",
            DisplayName = "Application roles"
        });
    }
}
