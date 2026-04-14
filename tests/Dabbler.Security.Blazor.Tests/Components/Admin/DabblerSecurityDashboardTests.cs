using Bunit;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Blazor.Components.Admin;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Configuration;
using Dabbler.Security.Registration;
using Dabbler.Security.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Dabbler.Security.Blazor.Tests.Components.Admin;

public class DabblerSecurityDashboardTests
{
    private static void RegisterDashboardServices(
        IServiceCollection svc,
        SecurityConfiguration? config = null)
    {
        config ??= new SecurityConfiguration();
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        store.Setup(s => s.SaveAsync(It.IsAny<SecurityConfiguration>(), default))
             .Returns(Task.CompletedTask);
        var manager = new SecurityConfigurationManager(store.Object);

        var userListProvider = new Mock<IUserListProvider>();
        userListProvider.Setup(p => p.GetUsersAsync(default))
                        .ReturnsAsync(new List<UserSummary>());

        var roleStore = new Mock<IUserRoleStore>();
        roleStore.Setup(s => s.GetAllAsync(default))
                 .ReturnsAsync((IReadOnlyDictionary<string, IReadOnlyList<string>>)
                     new Dictionary<string, IReadOnlyList<string>>());

        svc.AddSingleton(manager);
        svc.AddSingleton(ResourceRegistry.Empty);
        svc.AddSingleton<AuthorizationStateNotifier>();
        svc.AddSingleton(userListProvider.Object);
        svc.AddSingleton(roleStore.Object);
    }

    [Fact]
    public void Dashboard_RendersAllTabs()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc => RegisterDashboardServices(svc));

        var cut = ctx.RenderComponent<DabblerSecurityDashboard>();

        cut.Markup.Should().Contain("Roles");
        cut.Markup.Should().Contain("Page Access");
        cut.Markup.Should().Contain("Entity Access");
        cut.Markup.Should().Contain("Endpoint Access");
        cut.Markup.Should().Contain("User Assignments");
    }

    [Fact]
    public void Dashboard_RendersRolesTabContent()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles = [new RoleDefinition { RoleName = "SysAdmin" }]
        };
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc => RegisterDashboardServices(svc, config));

        var cut = ctx.RenderComponent<DabblerSecurityDashboard>();

        // Role Management header comes from DabblerRoleManager rendered in the Roles tab
        cut.Markup.Should().Contain("Role Management");
    }
}
