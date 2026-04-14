using System.Security.Claims;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Authorization.Handlers;
using Dabbler.Security.Authorization.Requirements;
using Dabbler.Security.Configuration;
using Dabbler.Security.Options;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Authorization;

namespace Dabbler.Security.Tests.Authorization;

public class ControlAuthorizationHandlerTests
{
    private static ControlAuthorizationHandler CreateHandler(
        SecurityConfiguration config,
        UnregisteredResourcePolicy policy = UnregisteredResourcePolicy.FailOpen,
        IRoleSwitcher? roleSwitcher = null)
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        var manager = new SecurityConfigurationManager(store.Object);
        var options = Microsoft.Extensions.Options.Options.Create(new DabblerSecurityOptions { UnregisteredResourcePolicy = policy });
        return new ControlAuthorizationHandler(manager, options, roleSwitcher ?? new SessionRoleSwitcher());
    }

    private static ClaimsPrincipal UserWithRoles(params string[] roles)
    {
        var claims = roles.Select(r => new Claim(ClaimTypes.Role, r))
            .Append(new Claim(ClaimTypes.NameIdentifier, "1"))
            .ToArray();
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static AuthorizationHandlerContext CreateContext(ClaimsPrincipal user, ControlPermissionRequirement req)
        => new([req], user, null);

    [Fact]
    public async Task EnabledControl_Succeeds()
    {
        var config = new SecurityConfiguration
        {
            ControlPermissions =
            [
                new ControlPermission
                {
                    PageKey = "dashboard", ControlKey = "export-btn",
                    RolePermissions = [new ControlRolePermission { RoleName = "Admin", State = ControlState.Enabled }]
                }
            ]
        };
        var handler = CreateHandler(config);
        var context = CreateContext(UserWithRoles("Admin"), new ControlPermissionRequirement("dashboard", "export-btn"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task DisabledControl_Succeeds()
    {
        // Disabled means visible but inactive — authorization still succeeds (hidden would not)
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
        var handler = CreateHandler(config);
        var context = CreateContext(UserWithRoles("Viewer"), new ControlPermissionRequirement("orders", "delete-btn"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HiddenControl_DoesNotSucceed()
    {
        var config = new SecurityConfiguration
        {
            ControlPermissions =
            [
                new ControlPermission
                {
                    PageKey = "admin", ControlKey = "nuke-btn",
                    RolePermissions = [new ControlRolePermission { RoleName = "User", State = ControlState.Hidden }]
                }
            ]
        };
        var handler = CreateHandler(config);
        var context = CreateContext(UserWithRoles("User"), new ControlPermissionRequirement("admin", "nuke-btn"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task UnregisteredControl_FailOpen_Succeeds()
    {
        var config = new SecurityConfiguration();
        var handler = CreateHandler(config, UnregisteredResourcePolicy.FailOpen);
        var context = CreateContext(UserWithRoles("Admin"), new ControlPermissionRequirement("page", "unknown-ctrl"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task SwitchedRole_OverridesClaimsRoles()
    {
        var config = new SecurityConfiguration
        {
            ControlPermissions =
            [
                new ControlPermission
                {
                    PageKey = "dashboard", ControlKey = "export-btn",
                    RolePermissions = [new ControlRolePermission { RoleName = "Viewer", State = ControlState.Enabled }]
                }
            ]
        };
        var switcher = new SessionRoleSwitcher();
        switcher.SwitchToRole("Viewer");

        var handler = CreateHandler(config, roleSwitcher: switcher);
        var context = CreateContext(UserWithRoles("Admin"), new ControlPermissionRequirement("dashboard", "export-btn"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }
}
