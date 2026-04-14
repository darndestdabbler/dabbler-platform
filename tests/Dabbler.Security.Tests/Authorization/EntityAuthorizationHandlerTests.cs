using System.Security.Claims;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Authorization.Handlers;
using Dabbler.Security.Authorization.Requirements;
using Dabbler.Security.Configuration;
using Dabbler.Security.Options;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Authorization;

namespace Dabbler.Security.Tests.Authorization;

public class EntityAuthorizationHandlerTests
{
    private static EntityAuthorizationHandler CreateHandler(
        SecurityConfiguration config,
        UnregisteredResourcePolicy policy = UnregisteredResourcePolicy.FailOpen,
        IRoleSwitcher? roleSwitcher = null)
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        var manager = new SecurityConfigurationManager(store.Object);
        var options = Microsoft.Extensions.Options.Options.Create(new DabblerSecurityOptions { UnregisteredResourcePolicy = policy });
        return new EntityAuthorizationHandler(manager, options, roleSwitcher ?? new SessionRoleSwitcher());
    }

    private static ClaimsPrincipal UserWithRoles(params string[] roles)
    {
        var claims = roles.Select(r => new Claim(ClaimTypes.Role, r))
            .Append(new Claim(ClaimTypes.NameIdentifier, "1"))
            .ToArray();
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static AuthorizationHandlerContext CreateContext(ClaimsPrincipal user, EntityPermissionRequirement req)
        => new([req], user, null);

    [Fact]
    public async Task RoleHasOperation_Succeeds()
    {
        var config = new SecurityConfiguration
        {
            EntityPermissions =
            [
                new EntityPermission
                {
                    EntityKey = "Invoice",
                    RolePermissions =
                    [
                        new EntityRolePermission { RoleName = "Accountant", AllowedOperations = ["Read", "Create"] }
                    ]
                }
            ]
        };
        var handler = CreateHandler(config);
        var context = CreateContext(UserWithRoles("Accountant"), new EntityPermissionRequirement("Invoice", "Read"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task RoleLacksOperation_DoesNotSucceed()
    {
        var config = new SecurityConfiguration
        {
            EntityPermissions =
            [
                new EntityPermission
                {
                    EntityKey = "Invoice",
                    RolePermissions =
                    [
                        new EntityRolePermission { RoleName = "Viewer", AllowedOperations = ["Read"] }
                    ]
                }
            ]
        };
        var handler = CreateHandler(config);
        var context = CreateContext(UserWithRoles("Viewer"), new EntityPermissionRequirement("Invoice", "Delete"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task UnregisteredEntity_FailOpen_Succeeds()
    {
        var config = new SecurityConfiguration();
        var handler = CreateHandler(config, UnregisteredResourcePolicy.FailOpen);
        var context = CreateContext(UserWithRoles("Admin"), new EntityPermissionRequirement("Unknown", "Read"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task UnauthenticatedUser_DoesNotSucceed()
    {
        var config = new SecurityConfiguration
        {
            EntityPermissions =
            [
                new EntityPermission
                {
                    EntityKey = "Order",
                    RolePermissions = [new EntityRolePermission { RoleName = "Admin", AllowedOperations = ["Read"] }]
                }
            ]
        };
        var handler = CreateHandler(config);
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        var context = CreateContext(anonymous, new EntityPermissionRequirement("Order", "Read"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task SwitchedRole_OverridesClaimsRoles()
    {
        var config = new SecurityConfiguration
        {
            EntityPermissions =
            [
                new EntityPermission
                {
                    EntityKey = "Invoice",
                    RolePermissions = [new EntityRolePermission { RoleName = "Viewer", AllowedOperations = ["Read"] }]
                }
            ]
        };
        var switcher = new SessionRoleSwitcher();
        switcher.SwitchToRole("Viewer");

        var handler = CreateHandler(config, roleSwitcher: switcher);
        var context = CreateContext(UserWithRoles("Admin"), new EntityPermissionRequirement("Invoice", "Read"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }
}
