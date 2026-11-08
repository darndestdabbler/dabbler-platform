using System.Reflection;

namespace AcmeCoffeeOnline.Shared.Diagnostics;

public record HealthResponse(
    string Service,
    string Status,
    string Environment,
    string Version);

public sealed record WebHealthResponse(
    string Service,
    string Status,
    string Environment,
    string Version,
    string DeliveryMode) : HealthResponse(Service, Status, Environment, Version);

public static class HealthResponseFactory
{
    public static HealthResponse Create(string service, string environmentName, Assembly assembly)
        => new(
            service,
            "healthy",
            environmentName,
            GetVersion(assembly));

    public static WebHealthResponse CreateWeb(string environmentName, Assembly assembly, string deliveryMode)
        => new(
            "web",
            "healthy",
            environmentName,
            GetVersion(assembly),
            deliveryMode);

    private static string GetVersion(Assembly assembly)
        => assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
           ?? assembly.GetName().Version?.ToString()
           ?? "unknown";
}
