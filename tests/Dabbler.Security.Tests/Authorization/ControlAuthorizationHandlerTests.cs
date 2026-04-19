using System.Security.Claims;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Authorization;
using Dabbler.Security.Authorization.Handlers;
using Dabbler.Security.Authorization.Requirements;
using Dabbler.Security.Configuration;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Authorization;

namespace Dabbler.Security.Tests.Authorization;

public class ControlAuthorizationHandlerTests
{
    private static ControlAuthorizationHandler CreateHandler(
        SecurityConfiguration config,
        IRoleSwitcher? roleSwitcher = null)
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        var manager = new SecurityConfigurationManager(store.Object);

        return new ControlAuthorizationHandler(
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

    private static AuthorizationHandlerContext CreateContext(ClaimsPrincipal user, ControlPermissionRequirement req)
        => new([req], user, null);

    [Fact]
    public async Task BakedInRole_Succeeds_ForAnyControl()
    {
        var handler = CreateHandler(new SecurityConfiguration());
        var context = CreateContext(
            UserWithRoles(BakedInRoles.SystemAdmin),
            new ControlPermissionRequirement("dashboard", "export-btn"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task UserTypeRole_WithoutRestrictions_IsDefaultPermissive()
    {
        var config = new SecurityConfiguration
        {
            UserTypeRoles = [new RoleDefinition { RoleName = "BackofficeUser" }]
        };
        var handler = CreateHandler(config);
        var context = CreateContext(
            UserWithRoles("BackofficeUser"),
            new ControlPermissionRequirement("orders", "delete-btn"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HiddenControl_Fails()
    {
        var config = new SecurityConfiguration
        {
            UserTypeRoles =
            [
                new RoleDefinition
                {
                    RoleName = "Customer",
                    UiRestrictions =
                    [
                        new UiRestriction { PageKey = "admin", ControlKey = "nuke-btn", Kind = UiRestrictionKind.Hidden }
                    ]
                }
            ]
        };
        var handler = CreateHandler(config);
        var context = CreateContext(
            UserWithRoles("Customer"),
            new ControlPermissionRequirement("admin", "nuke-btn"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task DisabledControl_Succeeds()
    {
        // Disabled means visible but non-interactive; authorization must succeed so the
        // control renders.
        var config = new SecurityConfiguration
        {
            UserTypeRoles =
            [
                new RoleDefinition
                {
                    RoleName = "Customer",
                    UiRestrictions =
                    [
                        new UiRestriction { PageKey = "orders", ControlKey = "delete-btn", Kind = UiRestrictionKind.Disabled }
                    ]
                }
            ]
        };
        var handler = CreateHandler(config);
        var context = CreateContext(
            UserWithRoles("Customer"),
            new ControlPermissionRequirement("orders", "delete-btn"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task EntirePageHidden_AlsoHidesEveryControlOnIt()
    {
        var config = new SecurityConfiguration
        {
            UserTypeRoles =
            [
                new RoleDefinition
                {
                    RoleName = "Customer",
                    UiRestrictions =
                    [
                        new UiRestriction { PageKey = "admin", ControlKey = "*", Kind = UiRestrictionKind.Hidden }
                    ]
                }
            ]
        };
        var handler = CreateHandler(config);
        var context = CreateContext(
            UserWithRoles("Customer"),
            new ControlPermissionRequirement("admin", "arbitrary-btn"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task UnknownNonBakedInRole_IsDefaultPermissive()
    {
        // Default-permissive: a role not in UserTypeRoles has no explicit
        // restriction, so the control is visible.
        var handler = CreateHandler(new SecurityConfiguration());
        var context = CreateContext(
            UserWithRoles("BackofficeUser"),
            new ControlPermissionRequirement("dashboard", "export-btn"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task SwitchedRole_OverridesClaimsRoles()
    {
        var config = new SecurityConfiguration
        {
            UserTypeRoles =
            [
                new RoleDefinition
                {
                    RoleName = "Customer",
                    UiRestrictions =
                    [
                        new UiRestriction { PageKey = "admin", ControlKey = "nuke-btn", Kind = UiRestrictionKind.Hidden }
                    ]
                }
            ]
        };
        var switcher = new SessionRoleSwitcher();
        switcher.SwitchToRole("Customer");

        var handler = CreateHandler(config, roleSwitcher: switcher);
        var context = CreateContext(
            UserWithRoles(BakedInRoles.SystemAdmin),
            new ControlPermissionRequirement("admin", "nuke-btn"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse("switcher overrides the SystemAdmin claim with the restricted Customer role");
    }
}
