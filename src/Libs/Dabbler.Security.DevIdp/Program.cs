using Dabbler.Security.DevIdp.Claims;
using Dabbler.Security.DevIdp.Config;
using Dabbler.Security.DevIdp.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<DevIdpOptions>(
    builder.Configuration.GetSection(DevIdpOptions.SectionName));

// EF Core + SQLite
builder.Services.AddDbContext<DevIdpDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DevIdp")
        ?? "Data Source=devidp.db");
    options.UseOpenIddict();
});

// ASP.NET Core Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<DevIdpDbContext>()
    .AddDefaultTokenProviders();

// Configure cookie for login
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login";
});

// OpenIddict
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<DevIdpDbContext>();
    })
    .AddServer(options =>
    {
        options.AllowAuthorizationCodeFlow()
            .RequireProofKeyForCodeExchange();

        options.SetAuthorizationEndpointUris("/connect/authorize")
            .SetTokenEndpointUris("/connect/token");

        options.RegisterScopes(
            OpenIddict.Abstractions.OpenIddictConstants.Scopes.OpenId,
            OpenIddict.Abstractions.OpenIddictConstants.Scopes.Profile,
            OpenIddict.Abstractions.OpenIddictConstants.Scopes.Email,
            "roles");

        options.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();

        // Dev-only: emit plain JWT access tokens so they're inspectable for debugging
        options.DisableAccessTokenEncryption();

        var aspNetCoreIntegration = options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableTokenEndpointPassthrough();

        if (builder.Environment.IsDevelopment())
        {
            aspNetCoreIntegration.DisableTransportSecurityRequirement();
        }
    });

// Claims handler for Entra ID-compatible tokens
builder.Services.AddScoped<EntraIdClaimsHandler>();

// Razor Pages (for login UI) + Controllers (for OIDC endpoints)
builder.Services.AddRazorPages();
builder.Services.AddControllers();

var app = builder.Build();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DevIdpDbContext>();
    await db.Database.EnsureCreatedAsync();
    await SeedData.SeedAsync(scope.ServiceProvider);
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();
