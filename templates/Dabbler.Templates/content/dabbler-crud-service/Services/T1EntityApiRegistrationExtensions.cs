using Microsoft.Extensions.DependencyInjection;

namespace T3ApiNs.Services;

/// <summary>
/// One-line wire-up for the scaffolded T1Entity service. Call from Api/Program.cs:
///
/// <code>
/// builder.Services.AddT1EntityApiServices();
/// </code>
/// </summary>
public static class T1EntityApiRegistrationExtensions
{
    /// <summary>Registers IT1EntityService -> T1EntityService in DI (scoped).</summary>
    public static IServiceCollection AddT1EntityApiServices(this IServiceCollection services)
    {
        services.AddScoped<IT1EntityService, T1EntityService>();
        return services;
    }
}