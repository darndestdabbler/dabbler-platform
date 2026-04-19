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
using Microsoft.AspNetCore.Components.Web;
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

        svc.AddSingleton(manager);
        svc.AddSingleton<AuthorizationStateNotifier>();
        svc.AddSingleton<IBakedInRoleProvider, PlatformBakedInRoleProvider>();
        svc.AddSingleton(MetadataRegistry.Empty);

        var dbOptions = new DbContextOptionsBuilder<DabblerSecurityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        svc.AddScoped(_ => new DabblerSecurityDbContext(dbOptions));

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
    public void Dashboard_CrossOrgUserAdmin_SeesFourTabs()
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

    [Fact]
    public void Dashboard_ArrowRight_AdvancesActivePanel()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(
            roles: [BakedInRoles.SystemAdmin],
            configureServices: svc => RegisterDashboardServices(svc));

        var cut = ctx.RenderComponent<DabblerSecurityDashboard>();
        cut.Instance.ActivePanelIndex.Should().Be(0);

        var tabStrip = cut.Find("[data-testid='security-dashboard-tabstrip']");
        tabStrip.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        cut.Instance.ActivePanelIndex.Should().Be(1);
    }

    [Fact]
    public void Dashboard_EndKey_JumpsToLastVisiblePanel()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(
            roles: [BakedInRoles.SystemAdmin],
            configureServices: svc => RegisterDashboardServices(svc));

        var cut = ctx.RenderComponent<DabblerSecurityDashboard>();
        var tabStrip = cut.Find("[data-testid='security-dashboard-tabstrip']");

        tabStrip.KeyDown(new KeyboardEventArgs { Key = "End" });

        cut.Instance.ActivePanelIndex.Should().Be(cut.Instance.TabCount - 1);
    }

    [Fact]
    public void Dashboard_ArrowLeftFromFirstPanel_WrapsToLast()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(
            roles: [BakedInRoles.SystemAdmin],
            configureServices: svc => RegisterDashboardServices(svc));

        var cut = ctx.RenderComponent<DabblerSecurityDashboard>();
        var tabStrip = cut.Find("[data-testid='security-dashboard-tabstrip']");

        tabStrip.KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });

        cut.Instance.ActivePanelIndex.Should().Be(cut.Instance.TabCount - 1);
    }

    [Theory]
    [InlineData(0, "ArrowRight", 5, 1)]
    [InlineData(4, "ArrowRight", 5, 0)]
    [InlineData(0, "ArrowLeft", 5, 4)]
    [InlineData(2, "ArrowLeft", 5, 1)]
    [InlineData(2, "Home", 5, 0)]
    [InlineData(2, "End", 5, 4)]
    [InlineData(2, "a", 5, null)]
    [InlineData(2, "Tab", 5, null)]
    [InlineData(0, "ArrowRight", 0, null)]
    public void NextPanelIndex_ReturnsExpectedIndex(int current, string key, int count, object? expected)
    {
        var actual = DabblerSecurityDashboard.NextPanelIndex(current, key, count);
        actual.Should().Be((int?)expected);
    }
}
