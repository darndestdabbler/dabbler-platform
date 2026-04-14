using System.Security.Claims;
using System.Text;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Blazor.Extensions;
using Dabbler.Security.Configuration;
using Dabbler.Security.Data;
using Dabbler.Security.Extensions;
using Dabbler.Security.Options;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SampleApp.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// ── Dabbler.Security (same DB as SampleApp.Api) ──────────────────────────────
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

    // ── OIDC: register Entra ID (points to DevIdp in development) ────────────
    var oidcSection = builder.Configuration.GetSection("DabblerSecurity:Oidc");
    if (oidcSection.Exists())
    {
        options.AddEntraId(entra =>
        {
            entra.Authority = oidcSection["Authority"] ?? string.Empty;
            entra.ClientId = oidcSection["ClientId"] ?? string.Empty;
            entra.ClientSecret = oidcSection["ClientSecret"] ?? string.Empty;
            entra.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            entra.AutoProvisionUsers = true;
            entra.DefaultRoles = ["Viewer"];
        });
    }

    // Register protectable resources so the admin UI can render permission matrices.
    options.RegisterPages(pages =>
    {
        pages.Add("/", "Home");
        pages.Add("/widgets", "Widgets", "Widget list page — requires Widget.Read");
        pages.Add("/admin/security", "Security Dashboard", "Runtime auth configurator — SystemAdministrator only");
        pages.Add("/admin/users", "User Admin", "User + role management — UserAdministrator only");
    });

    options.RegisterEntities(entities =>
    {
        entities.Add("Widget", "Widgets", fields =>
        {
            fields.Add("Name", "Name");
        });
    });

    options.RegisterEndpoints(endpoints =>
    {
        endpoints.Add("GET /api/widgets", "GET /api/widgets", "List widgets");
    });

    options.RegisterControls(controls =>
    {
        controls.Add("AddButton",    "/widgets", "Add Widget button");
        controls.Add("EditButton",   "/widgets", "Edit Widget button (per-row)");
        controls.Add("DeleteButton", "/widgets", "Delete Widget button (per-row)");
        controls.Add("ExportButton", "/widgets", "Export CSV button");
    });
})
.UseJsonFileStore(Path.Combine(builder.Environment.ContentRootPath, "security-config.json"));

// ── Sample IUserListProvider — reads users from the Dabbler EF store ──────────
builder.Services.AddScoped<IUserListProvider, EfUserListProvider>();
builder.Services.AddScoped<IAuditService, AuditService>();

// ── Cookie authentication ─────────────────────────────────────────────────────
// AddDabblerSecurity already calls AddAuthentication().AddCookie() with sensible
// defaults. Override only the options that differ from the library defaults.
builder.Services.PostConfigure<CookieAuthenticationOptions>(
    CookieAuthenticationDefaults.AuthenticationScheme,
    options =>
    {
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.PostConfigure<CookieAuthenticationOptions>(
    "DabblerExternalAuth",
    options =>
    {
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
    });

// ── Exchange-token store for Blazor Server auth hand-off ─────────────────────
// LoginPage (Blazor component) calls /auth/verify via HttpClient.
// That is a server-side call; the Set-Cookie never reaches the browser.
// /auth/verify instead stores a one-time token here, returns it in the JSON,
// and the component does forceLoad to /auth/complete?token=... which IS a real
// browser request → cookie is set correctly.
builder.Services.AddMemoryCache();

// ── Blazor + MudBlazor + Dabbler Security Blazor ─────────────────────────────
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddDabblerSecurityBlazor();

// ── HttpClient used by Dabbler.Security.Blazor components ────────────────────
// Points to this same Web app so /auth/send-magic-link and /auth/verify resolve
// to the endpoints we define below.
var webBaseUrl = builder.Configuration["DabblerWeb:BaseUrl"] ?? "http://localhost:5084";
builder.Services.AddHttpClient("DabblerWeb", client =>
    client.BaseAddress = new Uri(webBaseUrl));

// Inject the named client as the default HttpClient (components use @inject HttpClient)
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("DabblerWeb"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ── Auth endpoints (mirrors SampleApp.Api; cookie is set on THIS app) ─────────

app.MapPost("/auth/send-magic-link", async (
    MagicLinkRequest req,
    HttpContext ctx,
    IMagicLinkService svc,
    CancellationToken ct) =>
{
    var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}/login";
    var result = await svc.IssueAsync(req.Email, baseUrl, ctx.Connection.RemoteIpAddress?.ToString(), ct);
    return Results.Ok(result);
});

app.MapPost("/auth/verify", async (
    VerifyMagicLinkRequest req,
    IMagicLinkService svc,
    DabblerSecurityDbContext db,
    IMemoryCache cache,
    CancellationToken ct) =>
{
    var result = !string.IsNullOrWhiteSpace(req.Token)
        ? await svc.VerifyByTokenAsync(req.Token, ct)
        : await svc.VerifyByCodeAsync(req.Email ?? string.Empty, req.Code ?? string.Empty, ct);

    if (!result.Success || result.UserId is null || result.Email is null)
    {
        return Results.BadRequest(result);
    }

    if (result.RequiresMfa)
    {
        return Results.Ok(new
        {
            result.Success,
            result.RequiresMfa,
            UserId = result.UserId.Value
        });
    }

    var user = await db.AppUsers
        .Include(x => x.Roles)
        .ThenInclude(x => x.Role)
        .FirstAsync(x => x.Id == result.UserId.Value, ct);

    // Store claims in memory so /auth/complete can sign in via a real browser request.
    var exchangeToken = Guid.NewGuid().ToString("N");
    var userInfo = new ExchangeTokenInfo(
        user.Id,
        user.Email,
        user.DisplayName,
        user.Roles.Select(r => r.Role!.Name).ToArray()
    );

    cache.Set(exchangeToken, userInfo, TimeSpan.FromSeconds(60));

    return Results.Ok(new
    {
        result.Success,
        result.RequiresMfa,
        user.Id,
        user.Email,
        ExchangeToken = exchangeToken
    });
});

app.MapPost("/auth/verify-totp", async (
    VerifyTotpRequest req,
    IMfaService mfaService,
    DabblerSecurityDbContext db,
    IMemoryCache cache,
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

    var exchangeToken = Guid.NewGuid().ToString("N");
    var userInfo = new ExchangeTokenInfo(
        user.Id,
        user.Email,
        user.DisplayName,
        user.Roles.Select(r => r.Role!.Name).ToArray()
    );

    cache.Set(exchangeToken, userInfo, TimeSpan.FromSeconds(60));

    return Results.Ok(new
    {
        Success = true,
        user.Id,
        user.Email,
        ExchangeToken = exchangeToken
    });
});

// Real browser request → cookie is set on the browser correctly.
app.MapGet("/auth/complete", async (
    string token,
    string? returnUrl,
    HttpContext http,
    IMemoryCache cache) =>
{
    if (!cache.TryGetValue<ExchangeTokenInfo>(token, out var userInfo) || userInfo is null)
    {
        return Results.Redirect("/login?error=expired");
    }

    cache.Remove(token);

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, userInfo.UserId.ToString()),
        new(ClaimTypes.Email, userInfo.Email),
        new(ClaimTypes.Name, userInfo.DisplayName)
    };
    claims.AddRange(userInfo.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await http.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity));

    var destination = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
    return Results.Redirect(destination);
});

app.MapGet("/auth/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

// ── OIDC: challenge + callback (real browser redirects, not Blazor HTTP) ─────

app.MapGet("/auth/challenge/{scheme}", async (
    string scheme,
    string? returnUrl,
    HttpContext context) =>
{
    // Sanitize returnUrl to prevent open-redirect attacks
    var safeReturnUrl = IsLocalUrl(returnUrl) ? returnUrl! : "/";

    var properties = new AuthenticationProperties
    {
        RedirectUri = $"/auth/external-callback?returnUrl={Uri.EscapeDataString(safeReturnUrl)}",
        Items = { ["scheme"] = scheme, ["provider"] = scheme }
    };

    await context.ChallengeAsync(scheme, properties);
    return Results.Empty;
});

app.MapGet("/auth/external-callback", async (
    string? returnUrl,
    IExternalLoginService externalLoginService,
    DabblerSecurityDbContext db,
    ILoggerFactory loggerFactory,
    HttpContext context) =>
{
    var logger = loggerFactory.CreateLogger("SampleApp.ExternalAuth");

    // Read the temporary external cookie set by the OIDC middleware
    var authResult = await context.AuthenticateAsync("DabblerExternalAuth");
    if (!authResult.Succeeded || authResult.Principal == null)
    {
        logger.LogWarning("External authentication failed: {Failure}",
            authResult.Failure?.Message ?? "No principal");
        return Results.Redirect("/login?error=external-auth-failed");
    }

    var providerName = authResult.Properties?.Items["provider"] ?? "EntraId";

    // Extract user info using provider-specific claim mapping
    ExternalClaimMapper.ExternalUserInfo userInfo;
    try
    {
        userInfo = ExternalClaimMapper.ExtractUserInfo(providerName, authResult.Principal);
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "Failed to extract user info from external claims");
        return Results.Redirect("/login?error=external-auth-failed");
    }

    // Resolve or auto-provision the AppUser
    var user = await externalLoginService.FindByExternalLoginAsync(providerName, userInfo.SubjectId);
    if (user == null)
    {
        try
        {
            user = await externalLoginService.ProvisionAndLinkAsync(
                providerName, userInfo.SubjectId, userInfo.Email, userInfo.DisplayName, ["Viewer"]);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Failed to provision user from {Provider}", providerName);
            return Results.Redirect("/login?error=provisioning-failed");
        }
    }

    // Issue the application cookie (same claims as magic link flow)
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Email, user.Email),
        new(ClaimTypes.Name, user.DisplayName)
    };
    claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r.Role!.Name)));

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity));

    // Clean up the temporary external cookie
    await context.SignOutAsync("DabblerExternalAuth");

    logger.LogInformation("External login successful: {Provider} user {UserId}", providerName, user.Id);

    var safeReturnUrl = IsLocalUrl(returnUrl) ? returnUrl! : "/";
    return Results.Redirect(safeReturnUrl);
});

static bool IsLocalUrl(string? url)
{
    if (string.IsNullOrEmpty(url)) return false;
    return url.StartsWith('/') && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));
}

// ── Seed-config reset (dev only) ──────────────────────────────────────────────
// Resets security-config.json and user-roles.json to the checked-in seed
// templates so testers can start fresh without restarting the app.
app.MapPost("/seed-config", async (
    SecurityConfigurationManager configMgr,
    IWebHostEnvironment env,
    CancellationToken ct) =>
{
    if (!env.IsDevelopment())
        return Results.Forbid();

    var root = env.ContentRootPath;

    var configSeed = Path.Combine(root, "security-config.seed.json");
    var configLive = Path.Combine(root, "security-config.json");
    if (File.Exists(configSeed))
        File.Copy(configSeed, configLive, overwrite: true);

    var rolesSeed = Path.Combine(root, "user-roles.seed.json");
    var rolesLive = Path.Combine(root, "user-roles.json");
    if (File.Exists(rolesSeed))
        File.Copy(rolesSeed, rolesLive, overwrite: true);

    await configMgr.RefreshAsync(ct);

    return Results.Ok(new { Reset = true, Message = "security-config.json and user-roles.json reset to seed state." });
});

// ── Test-only: apply arbitrary security config (dev only) ────────────────────
// Used by Playwright tests to mutate permissions at runtime without restarting.
// Accepts a full SecurityConfiguration JSON body, writes it to security-config.json,
// and calls RefreshAsync so the change is live immediately.
app.MapPost("/test-apply-config", async (
    HttpContext ctx,
    SecurityConfigurationManager configMgr,
    IWebHostEnvironment env,
    CancellationToken ct) =>
{
    if (!env.IsDevelopment())
        return Results.Forbid();

    using var reader = new System.IO.StreamReader(ctx.Request.Body);
    var json = await reader.ReadToEndAsync(ct);

    var configPath = Path.Combine(env.ContentRootPath, "security-config.json");
    await File.WriteAllTextAsync(configPath, json, ct);
    await configMgr.RefreshAsync(ct);

    return Results.Ok(new { Applied = true });
});

app.MapStaticAssets(); // .NET 9/10: serves _framework/blazor.server.js and other framework files
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

public record MagicLinkRequest(string Email);
public record VerifyMagicLinkRequest(string? Token, string? Email, string? Code);
public record VerifyTotpRequest(int UserId, string Code);
public record ExchangeTokenInfo(int UserId, string Email, string DisplayName, string[] Roles);
