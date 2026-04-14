using System.Security.Claims;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Authorization.Handlers;
using Dabbler.Security.Authorization.Requirements;
using Dabbler.Security.Configuration;
using Dabbler.Security.Options;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Authorization;

namespace Dabbler.Security.Tests.Authorization;

public class PageAuthorizationHandlerTests
{
    private static PageAuthorizationHandler CreateHandler(
        SecurityConfiguration config,
        UnregisteredResourcePolicy policy = UnregisteredResourcePolicy.FailOpen,
        IRoleSwitcher? roleSwitcher = null)
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        var manager = new SecurityConfigurationManager(store.Object);

        var options = Microsoft.Extensions.Options.Options.Create(new DabblerSecurityOptions { UnregisteredResourcePolicy = policy });
        return new PageAuthorizationHandler(manager, options, roleSwitcher ?? new SessionRoleSwitcher());
    }

    private static ClaimsPrincipal UserWithRoles(params string[] roles)
    {
        var claims = roles.Select(r => new Claim(ClaimTypes.Role, r))
            .Append(new Claim(ClaimTypes.NameIdentifier, "1"))
            .ToArray();
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static AuthorizationHandlerContext CreateContext(ClaimsPrincipal user, PagePermissionRequirement req)
        => new([req], user, null);

    [Fact]
    public async Task AllowedRole_Succeeds()
    {
        var config = new SecurityConfiguration
        {
            PagePermissions = [new PagePermission { PageKey = "orders", AllowedRoles = ["Manager"] }]
        };
        var handler = CreateHandler(config);
        var context = CreateContext(UserWithRoles("Manager"), new PagePermissionRequirement("orders"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task DeniedRole_DoesNotSucceed_EvenIfAllowed()
    {
        var config = new SecurityConfiguration
        {
            PagePermissions = [new PagePermission
            {
                PageKey = "admin",
                AllowedRoles = ["Admin"],
                DeniedRoles = ["Admin"]  // deny wins
            }]
        };
        var handler = CreateHandler(config);
        var context = CreateContext(UserWithRoles("Admin"), new PagePermissionRequirement("admin"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task InheritedRole_Succeeds()
    {
        // Junior inherits from Manager; page allows Manager
        var config = new SecurityConfiguration
        {
            Roles = [new RoleDefinition { RoleName = "Junior", InheritsFrom = "Manager" }],
            PagePermissions = [new PagePermission { PageKey = "reports", AllowedRoles = ["Manager"] }]
        };
        var handler = CreateHandler(config);
        var context = CreateContext(UserWithRoles("Junior"), new PagePermissionRequirement("reports"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task UnregisteredPage_FailOpen_Succeeds()
    {
        var config = new SecurityConfiguration();
        var handler = CreateHandler(config, UnregisteredResourcePolicy.FailOpen);
        var context = CreateContext(UserWithRoles("Admin"), new PagePermissionRequirement("unknown-page"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task UnregisteredPage_FailClosed_DoesNotSucceed()
    {
        var config = new SecurityConfiguration();
        var handler = CreateHandler(config, UnregisteredResourcePolicy.FailClosed);
        var context = CreateContext(UserWithRoles("Admin"), new PagePermissionRequirement("unknown-page"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task UnauthenticatedUser_DoesNotSucceed()
    {
        var config = new SecurityConfiguration
        {
            PagePermissions = [new PagePermission { PageKey = "home", AllowedRoles = ["Guest"] }]
        };
        var handler = CreateHandler(config);
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        var context = CreateContext(anonymous, new PagePermissionRequirement("home"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task SwitchedRole_OverridesClaimsRoles()
    {
        // User has Admin claim, but is switched to Viewer which IS allowed
        var config = new SecurityConfiguration
        {
            PagePermissions = [new PagePermission { PageKey = "reports", AllowedRoles = ["Viewer"] }]
        };
        var switcher = new SessionRoleSwitcher();
        switcher.SwitchToRole("Viewer");

        var handler = CreateHandler(config, roleSwitcher: switcher);
        var context = CreateContext(UserWithRoles("Admin"), new PagePermissionRequirement("reports"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }
}
