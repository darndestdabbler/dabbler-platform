using Bunit;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Authorization;
using Dabbler.Security.Blazor.Components.Admin.Role;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Configuration;
using Dabbler.Security.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Dabbler.Security.Blazor.Tests.Components.Admin.Role;

public class MaskExemptionsSubTabTests
{
    private static void RegisterServices(IServiceCollection svc, SecurityConfiguration config)
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        store.Setup(s => s.SaveAsync(It.IsAny<SecurityConfiguration>(), default))
             .Returns(Task.CompletedTask);
        var manager = new SecurityConfigurationManager(store.Object);
        svc.AddSingleton(manager);
        svc.AddSingleton<AuthorizationStateNotifier>();
        svc.AddSingleton<IBakedInRoleProvider, PlatformBakedInRoleProvider>();
    }

    [Fact]
    public async Task NoDefaultMasks_ShowsGuidanceAlert()
    {
        var config = new SecurityConfiguration(); // no DefaultMasks

        await using var ctx = new BunitContext();
        ctx.SetupForDabbler(
            roles: [BakedInRoles.SystemAdmin],
            configureServices: svc => RegisterServices(svc, config));

        var cut = ctx.Render<MaskExemptionsSubTab>(p => p
            .Add(c => c.TargetRoleName, "BackofficeUser"));

        cut.Find("[data-testid='mask-exemption-no-defaults']").Should().NotBeNull();
    }

    [Fact]
    public async Task ExistingExemptionForRole_RendersInGrid()
    {
        var config = new SecurityConfiguration
        {
            DefaultMasks =
            [
                new DefaultMaskRule { EntityKey = "Customer", ColumnKey = "SocialSecurityNumber", MaskFormat = "XXX-XX-{last4}" },
            ],
            MaskExemptions =
            [
                new MaskExemption
                {
                    EntityKey = "Customer",
                    ColumnKey = "SocialSecurityNumber",
                    TargetRoleName = "BackofficeUser",
                    GrantedBy = "sysadmin@test.local",
                    GrantedAt = DateTimeOffset.UtcNow.AddDays(-1),
                    ExpiresAt = null,
                },
            ],
        };

        await using var ctx = new BunitContext();
        ctx.SetupForDabbler(
            roles: [BakedInRoles.SystemAdmin],
            configureServices: svc => RegisterServices(svc, config));

        var cut = ctx.Render<MaskExemptionsSubTab>(p => p
            .Add(c => c.TargetRoleName, "BackofficeUser"));

        cut.Find("[data-testid='mask-exemption-grid']").Should().NotBeNull();
        cut.Find("[data-testid='mask-exemption-status-active-Customer-SocialSecurityNumber']")
            .Should().NotBeNull();
    }

    [Fact]
    public async Task ExpiredExemption_RendersWithExpiredChipAndRenewAction()
    {
        var config = new SecurityConfiguration
        {
            DefaultMasks =
            [
                new DefaultMaskRule { EntityKey = "Customer", ColumnKey = "SocialSecurityNumber", MaskFormat = "XXX-XX-{last4}" },
            ],
            MaskExemptions =
            [
                new MaskExemption
                {
                    EntityKey = "Customer",
                    ColumnKey = "SocialSecurityNumber",
                    TargetRoleName = "BackofficeUser",
                    GrantedBy = "sysadmin@test.local",
                    GrantedAt = DateTimeOffset.UtcNow.AddDays(-10),
                    ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
                },
            ],
        };

        await using var ctx = new BunitContext();
        ctx.SetupForDabbler(
            roles: [BakedInRoles.SystemAdmin],
            configureServices: svc => RegisterServices(svc, config));

        var cut = ctx.Render<MaskExemptionsSubTab>(p => p
            .Add(c => c.TargetRoleName, "BackofficeUser"));

        cut.Find("[data-testid='mask-exemption-status-expired-Customer-SocialSecurityNumber']")
            .Should().NotBeNull();
        cut.Find("[data-testid='mask-exemption-renew-Customer-SocialSecurityNumber']")
            .Should().NotBeNull();
    }

    [Fact]
    public async Task ExemptionsForDifferentRole_DoNotAppear()
    {
        var config = new SecurityConfiguration
        {
            DefaultMasks =
            [
                new DefaultMaskRule { EntityKey = "Customer", ColumnKey = "SocialSecurityNumber", MaskFormat = "XXX-XX-{last4}" },
            ],
            MaskExemptions =
            [
                new MaskExemption
                {
                    EntityKey = "Customer",
                    ColumnKey = "SocialSecurityNumber",
                    TargetRoleName = "Customer",   // different role
                    GrantedBy = "sysadmin@test.local",
                    GrantedAt = DateTimeOffset.UtcNow,
                },
            ],
        };

        await using var ctx = new BunitContext();
        ctx.SetupForDabbler(
            roles: [BakedInRoles.SystemAdmin],
            configureServices: svc => RegisterServices(svc, config));

        var cut = ctx.Render<MaskExemptionsSubTab>(p => p
            .Add(c => c.TargetRoleName, "BackofficeUser"));

        cut.Find("[data-testid='mask-exemption-empty']").Should().NotBeNull();
    }
}
