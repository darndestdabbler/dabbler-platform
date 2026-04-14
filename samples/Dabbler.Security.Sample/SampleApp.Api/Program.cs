using System.Security.Claims;
using System.Text;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Data;
using SampleApp.Shared;
using Dabbler.Security.Entities;
using Dabbler.Security.Extensions;
using Dabbler.Security.Options;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDabblerSecurity(options =>
{
    options.ConnectionString =
        builder.Configuration.GetConnectionString("DabblerSecurity")
        ?? "Server=(localdb)\\MSSQLLocalDB;Database=DabblerSecuritySample;Trusted_Connection=True;TrustServerCertificate=True";
    options.MagicLinkExpiry = TimeSpan.FromMinutes(10);
    options.RequireMfa = false;
    options.InternalApiKey = "sample-internal-key";
    options.Email.SenderType = builder.Configuration.GetValue("DabblerSecurity:EmailSenderType", EmailSenderType.DevMailHog);
    options.Email.SmtpHost     = builder.Configuration["DabblerSecurity:SmtpHost"]                       ?? string.Empty;
    options.Email.SmtpPort     = builder.Configuration.GetValue("DabblerSecurity:SmtpPort",     587);
    options.Email.SmtpEnableSsl= builder.Configuration.GetValue("DabblerSecurity:SmtpEnableSsl", true);
    options.Email.FromAddress  = builder.Configuration["DabblerSecurity:FromAddress"]                    ?? string.Empty;
    options.Email.FromName     = builder.Configuration["DabblerSecurity:FromName"]                       ?? string.Empty;
    options.Mfa.EncryptionKey = builder.Configuration["DabblerSecurity:Mfa:EncryptionKey"]
        ?? "c2FtcGxlLWRldi1rZXktMzItYnl0ZXMtbG9uZyEhISE="; // dev-only default, 32 bytes base64
    options.Mfa.RequireMfaForRoles = ["UserAdministrator", "Editor"];
});

// Register a JSON-file config store so the Dabbler authorization handlers
// (registered by AddDabblerSecurity) can be constructed from DI.  The API
// does not ship a security-config.json, so the store returns an empty config;
// all Dabbler handler checks fail-open and the API's own IPermissionService
// logic (DB-based) governs access instead.
builder.Services.UseJsonFileStore(
    Path.Combine(builder.Environment.ContentRootPath, "security-config.json"));

builder.Services.PostConfigure<CookieAuthenticationOptions>(
    CookieAuthenticationDefaults.AuthenticationScheme,
    options =>
    {
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
    });

builder.Services.AddOpenApi("v1");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Dabbler Security Sample API";
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/auth/send-magic-link", async (
    MagicLinkRequest req,
    HttpContext ctx,
    IMagicLinkService svc,
    CancellationToken ct) =>
{
    var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}/auth/verify";
    var result = await svc.IssueAsync(req.Email, baseUrl, ctx.Connection.RemoteIpAddress?.ToString(), ct);
    return Results.Ok(result);
})
.WithTags("Auth")
.WithSummary("Request a magic link / 6-digit code")
.WithDescription("Sends a magic-link email and prints the 6-digit code to the **API console** (DevMailHog mode). Call this first to start the login flow.")
;

app.MapPost("/auth/verify", async (
    VerifyMagicLinkRequest req,
    IMagicLinkService svc,
    DabblerSecurityDbContext db,
    HttpContext http,
    CancellationToken ct) =>
{
    var result = !string.IsNullOrWhiteSpace(req.Token)
        ? await svc.VerifyByTokenAsync(req.Token, ct)
        : await svc.VerifyByCodeAsync(req.Email ?? string.Empty, req.Code ?? string.Empty, ct);

    if (!result.Success || result.UserId is null || result.Email is null)
    {
        return Results.BadRequest(result);
    }

    var user = await db.AppUsers
        .Include(x => x.Roles)
        .ThenInclude(x => x.Role)
        .FirstAsync(x => x.Id == result.UserId.Value, ct);

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Email, user.Email),
        new(ClaimTypes.Name, user.DisplayName)
    };

    claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r.Role!.Name)));

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await http.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity));

    return Results.Ok(new { result.Success, result.RequiresMfa, user.Id, user.Email });
})
.WithTags("Auth")
.WithSummary("Verify the 6-digit code and sign in")
.WithDescription("Verifies the code (or token from the email link). On success the API sets an auth cookie. In Swagger UI, this cookie is automatically used for subsequent requests.")
;

app.MapPost("/auth/verify-totp", async (
    VerifyTotpRequest req,
    IMfaService mfaService,
    DabblerSecurityDbContext db,
    HttpContext http,
    CancellationToken ct) =>
{
    var isValid = await mfaService.ValidateTotpAsync(req.UserId, req.Code);
    if (!isValid)
    {
        return Results.BadRequest("Invalid TOTP code");
    }

    var user = await db.AppUsers
        .Include(x => x.Roles)
        .ThenInclude(x => x.Role)
        .FirstOrDefaultAsync(x => x.Id == req.UserId, ct);

    if (user == null)
    {
        return Results.BadRequest("User not found");
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Email, user.Email),
        new(ClaimTypes.Name, user.DisplayName)
    };

    claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r.Role!.Name)));

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await http.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity));

    return Results.Ok(new { Success = true, user.Id, user.Email });
})
.WithTags("Auth")
.WithSummary("Verify a TOTP code and finish sign-in")
.WithDescription("Completes sign-in for MFA-enabled users after the magic link has been verified.")
;

app.MapGet("/api/widgets", async (ClaimsPrincipal principal, IPermissionService permissions) =>
{
    var idClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(idClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var canRead = await permissions.HasPermissionAsync(userId, "Widget", "Read");
    if (!canRead)
    {
        return Results.Forbid();
    }

    return Results.Ok(new[]
    {
        new WidgetDto(1, "Widget A"),
        new WidgetDto(2, "Widget B")
    });
})
.RequireAuthorization()
.WithTags("Widgets")
.WithSummary("List widgets (requires Widget.Read permission)")
.WithDescription("Returns the widget list. Requires an authenticated session **and** the Widget.Read permission. Manager and Viewer roles both have this permission.")
;

app.MapPost("/seed", async (DabblerSecurityDbContext db, IEncryptionService encryption, CancellationToken ct) =>
{
    await db.Database.EnsureDeletedAsync(ct);
    await db.Database.EnsureCreatedAsync(ct);

    var sysAdminRole  = new AppRole { Name = "SystemAdministrator", Description = "Runtime security configurator access", IsSystemRole = true };
    var userAdminRole = new AppRole { Name = "UserAdministrator",   Description = "User and role management",           IsSystemRole = true };
    var managerRole   = new AppRole { Name = "Manager",             Description = "Full widget access" };
    var editorRole    = new AppRole { Name = "Editor",              Description = "Edit widget access" };
    var viewerRole    = new AppRole { Name = "Viewer",              Description = "Read-only widget access" };

    db.AppRoles.AddRange(sysAdminRole, userAdminRole, managerRole, editorRole, viewerRole);
    await db.SaveChangesAsync(ct);

    db.AppRolePermissions.AddRange(
        new AppRolePermission { RoleId = managerRole.Id, EntityName = "Widget", PermissionType = "Read" },
        new AppRolePermission { RoleId = managerRole.Id, EntityName = "Widget", PermissionType = "Create" },
        new AppRolePermission { RoleId = managerRole.Id, EntityName = "Widget", PermissionType = "Update" },
        new AppRolePermission { RoleId = managerRole.Id, EntityName = "Widget", PermissionType = "Delete" },
        new AppRolePermission { RoleId = editorRole.Id,  EntityName = "Widget", PermissionType = "Read" },
        new AppRolePermission { RoleId = editorRole.Id,  EntityName = "Widget", PermissionType = "Update" },
        new AppRolePermission { RoleId = viewerRole.Id,  EntityName = "Widget", PermissionType = "Read" }
    );

    // -- Magic-link users (existing) --
    var sysAdmin = new AppUser { Email = "sysadmin@sample.local", DisplayName = "System Admin", IsActive = true, CreatedAt = DateTime.UtcNow };
    var admin    = new AppUser { Email = "admin@sample.local",    DisplayName = "Admin",        IsActive = true, CreatedAt = DateTime.UtcNow };
    var editor   = new AppUser { Email = "editor@sample.local",   DisplayName = "Editor",       IsActive = true, CreatedAt = DateTime.UtcNow };
    var viewer   = new AppUser { Email = "viewer@sample.local",   DisplayName = "Viewer",       IsActive = true, CreatedAt = DateTime.UtcNow };

    // -- OIDC users (internal via DevIdp / Entra ID) --
    var oidcAdmin  = new AppUser { Email = "admin@contoso.local",  DisplayName = "Admin User", IsActive = true, CreatedAt = DateTime.UtcNow };
    var oidcEditor = new AppUser { Email = "jane@contoso.local",   DisplayName = "Jane Editor", IsActive = true, CreatedAt = DateTime.UtcNow };
    var oidcViewer = new AppUser { Email = "viewer@contoso.local", DisplayName = "View Only",   IsActive = true, CreatedAt = DateTime.UtcNow };

    // -- External user (magic link, no MFA) --
    var partner = new AppUser { Email = "partner@external.com", DisplayName = "External Partner", IsActive = true, CreatedAt = DateTime.UtcNow };

    db.AppUsers.AddRange(sysAdmin, admin, editor, viewer, oidcAdmin, oidcEditor, oidcViewer, partner);
    await db.SaveChangesAsync(ct);

    db.AppUserRoles.AddRange(
        // Magic-link users
        new AppUserRole { UserId = sysAdmin.Id, RoleId = sysAdminRole.Id,  AssignedAt = DateTime.UtcNow, AssignedBy = sysAdmin.Id },
        new AppUserRole { UserId = admin.Id,    RoleId = userAdminRole.Id, AssignedAt = DateTime.UtcNow, AssignedBy = sysAdmin.Id },
        new AppUserRole { UserId = admin.Id,    RoleId = managerRole.Id,   AssignedAt = DateTime.UtcNow, AssignedBy = sysAdmin.Id },
        new AppUserRole { UserId = editor.Id,   RoleId = editorRole.Id,    AssignedAt = DateTime.UtcNow, AssignedBy = sysAdmin.Id },
        new AppUserRole { UserId = viewer.Id,   RoleId = viewerRole.Id,    AssignedAt = DateTime.UtcNow, AssignedBy = sysAdmin.Id },
        // OIDC users
        new AppUserRole { UserId = oidcAdmin.Id,  RoleId = userAdminRole.Id, AssignedAt = DateTime.UtcNow, AssignedBy = sysAdmin.Id },
        new AppUserRole { UserId = oidcAdmin.Id,  RoleId = managerRole.Id,   AssignedAt = DateTime.UtcNow, AssignedBy = sysAdmin.Id },
        new AppUserRole { UserId = oidcEditor.Id, RoleId = editorRole.Id,    AssignedAt = DateTime.UtcNow, AssignedBy = sysAdmin.Id },
        new AppUserRole { UserId = oidcViewer.Id, RoleId = viewerRole.Id,    AssignedAt = DateTime.UtcNow, AssignedBy = sysAdmin.Id },
        // External user
        new AppUserRole { UserId = partner.Id, RoleId = viewerRole.Id, AssignedAt = DateTime.UtcNow, AssignedBy = sysAdmin.Id }
    );

    await db.SaveChangesAsync(ct);

    // Link OIDC users to their DevIdp oid values (stable GUIDs)
    db.AppExternalLogins.AddRange(
        new AppExternalLogin
        {
            UserId = oidcAdmin.Id,
            Provider = "EntraId",
            ProviderSubjectId = "11111111-1111-1111-1111-111111111111",
            ProviderEmail = "admin@contoso.local",
            LinkedAt = DateTime.UtcNow
        },
        new AppExternalLogin
        {
            UserId = oidcEditor.Id,
            Provider = "EntraId",
            ProviderSubjectId = "22222222-2222-2222-2222-222222222222",
            ProviderEmail = "jane@contoso.local",
            LinkedAt = DateTime.UtcNow
        },
        new AppExternalLogin
        {
            UserId = oidcViewer.Id,
            Provider = "EntraId",
            ProviderSubjectId = "33333333-3333-3333-3333-333333333333",
            ProviderEmail = "viewer@contoso.local",
            LinkedAt = DateTime.UtcNow
        }
    );
    await db.SaveChangesAsync(ct);

    var adminMfaSecret = Encoding.ASCII.GetBytes("admin-magic-totp-seed");
    var adminMfaEncrypted = encryption.Encrypt(adminMfaSecret, out var adminMfaIv);
    var editorMfaSecret = Encoding.ASCII.GetBytes("editor-magic-totp-seed");
    var editorMfaEncrypted = encryption.Encrypt(editorMfaSecret, out var editorMfaIv);

    db.AppUserMfas.AddRange(
        new AppUserMfa
        {
            UserId = admin.Id,
            MfaType = "TOTP",
            SecretEncrypted = adminMfaEncrypted,
            SecretIv = adminMfaIv,
            IsVerified = true,
            EnabledAt = DateTime.UtcNow,
            LastValidatedAt = null,
            FailedAttemptCount = 0,
            LockedUntil = null,
            RecoveryCodes = "[]"
        },
        new AppUserMfa
        {
            UserId = editor.Id,
            MfaType = "TOTP",
            SecretEncrypted = editorMfaEncrypted,
            SecretIv = editorMfaIv,
            IsVerified = true,
            EnabledAt = DateTime.UtcNow,
            LastValidatedAt = null,
            FailedAttemptCount = 0,
            LockedUntil = null,
            RecoveryCodes = "[]"
        }
    );
    await db.SaveChangesAsync(ct);

    return Results.Ok(new
    {
        Seeded = true,
        Users = new[]
        {
            $"sysadmin@sample.local (SystemAdministrator) — id={sysAdmin.Id}",
            $"admin@sample.local (UserAdministrator, Manager) — id={admin.Id}",
            $"editor@sample.local (Editor) — id={editor.Id}",
            $"viewer@sample.local (Viewer) — id={viewer.Id}",
            $"admin@contoso.local [OIDC] (UserAdministrator, Manager) — id={oidcAdmin.Id}",
            $"jane@contoso.local [OIDC] (Editor) — id={oidcEditor.Id}",
            $"viewer@contoso.local [OIDC] (Viewer) — id={oidcViewer.Id}",
            $"partner@external.com [External] (Viewer) — id={partner.Id}"
        }
    });
})
.WithTags("Setup")
.WithSummary("Reset and seed the database")
.WithDescription("""
    Drops and recreates the database, then seeds roles, permissions, and users.
    **Run this first** before testing any auth endpoints. Warning: all existing data is deleted.

    **Seeded users (magic link):**
    | Email | Roles |
    |-------|-------|
    | sysadmin@sample.local | SystemAdministrator |
    | admin@sample.local | UserAdministrator, Manager |
    | editor@sample.local | Editor |
    | viewer@sample.local | Viewer |

    **Seeded users (OIDC via DevIdp / Entra ID):**
    | Email | Roles |
    |-------|-------|
    | admin@contoso.local | UserAdministrator, Manager |
    | jane@contoso.local | Editor |
    | viewer@contoso.local | Viewer |

    **External user (magic link, no MFA):**
    | Email | Roles |
    |-------|-------|
    | partner@external.com | Viewer |

    After seeding, also POST `/seed-config` on **SampleApp.Web** (port 5084) to reset
    security-config.json and user-roles.json to match these users.
    """)
;

app.MapPost("/test/prepare-oidc-auto-provision", async (
    AutoProvisionPrepRequest req,
    DabblerSecurityDbContext db,
    IWebHostEnvironment env,
    CancellationToken ct) =>
{
    if (!env.IsDevelopment())
        return Results.Forbid();

    if (string.IsNullOrWhiteSpace(req.Email))
        return Results.BadRequest(new { Error = "Email is required." });

    var email = req.Email.Trim();
    var userIds = await db.AppUsers
        .Where(u => u.Email == email)
        .Select(u => u.Id)
        .ToListAsync(ct);

    if (userIds.Count > 0)
    {
        var userRoles = await db.AppUserRoles
            .Where(x => userIds.Contains(x.UserId))
            .ToListAsync(ct);
        var externalLogins = await db.AppExternalLogins
            .Where(x => userIds.Contains(x.UserId))
            .ToListAsync(ct);
        var users = await db.AppUsers
            .Where(x => userIds.Contains(x.Id))
            .ToListAsync(ct);

        db.AppUserRoles.RemoveRange(userRoles);
        db.AppExternalLogins.RemoveRange(externalLogins);
        db.AppUsers.RemoveRange(users);
        await db.SaveChangesAsync(ct);
    }

    return Results.Ok(new
    {
        Prepared = true,
        Email = email,
        RemovedUsers = userIds.Count
    });
})
.WithTags("Setup")
.WithSummary("Prepare an OIDC auto-provision scenario")
.WithDescription("Development-only helper that removes the app-side user record for a known OIDC email so the next OIDC login auto-provisions it again.")
;

app.Run();

// Exposed so WebApplicationFactory<Program> can reference this assembly from tests.
public partial class Program { }

public record MagicLinkRequest(string Email);
public record VerifyMagicLinkRequest(string? Token, string? Email, string? Code);
public record VerifyTotpRequest(int UserId, string Code);
public record AutoProvisionPrepRequest(string Email);
