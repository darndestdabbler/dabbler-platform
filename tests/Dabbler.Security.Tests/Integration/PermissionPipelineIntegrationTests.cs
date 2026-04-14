using System.Security.Claims;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Authorization.Handlers;
using Dabbler.Security.Authorization.Requirements;
using Dabbler.Security.Configuration;
using Dabbler.Security.Options;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace Dabbler.Security.Tests.Integration;

/// <summary>
/// Integration-style tests that exercise the full permission pipeline:
///   SecurityConfigurationManager (in-memory store) → handler → authorization result.
/// These cover the Session 7 integration scenarios from the implementation plan.
/// </summary>
public class PermissionPipelineIntegrationTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static SecurityConfigurationManager BuildManager(SecurityConfiguration config)
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(config);
        store.Setup(s => s.SaveAsync(It.IsAny<SecurityConfiguration>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);
        return new SecurityConfigurationManager(store.Object);
    }

    private static IOptions<DabblerSecurityOptions> FailOpenOptions() =>
        MsOptions.Create(new DabblerSecurityOptions { UnregisteredResourcePolicy = UnregisteredResourcePolicy.FailOpen });

    private static IOptions<DabblerSecurityOptions> FailClosedOptions() =>
        MsOptions.Create(new DabblerSecurityOptions { UnregisteredResourcePolicy = UnregisteredResourcePolicy.FailClosed });

    private static ClaimsPrincipal UserWithRoles(params string[] roles)
    {
        var claims = roles.Select(r => new Claim(ClaimTypes.Role, r))
            .Prepend(new Claim(ClaimTypes.NameIdentifier, "user-1"))
            .ToArray();
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static AuthorizationHandlerContext Ctx(ClaimsPrincipal user, IAuthorizationRequirement req)
        => new([req], user, null);

    // ── Scenario 1: Full permission matrix change ──────────────────────────
    // Add entity permission for a role → assert handler allows access.
    // Then remove it → assert handler denies.

    [Fact]
    public async Task EntityPermissionAdded_HandlerAllowsAccess()
    {
        var config = new SecurityConfiguration
        {
            EntityPermissions =
            [
                new EntityPermission
                {
                    EntityKey = "Widget",
                    RolePermissions =
                    [
                        new EntityRolePermission { RoleName = "Manager", AllowedOperations = ["Read", "Create"] }
                    ]
                }
            ]
        };
        var manager = BuildManager(config);
        var handler = new EntityAuthorizationHandler(manager, FailOpenOptions(), new SessionRoleSwitcher());
        var context = Ctx(UserWithRoles("Manager"), new EntityPermissionRequirement("Widget", "Create"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue("Manager was granted Widget.Create");
    }

    [Fact]
    public async Task EntityPermissionNotGranted_HandlerDeniesAccess()
    {
        var config = new SecurityConfiguration
        {
            EntityPermissions =
            [
                new EntityPermission
                {
                    EntityKey = "Widget",
                    RolePermissions =
                    [
                        new EntityRolePermission { RoleName = "Viewer", AllowedOperations = ["Read"] }
                    ]
                }
            ]
        };
        var manager = BuildManager(config);
        var handler = new EntityAuthorizationHandler(manager, FailOpenOptions(), new SessionRoleSwitcher());
        var context = Ctx(UserWithRoles("Viewer"), new EntityPermissionRequirement("Widget", "Delete"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse("Viewer was not granted Widget.Delete");
    }

    [Fact]
    public async Task EntityPermissionChange_LiveUpdateApplied()
    {
        // Start with Viewer having only Read
        var initialConfig = new SecurityConfiguration
        {
            EntityPermissions =
            [
                new EntityPermission
                {
                    EntityKey = "Widget",
                    RolePermissions =
                    [
                        new EntityRolePermission { RoleName = "Viewer", AllowedOperations = ["Read"] }
                    ]
                }
            ]
        };

        // New config grants Viewer Create as well
        var updatedConfig = new SecurityConfiguration
        {
            EntityPermissions =
            [
                new EntityPermission
                {
                    EntityKey = "Widget",
                    RolePermissions =
                    [
                        new EntityRolePermission { RoleName = "Viewer", AllowedOperations = ["Read", "Create"] }
                    ]
                }
            ]
        };

        var store = new Mock<ISecurityConfigurationStore>();
        store.SetupSequence(s => s.LoadAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(initialConfig)
             .ReturnsAsync(updatedConfig);
        store.Setup(s => s.SaveAsync(It.IsAny<SecurityConfiguration>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var manager = new SecurityConfigurationManager(store.Object);
        var handler = new EntityAuthorizationHandler(manager, FailOpenOptions(), new SessionRoleSwitcher());

        // Before config change — Create denied
        var ctx1 = Ctx(UserWithRoles("Viewer"), new EntityPermissionRequirement("Widget", "Create"));
        await handler.HandleAsync(ctx1);
        ctx1.HasSucceeded.Should().BeFalse("Viewer should not have Create before config update");

        // Simulate config change by saving new config (triggers reload)
        await manager.SaveAsync(updatedConfig);

        // After config change — Create granted
        var ctx2 = Ctx(UserWithRoles("Viewer"), new EntityPermissionRequirement("Widget", "Create"));
        await handler.HandleAsync(ctx2);
        ctx2.HasSucceeded.Should().BeTrue("Viewer should have Create after config update");
    }

    // ── Scenario 2: Page permission deny ──────────────────────────────────
    // Configure page with a DeniedRoles entry → assert 403-equivalent (handler does not succeed).

    [Fact]
    public async Task PagePermission_AllowedRole_Succeeds()
    {
        var config = new SecurityConfiguration
        {
            PagePermissions =
            [
                new PagePermission { PageKey = "/widgets", AllowedRoles = ["Manager", "Viewer"] }
            ]
        };
        var manager = BuildManager(config);
        var handler = new PageAuthorizationHandler(manager, FailOpenOptions(), new SessionRoleSwitcher());
        var context = Ctx(UserWithRoles("Viewer"), new PagePermissionRequirement("/widgets"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue("Viewer is in AllowedRoles for /widgets");
    }

    [Fact]
    public async Task PagePermission_DeniedRole_DoesNotSucceed()
    {
        var config = new SecurityConfiguration
        {
            PagePermissions =
            [
                new PagePermission
                {
                    PageKey = "/admin/security",
                    AllowedRoles = ["SystemAdministrator"],
                    DeniedRoles = ["Viewer"]
                }
            ]
        };
        var manager = BuildManager(config);
        var handler = new PageAuthorizationHandler(manager, FailClosedOptions(), new SessionRoleSwitcher());
        var context = Ctx(UserWithRoles("Viewer"), new PagePermissionRequirement("/admin/security"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse("Viewer is in DeniedRoles for /admin/security");
    }

    [Fact]
    public async Task PagePermission_RoleNotInAllowed_DoesNotSucceed()
    {
        var config = new SecurityConfiguration
        {
            PagePermissions =
            [
                new PagePermission { PageKey = "/admin/security", AllowedRoles = ["SystemAdministrator"] }
            ]
        };
        var manager = BuildManager(config);
        var handler = new PageAuthorizationHandler(manager, FailClosedOptions(), new SessionRoleSwitcher());
        var context = Ctx(UserWithRoles("Manager"), new PagePermissionRequirement("/admin/security"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse("Manager is not in AllowedRoles for /admin/security");
    }

    // ── Scenario 3: Row filter scoping ────────────────────────────────────
    // Configure "own records only" row filter → assert query scope changes.

    private sealed class Widget { public int OwnerId { get; set; } public string Name { get; set; } = ""; }

    [Fact]
    public async Task RowFilter_OwnRecordsOnly_FiltersCorrectly()
    {
        var config = new SecurityConfiguration
        {
            RowFilters =
            [
                new RowFilter { EntityKey = "Widget", RoleName = "Viewer", FilterExpression = "OwnerId == 42" }
            ]
        };
        var manager = BuildManager(config);
        await manager.EnsureLoadedAsync();
        var service = new RowFilterService(manager, Microsoft.Extensions.Logging.Abstractions.NullLogger<RowFilterService>.Instance);

        var filter = service.GetFilter<Widget>("Widget", "Viewer").Compile();

        filter(new Widget { OwnerId = 42 }).Should().BeTrue("OwnerId 42 matches filter");
        filter(new Widget { OwnerId = 99 }).Should().BeFalse("OwnerId 99 does not match filter");
    }

    [Fact]
    public async Task RowFilter_NoFilterConfigured_AllowsAll()
    {
        var config = new SecurityConfiguration();
        var manager = BuildManager(config);
        await manager.EnsureLoadedAsync();
        var service = new RowFilterService(manager, Microsoft.Extensions.Logging.Abstractions.NullLogger<RowFilterService>.Instance);

        var filter = service.GetFilter<Widget>("Widget", "Manager").Compile();

        filter(new Widget { OwnerId = 1 }).Should().BeTrue("no filter = pass-through");
        filter(new Widget { OwnerId = 999 }).Should().BeTrue("no filter = pass-through");
    }

    // ── Scenario 4: Role switching ────────────────────────────────────────
    // Switch to Viewer role → confirm entity and page restrictions apply.

    [Fact]
    public async Task RoleSwitching_EntityAccess_Restricted()
    {
        var config = new SecurityConfiguration
        {
            EntityPermissions =
            [
                new EntityPermission
                {
                    EntityKey = "Widget",
                    RolePermissions =
                    [
                        new EntityRolePermission { RoleName = "Manager", AllowedOperations = ["Create", "Read", "Update", "Delete"] },
                        new EntityRolePermission { RoleName = "Viewer", AllowedOperations = ["Read"] }
                    ]
                }
            ]
        };
        var manager = BuildManager(config);

        // User is naturally a Manager but has switched to Viewer
        var roleSwitcher = new SessionRoleSwitcher();
        roleSwitcher.SwitchToRole("Viewer");

        var handler = new EntityAuthorizationHandler(manager, FailOpenOptions(), roleSwitcher);
        var user = UserWithRoles("Manager"); // natural role is Manager

        var readCtx = Ctx(user, new EntityPermissionRequirement("Widget", "Read"));
        var deleteCtx = Ctx(user, new EntityPermissionRequirement("Widget", "Delete"));

        await handler.HandleAsync(readCtx);
        await handler.HandleAsync(deleteCtx);

        readCtx.HasSucceeded.Should().BeTrue("Viewer can Read");
        deleteCtx.HasSucceeded.Should().BeFalse("Viewer cannot Delete even though natural role is Manager");
    }

    [Fact]
    public async Task RoleSwitching_ClearSwitch_RestoresNaturalAccess()
    {
        var config = new SecurityConfiguration
        {
            EntityPermissions =
            [
                new EntityPermission
                {
                    EntityKey = "Widget",
                    RolePermissions =
                    [
                        new EntityRolePermission { RoleName = "Manager", AllowedOperations = ["Create", "Read", "Update", "Delete"] },
                        new EntityRolePermission { RoleName = "Viewer", AllowedOperations = ["Read"] }
                    ]
                }
            ]
        };
        var manager = BuildManager(config);
        var roleSwitcher = new SessionRoleSwitcher();
        roleSwitcher.SwitchToRole("Viewer");

        var handler = new EntityAuthorizationHandler(manager, FailOpenOptions(), roleSwitcher);
        var user = UserWithRoles("Manager");

        // While switched → Delete denied
        var switchedCtx = Ctx(user, new EntityPermissionRequirement("Widget", "Delete"));
        await handler.HandleAsync(switchedCtx);
        switchedCtx.HasSucceeded.Should().BeFalse();

        // After clearing switch → Delete allowed
        roleSwitcher.ClearSwitch();
        var clearedCtx = Ctx(user, new EntityPermissionRequirement("Widget", "Delete"));
        await handler.HandleAsync(clearedCtx);
        clearedCtx.HasSucceeded.Should().BeTrue("natural Manager role has Delete");
    }

    [Fact]
    public async Task RoleSwitching_PageAccess_Restricted()
    {
        var config = new SecurityConfiguration
        {
            PagePermissions =
            [
                new PagePermission { PageKey = "/admin/security", AllowedRoles = ["SystemAdministrator"] }
            ]
        };
        var manager = BuildManager(config);

        // User is naturally SystemAdministrator but switched to Viewer
        var roleSwitcher = new SessionRoleSwitcher();
        roleSwitcher.SwitchToRole("Viewer");

        var handler = new PageAuthorizationHandler(manager, FailClosedOptions(), roleSwitcher);
        var user = UserWithRoles("SystemAdministrator");
        var context = Ctx(user, new PagePermissionRequirement("/admin/security"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse("Viewer does not have access to /admin/security even though natural role is SystemAdministrator");
    }
}
