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
