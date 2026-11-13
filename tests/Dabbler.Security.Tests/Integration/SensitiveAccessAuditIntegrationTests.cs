using System.Security.Claims;
using Dabbler.Security.Data;
using Dabbler.Security.Models;
using Dabbler.Security.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dabbler.Security.Tests.Integration;

/// <summary>
/// Integration tests for the Session 2 audit pipeline focused on the auditor +
/// role-switch decorator. The binder-driven <c>ExemptionExpired</c> coverage
/// lives in <c>Dabbler.Security.Blazor.Tests.Integration</c> (the binder type
/// lives in the Blazor project).
///
/// Runs against the EF Core InMemory provider so the test does not depend on
/// a SQL Server instance — temporal-table semantics themselves are SQL Server
/// only and are validated through the migration shape rather than runtime
/// behaviour here.
/// </summary>
public class SensitiveAccessAuditIntegrationTests
{
    [Fact]
    public async Task RoleSwitch_WhenAuditorInvokedDirectly_EmitsRoleSwitchRow()
    {
        await using var harness = AuditHarness.Build();

        await harness.Auditor.AuditRoleSwitchAsync(BuildPrincipal(userId: 42), "CrossOrgUserAdmin", "switch");

        var rows = await harness.LoadRowsAsync();
        rows.Should().HaveCount(1);
        var row = rows.Single();
        row.EventKind.Should().Be(SensitiveAccessEventKind.RoleSwitch);
        row.UserId.Should().Be(42);
        row.RoleName.Should().Be("CrossOrgUserAdmin");
        row.CorrelationId.Should().NotBeEmpty();
        row.Details.Should().Be("switch");
        row.EntityKey.Should().BeNull();
        row.ColumnKey.Should().BeNull();
    }

    [Fact]
    public async Task AuditingRoleSwitcher_DecoratesSwitchToRoleAndClearSwitch()
    {
        await using var harness = AuditHarness.Build();
        var principal = BuildPrincipal(userId: 7, role: "User");
        harness.SetPrincipal(principal);

        var switcher = harness.RoleSwitcher;
        switcher.SwitchToRole("CrossOrgMonitor");
        switcher.ClearSwitch();

        // The decorator emits audit calls fire-and-forget; give them a beat
        // to land before the read so the test does not race.
        await harness.WaitForRowsAsync(expectedCount: 2);

        var rows = (await harness.LoadRowsAsync())
            .OrderBy(r => r.Id)
            .ToList();

        rows.Should().HaveCount(2);
        rows[0].EventKind.Should().Be(SensitiveAccessEventKind.RoleSwitch);
        rows[0].RoleName.Should().Be("CrossOrgMonitor");
        rows[0].Details.Should().Be("switch");
        rows[1].EventKind.Should().Be(SensitiveAccessEventKind.RoleSwitch);
        rows[1].RoleName.Should().Be("CrossOrgMonitor");
        rows[1].Details.Should().Be("revert");
    }

    [Fact]
    public async Task ExemptionGranted_PopulatesGranterTargetExpiryFields()
    {
        await using var harness = AuditHarness.Build();

        var expiresAt = new DateTimeOffset(2026, 5, 31, 0, 0, 0, TimeSpan.Zero);
        await harness.Auditor.AuditExemptionGrantedAsync(
            BuildPrincipal(userId: 9, role: "CrossOrgUserAdmin"),
            entityKey: "Customer",
            columnKey: "SocialSecurityNumber",
            targetRoleName: "Customer",
            granterUserId: 9,
            expiresAt: expiresAt,
            details: "grant");

        var row = (await harness.LoadRowsAsync()).Single();
        row.EventKind.Should().Be(SensitiveAccessEventKind.ExemptionGranted);
        row.EntityKey.Should().Be("Customer");
        row.ColumnKey.Should().Be("SocialSecurityNumber");
        row.TargetRoleName.Should().Be("Customer");
        row.GranterUserId.Should().Be(9);
        row.ExpiresAt.Should().Be(expiresAt);
        row.RoleName.Should().Be("CrossOrgUserAdmin");
        row.UserId.Should().Be(9);
    }

    [Fact]
    public async Task ExemptionExpired_PopulatesEntityColumnTargetExpiryFields()
    {
        await using var harness = AuditHarness.Build();

        var expiry = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        await harness.Auditor.AuditExemptionExpiredAsync(
            BuildPrincipal(userId: 11, role: "Customer"),
            entityKey: "Customer",
            columnKey: "SocialSecurityNumber",
            targetRoleName: "Customer",
            expiresAt: expiry);

        var row = (await harness.LoadRowsAsync()).Single();
        row.EventKind.Should().Be(SensitiveAccessEventKind.ExemptionExpired);
        row.EntityKey.Should().Be("Customer");
        row.ColumnKey.Should().Be("SocialSecurityNumber");
        row.TargetRoleName.Should().Be("Customer");
        row.ExpiresAt.Should().Be(expiry);
    }

    private static ClaimsPrincipal BuildPrincipal(int userId, string role = "User")
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
            },
            authenticationType: "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    private sealed class AuditHarness : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;

        private AuditHarness(ServiceProvider provider)
        {
            _provider = provider;
        }

        public ISensitiveAccessAuditor Auditor => _provider.GetRequiredService<ISensitiveAccessAuditor>();
        public IRoleSwitcher RoleSwitcher => _provider.GetRequiredService<IRoleSwitcher>();
        public MutableAuthStateProvider AuthStateProvider => _provider.GetRequiredService<MutableAuthStateProvider>();

        public void SetPrincipal(ClaimsPrincipal principal) => AuthStateProvider.SetPrincipal(principal);

        public async Task<List<AppSensitiveAccessLog>> LoadRowsAsync()
        {
            await using var db = await _provider.GetRequiredService<IDbContextFactory<DabblerSecurityDbContext>>()
                .CreateDbContextAsync();
            return await db.AppSensitiveAccessLogs.AsNoTracking().ToListAsync();
        }

        public async Task WaitForRowsAsync(int expectedCount, int timeoutMs = 1000)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                var rows = await LoadRowsAsync();
                if (rows.Count >= expectedCount) return;
                await Task.Delay(20);
            }
        }

        public static AuditHarness Build()
        {
            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));
            services.AddDbContextFactory<DabblerSecurityDbContext>(opts =>
                opts.UseInMemoryDatabase(dbName));

            services.AddSingleton<MutableAuthStateProvider>();
            services.AddSingleton<AuthenticationStateProvider>(sp => sp.GetRequiredService<MutableAuthStateProvider>());

            services.AddSingleton<IRenderCorrelationScope, RenderCorrelationScope>();
            services.AddSingleton<ISensitiveAccessAuditor, SensitiveAccessAuditor>();

            services.AddSingleton<SessionRoleSwitcher>();
            services.AddSingleton<IRoleSwitcher>(sp => new AuditingRoleSwitcher(
                sp.GetRequiredService<SessionRoleSwitcher>(),
                sp.GetRequiredService<ISensitiveAccessAuditor>(),
                sp));

            return new AuditHarness(services.BuildServiceProvider(validateScopes: false));
        }

        public ValueTask DisposeAsync() => _provider.DisposeAsync();
    }

    public sealed class MutableAuthStateProvider : AuthenticationStateProvider
    {
        private AuthenticationState _state = new(new ClaimsPrincipal(new ClaimsIdentity()));

        public void SetPrincipal(ClaimsPrincipal principal) => _state = new AuthenticationState(principal);

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_state);
    }
}
