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

public class DabblerPagePermissionMatrixTests
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

    private static ResourceRegistry BuildRegistry(params string[] pageKeys) =>
        new ResourceRegistry(
            pageKeys.Select(k => new RegisteredPage { Key = k, DisplayName = k }).ToList(),
            [], [], []);

    [Fact]
    public void Renders_RolesAndPages()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles = [new RoleDefinition { RoleName = "Admin" }, new RoleDefinition { RoleName = "Viewer" }]
        };
        var (manager, _) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(BuildRegistry("dashboard", "reports"));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerPagePermissionMatrix>();

        cut.Markup.Should().Contain("Admin");
        cut.Markup.Should().Contain("Viewer");
        cut.Markup.Should().Contain("dashboard");
        cut.Markup.Should().Contain("reports");
    }

    [Fact]
    public void NoRoles_ShowsHelpMessage()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration();
        var (manager, _) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(BuildRegistry("dashboard"));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerPagePermissionMatrix>();

        cut.Markup.Should().Contain("No roles defined");
    }

    [Fact]
    public void NoPages_ShowsHelpMessage()
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

        var cut = ctx.RenderComponent<DabblerPagePermissionMatrix>();

        cut.Markup.Should().Contain("No pages registered");
    }

    [Fact]
    public void ExistingAllowedPermissions_CheckedOnRender()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles = [new RoleDefinition { RoleName = "Admin" }],
            PagePermissions =
            [
                new PagePermission { PageKey = "dashboard", AllowedRoles = ["Admin"] }
            ]
        };
        var (manager, _) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(BuildRegistry("dashboard"));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerPagePermissionMatrix>();

        // The "Allow" checkbox for Admin/dashboard should be checked
        var checkboxes = cut.FindAll("input[type='checkbox']");
        checkboxes.Should().NotBeEmpty();
        // First checkbox = Allow for Admin/dashboard — should be checked
        checkboxes[0].GetAttribute("checked").Should().NotBeNull();
    }

    [Fact]
    public async Task SaveButton_CallsStoreWithRebuildPermissions()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles = [new RoleDefinition { RoleName = "Admin" }],
            PagePermissions =
            [
                new PagePermission { PageKey = "reports", AllowedRoles = ["Admin"] }
            ]
        };
        var (manager, store) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(BuildRegistry("reports"));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerPagePermissionMatrix>();

        var saveButton = cut.Find("button.mud-button-filled");
        await cut.InvokeAsync(() => saveButton.Click());

        store.Verify(s => s.SaveAsync(
            It.Is<SecurityConfiguration>(c => c.PagePermissions.Any(p =>
                p.PageKey == "reports" && p.AllowedRoles.Contains("Admin"))),
            default), Times.Once);
    }
}
