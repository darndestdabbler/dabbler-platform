using AcmeCoffeeOnline.Web.Components;
using AcmeCoffeeOnline.Web.Configuration;
using AcmeCoffeeOnline.Web.Infrastructure;
using AcmeCoffeeOnline.Web.Services;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("security-config.json", optional: false, reloadOnChange: true);

builder.Services
    .AddOptions<DabblerAssetOptions>()
    .Bind(builder.Configuration.GetSection(DabblerAssetOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<DabblerAssetOptions>, DabblerAssetOptionsValidator>();

builder.Services
    .AddOptions<AppAuthenticationOptions>()
    .Bind(builder.Configuration.GetSection(AppAuthenticationOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<AppAuthenticationOptions>, AppAuthenticationOptionsValidator>();

builder.Services
    .AddOptions<SecurityConfigOptions>()
    .Bind(builder.Configuration.GetSection(SecurityConfigOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<SecurityConfigOptions>, SecurityConfigOptionsValidator>();

var securityConfig = builder.Configuration
    .GetSection(SecurityConfigOptions.SectionName)
    .Get<SecurityConfigOptions>() ?? new SecurityConfigOptions();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.SlidingExpiration = true;
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/auth-check"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                if (context.Request.Path.StartsWithSegments("/auth-check"))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    foreach (var policy in securityConfig.Policies)
    {
        if (string.IsNullOrWhiteSpace(policy.Name) || policy.Roles.Length == 0)
        {
            continue;
        }

        options.AddPolicy(policy.Name, configure =>
        {
            configure.RequireAuthenticatedUser();
            configure.RequireRole(policy.Roles);
        });
    }
});
builder.Services.AddHttpClient<PlatformStatusClient>((serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["Services:ApiBaseUrl"] ?? "http://localhost:5011";
    client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

var authOptions = app.Services.GetRequiredService<IOptions<AppAuthenticationOptions>>().Value;

app.MapGet("/health", () => Results.Ok(new
{
    service = "web",
    status = "healthy",
    stage = "session-4-guardrails",
    deliveryMode = authOptions.DeliveryMode
}));

app.MapGet("/auth-check", (ClaimsPrincipal user) => Results.Ok(new
{
    status = "authorized",
    user = user.Identity?.Name ?? user.FindFirstValue(ClaimTypes.NameIdentifier),
    policy = SecurityPolicies.OperationsViewer
})).RequireAuthorization(SecurityPolicies.OperationsViewer);

if ((app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Test"))
    && authOptions.SeededSession.Enabled)
{
    app.MapGet("/test-auth/seeded-session", async (
        HttpContext context,
        string? roles,
        string? displayName,
        string? email,
        string? userId) =>
    {
        if (!IsLoopbackOrInMemoryRequest(context))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!context.Request.Headers.TryGetValue("X-Test-Auth-Secret", out var providedSecret)
            || !string.Equals(providedSecret, authOptions.SeededSession.SharedSecret, StringComparison.Ordinal))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var requestedRoles = ParseRequestedRoles(roles) is { Length: > 0 } parsedRoles
            ? parsedRoles
            : [SecurityPolicies.OperationsViewer];

        var allowedRoles = authOptions.SeededSession.AllowedRoles;
        if (requestedRoles.Any(role => !allowedRoles.Contains(role, StringComparer.Ordinal)))
        {
            return Results.BadRequest(new { error = "One or more requested roles are not allowed for seeded sessions." });
        }

        var effectiveDisplayName = string.IsNullOrWhiteSpace(displayName)
            ? "Seeded Session User"
            : displayName;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId ?? "seeded-user"),
            new(ClaimTypes.Name, effectiveDisplayName),
            new(ClaimTypes.Email, email ?? "seeded.user@acme.test")
        };

        claims.AddRange(requestedRoles.Select(role => new Claim(ClaimTypes.Role, role)));

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true
            });

        return Results.Ok(new
        {
            status = "signed-in",
            roles = requestedRoles
        });
    });

    app.MapGet("/test-auth/sign-out", async (HttpContext context) =>
    {
        if (!IsLoopbackOrInMemoryRequest(context))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!context.Request.Headers.TryGetValue("X-Test-Auth-Secret", out var providedSecret)
            || !string.Equals(providedSecret, authOptions.SeededSession.SharedSecret, StringComparison.Ordinal))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Ok(new { status = "signed-out" });
    });
}

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static bool IsLoopbackOrInMemoryRequest(HttpContext context)
{
    var remoteIp = context.Connection.RemoteIpAddress;
    return remoteIp is null || IPAddress.IsLoopback(remoteIp);
}

static string[] ParseRequestedRoles(string? roles)
{
    if (string.IsNullOrWhiteSpace(roles))
    {
        return [];
    }

    return roles
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public partial class Program;
