using System.Security.Claims;
using Dabbler.Components.Csv;
using Dabbler.Security.Blazor.Components.Restriction;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MudBlazor.Services;

namespace Dabbler.Security.Blazor.Extensions;

/// <summary>
/// Provides extension methods for setting up Dabbler Security services in a Blazor application's dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Dabbler Security services required for Blazor applications to the specified <see cref="IServiceCollection"/>.
    /// This includes services for authorization, UI components, and a delegating handler for API requests.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddDabblerSecurityBlazor(this IServiceCollection services)
    {
        services.AddMudServices();
        services.AddAuthorizationCore();
        services.AddCascadingAuthenticationState();

        // Defensive: AddDabblerSecurity() is the canonical registration point
        // for the per-circuit / per-request correlation scope, but a Blazor
        // host that calls AddDabblerSecurityBlazor() in isolation (for unit
        // tests, prototype hosts) would otherwise resolve a binder without a
        // scope and silently fall back to a per-binder dedupe set that never
        // resets on navigation. TryAddScoped is a no-op when the core
        // registration is already present.
        services.TryAddScoped<IRenderCorrelationScope, RenderCorrelationScope>();

        // Data-bound restriction binder consumed by RestrictedFormField /
        // RestrictedGridColumn. Scoped lifetime matches IRoleSwitcher so the
        // binder sees role-switch state for the current circuit. The binder
        // takes its dependencies positionally so the optional auditor +
        // correlation-scope arguments resolve through DI.
        services.AddScoped<IRestrictionBinder>(sp => new RestrictionBinder(
            sp.GetRequiredService<Dabbler.Security.Configuration.SecurityConfigurationManager>(),
            sp.GetRequiredService<AuthenticationStateProvider>(),
            sp.GetRequiredService<Dabbler.Security.Services.IRoleSwitcher>(),
            sp.GetRequiredService<Dabbler.Security.Authorization.IBakedInRoleProvider>(),
            sp.GetService<Dabbler.Security.Services.ISensitiveAccessAuditor>(),
            sp.GetService<Dabbler.Security.Services.IRenderCorrelationScope>()));

        // CSV export service consumed by the Compliance Review tab (Session 4
        // of sensitive-field-masking-audit). Stateless, RFC 4180-compliant.
        services.TryAddSingleton<ICsvExportService, CsvExportService>();

        // Register the delegating handler to pass user headers to the API
        services.AddTransient<UserIdentityHandler>();

        return services;
    }
}

/// <summary>
/// A <see cref="DelegatingHandler"/> that attaches the authenticated user's identity information
/// as custom headers to outgoing HTTP requests. This is used to securely identify the user
/// to downstream API services.
/// </summary>
public class UserIdentityHandler : DelegatingHandler
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserIdentityHandler"/> class.
    /// </summary>
    /// <param name="authStateProvider">The authentication state provider used to retrieve user information.</param>
    /// <param name="httpContextAccessor">Provides access to the current HTTP context when prerendered requests fall back outside a Razor component scope.</param>
    public UserIdentityHandler(
        AuthenticationStateProvider authStateProvider,
        IHttpContextAccessor httpContextAccessor)
    {
        _authStateProvider = authStateProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Sends an HTTP request, adding user identity headers if the user is authenticated.
    /// </summary>
    /// <param name="request">The HTTP request message to send.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the HTTP response message.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        ClaimsPrincipal user;

        try
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            user = authState.User;
        }
        catch (InvalidOperationException)
        {
            user = _httpContextAccessor.HttpContext?.User
                ?? new ClaimsPrincipal(new ClaimsIdentity());
        }
        
        if (user.Identity?.IsAuthenticated == true)
        {
            var email = user.FindFirst(ClaimTypes.Email)?.Value;
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var activeOrganizationId = user.FindFirst(UserSessionContextConstants.ActiveOrganizationIdClaimType)?.Value;

            if (!string.IsNullOrEmpty(email))
            {
                request.Headers.Add("X-App-User", email);
            }
            
            if (!string.IsNullOrEmpty(userId))
            {
                request.Headers.Add("X-App-User-Id", userId);
            }

            if (!string.IsNullOrEmpty(activeOrganizationId))
            {
                request.Headers.Add(UserSessionContextConstants.ActiveOrganizationIdHeaderName, activeOrganizationId);
            }
        }
        
        return await base.SendAsync(request, ct);
    }
}
