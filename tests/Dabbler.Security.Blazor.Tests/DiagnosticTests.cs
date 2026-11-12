using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Dabbler.Security.Blazor.Tests;

/// <summary>
/// Minimal tests to diagnose bUnit service provider initialization issues.
/// Delete once the root cause is understood.
/// </summary>
public class DiagnosticTests
{
    [Fact]
    public void FreshContext_CanAddServices()
    {
        using var ctx = new TestContext();
        // Can we add any service on a brand-new context?
        ctx.Services.AddSingleton<string>("hello");
        // No assertion needed — just check it doesn't throw
    }

    [Fact]
    public void FreshContext_AddTestAuthorization_Works()
    {
        using var ctx = new TestContext();
        // AddTestAuthorization immediately on a fresh context
        ctx.AddTestAuthorization();
    }

    [Fact]
    public void FreshContext_AddMudServices_ThenAddSingleton_Works()
    {
        using var ctx = new TestContext();
        ctx.Services.AddMudServices();
        // Can we still add services after AddMudServices?
        ctx.Services.AddSingleton<string>("hello");
    }

    [Fact]
    public void FreshContext_AddMudServices_ThenAddTestAuthorization_Works()
    {
        using var ctx = new TestContext();
        ctx.Services.AddMudServices();
        ctx.AddTestAuthorization(); // is this still possible?
    }
}
