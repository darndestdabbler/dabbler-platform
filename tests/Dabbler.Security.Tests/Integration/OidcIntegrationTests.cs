using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Data;
using Dabbler.Security.Entities;
using Dabbler.Security.Extensions;
using Dabbler.Security.Options;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dabbler.Security.Tests.Integration;

/// <summary>
/// Integration tests for the OIDC external login flow.
/// Each test builds its own minimal test app with an in-memory DB,
/// the library's auth endpoints, and a test helper that simulates
/// the external authentication cookie (as if the OIDC middleware set it).
/// </summary>
public class OidcIntegrationTests
{
    // ── Test 1: Existing user resolved via AppExternalLogin ───────────────

    [Fact]
    public async Task ExternalCallback_ExistingUser_IssuesCookieAndRedirects()
    {
        using var app = BuildApp(db =>
        {
            var role = new AppRole { Name = "Manager", Description = "Full access" };
            db.AppRoles.Add(role);
            db.SaveChanges();

            var user = new AppUser
            {
                Email = "admin@contoso.local",
                DisplayName = "Admin User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.AppUsers.Add(user);
            db.SaveChanges();

            db.AppUserRoles.Add(new AppUserRole
            {
                UserId = user.Id, RoleId = role.Id,
                AssignedAt = DateTime.UtcNow, AssignedBy = user.Id
            });
            db.AppExternalLogins.Add(new AppExternalLogin
            {
                UserId = user.Id, Provider = "EntraId",
                ProviderSubjectId = "oid-admin-123",
                ProviderEmail = "admin@contoso.local"
            });
            db.SaveChanges();
        });

        await app.SetExternalAuth("oid-admin-123", "admin@contoso.local", "Admin User");

        var response = await app.Client.GetAsync("/auth/external-callback?returnUrl=/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().Be("/dashboard");

        var whoami = await app.Client.GetAsync("/test/whoami");
        whoami.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await whoami.Content.ReadFromJsonAsync<WhoAmIResponse>();
        data!.Email.Should().Be("admin@contoso.local");
        data.Roles.Should().Contain("Manager");
    }

    // ── Test 2: Auto-provisioning creates user + link + default roles ────

    [Fact]
    public async Task ExternalCallback_AutoProvision_CreatesUserWithDefaultRoles()
    {
        using var app = BuildApp(db =>
        {
            db.AppRoles.Add(new AppRole { Name = "Viewer", Description = "Read-only" });
            db.SaveChanges();
        });

        await app.SetExternalAuth("new-oid-456", "newuser@contoso.local", "New User");

        var response = await app.Client.GetAsync("/auth/external-callback?returnUrl=/");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        // Verify user was created in DB with external login and default role
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DabblerSecurityDbContext>();

        var user = await db.AppUsers
            .Include(u => u.ExternalLogins)
            .Include(u => u.Roles).ThenInclude(r => r.Role)
            .FirstOrDefaultAsync(u => u.Email == "newuser@contoso.local");

        user.Should().NotBeNull();
        user!.DisplayName.Should().Be("New User");
        user.ExternalLogins.Should().HaveCount(1);
        user.ExternalLogins.First().Provider.Should().Be("EntraId");
        user.ExternalLogins.First().ProviderSubjectId.Should().Be("new-oid-456");
        user.Roles.Should().ContainSingle(r => r.Role!.Name == "Viewer");

        // Verify the auto-provisioned user is authenticated
        var whoami = await app.Client.GetAsync("/test/whoami");
        whoami.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Test 3: Auto-provisioning disabled → redirect to error ───────────

    [Fact]
    public async Task ExternalCallback_AutoProvisionDisabled_RedirectsToError()
    {
        using var app = BuildApp(
            seedDb: db =>
            {
                db.AppRoles.Add(new AppRole { Name = "Viewer", Description = "Read-only" });
                db.SaveChanges();
            },
            autoProvision: false);

        await app.SetExternalAuth("unknown-oid", "unknown@contoso.local", "Unknown");

        var response = await app.Client.GetAsync("/auth/external-callback?returnUrl=/");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().Contain("user-not-provisioned");

        // User should NOT be authenticated
        var whoami = await app.Client.GetAsync("/test/whoami");
        whoami.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Test 4: OIDC user with existing magic-link account ───────────────

    [Fact]
    public async Task ExternalCallback_ExistingMagicLinkUser_LinksAndAuthenticates()
    {
        using var app = BuildApp(db =>
        {
            var role = new AppRole { Name = "Editor", Description = "Edit access" };
            db.AppRoles.Add(role);
            db.SaveChanges();

            // User exists from magic link registration but has no external login
            var user = new AppUser
            {
                Email = "jane@contoso.local",
                DisplayName = "Jane Editor",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.AppUsers.Add(user);
            db.SaveChanges();

            db.AppUserRoles.Add(new AppUserRole
            {
                UserId = user.Id, RoleId = role.Id,
                AssignedAt = DateTime.UtcNow, AssignedBy = user.Id
            });
            db.SaveChanges();
        });

        // OIDC callback with email matching existing user → auto-provision links to existing user
        await app.SetExternalAuth("oid-jane-789", "jane@contoso.local", "Jane Editor");

        var response = await app.Client.GetAsync("/auth/external-callback?returnUrl=/");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        // Verify external login was linked to the existing user (not a new user)
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DabblerSecurityDbContext>();

        var users = await db.AppUsers.Where(u => u.Email == "jane@contoso.local").ToListAsync();
        users.Should().HaveCount(1, "should link to existing user, not create a duplicate");

        var extLogin = await db.AppExternalLogins
            .FirstOrDefaultAsync(e => e.ProviderSubjectId == "oid-jane-789");
        extLogin.Should().NotBeNull();
        extLogin!.UserId.Should().Be(users.First().Id);
    }

    // ── Test 5: Role change applies on next OIDC login ───────────────────

    [Fact]
    public async Task ExternalCallback_RoleChange_ReflectsNewRoles()
    {
        int userId = 0;
        using var app = BuildApp(db =>
        {
            var viewerRole = new AppRole { Name = "Viewer", Description = "Read-only" };
            var managerRole = new AppRole { Name = "Manager", Description = "Full access" };
            db.AppRoles.AddRange(viewerRole, managerRole);
            db.SaveChanges();

            var user = new AppUser
            {
                Email = "promoted@contoso.local",
                DisplayName = "Promotable",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.AppUsers.Add(user);
            db.SaveChanges();
            userId = user.Id;

            db.AppUserRoles.Add(new AppUserRole
            {
                UserId = user.Id, RoleId = viewerRole.Id,
                AssignedAt = DateTime.UtcNow, AssignedBy = user.Id
            });
            db.AppExternalLogins.Add(new AppExternalLogin
            {
                UserId = user.Id, Provider = "EntraId",
                ProviderSubjectId = "oid-promote", ProviderEmail = "promoted@contoso.local"
            });
            db.SaveChanges();
        });

        // First login: user has Viewer role
        await app.SetExternalAuth("oid-promote", "promoted@contoso.local", "Promotable");
        await app.Client.GetAsync("/auth/external-callback?returnUrl=/");

        var whoami1 = await app.Client.GetAsync("/test/whoami");
        var data1 = await whoami1.Content.ReadFromJsonAsync<WhoAmIResponse>();
        data1!.Roles.Should().Contain("Viewer");
        data1.Roles.Should().NotContain("Manager");

        // Admin promotes user to Manager
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DabblerSecurityDbContext>();
            var managerRole = await db.AppRoles.FirstAsync(r => r.Name == "Manager");
            db.AppUserRoles.Add(new AppUserRole
            {
                UserId = userId, RoleId = managerRole.Id,
                AssignedAt = DateTime.UtcNow, AssignedBy = userId
            });
            await db.SaveChangesAsync();
        }

        // Second login: user now has Manager role too
        // Sign out first, then re-authenticate
        await app.Client.PostAsync("/auth/logout", null);
        await app.SetExternalAuth("oid-promote", "promoted@contoso.local", "Promotable");
        await app.Client.GetAsync("/auth/external-callback?returnUrl=/");

        var whoami2 = await app.Client.GetAsync("/test/whoami");
        var data2 = await whoami2.Content.ReadFromJsonAsync<WhoAmIResponse>();
        data2!.Roles.Should().Contain("Viewer");
        data2.Roles.Should().Contain("Manager");
    }

    // ── Test 6: Magic link still works when OIDC is configured ───────────

    [Fact]
    public async Task MagicLink_StillWorks_WhenOidcConfigured()
    {
        using var app = BuildApp(db =>
        {
            var role = new AppRole { Name = "Viewer", Description = "Read-only" };
            db.AppRoles.Add(role);
            db.SaveChanges();

            var user = new AppUser
            {
                Email = "magic@sample.local",
                DisplayName = "Magic User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.AppUsers.Add(user);
            db.SaveChanges();

            db.AppUserRoles.Add(new AppUserRole
            {
                UserId = user.Id, RoleId = role.Id,
                AssignedAt = DateTime.UtcNow, AssignedBy = user.Id
            });
            db.SaveChanges();
        }, configureMagicLink: mock =>
        {
            mock.Setup(s => s.VerifyByCodeAsync("magic@sample.local", "123456", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MagicLinkVerifyResult(true, false, 1, "magic@sample.local"));
        });

        var response = await app.Client.PostAsJsonAsync("/auth/verify", new
        {
            Email = "magic@sample.local",
            Code = "123456"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Test 7: External callback with no auth result → error ────────────

    [Fact]
    public async Task ExternalCallback_NoExternalCookie_RedirectsToError()
    {
        using var app = BuildApp();

        // Call callback without setting external auth first
        var response = await app.Client.GetAsync("/auth/external-callback?returnUrl=/");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().Contain("external-auth-failed");
    }

    // ── Test 8: Challenge endpoint with unknown scheme → 400 ─────────────

    [Fact]
    public async Task Challenge_UnknownScheme_Returns400()
    {
        using var app = BuildApp();

        var response = await app.Client.GetAsync("/auth/challenge/UnknownProvider");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Test 9: External callback sanitizes returnUrl ─────────────────────

    [Fact]
    public async Task ExternalCallback_OpenRedirectUrl_RedirectsToRoot()
    {
        using var app = BuildApp(db =>
        {
            db.AppRoles.Add(new AppRole { Name = "Viewer", Description = "Read-only" });
            db.SaveChanges();
        });

        await app.SetExternalAuth("oid-redir", "redir@contoso.local", "Redir User");

        // Attempt open-redirect via returnUrl
        var response = await app.Client.GetAsync(
            "/auth/external-callback?returnUrl=https://evil.com/steal");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().Be("/");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Test infrastructure
    // ═══════════════════════════════════════════════════════════════════════

    private static OidcTestApp BuildApp(
        Action<DabblerSecurityDbContext>? seedDb = null,
        bool autoProvision = true,
        string[]? defaultRoles = null,
        Action<Mock<IMagicLinkService>>? configureMagicLink = null)
    {
        defaultRoles ??= ["Viewer"];
        var dbName = "OidcIntTest-" + Guid.NewGuid();

        // Build DabblerSecurityOptions with EntraId configured
        var securityOptions = new DabblerSecurityOptions();
        securityOptions.AddEntraId(entra =>
        {
            entra.Authority = "https://fake.example.com";
            entra.ClientId = "test-client";
            entra.ClientSecret = "test-secret";
            entra.AutoProvisionUsers = autoProvision;
            entra.DefaultRoles = defaultRoles;
        });

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();

        // Register configured options
        builder.Services.AddSingleton(
            Microsoft.Extensions.Options.Options.Create(securityOptions));

        // In-memory DB
        builder.Services.AddDbContext<DabblerSecurityDbContext>(opts =>
            opts.UseInMemoryDatabase(dbName));

        // Real services needed by the callback handler
        builder.Services.AddScoped<IExternalLoginService, ExternalLoginService>();
        builder.Services.AddScoped<IUserService, UserService>();

        // Magic link service (mock — not the focus of these tests)
        var mockMl = new Mock<IMagicLinkService>();
        configureMagicLink?.Invoke(mockMl);
        builder.Services.AddSingleton<IMagicLinkService>(mockMl.Object);

        // Authentication: default cookie + external cookie (no real OIDC scheme)
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(opts =>
            {
                opts.LoginPath = "/login";
                opts.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
                opts.Events.OnRedirectToLogin = ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                opts.Events.OnRedirectToAccessDenied = ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            })
            .AddCookie(ExternalProviderConfig.ExternalAuthScheme, opts =>
            {
                opts.Cookie.Name = ".Test.External";
                opts.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
                opts.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            });

        builder.Services.AddAuthorization();

        var app = builder.Build();

        // Seed DB
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DabblerSecurityDbContext>();
            db.Database.EnsureCreated();
            seedDb?.Invoke(db);
        }

        app.UseAuthentication();
        app.UseAuthorization();

        // Map library auth endpoints (challenge, callback, verify, etc.)
        Dabbler.Security.Extensions.EndpointRouteBuilderExtensions.MapDabblerSecurityEndpoints(app);

        // Test helper: simulate the external authentication cookie
        // (normally set by the OIDC middleware after identity provider redirect)
        app.MapPost("/test/set-external-auth", async (HttpContext ctx) =>
        {
            var data = await ctx.Request.ReadFromJsonAsync<SetExternalAuthData>();
            if (data is null) return Results.BadRequest();

            var claims = new List<Claim>();
            if (data.SubjectId is not null)
                claims.Add(new Claim("oid", data.SubjectId));
            if (data.Email is not null)
                claims.Add(new Claim(ClaimTypes.Email, data.Email));
            if (data.DisplayName is not null)
                claims.Add(new Claim(ClaimTypes.Name, data.DisplayName));

            var identity = new ClaimsIdentity(claims, ExternalProviderConfig.ExternalAuthScheme);
            var principal = new ClaimsPrincipal(identity);
            var properties = new AuthenticationProperties
            {
                Items = { ["scheme"] = "EntraId", ["provider"] = "EntraId" }
            };

            await ctx.SignInAsync(
                ExternalProviderConfig.ExternalAuthScheme, principal, properties);
            return Results.Ok();
        });

        // Test helper: check if caller is authenticated (validates app cookie)
        app.MapGet("/test/whoami", (ClaimsPrincipal user) =>
        {
            if (user.Identity?.IsAuthenticated != true)
                return Results.Json(
                    new WhoAmIResponse(null, null, null, []),
                    statusCode: StatusCodes.Status401Unauthorized);

            return Results.Ok(new WhoAmIResponse(
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                user.FindFirst(ClaimTypes.Email)?.Value,
                user.FindFirst(ClaimTypes.Name)?.Value,
                user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()));
        }).RequireAuthorization();

        app.StartAsync().GetAwaiter().GetResult();

        var inner = app.GetTestServer().CreateHandler();
        var cookieHandler = new CookieHandler(inner);
        var client = new HttpClient(cookieHandler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        return new OidcTestApp(app, client);
    }

    // ── Wrapper for the test app + client ────────────────────────────────

    private sealed class OidcTestApp : IDisposable
    {
        private readonly WebApplication _app;
        public HttpClient Client { get; }
        public IServiceProvider Services => _app.Services;

        public OidcTestApp(WebApplication app, HttpClient client)
        {
            _app = app;
            Client = client;
        }

        public async Task SetExternalAuth(
            string subjectId, string email, string displayName)
        {
            var response = await Client.PostAsJsonAsync(
                "/test/set-external-auth",
                new SetExternalAuthData(subjectId, email, displayName));
            response.EnsureSuccessStatusCode();
        }

        public void Dispose()
        {
            Client.Dispose();
            _app.StopAsync().GetAwaiter().GetResult();
            _app.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    // ── DTOs ──────────────────────────────────────────────────────────────

    private record SetExternalAuthData(
        string? SubjectId, string? Email, string? DisplayName);

    private record WhoAmIResponse(
        string? UserId, string? Email, string? DisplayName, string[] Roles);
}
