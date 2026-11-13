using System.Security.Claims;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Authorization;
using Dabbler.Security.Blazor.Components.Restriction;
using Dabbler.Security.Configuration;
using Dabbler.Security.Models;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Dabbler.Security.Blazor.Tests.Components.Restriction;

/// <summary>
/// Integration coverage for the binder's <c>FieldUnmaskedRead</c> emission and
/// per-correlation batching. Verifies the four spec-mandated behaviors:
/// (1) one row per (correlation, entity, id, column) regardless of repeat
/// resolves; (2) navigation regenerates the correlation and the next read
/// emits a fresh row; (3) a grid render across N entity instances emits 2N
/// rows for two sensitive columns and no more on repaint within the same
/// correlation; (4) no row is emitted when the column has no default mask
/// (not sensitive) or when the user is not exempted (still masked).
/// </summary>
public class RestrictionBinderUnmaskedReadAuditTests
{
    [Fact]
    public async Task ExemptedRead_EmitsOneRowPerCorrelationEntityAndColumn()
    {
        var harness = await Harness.BuildAsync(WithExemptedConfig);

        var first = await harness.Binder.ResolveAsync("Customer", "SocialSecurityNumber", entityId: "5");
        var second = await harness.Binder.ResolveAsync("Customer", "SocialSecurityNumber", entityId: "5");
        var third = await harness.Binder.ResolveAsync("Customer", "SocialSecurityNumber", entityId: "5");

        first.IsMasked.Should().BeFalse();
        second.IsMasked.Should().BeFalse();
        third.IsMasked.Should().BeFalse();

        harness.Auditor.UnmaskedReads.Should().ContainSingle();
        var (entity, id, column) = harness.Auditor.UnmaskedReads.Single();
        entity.Should().Be("Customer");
        id.Should().Be("5");
        column.Should().Be("SocialSecurityNumber");
    }

    [Fact]
    public async Task NavigationRegeneratesCorrelation_NextReadEmitsAnotherRow()
    {
        var harness = await Harness.BuildAsync(WithExemptedConfig);

        await harness.Binder.ResolveAsync("Customer", "SocialSecurityNumber", entityId: "5");
        harness.Auditor.UnmaskedReads.Should().HaveCount(1);

        // Simulate navigation: reset correlation + dedupe set.
        harness.CorrelationScope.ResetForRequest();

        await harness.Binder.ResolveAsync("Customer", "SocialSecurityNumber", entityId: "5");
        harness.Auditor.UnmaskedReads.Should().HaveCount(2);
    }

    [Fact]
    public async Task GridWithMultipleEntities_EmitsOneRowPerEntityPerColumn()
    {
        var harness = await Harness.BuildAsync(cfg =>
        {
            WithExemptedConfig(cfg);
            cfg.DefaultMasks.Add(new DefaultMaskRule
            {
                EntityKey = "Customer",
                ColumnKey = "PhoneNumber",
                MaskFormat = "***-***-{last4}",
            });
            cfg.MaskExemptions.Add(new MaskExemption
            {
                EntityKey = "Customer",
                ColumnKey = "PhoneNumber",
                TargetRoleName = "Customer",
                GrantedBy = "admin@acmecoffee.com",
                GrantedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            });
        });

        // First render pass: 5 rows, 2 sensitive columns -> 10 distinct keys.
        for (var i = 1; i <= 5; i++)
        {
            await harness.Binder.ResolveAsync("Customer", "SocialSecurityNumber", entityId: i.ToString());
            await harness.Binder.ResolveAsync("Customer", "PhoneNumber", entityId: i.ToString());
        }
        harness.Auditor.UnmaskedReads.Should().HaveCount(10);

        // Second render pass without navigation: same correlation, same keys -> no new rows.
        for (var i = 1; i <= 5; i++)
        {
            await harness.Binder.ResolveAsync("Customer", "SocialSecurityNumber", entityId: i.ToString());
            await harness.Binder.ResolveAsync("Customer", "PhoneNumber", entityId: i.ToString());
        }
        harness.Auditor.UnmaskedReads.Should().HaveCount(10);
    }

    [Fact]
    public async Task ColumnWithoutDefaultMask_NoUnmaskedReadEmitted()
    {
        var harness = await Harness.BuildAsync(_ => { /* no default masks at all */ });

        await harness.Binder.ResolveAsync("Customer", "Name", entityId: "5");

        harness.Auditor.UnmaskedReads.Should().BeEmpty();
    }

    [Fact]
    public async Task MaskedColumnWithoutExemption_NoUnmaskedReadEmitted()
    {
        var harness = await Harness.BuildAsync(cfg =>
        {
            cfg.DefaultMasks.Add(new DefaultMaskRule
            {
                EntityKey = "Customer",
                ColumnKey = "SocialSecurityNumber",
                MaskFormat = "XXX-XX-{last4}",
            });
            // No MaskExemption -> resolution returns IsMasked=true -> no audit.
        });

        var resolution = await harness.Binder.ResolveAsync("Customer", "SocialSecurityNumber", entityId: "5");

        resolution.IsMasked.Should().BeTrue();
        harness.Auditor.UnmaskedReads.Should().BeEmpty();
    }

    [Fact]
    public async Task NullEntityId_NoUnmaskedReadEmitted_StillResolves()
    {
        var harness = await Harness.BuildAsync(WithExemptedConfig);

        var resolution = await harness.Binder.ResolveAsync("Customer", "SocialSecurityNumber");

        resolution.IsMasked.Should().BeFalse();
        harness.Auditor.UnmaskedReads.Should().BeEmpty(
            because: "without an entity instance the audit row would not be reconstructable");
    }

    [Fact]
    public async Task DifferentEntityInstances_EmitDistinctRows()
    {
        var harness = await Harness.BuildAsync(WithExemptedConfig);

        await harness.Binder.ResolveAsync("Customer", "SocialSecurityNumber", entityId: "1");
        await harness.Binder.ResolveAsync("Customer", "SocialSecurityNumber", entityId: "2");
        await harness.Binder.ResolveAsync("Customer", "SocialSecurityNumber", entityId: "1");
        await harness.Binder.ResolveAsync("Customer", "SocialSecurityNumber", entityId: "2");

        harness.Auditor.UnmaskedReads.Should().HaveCount(2);
        harness.Auditor.UnmaskedReads.Select(r => r.EntityId)
            .Should().BeEquivalentTo(new[] { "1", "2" });
    }

    private static void WithExemptedConfig(SecurityConfiguration cfg)
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
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
        });
    }

    private sealed class Harness
    {
        public required RestrictionBinder Binder { get; init; }
        public required RecordingAuditor Auditor { get; init; }
        public required IRenderCorrelationScope CorrelationScope { get; init; }

        public static async Task<Harness> BuildAsync(Action<SecurityConfiguration> populate)
        {
            var configuration = new SecurityConfiguration();
            populate(configuration);
            var manager = new SecurityConfigurationManager(new InMemorySecurityConfigurationStore(configuration));
            await manager.EnsureLoadedAsync();

            var auditor = new RecordingAuditor();
            var scope = new RenderCorrelationScope(new ServiceCollection().BuildServiceProvider());

            var binder = new RestrictionBinder(
                manager,
                new StaticAuthStateProvider(BuildPrincipal(userId: 11, role: "Customer")),
                new SessionRoleSwitcher(),
                new PlatformBakedInRoleProvider(),
                auditor,
                scope);

            return new Harness { Binder = binder, Auditor = auditor, CorrelationScope = scope };
        }
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
        public List<(string EntityKey, string EntityId, string ColumnKey)> UnmaskedReads { get; } = new();

        public Task AuditRoleSwitchAsync(ClaimsPrincipal user, string roleName, string? details = null, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task AuditExemptionGrantedAsync(ClaimsPrincipal user, string entityKey, string columnKey, string targetRoleName, int? granterUserId, DateTimeOffset? expiresAt, string? details = null, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task AuditExemptionExpiredAsync(ClaimsPrincipal user, string entityKey, string columnKey, string targetRoleName, DateTimeOffset expiresAt, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task AuditFieldUnmaskedReadAsync(ClaimsPrincipal user, string entityKey, string entityId, string columnKey, CancellationToken ct = default)
        {
            UnmaskedReads.Add((entityKey, entityId, columnKey));
            return Task.CompletedTask;
        }

        public Task AuditAsync(AppSensitiveAccessLog row, CancellationToken ct = default) => Task.CompletedTask;
    }
}
