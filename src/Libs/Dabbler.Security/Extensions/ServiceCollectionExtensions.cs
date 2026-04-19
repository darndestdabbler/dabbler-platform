using Dabbler.Security.Abstractions;
using Dabbler.Security.Authorization;
using Dabbler.Security.Authorization.Handlers;
using Dabbler.Security.Configuration;
using Dabbler.Security.Data;
using Dabbler.Security.Options;
using Dabbler.Security.Persistence;
using Dabbler.Security.Registration;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace Dabbler.Security.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to register Dabbler.Security services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Dabbler.Security services to the specified <see cref="IServiceCollection"/>.
    /// This is the main entry point for registering the security system.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">An <see cref="Action{DabblerSecurityOptions}"/> to configure the provided <see cref="DabblerSecurityOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddDabblerSecurity(
        this IServiceCollection services,
        Action<DabblerSecurityOptions> configure)
    {
        // Build options eagerly so we can inspect ExternalProviders during registration.
        // The same configure action is also registered via Configure<T> for runtime resolution.
        var earlyOptions = new DabblerSecurityOptions();
        configure(earlyOptions);

        services.Configure(configure);

        services.AddSingleton<IValidateOptions<DabblerSecurityOptions>, DabblerSecurityOptionsValidator>();

        services.AddDbContext<DabblerSecurityDbContext>((sp, opts) =>
        {
            var cfg = sp.GetRequiredService<IOptions<DabblerSecurityOptions>>().Value;
            opts.UseSqlServer(cfg.ConnectionString);
        });

        var authBuilder = services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(cookieOpts =>
            {
                cookieOpts.LoginPath = "/login";
                cookieOpts.AccessDeniedPath = "/access-denied";
                cookieOpts.ExpireTimeSpan = TimeSpan.FromHours(24);
                cookieOpts.SlidingExpiration = true;
                cookieOpts.Cookie.HttpOnly = true;
                cookieOpts.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                cookieOpts.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
            });

        // Register external authentication schemes when OIDC/OAuth providers are configured.
        if (earlyOptions.ExternalProviders.Count > 0)
        {
            // Temporary cookie used during the external auth round-trip. The OIDC/OAuth
            // middleware writes the external identity here; Session 3's callback handler
            // converts it into the main application cookie.
            authBuilder.AddCookie(ExternalProviderConfig.ExternalAuthScheme, extCookieOpts =>
            {
                extCookieOpts.Cookie.Name = ".Dabbler.External";
                extCookieOpts.ExpireTimeSpan = TimeSpan.FromMinutes(5);
                extCookieOpts.Cookie.HttpOnly = true;
                extCookieOpts.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                extCookieOpts.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
            });

            foreach (var provider in earlyOptions.ExternalProviders)
            {
                if (provider.IsOidc && provider.OidcOptions is { } oidcOpts)
                {
                    authBuilder.AddOpenIdConnect(provider.AuthenticationScheme, oidcSchemeOpts =>
                    {
                        oidcSchemeOpts.Authority = oidcOpts.Authority;
                        oidcSchemeOpts.ClientId = oidcOpts.ClientId;
                        oidcSchemeOpts.ClientSecret = oidcOpts.ClientSecret;
                        oidcSchemeOpts.CallbackPath = oidcOpts.CallbackPath
                            ?? $"/signin-{provider.AuthenticationScheme}";
                        oidcSchemeOpts.SignedOutCallbackPath = oidcOpts.SignedOutCallbackPath
                            ?? $"/signout-callback-{provider.AuthenticationScheme}";
                        oidcSchemeOpts.RequireHttpsMetadata = oidcOpts.RequireHttpsMetadata;
                        oidcSchemeOpts.SignInScheme = ExternalProviderConfig.ExternalAuthScheme;
                        oidcSchemeOpts.ResponseType = "code";
                        oidcSchemeOpts.SaveTokens = false;
                        oidcSchemeOpts.GetClaimsFromUserInfoEndpoint = true;
                        oidcSchemeOpts.Scope.Clear();
                        oidcSchemeOpts.Scope.Add("openid");
                        oidcSchemeOpts.Scope.Add("profile");
                        oidcSchemeOpts.Scope.Add("email");
                    });
                }
            }
        }

        services.AddAuthorization();
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<IAuthorizationPolicyProvider, Dabbler.Security.Authorization.DabblerAuthorizationPolicyProvider>();

        // The platform supplies the five canonical baked-in roles. Applications
        // that need additional non-user-type baked-in roles can replace this
        // registration with their own IBakedInRoleProvider implementation.
        services.TryAddSingleton<IBakedInRoleProvider, PlatformBakedInRoleProvider>();

        // The three resource-specific handlers are scoped so they can receive the scoped IRoleSwitcher.
        // Entity and Field handlers were removed in Session 3 of
        // rearchitect-role-administration-foundations: entity CRUD is now default-permissive,
        // and field access is shaped by ColumnRestriction + DefaultMasks / MaskExemptions.
        services.AddScoped<IAuthorizationHandler, PageAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, EndpointAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, ControlAuthorizationHandler>();

        // IRoleSwitcher is scoped: per-request in Web API, per-circuit in Blazor Server.
        services.AddScoped<IRoleSwitcher, SessionRoleSwitcher>();
        services.AddScoped<IUserSessionContextService, UserSessionContextService>();

        services.AddScoped<IHashingService, HashingService>();
        services.AddScoped<IEncryptionService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DabblerSecurityOptions>>().Value;
            return new EncryptionService(options.Mfa.EncryptionKey);
        });
        services.AddScoped<IMagicLinkService, MagicLinkService>();
        services.AddScoped<IMfaService, MfaService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IRowFilterService, RowFilterService>();
        services.AddScoped<IRowRestrictionBroker, RowRestrictionBroker>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IExternalLoginService, ExternalLoginService>();
        services.TryAddSingleton<DevEmailInbox>();

        services.AddScoped<IEmailSender>(sp =>
        {
            var o = sp.GetRequiredService<IOptions<DabblerSecurityOptions>>().Value;
            return o.Email.SenderType == EmailSenderType.Smtp
                ? new SmtpEmailSender(sp.GetRequiredService<IOptions<DabblerSecurityOptions>>(), sp.GetRequiredService<ILogger<SmtpEmailSender>>())
                : new DevEmailSender(
                    sp.GetRequiredService<ILogger<DevEmailSender>>(),
                    sp.GetRequiredService<DevEmailInbox>());
        });

        // SecurityConfigurationManager is registered as a singleton.
        // The backing store is registered separately via UseJsonFileStore() or UseEfCoreStore().
        services.AddSingleton<SecurityConfigurationManager>(sp =>
        {
            var store = sp.GetRequiredService<ISecurityConfigurationStore>();
            return new SecurityConfigurationManager(store);
        });

        // AuthorizationStateNotifier wires into SecurityConfigurationManager.ConfigurationChanged
        // so Blazor components can subscribe and re-render when permissions change.
        services.AddSingleton<AuthorizationStateNotifier>();

        // Rate limit store: use distributed cache if configured, otherwise in-memory.
        services.TryAddSingleton<IRateLimitStore>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<DabblerSecurityOptions>>().Value;
            if (opts.RateLimiting.UseDistributedCache)
            {
                var cache = sp.GetRequiredService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
                return new DistributedRateLimitStore(cache);
            }
            return new InMemoryRateLimitStore();
        });

        // NullUserListProvider is a safe default so admin components don't throw when the
        // host hasn't registered a real IUserListProvider.
        services.TryAddSingleton<IUserListProvider, NullUserListProvider>();

        // ResourceRegistry is built once from the options registration calls made at startup.
        services.AddSingleton<ResourceRegistry>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<DabblerSecurityOptions>>().Value;
            return new ResourceRegistry(
                opts.RegisteredPages,
                opts.RegisteredEntities,
                opts.RegisteredEndpoints,
                opts.RegisteredControls);
        });

        // MetadataRegistry is the semantic table/column facade over ResourceRegistry
        // used by the restriction-based Role Master Details UI. Handlers keep using
        // ResourceRegistry directly; admin cascading dropdowns use MetadataRegistry.
        services.AddSingleton<MetadataRegistry>();

        return services;
    }

    /// <summary>
    /// Configures the JSON file backing store for SecurityConfiguration and user-role assignments.
    /// Suitable for development, single-server deployments, and demos.
    /// Call this OR <see cref="UseEfCoreStore(IServiceCollection)"/> — not both.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="filePath">Path to the security-config JSON file.</param>
    /// <param name="userRoleFilePath">
    /// Optional path to the user-roles JSON file. Defaults to <c>user-roles.json</c>
    /// in the same directory as <paramref name="filePath"/>.
    /// </param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection UseJsonFileStore(
        this IServiceCollection services,
        string filePath,
        string? userRoleFilePath = null)
    {
        services.AddSingleton<ISecurityConfigurationStore>(sp =>
            new JsonFileSecurityStore(
                filePath,
                sp.GetRequiredService<ILogger<JsonFileSecurityStore>>()));

        var roleFilePath = userRoleFilePath
            ?? Path.Combine(
                Path.GetDirectoryName(filePath) ?? ".",
                "user-roles.json");

        services.AddSingleton<IUserRoleStore>(sp =>
            new JsonFileUserRoleStore(
                roleFilePath,
                sp.GetRequiredService<ILogger<JsonFileUserRoleStore>>()));

        return services;
    }

    /// <summary>
    /// Configures the EF Core backing store for SecurityConfiguration and user-role assignments.
    /// Uses the same <see cref="DabblerSecurityDbContext"/> registered by <see cref="AddDabblerSecurity(IServiceCollection, Action{DabblerSecurityOptions})"/>.
    /// Call this OR <see cref="UseJsonFileStore(IServiceCollection, string, string?)"/> — not both.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection UseEfCoreStore(this IServiceCollection services)
    {
        services.AddDbContextFactory<DabblerSecurityDbContext>((sp, opts) =>
        {
            var cfg = sp.GetRequiredService<IOptions<DabblerSecurityOptions>>().Value;
            opts.UseSqlServer(cfg.ConnectionString);
        });
        services.AddSingleton<ISecurityConfigurationStore, EfCoreSecurityStore>();
        services.AddSingleton<IUserRoleStore, EfCoreUserRoleStore>();
        return services;
    }
}
