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
    public void ExistingAllowedEndpoints_RenderSwitchInAllowState()
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

        var toggle = cut.FindComponent<DenyAllowSwitch>();
        toggle.Instance.Value.Should().Be(PermissionState.Allow);
    }

    [Fact]
    public void ExistingDeniedEndpoints_RenderSwitchInDenyState()
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
                    DeniedRoles = ["Admin"]
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

        var toggle = cut.FindComponent<DenyAllowSwitch>();
        toggle.Instance.Value.Should().Be(PermissionState.Deny);
    }

    [Fact]
    public void RendersDenyAllowHeaderSvg_NotSeparateAllowAndDenyColumns()
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
                svc.AddSingleton(BuildRegistry(("GET /api/orders", "List Orders", "GET")));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerEndpointPermissionMatrix>();

        cut.Markup.Should().Contain("_content/Dabbler.Security.Blazor/deny-allow-header.svg");
        // Old header text columns should be gone (the switch shows them inline instead).
        var headerCells = cut.FindAll("th").Select(th => th.TextContent.Trim()).ToList();
        headerCells.Should().NotContain("Allow");
        headerCells.Should().NotContain("Deny");
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

    /// <summary>
    /// The mutual-exclusion invariant for endpoints. Flipping a cell from Allow to Deny must drop
    /// the role from AllowedRoles AND add it to DeniedRoles in the saved configuration.
    /// </summary>
    [Fact]
    public async Task FlipAllowToDeny_DropsAllow_AndAddsDeny_OnSave()
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
        var (manager, store) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(BuildRegistry(("GET /api/orders", "List Orders", "GET")));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerEndpointPermissionMatrix>();

        var toggle = cut.FindComponent<DenyAllowSwitch>();
        toggle.Instance.Value.Should().Be(PermissionState.Allow);

        await cut.InvokeAsync(() =>
            toggle.Instance.ValueChanged.InvokeAsync(PermissionState.Deny));

        toggle = cut.FindComponent<DenyAllowSwitch>();
        toggle.Instance.Value.Should().Be(PermissionState.Deny);

        var saveButton = cut.Find("button.mud-button-filled");
        await cut.InvokeAsync(() => saveButton.Click());

        store.Verify(s => s.SaveAsync(
            It.Is<SecurityConfiguration>(c => c.EndpointPermissions.Any(e =>
                e.EndpointKey == "GET /api/orders"
                && e.DeniedRoles.Contains("Admin")
                && !e.AllowedRoles.Contains("Admin"))),
            default), Times.Once);
    }

    /// <summary>
    /// Inverse invariant: Deny -> Allow drops Deny and adds Allow.
    /// </summary>
    [Fact]
    public async Task FlipDenyToAllow_DropsDeny_AndAddsAllow_OnSave()
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
                    DeniedRoles = ["Admin"]
                }
            ]
        };
        var (manager, store) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(BuildRegistry(("GET /api/orders", "List Orders", "GET")));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerEndpointPermissionMatrix>();

        var toggle = cut.FindComponent<DenyAllowSwitch>();
        toggle.Instance.Value.Should().Be(PermissionState.Deny);

        await cut.InvokeAsync(() =>
            toggle.Instance.ValueChanged.InvokeAsync(PermissionState.Allow));

        var saveButton = cut.Find("button.mud-button-filled");
        await cut.InvokeAsync(() => saveButton.Click());

        store.Verify(s => s.SaveAsync(
            It.Is<SecurityConfiguration>(c => c.EndpointPermissions.Any(e =>
                e.EndpointKey == "GET /api/orders"
                && e.AllowedRoles.Contains("Admin")
                && !e.DeniedRoles.Contains("Admin"))),
            default), Times.Once);
    }

    /// <summary>
    /// Session 6 invariant: when a role has no own endpoint entry but a parent role does,
    /// the cell renders with the "inherited" class and a tooltip that names the parent.
    /// </summary>
    [Fact]
    public void InheritedCell_RendersInheritedClass_AndTooltipNamesParentRole()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles =
            [
                new RoleDefinition { RoleName = "Admin" },
                new RoleDefinition { RoleName = "Manager", InheritsFrom = "Admin" }
            ],
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

        cut.FindAll("div.permission-cell.inherited").Should().HaveCount(1,
            "only Manager's cell is inherited; Admin's cell is explicit");

        var toggles = cut.FindComponents<DenyAllowSwitch>();
        toggles.Should().HaveCount(2);
        toggles.Select(t => t.Instance.Value).Should().AllBeEquivalentTo(PermissionState.Allow);

        var tooltips = cut.FindComponents<MudBlazor.MudTooltip>();
        tooltips.Select(t => t.Instance.Text).Should().Contain("inherited from Admin");
    }

    [Fact]
    public void InheritedLegend_IsRendered_WhenRolesAndEndpointsExist()
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
                svc.AddSingleton(BuildRegistry(("GET /api/orders", "List Orders", "GET")));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerEndpointPermissionMatrix>();

        cut.FindAll("[data-testid='inherited-legend']").Should().NotBeEmpty();
    }

    /// <summary>
    /// Unset = role absent from both AllowedRoles and DeniedRoles.
    /// </summary>
    [Fact]
    public async Task SetToUnset_OmitsRoleFromBothLists_OnSave()
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
        var (manager, store) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(BuildRegistry(("GET /api/orders", "List Orders", "GET")));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerEndpointPermissionMatrix>();

        var toggle = cut.FindComponent<DenyAllowSwitch>();
        await cut.InvokeAsync(() =>
            toggle.Instance.ValueChanged.InvokeAsync(PermissionState.Unset));

        var saveButton = cut.Find("button.mud-button-filled");
        await cut.InvokeAsync(() => saveButton.Click());

        store.Verify(s => s.SaveAsync(
            It.Is<SecurityConfiguration>(c => c.EndpointPermissions.Any(e =>
                e.EndpointKey == "GET /api/orders"
                && !e.AllowedRoles.Contains("Admin")
                && !e.DeniedRoles.Contains("Admin"))),
            default), Times.Once);
    }
}
