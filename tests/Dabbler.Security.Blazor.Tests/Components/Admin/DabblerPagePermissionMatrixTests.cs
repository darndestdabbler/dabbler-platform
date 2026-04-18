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
    public void ExistingAllowedPermissions_RenderSwitchInAllowState()
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

        // Single role × single page = exactly one switch, which should load in the Allow state.
        var toggle = cut.FindComponent<DenyAllowSwitch>();
        toggle.Instance.Value.Should().Be(PermissionState.Allow);
    }

    [Fact]
    public void ExistingDeniedPermissions_RenderSwitchInDenyState()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles = [new RoleDefinition { RoleName = "Admin" }],
            PagePermissions =
            [
                new PagePermission { PageKey = "dashboard", DeniedRoles = ["Admin"] }
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
                svc.AddSingleton(BuildRegistry("dashboard"));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerPagePermissionMatrix>();

        cut.Markup.Should().Contain("_content/Dabbler.Security.Blazor/deny-allow-header.svg");
        // Old header text columns should be gone (the switch shows them inline instead).
        var headerCells = cut.FindAll("th").Select(th => th.TextContent.Trim()).ToList();
        headerCells.Should().NotContain("Allow");
        headerCells.Should().NotContain("Deny");
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

    /// <summary>
    /// The mutual-exclusion invariant. Flipping a cell from Allow to Deny must drop the
    /// role from AllowedRoles AND add it to DeniedRoles in the saved configuration. The
    /// two states cannot co-exist for the same role on the same page.
    /// </summary>
    [Fact]
    public async Task FlipAllowToDeny_DropsAllow_AndAddsDeny_OnSave()
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
        var (manager, store) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(BuildRegistry("dashboard"));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerPagePermissionMatrix>();

        // Precondition: the single switch loads as Allow.
        var toggle = cut.FindComponent<DenyAllowSwitch>();
        toggle.Instance.Value.Should().Be(PermissionState.Allow);

        // Flip Allow -> Deny through the switch's own ValueChanged callback (the same
        // path the MudToggleGroup fires when the user picks a different option).
        await cut.InvokeAsync(() =>
            toggle.Instance.ValueChanged.InvokeAsync(PermissionState.Deny));

        // The re-rendered switch now reflects Deny.
        toggle = cut.FindComponent<DenyAllowSwitch>();
        toggle.Instance.Value.Should().Be(PermissionState.Deny);

        // Save and verify the stored config has Admin in DeniedRoles and NOT AllowedRoles.
        var saveButton = cut.Find("button.mud-button-filled");
        await cut.InvokeAsync(() => saveButton.Click());

        store.Verify(s => s.SaveAsync(
            It.Is<SecurityConfiguration>(c => c.PagePermissions.Any(p =>
                p.PageKey == "dashboard"
                && p.DeniedRoles.Contains("Admin")
                && !p.AllowedRoles.Contains("Admin"))),
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
            PagePermissions =
            [
                new PagePermission { PageKey = "dashboard", DeniedRoles = ["Admin"] }
            ]
        };
        var (manager, store) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(BuildRegistry("dashboard"));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerPagePermissionMatrix>();

        var toggle = cut.FindComponent<DenyAllowSwitch>();
        toggle.Instance.Value.Should().Be(PermissionState.Deny);

        await cut.InvokeAsync(() =>
            toggle.Instance.ValueChanged.InvokeAsync(PermissionState.Allow));

        var saveButton = cut.Find("button.mud-button-filled");
        await cut.InvokeAsync(() => saveButton.Click());

        store.Verify(s => s.SaveAsync(
            It.Is<SecurityConfiguration>(c => c.PagePermissions.Any(p =>
                p.PageKey == "dashboard"
                && p.AllowedRoles.Contains("Admin")
                && !p.DeniedRoles.Contains("Admin"))),
            default), Times.Once);
    }

    /// <summary>
    /// Unset = role absent from both AllowedRoles and DeniedRoles. Saving with an Unset
    /// cell must not put the role into either list.
    /// </summary>
    [Fact]
    public async Task SetToUnset_OmitsRoleFromBothLists_OnSave()
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
        var (manager, store) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(BuildRegistry("dashboard"));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerPagePermissionMatrix>();

        var toggle = cut.FindComponent<DenyAllowSwitch>();
        await cut.InvokeAsync(() =>
            toggle.Instance.ValueChanged.InvokeAsync(PermissionState.Unset));

        var saveButton = cut.Find("button.mud-button-filled");
        await cut.InvokeAsync(() => saveButton.Click());

        store.Verify(s => s.SaveAsync(
            It.Is<SecurityConfiguration>(c => c.PagePermissions.Any(p =>
                p.PageKey == "dashboard"
                && !p.AllowedRoles.Contains("Admin")
                && !p.DeniedRoles.Contains("Admin"))),
            default), Times.Once);
    }
}
