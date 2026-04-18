using System.Security.Claims;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Authorization.Handlers;
using Dabbler.Security.Authorization.Requirements;
using Dabbler.Security.Configuration;
using Dabbler.Security.Options;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Authorization;

namespace Dabbler.Security.Tests.Authorization;

public class EndpointAuthorizationHandlerTests
{
    private static EndpointAuthorizationHandler CreateHandler(
        SecurityConfiguration config,
        UnregisteredResourcePolicy policy = UnregisteredResourcePolicy.FailOpen,
        IRoleSwitcher? roleSwitcher = null)
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        var manager = new SecurityConfigurationManager(store.Object);
        var options = Microsoft.Extensions.Options.Options.Create(new DabblerSecurityOptions { UnregisteredResourcePolicy = policy });
        return new EndpointAuthorizationHandler(manager, options, roleSwitcher ?? new SessionRoleSwitcher());
    }

    private static ClaimsPrincipal UserWithRoles(params string[] roles)
    {
        var claims = roles.Select(r => new Claim(ClaimTypes.Role, r))
            .Append(new Claim(ClaimTypes.NameIdentifier, "1"))
            .ToArray();
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static AuthorizationHandlerContext CreateContext(ClaimsPrincipal user, EndpointPermissionRequirement req)
        => new([req], user, null);

    [Fact]
    public async Task AllowedRole_Succeeds()
    {
        var config = new SecurityConfiguration
        {
            EndpointPermissions = [new EndpointPermission { EndpointKey = "POST /api/orders", AllowedRoles = ["Manager"] }]
        };
        var handler = CreateHandler(config);
        var context = CreateContext(UserWithRoles("Manager"), new EndpointPermissionRequirement("POST /api/orders"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task RoleNotInAllowList_DoesNotSucceed()
    {
        var config = new SecurityConfiguration
        {
            EndpointPermissions = [new EndpointPermission { EndpointKey = "DELETE /api/users", AllowedRoles = ["Admin"] }]
        };
        var handler = CreateHandler(config);
        var context = CreateContext(UserWithRoles("Viewer"), new EndpointPermissionRequirement("DELETE /api/users"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task UnregisteredEndpoint_FailOpen_Succeeds()
    {
        var config = new SecurityConfiguration();
        var handler = CreateHandler(config, UnregisteredResourcePolicy.FailOpen);
        var context = CreateContext(UserWithRoles("Admin"), new EndpointPermissionRequirement("GET /api/unknown"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task UnregisteredEndpoint_FailClosed_DoesNotSucceed()
    {
        var config = new SecurityConfiguration();
        var handler = CreateHandler(config, UnregisteredResourcePolicy.FailClosed);
        var context = CreateContext(UserWithRoles("Admin"), new EndpointPermissionRequirement("GET /api/unknown"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task DeniedRole_DoesNotSucceed_EvenWhenSameUserHasAnAllowedRole()
    {
        var config = new SecurityConfiguration
        {
            EndpointPermissions =
            [
                new EndpointPermission
                {
                    EndpointKey = "POST /api/orders",
                    AllowedRoles = ["Manager"],
                    DeniedRoles = ["Contractor"]
                }
            ]
        };
        var handler = CreateHandler(config);
        var context = CreateContext(
            UserWithRoles("Manager", "Contractor"),
            new EndpointPermissionRequirement("POST /api/orders"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task SwitchedRole_OverridesClaimsRoles()
    {
        var config = new SecurityConfiguration
        {
            EndpointPermissions = [new EndpointPermission { EndpointKey = "GET /api/reports", AllowedRoles = ["Viewer"] }]
        };
        var switcher = new SessionRoleSwitcher();
        switcher.SwitchToRole("Viewer");

        var handler = CreateHandler(config, roleSwitcher: switcher);
        var context = CreateContext(UserWithRoles("Admin"), new EndpointPermissionRequirement("GET /api/reports"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }
}
