using System.Security.Claims;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="UserIdentityHandler"/> class.
    /// </summary>
    /// <param name="authStateProvider">The authentication state provider used to retrieve user information.</param>
    public UserIdentityHandler(AuthenticationStateProvider authStateProvider)
    {
        _authStateProvider = authStateProvider;
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
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        
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
