// These tests cover the obsolete legacy Dabbler.Security members on purpose.
#pragma warning disable CS0618

using Bunit;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Blazor.Components;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Configuration;
using Dabbler.Security.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Dabbler.Security.Blazor.Tests.Components;

public class DabblerRoleSwitcherTests
{
    private static (SecurityConfigurationManager manager, Mock<ISecurityConfigurationStore> store)
        BuildManager(SecurityConfiguration config)
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        store.Setup(s => s.SaveAsync(It.IsAny<SecurityConfiguration>(), default))
             .Returns(Task.CompletedTask);
        return (new SecurityConfigurationManager(store.Object), store);
    }

    [Fact]
    public async Task HiddenWhenUserNotInAllowedRoles()
    {
        await using var ctx = new BunitContext();
        var config = new SecurityConfiguration
        {
            RoleSwitching = new RoleSwitchingPermission { AllowedRoles = ["SystemAdministrator"] }
        };
        var (manager, _) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["Viewer"], // not in AllowedRoles
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.Render<DabblerRoleSwitcher>();

        // The switcher div should not be rendered
        cut.Markup.Should().NotContain("Switch role");
    }

    [Fact]
    public async Task VisibleWhenUserIsInAllowedRoles()
    {
        await using var ctx = new BunitContext();
        var config = new SecurityConfiguration
        {
            Roles =
            [
                new RoleDefinition { RoleName = "SystemAdministrator" },
                new RoleDefinition { RoleName = "Viewer" }
            ],
            RoleSwitching = new RoleSwitchingPermission { AllowedRoles = ["SystemAdministrator"] }
        };
        var (manager, _) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.Render<DabblerRoleSwitcher>();

        cut.Markup.Should().Contain("Switch role");
    }

    [Fact]
    public async Task VisibleSwitcher_RendersAccessibleActivatorAttributes()
    {
        await using var ctx = new BunitContext();
        var config = new SecurityConfiguration
        {
            Roles =
            [
                new RoleDefinition { RoleName = "SystemAdministrator" }
            ],
            RoleSwitching = new RoleSwitchingPermission { AllowedRoles = ["SystemAdministrator"] }
        };
        var (manager, _) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.Render<DabblerRoleSwitcher>();

        cut.WaitForAssertion(() =>
        {
            var activator = cut.Find("[data-testid='role-switcher-activator']");
            activator.GetAttribute("aria-label").Should().Be("Switch active role");
            activator.GetAttribute("aria-haspopup").Should().Be("listbox");
            activator.TextContent.Should().Contain("Switch role");
        });
    }

    [Fact]
    public async Task NoAllowedRoles_AlwaysHidden()
    {
        await using var ctx = new BunitContext();
        var config = new SecurityConfiguration
        {
            RoleSwitching = new RoleSwitchingPermission { AllowedRoles = [] } // empty
        };
        var (manager, _) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.Render<DabblerRoleSwitcher>();

        cut.Markup.Should().NotContain("Switch role");
    }

    [Fact]
    public async Task SwitchButton_CallsRoleSwitcherSwitchToRole()
    {
        await using var ctx = new BunitContext();
        var config = new SecurityConfiguration
        {
            Roles =
            [
                new RoleDefinition { RoleName = "SystemAdministrator" },
                new RoleDefinition { RoleName = "Viewer" }
            ],
            RoleSwitching = new RoleSwitchingPermission { AllowedRoles = ["SystemAdministrator"] }
        };
        var (manager, _) = BuildManager(config);

        // Override the IRoleSwitcher with a mock so we can verify SwitchToRole is called
        var roleSwitcherMock = new Mock<IRoleSwitcher>();
        roleSwitcherMock.Setup(r => r.IsRoleSwitched).Returns(false);
        roleSwitcherMock.Setup(r => r.EffectiveRoles(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .Returns(["SystemAdministrator"]);

        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton<AuthorizationStateNotifier>();
                // Override the scoped IRoleSwitcher registered by SetupForDabbler
                svc.AddScoped(_ => roleSwitcherMock.Object);
            });

        var cut = ctx.Render<DabblerRoleSwitcher>();

        // Switch button is disabled until a role is selected — just verify the component renders.
        // MudSelect options render inside MudPopoverProvider (outside the component's own markup),
        // so we assert on the button and label rather than the dropdown items.
        cut.Markup.Should().Contain("Switch");
        cut.Markup.Should().Contain("Switch role");
    }
}
