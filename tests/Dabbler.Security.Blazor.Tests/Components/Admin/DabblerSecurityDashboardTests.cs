using System.Security.Claims;
using Bunit;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Authorization;
using Dabbler.Security.Blazor.Components.Admin;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Configuration;
using Dabbler.Security.Data;
using Dabbler.Security.Registration;
using Dabbler.Security.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Dabbler.Security.Blazor.Tests.Components.Admin;

public class DabblerSecurityDashboardTests
{
    private sealed class DashboardTestDbContextFactory : IDbContextFactory<DabblerSecurityDbContext>
    {
        private readonly DbContextOptions<DabblerSecurityDbContext> _options;

        public DashboardTestDbContextFactory(DbContextOptions<DabblerSecurityDbContext> options)
            => _options = options;

        public DabblerSecurityDbContext CreateDbContext() => new(_options);
    }

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

        svc.AddSingleton(manager);
        svc.AddSingleton<AuthorizationStateNotifier>();
        svc.AddSingleton<IBakedInRoleProvider, PlatformBakedInRoleProvider>();
        svc.AddSingleton(MetadataRegistry.Empty);

        var dbOptions = new DbContextOptionsBuilder<DabblerSecurityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        svc.AddScoped(_ => new DabblerSecurityDbContext(dbOptions));
        svc.AddSingleton<IDbContextFactory<DabblerSecurityDbContext>>(
            new DashboardTestDbContextFactory(dbOptions));

        // The Users and Role Assignment tab pulls these services as soon as it
        // is the active panel (e.g., for UserAdmin). Stub them out empty so the
        // dashboard renders cleanly under any role; per-tab unit tests cover
        // the populated paths.
        var queryMock = new Mock<IAdminUserQueryService>();
        queryMock.Setup(q => q.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        queryMock.Setup(q => q.GetActorActiveAdminRole()).Returns((string?)null);
        queryMock.Setup(q => q.GetActorUserId()).Returns(string.Empty);
        queryMock.Setup(q => q.GetActorAssignedOrganizationIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());
        svc.AddSingleton(queryMock.Object);

        var userRoleStore = new Mock<IUserRoleStore>();
        userRoleStore.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, IReadOnlyList<string>>());
        svc.AddSingleton(userRoleStore.Object);
    }

    [Fact]
    public void Dashboard_SystemAdmin_RendersAllFiveTabs()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(
            roles: [BakedInRoles.SystemAdmin],
            configureServices: svc => RegisterDashboardServices(svc));

        var cut = ctx.RenderComponent<DabblerSecurityDashboard>();

        cut.Markup.Should().Contain("Role Master Details");
        cut.Markup.Should().Contain("Default Masks");
        cut.Markup.Should().Contain("Organization Master Details");
        cut.Markup.Should().Contain("Users and Role Assignment");
        cut.Markup.Should().Contain("Role Documentation");
        cut.Instance.TabCount.Should().Be(5);
    }

    [Fact]
    public void Dashboard_UserAdmin_RendersOnlyPermittedTabs()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(
            roles: [BakedInRoles.UserAdmin],
            configureServices: svc => RegisterDashboardServices(svc));

        var cut = ctx.RenderComponent<DabblerSecurityDashboard>();

        cut.Instance.VisibleTabs.Select(t => t.Label).Should().BeEquivalentTo(
            "Users and Role Assignment", "Role Documentation");
        cut.Instance.TabCount.Should().Be(2);
    }

    [Fact]
    public void Dashboard_CrossOrgUserAdmin_SeesThreeTabs()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(
            roles: [BakedInRoles.CrossOrgUserAdmin],
            configureServices: svc => RegisterDashboardServices(svc));

        var cut = ctx.RenderComponent<DabblerSecurityDashboard>();

        cut.Instance.VisibleTabs.Select(t => t.Label).Should().BeEquivalentTo(
            "Organization Master Details", "Users and Role Assignment", "Role Documentation");
        cut.Instance.TabCount.Should().Be(3);
    }

    [Fact]
    public void Dashboard_PlainUser_SeesOnlyRoleDocumentation()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(
            roles: [BakedInRoles.User],
            configureServices: svc => RegisterDashboardServices(svc));

        var cut = ctx.RenderComponent<DabblerSecurityDashboard>();

        cut.Instance.VisibleTabs.Select(t => t.Label).Should().BeEquivalentTo("Role Documentation");
        cut.Instance.TabCount.Should().Be(1);
    }

    [Fact]
    public void Dashboard_CrossOrgMonitor_SeesOnlyRoleDocumentation()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(
            roles: [BakedInRoles.CrossOrgMonitor],
            configureServices: svc => RegisterDashboardServices(svc));

        var cut = ctx.RenderComponent<DabblerSecurityDashboard>();

        cut.Instance.VisibleTabs.Select(t => t.Label).Should().BeEquivalentTo("Role Documentation");
        cut.Instance.TabCount.Should().Be(1);
    }

}
