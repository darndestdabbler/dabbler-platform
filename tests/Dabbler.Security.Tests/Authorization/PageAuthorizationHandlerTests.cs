using System.Security.Claims;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Authorization;
using Dabbler.Security.Authorization.Handlers;
using Dabbler.Security.Authorization.Requirements;
using Dabbler.Security.Configuration;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Authorization;

namespace Dabbler.Security.Tests.Authorization;

public class PageAuthorizationHandlerTests
{
    private static PageAuthorizationHandler CreateHandler(
        SecurityConfiguration config,
        IRoleSwitcher? roleSwitcher = null)
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        var manager = new SecurityConfigurationManager(store.Object);

        return new PageAuthorizationHandler(
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

    private static AuthorizationHandlerContext CreateContext(ClaimsPrincipal user, PagePermissionRequirement req)
        => new([req], user, null);

    [Fact]
    public async Task BakedInSystemAdmin_Succeeds_ForAnyPage()
    {
        var handler = CreateHandler(new SecurityConfiguration());
        var context = CreateContext(UserWithRoles(BakedInRoles.SystemAdmin), new PagePermissionRequirement("orders"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task BakedInCrossOrgMonitor_Succeeds_ForAnyPage()
    {
        var handler = CreateHandler(new SecurityConfiguration());
        var context = CreateContext(UserWithRoles(BakedInRoles.CrossOrgMonitor), new PagePermissionRequirement("reports"));

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
        var context = CreateContext(UserWithRoles("BackofficeUser"), new PagePermissionRequirement("orders"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task UserTypeRole_WithEntirePageHidden_Fails()
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
        var context = CreateContext(UserWithRoles("Customer"), new PagePermissionRequirement("admin"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task UserTypeRole_WithEntirePageDisabled_Succeeds()
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
                        new UiRestriction { PageKey = "orders", ControlKey = "*", Kind = UiRestrictionKind.Disabled }
                    ]
                }
            ]
        };
        var handler = CreateHandler(config);
        var context = CreateContext(UserWithRoles("Customer"), new PagePermissionRequirement("orders"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task UserTypeRole_WithControlLevelHidden_DoesNotBlockEntirePage()
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
                        new UiRestriction { PageKey = "orders", ControlKey = "delete-btn", Kind = UiRestrictionKind.Hidden }
                    ]
                }
            ]
        };
        var handler = CreateHandler(config);
        var context = CreateContext(UserWithRoles("Customer"), new PagePermissionRequirement("orders"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task UnauthenticatedUser_DoesNotSucceed()
    {
        var handler = CreateHandler(new SecurityConfiguration());
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        var context = CreateContext(anonymous, new PagePermissionRequirement("home"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task UnknownNonBakedInRole_IsDefaultPermissive()
    {
        // Default-permissive is the user-type principle: no explicit restriction
        // is configured, so the role is allowed. This preserves the pre-Session-5
        // seed (where user-type roles still live in legacy Roles) without changing
        // the handler contract.
        var handler = CreateHandler(new SecurityConfiguration());
        var context = CreateContext(UserWithRoles("BackofficeUser"), new PagePermissionRequirement("orders"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task SwitchedRole_OverridesClaimsRoles()
    {
        // User has SystemAdmin claim but is switched to a restricted user-type role.
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
        var switcher = new SessionRoleSwitcher();
        switcher.SwitchToRole("Customer");

        var handler = CreateHandler(config, roleSwitcher: switcher);
        var context = CreateContext(UserWithRoles(BakedInRoles.SystemAdmin), new PagePermissionRequirement("admin"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task RoleInheritance_IsNotApplied()
    {
        // Legacy InheritsFrom must not grant ancestor access under the new model.
#pragma warning disable CS0618 // deliberate legacy field use for the inheritance-not-applied guarantee
        var config = new SecurityConfiguration
        {
            UserTypeRoles =
            [
                new RoleDefinition
                {
                    RoleName = "Junior",
                    InheritsFrom = BakedInRoles.SystemAdmin,
                    UiRestrictions =
                    [
                        new UiRestriction { PageKey = "admin", ControlKey = "*", Kind = UiRestrictionKind.Hidden }
                    ]
                }
            ]
        };
#pragma warning restore CS0618

        var handler = CreateHandler(config);
        var context = CreateContext(UserWithRoles("Junior"), new PagePermissionRequirement("admin"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse("role inheritance is removed; 'Junior' must not pick up SystemAdmin access");
    }
}
