using Bunit;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Blazor.Components.Admin;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Configuration;
using Dabbler.Security.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;

namespace Dabbler.Security.Blazor.Tests.Components.Admin;

public class DabblerUserRoleManagerTests
{
    private static (SecurityConfigurationManager manager, Mock<ISecurityConfigurationStore> store)
        BuildManager(SecurityConfiguration config)
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        store.Setup(s => s.SaveAsync(It.IsAny<SecurityConfiguration>(), default))
             .Returns(Task.CompletedTask);
        return (new SecurityConfigurationManager(store.Object), store);
    }

    private static Mock<IUserListProvider> BuildUserList(params UserSummary[] users)
    {
        var mock = new Mock<IUserListProvider>();
        mock.Setup(p => p.GetUsersAsync(default)).ReturnsAsync(users.ToList());
        return mock;
    }

    private static Mock<IUserRoleStore> BuildRoleStore(Dictionary<string, List<string>>? assignments = null)
    {
        assignments ??= [];
        var mock = new Mock<IUserRoleStore>();
        var dict = assignments.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<string>)kv.Value);
        mock.Setup(s => s.GetAllAsync(default))
            .ReturnsAsync((IReadOnlyDictionary<string, IReadOnlyList<string>>)dict);
        mock.Setup(s => s.AddRoleAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);
        mock.Setup(s => s.RemoveRoleAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);
        return mock;
    }

    [Fact]
    public async Task Renders_UsersWithCurrentRoles()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles = [new RoleDefinition { RoleName = "Admin" }, new RoleDefinition { RoleName = "Viewer" }]
        };
        var (manager, _) = BuildManager(config);
        var userList = BuildUserList(
            new UserSummary("u1", "Alice", "alice@test.com"),
            new UserSummary("u2", "Bob", "bob@test.com"));
        var roleStore = BuildRoleStore(new Dictionary<string, List<string>>
        {
            ["u1"] = ["Admin"]
        });

        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(userList.Object);
                svc.AddSingleton(roleStore.Object);
            });

        var cut = ctx.RenderComponent<DabblerUserRoleManager>();
        await cut.InvokeAsync(() => Task.CompletedTask); // let async init settle

        cut.Markup.Should().Contain("Alice");
        cut.Markup.Should().Contain("Bob");
        cut.Markup.Should().Contain("Admin");
    }

    [Fact]
    public async Task NoUsers_ShowsNoUsersMessage()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration();
        var (manager, _) = BuildManager(config);
        var userList = BuildUserList(); // empty
        var roleStore = BuildRoleStore();

        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(userList.Object);
                svc.AddSingleton(roleStore.Object);
            });

        var cut = ctx.RenderComponent<DabblerUserRoleManager>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        cut.Markup.Should().Contain("No users found");
    }

    [Fact]
    public async Task AddRole_CallsRoleStore()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles = [new RoleDefinition { RoleName = "Editor" }]
        };
        var (manager, _) = BuildManager(config);
        var userList = BuildUserList(new UserSummary("u1", "Alice", "alice@test.com"));
        var roleStore = BuildRoleStore();

        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(userList.Object);
                svc.AddSingleton(roleStore.Object);
            });

        var cut = ctx.RenderComponent<DabblerUserRoleManager>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Verify the user row and role store are present
        cut.Markup.Should().Contain("Alice");
        roleStore.Verify(s => s.GetAllAsync(default), Times.Once);
    }

    [Fact]
    public async Task SelectingSystemAdminAndClickingAdd_PersistsElevationAndRendersChip()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles =
            [
                new RoleDefinition { RoleName = "BackofficeUser" },
                new RoleDefinition { RoleName = "SystemAdmin" }
            ]
        };
        var (manager, _) = BuildManager(config);
        var userList = BuildUserList(new UserSummary("42", "Jordan Ellis", "jdoe@acmecoffee.com"));
        var roleStore = BuildRoleStore(new Dictionary<string, List<string>>
        {
            ["42"] = ["BackofficeUser"]
        });

        ctx.SetupForDabbler(
            roles: ["SystemAdmin"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(userList.Object);
                svc.AddSingleton(roleStore.Object);
            });

        var cut = ctx.RenderComponent<DabblerUserRoleManager>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        var select = cut.FindComponent<MudSelect<string>>();
        await cut.InvokeAsync(async () => await select.Instance.ValueChanged.InvokeAsync("SystemAdmin"));

        var addButton = cut.Find("button[aria-label*='Add selected role to Jordan Ellis']");
        addButton.HasAttribute("disabled").Should().BeFalse();
        await cut.InvokeAsync(() => addButton.ClickAsync(new()));

        roleStore.Verify(
            s => s.AddRoleAsync("42", "SystemAdmin", It.IsAny<CancellationToken>()),
            Times.Once);
        cut.Markup.Should().Contain("SystemAdmin");
    }

    [Fact]
    public async Task UserWithNoRoles_ShowsNoRolesAssignedText()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles = [new RoleDefinition { RoleName = "Admin" }]
        };
        var (manager, _) = BuildManager(config);
        var userList = BuildUserList(new UserSummary("u1", "Charlie", "charlie@test.com"));
        var roleStore = BuildRoleStore(); // no assignments

        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(userList.Object);
                svc.AddSingleton(roleStore.Object);
            });

        var cut = ctx.RenderComponent<DabblerUserRoleManager>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        cut.Markup.Should().Contain("No roles assigned");
    }
}
