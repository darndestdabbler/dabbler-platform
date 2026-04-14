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

public class DabblerEndpointPermissionMatrixTests
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

    private static ResourceRegistry BuildRegistry(params (string key, string display, string? method)[] endpoints) =>
        new ResourceRegistry(
            [], [],
            endpoints.Select(e => new RegisteredEndpoint { Key = e.key, DisplayName = e.display, HttpMethod = e.method }).ToList(),
            []);

    [Fact]
    public void Renders_RolesAndEndpoints()
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
                svc.AddSingleton(BuildRegistry(
                    ("GET /api/orders", "List Orders", "GET"),
                    ("POST /api/orders", "Create Order", "POST")));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerEndpointPermissionMatrix>();

        cut.Markup.Should().Contain("Admin");
        cut.Markup.Should().Contain("Viewer");
        cut.Markup.Should().Contain("List Orders");
        cut.Markup.Should().Contain("Create Order");
    }

    [Fact]
    public void NoEndpoints_ShowsHelpMessage()
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

        var cut = ctx.RenderComponent<DabblerEndpointPermissionMatrix>();

        cut.Markup.Should().Contain("No endpoints registered");
    }

    [Fact]
    public void ExistingAllowedEndpoints_CheckedOnRender()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles = [new RoleDefinition { RoleName = "Admin" }],
            EndpointPermissions =
            [
                new EndpointPermission
                {
                    EndpointKey = "GET /api/orders",
                    AllowedRoles = ["Admin"]
                }
            ]
        };
        var (manager, _) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(BuildRegistry(("GET /api/orders", "List Orders", "GET")));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerEndpointPermissionMatrix>();

        var checkboxes = cut.FindAll("input[type='checkbox']");
        checkboxes.Should().HaveCountGreaterOrEqualTo(1);
        checkboxes[0].GetAttribute("checked").Should().NotBeNull();
    }

    [Fact]
    public async Task SaveButton_PersistsEndpointPermissions()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles = [new RoleDefinition { RoleName = "Manager" }],
            EndpointPermissions =
            [
                new EndpointPermission
                {
                    EndpointKey = "POST /api/invoices",
                    AllowedRoles = ["Manager"]
                }
            ]
        };
        var (manager, store) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(BuildRegistry(("POST /api/invoices", "Create Invoice", "POST")));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerEndpointPermissionMatrix>();

        var saveButton = cut.Find("button.mud-button-filled");
        await cut.InvokeAsync(() => saveButton.Click());

        store.Verify(s => s.SaveAsync(
            It.Is<SecurityConfiguration>(c => c.EndpointPermissions.Any(e =>
                e.EndpointKey == "POST /api/invoices" && e.AllowedRoles.Contains("Manager"))),
            default), Times.Once);
    }
}
