using System.Globalization;
using Dabbler.Security.Extensions;
using Dabbler.Security.Blazor.Extensions;
using Dabbler.Security.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection connection string is required.");

builder.Services.AddDabblerSecurity(options =>
{
    options.ConnectionString = connectionString;
    options.InternalApiKey = builder.Configuration["DabblerSecurity:InternalApiKey"] ?? "dev-internal-key";
    options.MagicLinkExpiry = TimeSpan.FromMinutes(15);
    options.RequireMfa = false;
    options.UnregisteredResourcePolicy = UnregisteredResourcePolicy.FailOpen;
    // PRODUCTION: replace DevMailHog with EmailSenderType.Smtp + SMTP config
    options.Email.SenderType = EmailSenderType.DevMailHog;
    options.Mfa.EncryptionKey = builder.Configuration["DabblerSecurity:MfaEncryptionKey"]
        ?? Convert.ToBase64String(new byte[32]);

    options.RegisterPages(pages =>
    {
        pages.Add("/", "Home", "Landing page");
        pages.Add("/admin/security", "Security Dashboard", "Runtime security configuration");
    });
});

builder.Services.UseJsonFileStore(
    Path.Combine(builder.Environment.ContentRootPath, "security-config.json"));

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

builder.Services.AddMemoryCache();
builder.Services.AddDabblerSecurityBlazor();
builder.Services.AddHttpClient();

var app = builder.Build();

var culture = CultureInfo.GetCultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(culture),
    SupportedCultures = [culture],
    SupportedUICultures = [culture]
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapPost("/auth/send-magic-link", async (
    MagicLinkRequest req,
    HttpContext ctx,
    Dabbler.Security.Services.IMagicLinkService svc,
    CancellationToken ct) =>
{
    var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}/login";
    var result = await svc.IssueAsync(req.Email, baseUrl, ctx.Connection.RemoteIpAddress?.ToString(), ct);
    return Results.Ok(result);
});

app.MapPost("/auth/verify", async (
    VerifyRequest req,
    Dabbler.Security.Services.IMagicLinkService svc,
    Dabbler.Security.Data.DabblerSecurityDbContext db,
    IMemoryCache cache,
    CancellationToken ct) =>
{
    var result = !string.IsNullOrWhiteSpace(req.Token)
        ? await svc.VerifyByTokenAsync(req.Token, ct)
        : await svc.VerifyByCodeAsync(req.Email ?? string.Empty, req.Code ?? string.Empty, ct);

    if (!result.Success || result.UserId is null)
        return Results.BadRequest(result);

    var user = await db.AppUsers
        .Include(u => u.Roles).ThenInclude(r => r.Role)
        .FirstAsync(u => u.Id == result.UserId.Value, ct);

    var exchangeToken = Guid.NewGuid().ToString("N");
    cache.Set(exchangeToken, new ExchangeInfo(user.Id, user.Email, user.DisplayName,
        user.Roles.Select(r => r.Role!.Name).ToArray()), TimeSpan.FromSeconds(60));

    return Results.Ok(new { result.Success, user.Id, user.Email, ExchangeToken = exchangeToken });
});

app.MapGet("/auth/complete", async (
    string token,
    string? returnUrl,
    HttpContext http,
    IMemoryCache cache,
    Dabbler.Security.Services.IUserSessionContextService sessionContextService) =>
{
    if (!cache.TryGetValue<ExchangeInfo>(token, out var info) || info is null)
        return Results.Redirect("/login?error=expired");

    cache.Remove(token);
    var principal = await sessionContextService.CreatePrincipalAsync(
        info.UserId, CookieAuthenticationDefaults.AuthenticationScheme);
    if (principal is null)
        return Results.Redirect("/login?error=expired");

    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    return Results.Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
});

app.MapGet("/auth/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapStaticAssets();
app.MapRazorComponents<DabblerApp.Components.App>()
    .AddInteractiveServerRenderMode();

// Seed admin@local.test with SystemAdmin role on first run (A12)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Dabbler.Security.Data.DabblerSecurityDbContext>();
    await db.Database.MigrateAsync();

    if (!await db.AppUsers.AnyAsync())
    {
        var systemAdminRole = new Dabbler.Security.Entities.AppRole
        {
            Name = "SystemAdmin",
            Description = "Full-access baked-in system administrator role.",
            IsSystemRole = true,
            CreatedAt = DateTime.UtcNow
        };
        db.AppRoles.Add(systemAdminRole);
        await db.SaveChangesAsync();

        var adminUser = new Dabbler.Security.Entities.AppUser
        {
            Email = "admin@local.test",
            DisplayName = "Platform Admin",
            IsActive = true,
            IsProvisioned = true,
            CreatedAt = DateTime.UtcNow
        };
        db.AppUsers.Add(adminUser);
        await db.SaveChangesAsync();

        db.AppUserRoles.Add(new Dabbler.Security.Entities.AppUserRole
        {
            UserId = adminUser.Id,
            RoleId = systemAdminRole.Id,
            OrganizationMembershipId = null,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = adminUser.Id
        });
        await db.SaveChangesAsync();
    }
}

app.Run();

public record MagicLinkRequest(string Email);
public record VerifyRequest(string? Token, string? Email, string? Code);
public record ExchangeInfo(int UserId, string Email, string DisplayName, string[] Roles);
