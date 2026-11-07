using Dabbler.Security.DevIdp.Claims;
using Dabbler.Security.DevIdp.Config;
using Dabbler.Security.DevIdp.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dabbler.Security.DevIdp;

public static class DevIdpHost
{
    public static async Task RunAsync(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.Configure<DevIdpOptions>(
            builder.Configuration.GetSection(DevIdpOptions.SectionName));

        builder.Services.AddDbContext<DevIdpDbContext>(options =>
        {
            options.UseSqlite(builder.Configuration.GetConnectionString("DevIdp")
                ?? "Data Source=devidp.db");
            options.UseOpenIddict();
        });

        builder.Services.AddIdentity<IdentityUser, IdentityRole>()
            .AddEntityFrameworkStores<DevIdpDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Login";
        });

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

                options.DisableAccessTokenEncryption();

                var aspNetCoreIntegration = options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough();

                if (builder.Environment.IsDevelopment())
                {
                    aspNetCoreIntegration.DisableTransportSecurityRequirement();
                }
            });

        builder.Services.AddScoped<EntraIdClaimsHandler>();
        builder.Services.AddRazorPages();
        builder.Services.AddControllers();

        var app = builder.Build();

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

        await app.RunAsync();
    }
}
