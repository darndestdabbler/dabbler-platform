using Dabbler.Components.Data;
using Dabbler.Security.Blazor.Extensions;
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
/// Emitted by Dabbler.CrudSlice — matches the host's real production wiring:
///   * Base URL: configuration["Services:ApiBaseUrl"]
///         ?? configuration["ApiBaseUrl"]
///         ?? "http://localhost:5011"
///   * Header X-Internal-Api-Key: configuration["DabblerSecurity:InternalApiKey"]
///         ?? "dev-internal-key-acme"
///   * AddHttpMessageHandler&lt;UserIdentityHandler&gt;() for per-request user identity forwarding
/// </summary>
public static class SupplierScaffoldingExtensions
{
    /// <summary>Registers the scaffolded Supplier slice's API client and grid data provider.</summary>
    public static IHttpClientBuilder AddSupplierScaffolding(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var baseUrl = configuration["Services:ApiBaseUrl"]
            ?? configuration["ApiBaseUrl"]
            ?? "http://localhost:5011";
        var internalApiKey = configuration["DabblerSecurity:InternalApiKey"]
            ?? "dev-internal-key-acme";

        var httpBuilder = services.AddHttpClient<SupplierApiClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Add("X-Internal-Api-Key", internalApiKey);
        }).AddHttpMessageHandler<UserIdentityHandler>();

        services.AddTransient<IGridDataProvider<SupplierDto>, SupplierGridDataProvider>();

        return httpBuilder;
    }
}
