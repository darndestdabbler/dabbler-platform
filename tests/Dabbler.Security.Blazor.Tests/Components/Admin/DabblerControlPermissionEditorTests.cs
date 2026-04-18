using Bunit;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Blazor.Components.Admin;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Configuration;
using Dabbler.Security.Registration;
using Dabbler.Security.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Dabbler.Security.Blazor.Tests.Components.Admin;

public class DabblerControlPermissionEditorTests
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

    private static ResourceRegistry BuildRegistry(string pageKey, params string[] controlKeys) =>
        new ResourceRegistry(
            [new RegisteredPage { Key = pageKey, DisplayName = pageKey }],
            [], [],
            controlKeys.Select(k => new RegisteredControl { Key = k, PageKey = pageKey, DisplayName = k }).ToList());

    [Fact]
    public void NoControls_ShowsHelpMessage()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles = [new RoleDefinition { RoleName = "Admin" }]
        };
        var (manager, _) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(ResourceRegistry.Empty);
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerControlPermissionEditor>(p =>
            p.Add(c => c.PageKey, "dashboard"));

        cut.Markup.Should().Contain("No controls registered");
    }

    [Fact]
    public void Renders_ControlsAndRoles()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles =
            [
                new RoleDefinition { RoleName = "Admin" },
                new RoleDefinition { RoleName = "Viewer" }
            ]
        };
        var (manager, _) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(BuildRegistry("dashboard", "export-btn", "delete-btn"));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerControlPermissionEditor>(p =>
            p.Add(c => c.PageKey, "dashboard"));

        cut.Markup.Should().Contain("Admin");
        cut.Markup.Should().Contain("Viewer");
        cut.Markup.Should().Contain("export-btn");
        cut.Markup.Should().Contain("delete-btn");
    }

    [Fact]
    public void ExistingControlPermissions_ShowCorrectState()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles = [new RoleDefinition { RoleName = "Viewer" }],
            ControlPermissions =
            [
                new ControlPermission
                {
                    PageKey = "orders",
                    ControlKey = "delete-btn",
                    RolePermissions =
                    [
                        new ControlRolePermission { RoleName = "Viewer", State = ControlState.Hidden }
                    ]
                }
            ]
        };
        var (manager, _) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(BuildRegistry("orders", "delete-btn"));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerControlPermissionEditor>(p =>
            p.Add(c => c.PageKey, "orders"));

        cut.Markup.Should().Contain("Hidden");
    }

    [Fact]
    public void StateDropdown_RendersIconAndText_ForEachControlState()
    {
        // Three roles seeded with one of each ControlState so every state's
        // selected-display template renders in the same CUT markup.
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles =
            [
                new RoleDefinition { RoleName = "RoleEnabled" },
                new RoleDefinition { RoleName = "RoleDisabled" },
                new RoleDefinition { RoleName = "RoleHidden" }
            ],
            ControlPermissions =
            [
                new ControlPermission
                {
                    PageKey = "orders",
                    ControlKey = "delete-btn",
                    RolePermissions =
                    [
                        new ControlRolePermission { RoleName = "RoleEnabled",  State = ControlState.Enabled },
                        new ControlRolePermission { RoleName = "RoleDisabled", State = ControlState.Disabled },
                        new ControlRolePermission { RoleName = "RoleHidden",   State = ControlState.Hidden }
                    ]
                }
            ]
        };
        var (manager, _) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(BuildRegistry("orders", "delete-btn"));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerControlPermissionEditor>(p =>
            p.Add(c => c.PageKey, "orders"));

        cut.Markup.Should().Contain("_content/Dabbler.Security.Blazor/enabled.svg");
        cut.Markup.Should().Contain("_content/Dabbler.Security.Blazor/disabled.svg");
        cut.Markup.Should().Contain("_content/Dabbler.Security.Blazor/hidden.svg");
        cut.Markup.Should().Contain("Enabled");
        cut.Markup.Should().Contain("Disabled");
        cut.Markup.Should().Contain("Hidden");

        // Icons are decorative; screen readers read the text label only.
        var iconImgs = cut.FindAll("img[src*='_content/Dabbler.Security.Blazor/']");
        iconImgs.Should().NotBeEmpty();
        foreach (var img in iconImgs)
        {
            img.GetAttribute("aria-hidden").Should().Be("true");
            img.GetAttribute("alt").Should().Be(string.Empty);
        }
    }

    [Fact]
    public async Task SaveButton_PersistsControlPermissions()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles = [new RoleDefinition { RoleName = "Admin" }],
            ControlPermissions =
            [
                new ControlPermission
                {
                    PageKey = "settings",
                    ControlKey = "reset-btn",
                    RolePermissions =
                    [
                        new ControlRolePermission { RoleName = "Admin", State = ControlState.Enabled }
                    ]
                }
            ]
        };
        var (manager, store) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(BuildRegistry("settings", "reset-btn"));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerControlPermissionEditor>(p =>
            p.Add(c => c.PageKey, "settings"));

        var saveButton = cut.Find("button.mud-button-filled");
        await cut.InvokeAsync(() => saveButton.Click());

        store.Verify(s => s.SaveAsync(
            It.Is<SecurityConfiguration>(c => c.ControlPermissions.Any(cp =>
                cp.PageKey == "settings" && cp.ControlKey == "reset-btn" &&
                cp.RolePermissions.Any(r => r.RoleName == "Admin" && r.State == ControlState.Enabled))),
            default), Times.Once);
    }
}
