using Dabbler.Components.Data;
using AcmeCoffeeOnline.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AcmeCoffeeOnline.Web.Data;

/// <summary>
/// One-line wire-up for the scaffolded Supplier slice. Call from Program.cs once after
/// pre-staging the DTO and EF entity types:
///
/// <code>
/// builder.Services.AddSupplierScaffolding(builder.Configuration);
/// </code>
///
/// Registers:
///   * <see cref="SupplierApiClient"/> as a typed HttpClient (base address read from
///     configuration at key "Api:BaseUrl", default "http://localhost:5011").
///   * <see cref="IGridDataProvider{SupplierDto}"/> bound to
///     <see cref="SupplierGridDataProvider"/>, which adapts the MudDataGrid
///     <c>ServerData</c> contract to the QBE POST /query endpoint.
///
/// For multi-tenant / per-request authorization header injection, layer
/// <c>.AddHttpMessageHandler&lt;YourIdentityHandler&gt;()</c> onto the
/// returned <see cref="IHttpClientBuilder"/> at the call site.
/// </summary>
public static class SupplierScaffoldingExtensions
{
    /// <summary>
    /// Registers the scaffolded Supplier slice's API client and grid data provider.
    /// </summary>
    /// <param name="services">The application's service collection.</param>
    /// <param name="configuration">
    /// Configuration source consulted for the API base URL (key "Api:BaseUrl").
    /// </param>
    /// <param name="apiBaseUrlOverride">
    /// Optional explicit base URL — wins over configuration when supplied.
    /// </param>
    /// <returns>The <see cref="IHttpClientBuilder"/> for the typed client, so callers can
    /// chain additional <c>.AddHttpMessageHandler</c> / <c>.ConfigureHttpClient</c> calls.</returns>
    public static IHttpClientBuilder AddSupplierScaffolding(
        this IServiceCollection services,
        IConfiguration configuration,
        string? apiBaseUrlOverride = null)
    {
        var baseUrl = apiBaseUrlOverride
            ?? configuration["Api:BaseUrl"]
            ?? "http://localhost:5011";

        var httpBuilder = services.AddHttpClient<SupplierApiClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
        });

        services.AddTransient<IGridDataProvider<SupplierDto>, SupplierGridDataProvider>();

        return httpBuilder;
    }
}
