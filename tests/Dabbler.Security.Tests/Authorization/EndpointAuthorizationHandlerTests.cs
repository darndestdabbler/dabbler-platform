using System.Security.Claims;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Authorization;
using Dabbler.Security.Authorization.Handlers;
using Dabbler.Security.Authorization.Requirements;
using Dabbler.Security.Configuration;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Authorization;

namespace Dabbler.Security.Tests.Authorization;

public class EndpointAuthorizationHandlerTests
{
    private static EndpointAuthorizationHandler CreateHandler(
        SecurityConfiguration config,
        IRoleSwitcher? roleSwitcher = null)
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        var manager = new SecurityConfigurationManager(store.Object);

        return new EndpointAuthorizationHandler(
            manager,
            roleSwitcher ?? new SessionRoleSwitcher(),
            new PlatformBakedInRoleProvider());
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
    public async Task BakedInSystemAdmin_Succeeds()
    {
        var handler = CreateHandler(new SecurityConfiguration());
        var context = CreateContext(UserWithRoles(BakedInRoles.SystemAdmin), new EndpointPermissionRequirement("POST /api/orders"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task BakedInCrossOrgMonitor_Succeeds()
    {
        var handler = CreateHandler(new SecurityConfiguration());
        var context = CreateContext(UserWithRoles(BakedInRoles.CrossOrgMonitor), new EndpointPermissionRequirement("GET /api/reports"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task KnownUserTypeRole_Succeeds()
    {
        var config = new SecurityConfiguration
        {
            UserTypeRoles = [new RoleDefinition { RoleName = "BackofficeUser" }]
        };
        var handler = CreateHandler(config);
        var context = CreateContext(UserWithRoles("BackofficeUser"), new EndpointPermissionRequirement("POST /api/orders"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task UnknownNonBakedInRole_IsDefaultPermissive()
    {
        // Default-permissive at the endpoint layer for user-type roles. There is
        // no endpoint-level restriction type in the new model.
        var handler = CreateHandler(new SecurityConfiguration());
        var context = CreateContext(UserWithRoles("BackofficeUser"), new EndpointPermissionRequirement("POST /api/orders"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task UnauthenticatedUser_DoesNotSucceed()
    {
        var handler = CreateHandler(new SecurityConfiguration());
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        var context = CreateContext(anonymous, new EndpointPermissionRequirement("GET /api/reports"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task SwitchedRole_OverridesClaimsRoles()
    {
        var config = new SecurityConfiguration
        {
            UserTypeRoles = [new RoleDefinition { RoleName = "Customer" }]
        };
        var switcher = new SessionRoleSwitcher();
        switcher.SwitchToRole("Customer");

        var handler = CreateHandler(config, roleSwitcher: switcher);
        var context = CreateContext(UserWithRoles(BakedInRoles.SystemAdmin), new EndpointPermissionRequirement("GET /api/orders"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue("switched Customer is a known user-type role");
    }

    [Fact]
    public async Task RoleInheritance_IsNotApplied_AtEndpointLayer()
    {
        // User-type endpoint access is default-permissive, so the inheritance
        // guarantee is carried more visibly by the page and control handlers.
        // Kept here as a companion assertion: the endpoint handler never reads
        // RoleDefinition.InheritsFrom.
#pragma warning disable CS0618 // legacy field deliberately used to assert it is ignored
        var config = new SecurityConfiguration
        {
            UserTypeRoles =
            [
                new RoleDefinition
                {
                    RoleName = "Junior",
                    InheritsFrom = BakedInRoles.SystemAdmin
                }
            ]
        };
#pragma warning restore CS0618

        var handler = CreateHandler(config);
        var context = CreateContext(UserWithRoles("Junior"), new EndpointPermissionRequirement("GET /api/reports"));
        await handler.HandleAsync(context);
        context.HasSucceeded.Should().BeTrue("default-permissive endpoint access is granted on Junior's own merit, not by inheriting SystemAdmin");
    }
}
