using Bunit;
using Bunit.TestDoubles;
using Dabbler.Security.Options;
using Dabbler.Security.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace Dabbler.Security.Blazor.Tests.TestHelpers;

/// <summary>
/// Extension helpers for wiring up MudBlazor and Blazor authorization in bUnit tests.
///
/// ORDERING MATTERS in bUnit: AddTestAuthorization must be called before AddMudServices
/// because AddMudServices triggers internal service resolution that locks the bUnit
/// TestServiceProvider, preventing further registrations.
/// </summary>
public static class BunitContextExtensions
{
    /// <summary>
    /// Configure a <see cref="TestContext"/> with authorization and MudBlazor.
    /// Call this before rendering ANY component.
    /// </summary>
    public static TestContext SetupForDabbler(
        this TestContext ctx,
        string[]? roles = null,
        Action<IServiceCollection>? configureServices = null)
    {
        // ── 1. Authorization MUST come first — before anything that locks the provider ──
        var authContext = ctx.AddTestAuthorization();
        if (roles is { Length: > 0 })
        {
            authContext.SetAuthorized("testuser@test.local");
            authContext.SetRoles(roles);
        }

        // ── 2. MudBlazor services ────────────────────────────────────────────────────
        ctx.Services.AddMudServices(cfg =>
        {
            cfg.SnackbarConfiguration.ShowTransitionDuration = 0;
            cfg.SnackbarConfiguration.HideTransitionDuration = 0;
        });

        // ── 3. Default library services required by Dabbler components ───────────────
        ctx.Services.AddScoped<IRoleSwitcher, SessionRoleSwitcher>();
        ctx.Services.AddSingleton<AuthorizationStateNotifier>();
        ctx.Services.AddSingleton<IFieldMaskService, FieldMaskService>();
        ctx.Services.Configure<DabblerSecurityOptions>(_ => { });

        // ── 4. Caller-specific services (mocks, DbContext, HttpClient …) ─────────────
        configureServices?.Invoke(ctx.Services);

        // ── 5. Suppress all JS interop calls without explicit stub setup ─────────────
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        // ── 6. Mount MudBlazor infrastructure providers after all services are registered ──
        // MudBlazor ≥ 7 requires these to be present in the same render tree.
        ctx.RenderComponent<MudPopoverProvider>();
        ctx.RenderComponent<MudDialogProvider>();
        ctx.RenderComponent<MudSnackbarProvider>();

        return ctx;
    }
}
