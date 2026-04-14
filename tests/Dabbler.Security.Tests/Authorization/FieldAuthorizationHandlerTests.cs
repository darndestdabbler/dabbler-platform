using System.Security.Claims;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Authorization.Handlers;
using Dabbler.Security.Authorization.Requirements;
using Dabbler.Security.Configuration;
using Dabbler.Security.Options;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Authorization;
using FieldAccessLevel = Dabbler.Security.Configuration.FieldAccessLevel;

namespace Dabbler.Security.Tests.Authorization;

public class FieldAuthorizationHandlerTests
{
    private static FieldAuthorizationHandler CreateHandler(
        SecurityConfiguration config,
        UnregisteredResourcePolicy policy = UnregisteredResourcePolicy.FailOpen,
        IRoleSwitcher? roleSwitcher = null)
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        var manager = new SecurityConfigurationManager(store.Object);
        var options = Microsoft.Extensions.Options.Options.Create(new DabblerSecurityOptions { UnregisteredResourcePolicy = policy });
        return new FieldAuthorizationHandler(manager, options, roleSwitcher ?? new SessionRoleSwitcher());
    }

    private static ClaimsPrincipal UserWithRoles(params string[] roles)
    {
        var claims = roles.Select(r => new Claim(ClaimTypes.Role, r))
            .Append(new Claim(ClaimTypes.NameIdentifier, "1"))
            .ToArray();
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static AuthorizationHandlerContext CreateContext(ClaimsPrincipal user, FieldPermissionRequirement req)
        => new([req], user, null);

    [Fact]
    public async Task EditableField_Succeeds()
    {
        var config = new SecurityConfiguration
        {
            FieldPermissions =
            [
                new FieldPermission
                {
                    EntityKey = "Customer", FieldKey = "Email",
                    RolePermissions = [new FieldRolePermission { RoleName = "Admin", AccessLevel = FieldAccessLevel.Editable }]
                }
            ]
        };
        var handler = CreateHandler(config);
        var context = CreateContext(UserWithRoles("Admin"), new FieldPermissionRequirement("Customer", "Email"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task ReadOnlyField_Succeeds()
    {
        var config = new SecurityConfiguration
        {
            FieldPermissions =
            [
                new FieldPermission
                {
                    EntityKey = "Customer", FieldKey = "Salary",
                    RolePermissions = [new FieldRolePermission { RoleName = "HR", AccessLevel = FieldAccessLevel.ReadOnly }]
                }
            ]
        };
        var handler = CreateHandler(config);
        var context = CreateContext(UserWithRoles("HR"), new FieldPermissionRequirement("Customer", "Salary"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HiddenField_DoesNotSucceed()
    {
        var config = new SecurityConfiguration
        {
            FieldPermissions =
            [
                new FieldPermission
                {
                    EntityKey = "Customer", FieldKey = "SSN",
                    RolePermissions = [new FieldRolePermission { RoleName = "Viewer", AccessLevel = FieldAccessLevel.Hidden }]
                }
            ]
        };
        var handler = CreateHandler(config);
        var context = CreateContext(UserWithRoles("Viewer"), new FieldPermissionRequirement("Customer", "SSN"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task MostPermissiveWins_WhenMultipleRoles()
    {
        // Viewer=Hidden but Editor=Editable — combined should succeed (Editable wins)
        var config = new SecurityConfiguration
        {
            FieldPermissions =
            [
                new FieldPermission
                {
                    EntityKey = "Order", FieldKey = "Total",
                    RolePermissions =
                    [
                        new FieldRolePermission { RoleName = "Viewer", AccessLevel = FieldAccessLevel.Hidden },
                        new FieldRolePermission { RoleName = "Editor", AccessLevel = FieldAccessLevel.Editable }
                    ]
                }
            ]
        };
        var handler = CreateHandler(config);
        var context = CreateContext(UserWithRoles("Viewer", "Editor"), new FieldPermissionRequirement("Order", "Total"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task SwitchedRole_OverridesClaimsRoles()
    {
        var config = new SecurityConfiguration
        {
            FieldPermissions =
            [
                new FieldPermission
                {
                    EntityKey = "Customer", FieldKey = "Email",
                    RolePermissions = [new FieldRolePermission { RoleName = "Viewer", AccessLevel = FieldAccessLevel.Editable }]
                }
            ]
        };
        var switcher = new SessionRoleSwitcher();
        switcher.SwitchToRole("Viewer");

        var handler = CreateHandler(config, roleSwitcher: switcher);
        var context = CreateContext(UserWithRoles("Admin"), new FieldPermissionRequirement("Customer", "Email"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }
}
