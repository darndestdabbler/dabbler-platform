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
    public async Task FreshContext_CanAddServices()
    {
        await using var ctx = new BunitContext();
        // Can we add any service on a brand-new context?
        ctx.Services.AddSingleton<string>("hello");
        // No assertion needed — just check it doesn't throw
    }

    [Fact]
    public async Task FreshContext_AddTestAuthorization_Works()
    {
        await using var ctx = new BunitContext();
        // AddAuthorization immediately on a fresh context
        ctx.AddAuthorization();
    }

    [Fact]
    public async Task FreshContext_AddMudServices_ThenAddSingleton_Works()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddMudServices();
        // Can we still add services after AddMudServices?
        ctx.Services.AddSingleton<string>("hello");
    }

    [Fact]
    public async Task FreshContext_AddMudServices_ThenAddTestAuthorization_Works()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddMudServices();
        ctx.AddAuthorization(); // is this still possible?
    }
}
