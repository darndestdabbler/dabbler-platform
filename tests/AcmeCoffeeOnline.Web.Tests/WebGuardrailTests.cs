using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dabbler.Security.Data;
using Dabbler.Security.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AcmeCoffeeOnline.Web.Tests;

public sealed class WebGuardrailTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public WebGuardrailTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HomePage_RedirectsToDashboardAndRendersLayoutAsync()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Development"));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        using var signInRequest = new HttpRequestMessage(
            HttpMethod.Get,
            "/test-auth/seeded-session?roles=BackofficeUser&displayName=Guardrail%20Home%20User");
        signInRequest.Headers.Add("X-Test-Auth-Secret", "acme-local-seeded-session");

        using var signInResponse = await client.SendAsync(signInRequest);
        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);

        // Root redirects to /dashboard
        var rootResponse = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, rootResponse.StatusCode);
        Assert.Contains("/dashboard", rootResponse.Headers.Location?.OriginalString);

        // Follow the redirect to verify dashboard renders with MudBlazor layout
        var dashboardResponse = await client.GetAsync("/dashboard");
        var content = await dashboardResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, dashboardResponse.StatusCode);
        Assert.Contains("AcmeCoffeeDistributors", content);
        Assert.Contains("data-testid=\"routed-page-content\"", content);
    }

    [Fact]
    public async Task LoginPage_RendersWithAuthLayoutWithoutMainShellAsync()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/login");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"auth-shell\"", content);
        Assert.Contains("data-testid=\"login-page\"", content);
        Assert.DoesNotContain("data-testid=\"main-shell\"", content);
    }

    [Fact]
    public async Task SeededSessionEndpoint_RequiresSecretAndUnlocksConfiguredPolicyAsync()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Development"));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var unauthorizedResponse = await client.GetAsync("/auth-check");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedResponse.StatusCode);

        var forbiddenResponse = await client.GetAsync("/test-auth/seeded-session?roles=BackofficeUser");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        using var signInRequest = new HttpRequestMessage(
            HttpMethod.Get,
            "/test-auth/seeded-session?roles=BackofficeUser&displayName=Guardrail%20Test%20User");
        signInRequest.Headers.Add("X-Test-Auth-Secret", "acme-local-seeded-session");

        using var signInResponse = await client.SendAsync(signInRequest);
        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);

        var authorizedResponse = await client.GetAsync("/auth-check");
        var authorizedContent = await authorizedResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, authorizedResponse.StatusCode);
        Assert.Contains("\"BackofficeUser\"", authorizedContent);
        Assert.Contains("Guardrail Test User", authorizedContent);
    }

    [Fact]
    public async Task SeededSession_MultiOrgDiagnostics_ShowAndSwitchActiveOrganizationAsync()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Development"));
        await EnsureMultiOrgGuardrailUserAsync(factory.Services);

        using var client = await CreateAuthenticatedClientAsync("guardrail.multi.org@acme.test", factory);

        var initialSession = await client.GetFromJsonAsync<SessionInfoResponse>(
            "/test-auth/session-info",
            CreateJsonOptions());

        Assert.NotNull(initialSession);
        Assert.Equal("internal", initialSession!.ActiveOrganizationKey);
        Assert.Contains("BackofficeUser", initialSession.Roles);
        Assert.DoesNotContain("Customer", initialSession.Roles);
        Assert.NotNull(initialSession.AvailableOrganizations);
        Assert.True(initialSession.AvailableOrganizations!.Length >= 2);

        var retailOrganization = initialSession.AvailableOrganizations!
            .Single(organization => !string.Equals(organization.OrganizationKey, "internal", StringComparison.OrdinalIgnoreCase));

        var switchResponse = await client.GetAsync($"/test-auth/switch-organization?organizationId={retailOrganization.OrganizationId}");
        Assert.Equal(HttpStatusCode.OK, switchResponse.StatusCode);

        var switchedSession = await client.GetFromJsonAsync<SessionInfoResponse>(
            "/test-auth/session-info",
            CreateJsonOptions());

        Assert.NotNull(switchedSession);
        Assert.Equal(retailOrganization.OrganizationId.ToString(), switchedSession!.ActiveOrganizationId);
        Assert.Equal(retailOrganization.OrganizationKey, switchedSession.ActiveOrganizationKey);
        Assert.Contains("Customer", switchedSession.Roles);
        Assert.DoesNotContain("BackofficeUser", switchedSession.Roles);
    }

    [Fact]
    public async Task AuthenticatedOrganizationSwitch_UpdatesShellAndAdminScopeAsync()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Development"));
        await EnsureScopedAdminGuardrailUsersAsync(factory.Services);

        using var client = await CreateAuthenticatedClientAsync("guardrail.org.scope.admin@acme.test", factory);

        var initialUsersResponse = await client.GetAsync("/admin/users");
        var initialUsersContent = await initialUsersResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, initialUsersResponse.StatusCode);
        Assert.Contains("Switch into an organization where you hold user-admin access", initialUsersContent);

        var initialSession = await client.GetFromJsonAsync<SessionInfoResponse>(
            "/test-auth/session-info",
            CreateJsonOptions());

        Assert.NotNull(initialSession);
        var retailOrganization = initialSession!.AvailableOrganizations!
            .Single(organization => !string.Equals(organization.OrganizationKey, "internal", StringComparison.OrdinalIgnoreCase));

        var switchResponse = await client.GetAsync(
            $"/auth/switch-organization?organizationId={retailOrganization.OrganizationId}&returnUrl=%2Fadmin%2Fusers");

        Assert.Equal(HttpStatusCode.Redirect, switchResponse.StatusCode);
        Assert.Equal("/admin/users", switchResponse.Headers.Location?.OriginalString);

        var switchedUsersResponse = await client.GetAsync("/admin/users");
        var switchedUsersContent = await switchedUsersResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, switchedUsersResponse.StatusCode);
        Assert.Contains(retailOrganization.OrganizationName, switchedUsersContent);

        var switchedSession = await client.GetFromJsonAsync<SessionInfoResponse>(
            "/test-auth/session-info",
            CreateJsonOptions());

        Assert.NotNull(switchedSession);
        Assert.Equal(retailOrganization.OrganizationId.ToString(), switchedSession!.ActiveOrganizationId);
        Assert.Equal(retailOrganization.OrganizationKey, switchedSession.ActiveOrganizationKey);
    }

    [Fact]
    public async Task BackofficeAdmin_RouteAccess_MatchesFocusedAdminNavigationAsync()
    {
        using var client = await CreateAuthenticatedClientAsync("kbrown@acmecoffee.com");

        var dashboardResponse = await client.GetAsync("/dashboard");
        var dashboardContent = await dashboardResponse.Content.ReadAsStringAsync();
        var usersResponse = await client.GetAsync("/admin/users");
        var productsResponse = await client.GetAsync("/admin/products");
        var securityResponse = await client.GetAsync("/admin/security");

        Assert.Equal(HttpStatusCode.OK, dashboardResponse.StatusCode);
        Assert.Contains("Products", dashboardContent);
        Assert.Equal(HttpStatusCode.OK, usersResponse.StatusCode);

        // Transitional state introduced by the rearchitect-role-administration-foundations
        // session set: user-type roles are default-permissive at the page layer and the
        // legacy AllowedRoles/DeniedRoles grant entries have been retired. Admin-page
        // denial for BackofficeUser is restored by Session 5's seed migration + UiRestriction
        // entries (and further refined by the Set B dashboard rebuild). Until then, the
        // spec explicitly accepts "blank or minimal admin surfaces" for non-baked-in roles.
        Assert.Equal(HttpStatusCode.OK, productsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, securityResponse.StatusCode);
    }

    [Fact]
    public async Task SystemAdmin_RouteAccess_IncludesSystemAdminDestinationsAsync()
    {
        using var client = await CreateAuthenticatedClientAsync("admin@acmecoffee.com");

        var dashboardResponse = await client.GetAsync("/dashboard");
        var dashboardContent = await dashboardResponse.Content.ReadAsStringAsync();
        var usersResponse = await client.GetAsync("/admin/users");
        var productsResponse = await client.GetAsync("/admin/products");
        var securityResponse = await client.GetAsync("/admin/security");

        Assert.Equal(HttpStatusCode.OK, dashboardResponse.StatusCode);
        Assert.Contains("Products", dashboardContent);
        Assert.Equal(HttpStatusCode.OK, usersResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, productsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, securityResponse.StatusCode);
    }

    [Fact]
    public async Task PlatformStubRoute_RendersReusableStubHostProofAsync()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Development"));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var response = await client.GetAsync("/platform/stub");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"standalone-shell\"", content);
        // Session 3 of security-dashboard-uat-remediation switched App.razor
        // to `@Assets["_framework/blazor.web.js"]`, which the MapStaticAssets
        // pipeline may fingerprint to `_framework/blazor.web.<hash>.js`. The
        // guardrail we want is "the blazor web script was wired", not an
        // exact-path match, so assert the prefix + extension separately.
        Assert.Matches(@"_framework/blazor\.web[^""]*\.js", content);
        Assert.Contains("data-dabbler-asset=\"dabbler-shell.css\"", content);
        Assert.Contains("reusable platform stubs", content);
        Assert.Contains("Distributor Demo", content);
        Assert.DoesNotContain("data-testid=\"main-shell\"", content);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(
        string email,
        WebApplicationFactory<Program>? factoryOverride = null)
    {
        var factory = factoryOverride ?? _factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Development"));

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        using var signInRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"/test-auth/seeded-session?email={Uri.EscapeDataString(email)}");
        signInRequest.Headers.Add("X-Test-Auth-Secret", "acme-local-seeded-session");

        using var signInResponse = await client.SendAsync(signInRequest);
        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);

        client.DefaultRequestHeaders.Add("X-Test-Auth-Secret", "acme-local-seeded-session");
        return client;
    }

    private static async Task EnsureMultiOrgGuardrailUserAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DabblerSecurityDbContext>();

        if (await db.AppUsers.AnyAsync(user => user.Email == "guardrail.multi.org@acme.test"))
        {
            return;
        }

        var internalOrganization = await db.AppOrganizations
            .OrderBy(organization => organization.Id)
            .FirstOrDefaultAsync(organization => organization.IsInternal);
        if (internalOrganization is null)
        {
            internalOrganization = new AppOrganization
            {
                Key = "internal",
                Name = "Internal Operations",
                IsInternal = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.AppOrganizations.Add(internalOrganization);
            await db.SaveChangesAsync();
        }

        var retailOrganization = new AppOrganization
        {
            Key = "guardrail-retail-001",
            Name = "Guardrail Retail 001",
            IsInternal = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var user = new AppUser
        {
            Email = "guardrail.multi.org@acme.test",
            DisplayName = "Guardrail Multi Org",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.AppOrganizations.Add(retailOrganization);
        db.AppUsers.Add(user);
        await db.SaveChangesAsync();

        var internalMembership = new AppUserOrganizationMembership
        {
            UserId = user.Id,
            OrganizationId = internalOrganization.Id,
            IsDefault = true,
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        };
        var retailMembership = new AppUserOrganizationMembership
        {
            UserId = user.Id,
            OrganizationId = retailOrganization.Id,
            IsDefault = false,
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        };

        db.AppUserOrganizationMemberships.AddRange(internalMembership, retailMembership);
        await db.SaveChangesAsync();

        var backofficeRole = await db.AppRoles.FirstOrDefaultAsync(role => role.Name == "BackofficeUser");
        if (backofficeRole is null)
        {
            backofficeRole = new AppRole
            {
                Name = "BackofficeUser",
                Description = "Guardrail backoffice role",
                IsSystemRole = true,
                CreatedAt = DateTime.UtcNow
            };
            db.AppRoles.Add(backofficeRole);
        }

        var customerRole = await db.AppRoles.FirstOrDefaultAsync(role => role.Name == "Customer");
        if (customerRole is null)
        {
            customerRole = new AppRole
            {
                Name = "Customer",
                Description = "Guardrail customer role",
                IsSystemRole = true,
                CreatedAt = DateTime.UtcNow
            };
            db.AppRoles.Add(customerRole);
        }

        await db.SaveChangesAsync();

        db.AppUserRoles.AddRange(
            new AppUserRole
            {
                UserId = user.Id,
                RoleId = backofficeRole.Id,
                OrganizationMembershipId = internalMembership.Id,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = user.Id
            },
            new AppUserRole
            {
                UserId = user.Id,
                RoleId = customerRole.Id,
                OrganizationMembershipId = retailMembership.Id,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = user.Id
            });

        await db.SaveChangesAsync();
    }

    private static async Task EnsureScopedAdminGuardrailUsersAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DabblerSecurityDbContext>();

        if (await db.AppUsers.AnyAsync(user => user.Email == "guardrail.org.scope.admin@acme.test"))
        {
            return;
        }

        var internalOrganization = await db.AppOrganizations
            .OrderBy(organization => organization.Id)
            .FirstAsync(organization => organization.IsInternal);

        var allowedOrganization = new AppOrganization
        {
            Key = "guardrail-retail-allowed",
            Name = "Guardrail Retail Allowed",
            IsInternal = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.AppOrganizations.Add(allowedOrganization);

        var adminUser = new AppUser
        {
            Email = "guardrail.org.scope.admin@acme.test",
            DisplayName = "Guardrail Scope Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var memberUser = new AppUser
        {
            Email = "guardrail.org.scope.member@acme.test",
            DisplayName = "Guardrail Scope Member",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.AppUsers.AddRange(adminUser, memberUser);
        await db.SaveChangesAsync();

        var internalMembership = new AppUserOrganizationMembership
        {
            UserId = adminUser.Id,
            OrganizationId = internalOrganization.Id,
            IsDefault = true,
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        };
        var adminRetailMembership = new AppUserOrganizationMembership
        {
            UserId = adminUser.Id,
            OrganizationId = allowedOrganization.Id,
            IsDefault = false,
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        };
        var memberRetailMembership = new AppUserOrganizationMembership
        {
            UserId = memberUser.Id,
            OrganizationId = allowedOrganization.Id,
            IsDefault = true,
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        };

        db.AppUserOrganizationMemberships.AddRange(internalMembership, adminRetailMembership, memberRetailMembership);
        await db.SaveChangesAsync();

        var backofficeRole = await db.AppRoles.FirstAsync(role => role.Name == "BackofficeUser");
        var adminRole = await db.AppRoles.FirstAsync(role => role.Name == "BackofficeUserAdmin");
        var customerRole = await db.AppRoles.FirstAsync(role => role.Name == "Customer");

        db.AppUserRoles.AddRange(
            new AppUserRole
            {
                UserId = adminUser.Id,
                RoleId = backofficeRole.Id,
                OrganizationMembershipId = internalMembership.Id,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = adminUser.Id
            },
            new AppUserRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id,
                OrganizationMembershipId = adminRetailMembership.Id,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = adminUser.Id
            },
            new AppUserRole
            {
                UserId = memberUser.Id,
                RoleId = customerRole.Id,
                OrganizationMembershipId = memberRetailMembership.Id,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = adminUser.Id
            });

        await db.SaveChangesAsync();
    }

    private static JsonSerializerOptions CreateJsonOptions() => new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed record SessionInfoResponse(
        bool IsAuthenticated,
        string? UserId,
        string? ActiveOrganizationId,
        string? ActiveOrganizationKey,
        string[] Roles,
        SessionOrganizationResponse[]? AvailableOrganizations);

    private sealed record SessionOrganizationResponse(
        int OrganizationId,
        string OrganizationKey,
        string? OrganizationName);
}
