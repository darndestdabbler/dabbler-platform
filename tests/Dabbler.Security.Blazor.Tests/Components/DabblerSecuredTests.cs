using Bunit;
using Bunit.TestDoubles;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Blazor.Components;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Configuration;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Dabbler.Security.Blazor.Tests.Components;

public class DabblerSecuredTests
{
    private static SecurityConfigurationManager BuildManager(SecurityConfiguration config)
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        var manager = new SecurityConfigurationManager(store.Object);
        return manager;
    }

    [Fact]
    public void HiddenState_RendersNothing()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            ControlPermissions =
            [
                new ControlPermission
                {
                    PageKey = "dashboard", ControlKey = "secret-btn",
                    RolePermissions = [new ControlRolePermission { RoleName = "User", State = ControlState.Hidden }]
                }
            ]
        };
        ctx.SetupForDabbler(
            roles: ["User"],
            configureServices: svc => svc.AddSingleton(BuildManager(config)));

        var cut = ctx.RenderComponent<DabblerSecured>(p => p
            .Add(c => c.PageKey, "dashboard")
            .Add(c => c.ControlKey, "secret-btn")
            .Add(c => c.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<span>Secret</span>"))));

        cut.Markup.Should().NotContain("Secret");
    }

    [Fact]
    public void DisabledState_RendersWithDisabledWrapper()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            ControlPermissions =
            [
                new ControlPermission
                {
                    PageKey = "orders", ControlKey = "delete-btn",
                    RolePermissions = [new ControlRolePermission { RoleName = "Viewer", State = ControlState.Disabled }]
                }
            ]
        };
        ctx.SetupForDabbler(
            roles: ["Viewer"],
            configureServices: svc => svc.AddSingleton(BuildManager(config)));

        var cut = ctx.RenderComponent<DabblerSecured>(p => p
            .Add(c => c.PageKey, "orders")
            .Add(c => c.ControlKey, "delete-btn")
            .Add(c => c.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<button>Delete</button>"))));

        cut.Markup.Should().Contain("Delete");
        cut.Markup.Should().Contain("disabled");
    }

    [Fact]
    public void EnabledState_RendersNormally()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            ControlPermissions =
            [
                new ControlPermission
                {
                    PageKey = "admin", ControlKey = "create-btn",
                    RolePermissions = [new ControlRolePermission { RoleName = "Admin", State = ControlState.Enabled }]
                }
            ]
        };
        ctx.SetupForDabbler(
            roles: ["Admin"],
            configureServices: svc => svc.AddSingleton(BuildManager(config)));

        var cut = ctx.RenderComponent<DabblerSecured>(p => p
            .Add(c => c.PageKey, "admin")
            .Add(c => c.ControlKey, "create-btn")
            .Add(c => c.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<button>Create</button>"))));

        cut.Markup.Should().Contain("Create");
        cut.Markup.Should().NotContain("disabled");
    }

    [Fact]
    public void UnauthenticatedUser_RendersNothing()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            ControlPermissions =
            [
                new ControlPermission
                {
                    PageKey = "page", ControlKey = "btn",
                    RolePermissions = [new ControlRolePermission { RoleName = "Admin", State = ControlState.Enabled }]
                }
            ]
        };

        // No roles set — unauthenticated
        ctx.SetupForDabbler(
            configureServices: svc => svc.AddSingleton(BuildManager(config)));

        var cut = ctx.RenderComponent<DabblerSecured>(p => p
            .Add(c => c.PageKey, "page")
            .Add(c => c.ControlKey, "btn")
            .Add(c => c.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<span>Secured</span>"))));

        cut.Markup.Should().NotContain("Secured");
    }
}
