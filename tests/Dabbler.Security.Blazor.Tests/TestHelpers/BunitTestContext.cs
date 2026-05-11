using System.Security.Claims;
using Bunit;
using Bunit.TestDoubles;
using Dabbler.Security.Models;
using Dabbler.Security.Options;
using Dabbler.Security.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
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

        // Sensitive-field audit pipeline: bUnit components that render
        // MaskExemptionsSubTab inject ISensitiveAccessAuditor. Most tests
        // care only about render shape, so a no-op auditor is the right
        // default. Tests that need to assert audit behaviour can override
        // via configureServices.
        ctx.Services.AddSingleton<ISensitiveAccessAuditor, NullSensitiveAccessAuditor>();

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

/// <summary>
/// Extension helpers for the test mocks of <see cref="IAdminUserQueryService"/>.
///
/// The service exposes parallel no-arg and ClaimsPrincipal-accepting overloads
/// (the latter added 2026-05-11 to support interactive Blazor Server, where
/// IHttpContextAccessor.HttpContext is null and the no-arg path fails closed).
/// Blazor components in <c>Dabbler.Security.Blazor</c> call the ClaimsPrincipal
/// overload via <c>AuthenticationStateProvider</c>; older test setups stub only
/// the no-arg form and would otherwise leave the overload returning Moq's
/// default <c>null</c>/<c>default</c>. <see cref="MirrorActorOverloads"/> mirrors
/// each existing no-arg setup onto the ClaimsPrincipal overload so test bodies
/// don't need to be updated case-by-case.
/// </summary>
public static class AdminUserQueryServiceMockExtensions
{
    /// <summary>
    /// Mirror each existing no-arg actor-context setup onto its
    /// <see cref="ClaimsPrincipal"/>-accepting overload. Call after every
    /// existing <c>Setup(...)</c> chain on the mock.
    /// </summary>
    public static void MirrorActorOverloads(
        this Mock<IAdminUserQueryService> mock,
        string? actorActiveRole,
        string actorUserId,
        IReadOnlySet<int> actorOrganizationIds,
        IReadOnlyList<AdminUserRow>? users = null)
    {
        mock.Setup(q => q.GetActorActiveAdminRole(It.IsAny<ClaimsPrincipal>()))
            .Returns(actorActiveRole);
        mock.Setup(q => q.GetActorUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(actorUserId);
        mock.Setup(q => q.GetActorAssignedOrganizationIdsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(actorOrganizationIds);
        if (users is not null)
        {
            mock.Setup(q => q.GetUsersAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(users);
        }
    }
}

internal sealed class NullSensitiveAccessAuditor : ISensitiveAccessAuditor
{
    public Task AuditRoleSwitchAsync(ClaimsPrincipal user, string roleName, string? details = null, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task AuditExemptionGrantedAsync(ClaimsPrincipal user, string entityKey, string columnKey, string targetRoleName, int? granterUserId, DateTimeOffset? expiresAt, string? details = null, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task AuditExemptionExpiredAsync(ClaimsPrincipal user, string entityKey, string columnKey, string targetRoleName, DateTimeOffset expiresAt, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task AuditFieldUnmaskedReadAsync(ClaimsPrincipal user, string entityKey, string entityId, string columnKey, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task AuditAsync(AppSensitiveAccessLog row, CancellationToken ct = default)
        => Task.CompletedTask;
}
