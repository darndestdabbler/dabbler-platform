using Microsoft.Extensions.DependencyInjection;

namespace AcmeCoffeeOnline.Api.Services;

/// <summary>
/// One-line wire-up for the scaffolded Supplier service. Call from Api/Program.cs:
///
/// <code>
/// builder.Services.AddSupplierApiServices();
/// </code>
///
/// Emitted by Dabbler.CrudSlice — matches the host's real production wiring at
/// src/Apps/AcmeCoffeeOnline.Api/Program.cs:156-162.
/// </summary>
public static class SupplierApiRegistrationExtensions
{
    /// <summary>Registers ISupplierService -&gt; SupplierService in DI (scoped).</summary>
    public static IServiceCollection AddSupplierApiServices(this IServiceCollection services)
    {
        services.AddScoped<ISupplierService, SupplierService>();
        return services;
    }
}
