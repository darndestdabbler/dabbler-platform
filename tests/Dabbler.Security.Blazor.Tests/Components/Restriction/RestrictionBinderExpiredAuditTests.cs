using System.Security.Claims;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Authorization;
using Dabbler.Security.Blazor.Components.Restriction;
using Dabbler.Security.Configuration;
using Dabbler.Security.Models;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace Dabbler.Security.Blazor.Tests.Components.Restriction;

/// <summary>
/// Integration-flavored coverage for the <see cref="RestrictionBinder"/>'s
/// per-scope <c>ExemptionExpired</c> audit emission. Verifies that repeated
/// resolves of the same expired exemption produce exactly one audit row, per
/// the Session 2 spec rule "use an in-memory set of already-emitted exemption
/// IDs keyed per circuit to avoid duplicate rows on every subsequent resolve."
/// </summary>
public class RestrictionBinderExpiredAuditTests
{
    [Fact]
    public async Task RepeatedResolveOfExpiredExemption_EmitsExactlyOneAuditRow()
    {
        var configManager = await BuildConfigManagerAsync(cfg =>
        {
            cfg.DefaultMasks.Add(new DefaultMaskRule
            {
                EntityKey = "Customer",
                ColumnKey = "SocialSecurityNumber",
                MaskFormat = "XXX-XX-{last4}",
            });
            cfg.MaskExemptions.Add(new MaskExemption
            {
                EntityKey = "Customer",
                ColumnKey = "SocialSecurityNumber",
                TargetRoleName = "Customer",
                GrantedBy = "admin@acmecoffee.com",
                GrantedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                ExpiresAt = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero),
            });
        });

        var auditor = new RecordingAuditor();
        var principal = BuildPrincipal(userId: 11, role: "Customer");

        var binder = new RestrictionBinder(
            configManager,
            new StaticAuthStateProvider(principal),
            new SessionRoleSwitcher(),
            new PlatformBakedInRoleProvider(),
            auditor);

        var first = await binder.ResolveAsync("Customer", "SocialSecurityNumber");
        var second = await binder.ResolveAsync("Customer", "SocialSecurityNumber");
        var third = await binder.ResolveAsync("Customer", "SocialSecurityNumber");

        // The expired exemption falls back to default-masked rendering — IsMasked
        // remains true because the only matching exemption is past expiry.
        first.IsMasked.Should().BeTrue();
        second.IsMasked.Should().BeTrue();
        third.IsMasked.Should().BeTrue();

        auditor.ExpiredCalls.Should().HaveCount(1, because: "the binder de-dupes per scope so repeated resolves emit at most one row");
        var (entity, column, target, expiry) = auditor.ExpiredCalls.Single();
        entity.Should().Be("Customer");
        column.Should().Be("SocialSecurityNumber");
        target.Should().Be("Customer");
        expiry.Should().Be(new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task ResolveOfActiveExemption_DoesNotEmitExpiredAudit()
    {
        var configManager = await BuildConfigManagerAsync(cfg =>
        {
            cfg.DefaultMasks.Add(new DefaultMaskRule
            {
                EntityKey = "Customer",
                ColumnKey = "SocialSecurityNumber",
                MaskFormat = "XXX-XX-{last4}",
            });
            cfg.MaskExemptions.Add(new MaskExemption
            {
                EntityKey = "Customer",
                ColumnKey = "SocialSecurityNumber",
                TargetRoleName = "Customer",
                GrantedBy = "admin@acmecoffee.com",
                GrantedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddYears(1),
            });
        });

        var auditor = new RecordingAuditor();
        var binder = new RestrictionBinder(
            configManager,
            new StaticAuthStateProvider(BuildPrincipal(userId: 11, role: "Customer")),
            new SessionRoleSwitcher(),
            new PlatformBakedInRoleProvider(),
            auditor);

        await binder.ResolveAsync("Customer", "SocialSecurityNumber");

        auditor.ExpiredCalls.Should().BeEmpty();
    }

    private static async Task<SecurityConfigurationManager> BuildConfigManagerAsync(Action<SecurityConfiguration> populate)
    {
        var configuration = new SecurityConfiguration();
        populate(configuration);
        var manager = new SecurityConfigurationManager(new InMemorySecurityConfigurationStore(configuration));
        await manager.EnsureLoadedAsync();
        return manager;
    }

    private static ClaimsPrincipal BuildPrincipal(int userId, string role)
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

    private sealed class StaticAuthStateProvider : AuthenticationStateProvider
    {
        private readonly AuthenticationState _state;
        public StaticAuthStateProvider(ClaimsPrincipal principal) => _state = new AuthenticationState(principal);
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_state);
    }

    private sealed class InMemorySecurityConfigurationStore : ISecurityConfigurationStore
    {
        private SecurityConfiguration _current;
        public InMemorySecurityConfigurationStore(SecurityConfiguration initial) => _current = initial;
        public Task<SecurityConfiguration> LoadAsync(CancellationToken ct = default) => Task.FromResult(_current);
        public Task SaveAsync(SecurityConfiguration configuration, CancellationToken ct = default)
        {
            _current = configuration;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingAuditor : ISensitiveAccessAuditor
    {
        public List<(string Entity, string Column, string Target, DateTimeOffset Expiry)> ExpiredCalls { get; } = new();

        public Task AuditRoleSwitchAsync(ClaimsPrincipal user, string roleName, string? details = null, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task AuditExemptionGrantedAsync(ClaimsPrincipal user, string entityKey, string columnKey, string targetRoleName, int? granterUserId, DateTimeOffset? expiresAt, string? details = null, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task AuditExemptionExpiredAsync(ClaimsPrincipal user, string entityKey, string columnKey, string targetRoleName, DateTimeOffset expiresAt, CancellationToken ct = default)
        {
            ExpiredCalls.Add((entityKey, columnKey, targetRoleName, expiresAt));
            return Task.CompletedTask;
        }

        public Task AuditFieldUnmaskedReadAsync(ClaimsPrincipal user, string entityKey, string entityId, string columnKey, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task AuditAsync(AppSensitiveAccessLog row, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
