using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Configuration;
using Dabbler.Security.Data;
using Dabbler.Security.Entities;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Dabbler.Security.Tests.Integration;

/// <summary>
/// HTTP-level integration tests against SampleApp.Api.
/// Uses WebApplicationFactory with an in-memory database and a stubbed
/// IMagicLinkService so tests never touch SQL Server or email infrastructure.
/// </summary>
public class WebApiIntegrationTests : IAsyncLifetime
{
    private readonly WidgetsApiFactory _factory = new();

    public async Task InitializeAsync() => await _factory.SeedAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ── Scenario 1: unauthenticated request ───────────────────────────────

    [Fact]
    public async Task GetWidgets_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/widgets");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Scenario 2: Viewer role has Widget.Read ───────────────────────────

    [Fact]
    public async Task GetWidgets_ViewerWithReadPermission_Returns200()
    {
        var client = _factory.CreateClientWithCookies();
        await SignInAsync(client, TestUsers.ViewerEmail);

        var response = await client.GetAsync("/api/widgets");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var widgets = await response.Content.ReadFromJsonAsync<WidgetDto[]>();
        widgets.Should().NotBeNullOrEmpty();
    }

    // ── Scenario 3: Manager role has Widget.Read ──────────────────────────

    [Fact]
    public async Task GetWidgets_ManagerWithReadPermission_Returns200()
    {
        var client = _factory.CreateClientWithCookies();
        await SignInAsync(client, TestUsers.ManagerEmail);

        var response = await client.GetAsync("/api/widgets");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Scenario 4: UserAdministrator has no Widget permission → 403 ──────

    [Fact]
    public async Task GetWidgets_UserWithNoWidgetPermission_Returns403()
    {
        var client = _factory.CreateClientWithCookies();
        await SignInAsync(client, TestUsers.AdminOnlyEmail);

        var response = await client.GetAsync("/api/widgets");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── helper ─────────────────────────────────────────────────────────────

    private static async Task SignInAsync(HttpClient client, string email)
    {
        var payload = new { Email = email, Code = TestUsers.TestCode, Token = (string?)null };
        var response = await client.PostAsJsonAsync("/auth/verify", payload);
        response.EnsureSuccessStatusCode();
    }

    private record WidgetDto(int Id, string Name);
}

// ── Test users ─────────────────────────────────────────────────────────────

internal static class TestUsers
{
    public const string TestCode = "test-code-ok";

    public const string ViewerEmail = "viewer@test.local";
    public const string ManagerEmail = "manager@test.local";
    public const string AdminOnlyEmail = "adminonly@test.local";

    public static readonly int ViewerId = 1;
    public static readonly int ManagerId = 2;
    public static readonly int AdminOnlyId = 3;
}

// ── CookieHandler: persists cookies between requests ──────────────────────

internal sealed class CookieHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
{
    private readonly CookieContainer _jar = new();

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri!;
        var cookieHeader = _jar.GetCookieHeader(uri);
        if (!string.IsNullOrEmpty(cookieHeader))
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var sc in setCookies)
                _jar.SetCookies(uri, sc);
        }

        return response;
    }
}

// ── WebApplicationFactory ──────────────────────────────────────────────────

internal sealed class WidgetsApiFactory : WebApplicationFactory<Program>
{
    // Each factory instance gets its own isolated in-memory DB.
    private readonly string _dbName = "WebApiTest-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Setting the environment to "Test" prevents the IsDevelopment() block
        // in Program.cs from running, which means UseSwagger()/UseSwaggerUI()
        // middleware are never added. Swashbuckle 7.x has constructor-resolution
        // issues against .NET 10 preview; excluding it from the pipeline avoids
        // both the build-time validation error and runtime 500s.
        builder.UseEnvironment("Test");
        builder.UseDefaultServiceProvider(opts => opts.ValidateOnBuild = false);

        builder.ConfigureServices(services =>
        {
            // 1. Replace SQL Server DbContext with InMemory.
            // EF Core 9 stacks IDbContextOptionsConfiguration<T> entries when
            // AddDbContext is called; we must remove ALL of them (along with the
            // baked DbContextOptions<T>) before re-registering with InMemory to
            // prevent "two database providers registered" errors.
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<DabblerSecurityDbContext>) ||
                    d.ServiceType == typeof(IDbContextOptionsConfiguration<DabblerSecurityDbContext>))
                .ToList();
            foreach (var d in toRemove) services.Remove(d);

            services.AddDbContext<DabblerSecurityDbContext>(opts =>
                opts.UseInMemoryDatabase(_dbName));

            // 2. Provide a no-op ISecurityConfigurationStore so the typed
            //    auth handlers (registered by AddDabblerSecurity) can resolve
            //    SecurityConfigurationManager without hitting the DB.
            services.AddSingleton<ISecurityConfigurationStore>(
                new NoOpSecurityConfigStore());

            // 3. Override cookie auth redirect behavior: return 401/403 directly
            //    instead of redirecting to /login or /access-denied. This makes
            //    the API behave correctly for HTTP-level tests.
            //    Also relax SecurePolicy for test (WebApplicationFactory uses HTTP).
            services.PostConfigure<CookieAuthenticationOptions>(
                CookieAuthenticationDefaults.AuthenticationScheme,
                options =>
                {
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.Events.OnRedirectToLogin = ctx =>
                    {
                        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    };
                    options.Events.OnRedirectToAccessDenied = ctx =>
                    {
                        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    };
                });

            // 4. Replace IMagicLinkService with a test stub that accepts a
            //    known code for each test user.
            var mlDesc = services.SingleOrDefault(
                d => d.ServiceType == typeof(IMagicLinkService));
            if (mlDesc != null) services.Remove(mlDesc);

            services.AddScoped<IMagicLinkService>(_ => new FakeMagicLinkService());
        });
    }

    /// <summary>Seeds the in-memory DB with roles, users, and permissions.</summary>
    public async Task SeedAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DabblerSecurityDbContext>();
        await db.Database.EnsureCreatedAsync();

        var adminRole = new AppRole { Name = "UserAdministrator", Description = "Admin" };
        var managerRole = new AppRole { Name = "Manager", Description = "Full widget access" };
        var viewerRole = new AppRole { Name = "Viewer", Description = "Read-only widget access" };
        db.AppRoles.AddRange(adminRole, managerRole, viewerRole);
        await db.SaveChangesAsync();

        db.AppRolePermissions.AddRange(
            new AppRolePermission { RoleId = managerRole.Id, EntityName = "Widget", PermissionType = "Read" },
            new AppRolePermission { RoleId = managerRole.Id, EntityName = "Widget", PermissionType = "Create" },
            new AppRolePermission { RoleId = viewerRole.Id, EntityName = "Widget", PermissionType = "Read" }
        );

        var viewerUser = new AppUser
        {
            Id = TestUsers.ViewerId,
            Email = TestUsers.ViewerEmail,
            DisplayName = "Viewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var managerUser = new AppUser
        {
            Id = TestUsers.ManagerId,
            Email = TestUsers.ManagerEmail,
            DisplayName = "Manager",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var adminOnlyUser = new AppUser
        {
            Id = TestUsers.AdminOnlyId,
            Email = TestUsers.AdminOnlyEmail,
            DisplayName = "AdminOnly",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.AppUsers.AddRange(viewerUser, managerUser, adminOnlyUser);
        await db.SaveChangesAsync();

        db.AppUserRoles.AddRange(
            new AppUserRole { UserId = TestUsers.ViewerId, RoleId = viewerRole.Id, AssignedAt = DateTime.UtcNow, AssignedBy = TestUsers.ViewerId },
            new AppUserRole { UserId = TestUsers.ManagerId, RoleId = managerRole.Id, AssignedAt = DateTime.UtcNow, AssignedBy = TestUsers.ManagerId },
            new AppUserRole { UserId = TestUsers.AdminOnlyId, RoleId = adminRole.Id, AssignedAt = DateTime.UtcNow, AssignedBy = TestUsers.AdminOnlyId }
        );
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Creates a client whose cookies persist across requests (needed for the
    /// auth cookie set by /auth/verify to reach /api/widgets).
    /// </summary>
    public HttpClient CreateClientWithCookies()
    {
        var inner = Server.CreateHandler();
        var cookieHandler = new CookieHandler(inner);
        return new HttpClient(cookieHandler) { BaseAddress = Server.BaseAddress };
    }
}

// ── No-op ISecurityConfigurationStore ─────────────────────────────────────

internal sealed class NoOpSecurityConfigStore : ISecurityConfigurationStore
{
    public Task<SecurityConfiguration> LoadAsync(CancellationToken ct) =>
        Task.FromResult(new SecurityConfiguration());

    public Task SaveAsync(SecurityConfiguration config, CancellationToken ct) =>
        Task.CompletedTask;
}

// ── Fake IMagicLinkService ─────────────────────────────────────────────────

internal sealed class FakeMagicLinkService : IMagicLinkService
{
    private static readonly Dictionary<string, int> _users = new(StringComparer.OrdinalIgnoreCase)
    {
        [TestUsers.ViewerEmail] = TestUsers.ViewerId,
        [TestUsers.ManagerEmail] = TestUsers.ManagerId,
        [TestUsers.AdminOnlyEmail] = TestUsers.AdminOnlyId,
    };

    public Task<MagicLinkIssueResult> IssueAsync(
        string email, string baseVerifyUrl, string? ipAddress, CancellationToken ct) =>
        Task.FromResult(new MagicLinkIssueResult(true));

    public Task<MagicLinkVerifyResult> VerifyByCodeAsync(
        string email, string code, CancellationToken ct)
    {
        if (code == TestUsers.TestCode && _users.TryGetValue(email, out var userId))
            return Task.FromResult(new MagicLinkVerifyResult(true, false, userId, email));

        return Task.FromResult(new MagicLinkVerifyResult(false, false, null, null, "Invalid credentials"));
    }

    public Task<MagicLinkVerifyResult> VerifyByTokenAsync(string token, CancellationToken ct) =>
        Task.FromResult(new MagicLinkVerifyResult(false, false, null, null, "Token auth not used in tests"));
}
