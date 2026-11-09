using Dabbler.Components.Data;
using Dabbler.Security.Blazor.Extensions;
using T5DtoNs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace T3WebNs.Data;

/// <summary>
/// One-line wire-up for the scaffolded T1Entity slice. Call from Program.cs once after
/// pre-staging the DTO and EF entity types:
///
/// <code>
/// builder.Services.AddT1EntityScaffolding(builder.Configuration);
/// </code>
///
/// Registers:
///   * <see cref="T1EntityApiClient"/> as a typed HttpClient (base address read from
///     configuration at key "T6ApiBaseUrlKey", falling back to "ApiBaseUrl",
///     then defaulting to "http://localhost:5011").
///   * Header <c>X-Internal-Api-Key</c> from <c>DabblerSecurity:InternalApiKey</c>,
///     defaulting to <c>dev-internal-key-acme</c>.
///   * <c>AddHttpMessageHandler&lt;UserIdentityHandler&gt;()</c> for per-request identity forwarding.
///   * <see cref="IGridDataProvider{T1EntityDto}"/> bound to
///     <see cref="T1EntityGridDataProvider"/>, which adapts the MudDataGrid
///     <c>ServerData</c> contract to the QBE POST /query endpoint.
/// </summary>
public static class T1EntityScaffoldingExtensions
{
    /// <summary>
    /// Registers the scaffolded T1Entity slice's API client and grid data provider.
    /// </summary>
    /// <param name="services">The application's service collection.</param>
    /// <param name="configuration">
    /// Configuration source consulted for the API base URL (key "T6ApiBaseUrlKey").
    /// </param>
    /// <param name="apiBaseUrlOverride">
    /// Optional explicit base URL — wins over configuration when supplied.
    /// </param>
    /// <returns>The <see cref="IHttpClientBuilder"/> for the typed client, so callers can
    /// chain additional <c>.AddHttpMessageHandler</c> / <c>.ConfigureHttpClient</c> calls.</returns>
    public static IHttpClientBuilder AddT1EntityScaffolding(
        this IServiceCollection services,
        IConfiguration configuration,
        string? apiBaseUrlOverride = null)
    {
        var baseUrl = apiBaseUrlOverride
            ?? configuration["T6ApiBaseUrlKey"]
            ?? configuration["ApiBaseUrl"]
            ?? "http://localhost:5011";
        var internalApiKey = configuration["DabblerSecurity:InternalApiKey"]
            ?? "dev-internal-key-acme";

        var httpBuilder = services.AddHttpClient<T1EntityApiClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Add("X-Internal-Api-Key", internalApiKey);
        }).AddHttpMessageHandler<UserIdentityHandler>();

        services.AddTransient<IGridDataProvider<T1EntityDto>, T1EntityGridDataProvider>();

        return httpBuilder;
    }
}
