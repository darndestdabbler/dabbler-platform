using Bunit;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Blazor.Components.Admin;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Configuration;
using Dabbler.Security.Registration;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Dabbler.Security.Blazor.Tests.Components.Admin;

public class DabblerSecurityDashboardKeyboardTests
{
    private static void RegisterDashboardServices(IServiceCollection svc)
    {
        var config = new SecurityConfiguration();
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

    [Theory]
    [InlineData(0, "ArrowRight", 1)]
    [InlineData(6, "ArrowRight", 0)]
    [InlineData(0, "ArrowLeft", 6)]
    [InlineData(3, "ArrowLeft", 2)]
    [InlineData(3, "Home", 0)]
    [InlineData(3, "End", 6)]
    [InlineData(3, "a", null)]
    [InlineData(3, "Tab", null)]
    public void NextPanelIndex_ReturnsExpectedIndex(int current, string key, object? expected)
    {
        var actual = DabblerSecurityDashboard.NextPanelIndex(
            current, key, DabblerSecurityDashboard.TabCount);

        actual.Should().Be((int?)expected);
    }

    [Fact]
    public void ArrowRight_AdvancesActivePanelIndex()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: RegisterDashboardServices);

        var cut = ctx.RenderComponent<DabblerSecurityDashboard>();
        cut.Instance.ActivePanelIndex.Should().Be(0);

        var tabStrip = cut.Find("[data-testid='security-dashboard-tabstrip']");
        tabStrip.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        cut.Instance.ActivePanelIndex.Should().Be(1);
    }

    [Fact]
    public void EndKey_JumpsToLastPanel()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: RegisterDashboardServices);

        var cut = ctx.RenderComponent<DabblerSecurityDashboard>();

        var tabStrip = cut.Find("[data-testid='security-dashboard-tabstrip']");
        tabStrip.KeyDown(new KeyboardEventArgs { Key = "End" });

        cut.Instance.ActivePanelIndex.Should().Be(DabblerSecurityDashboard.TabCount - 1);
    }

    [Fact]
    public void ArrowLeft_FromFirstPanel_WrapsToLast()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: RegisterDashboardServices);

        var cut = ctx.RenderComponent<DabblerSecurityDashboard>();
        cut.Instance.ActivePanelIndex.Should().Be(0);

        var tabStrip = cut.Find("[data-testid='security-dashboard-tabstrip']");
        tabStrip.KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });

        cut.Instance.ActivePanelIndex.Should().Be(DabblerSecurityDashboard.TabCount - 1);
    }

    [Fact]
    public void UnhandledKey_DoesNotChangeActivePanelIndex()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: RegisterDashboardServices);

        var cut = ctx.RenderComponent<DabblerSecurityDashboard>();
        var original = cut.Instance.ActivePanelIndex;

        var tabStrip = cut.Find("[data-testid='security-dashboard-tabstrip']");
        tabStrip.KeyDown(new KeyboardEventArgs { Key = "a" });

        cut.Instance.ActivePanelIndex.Should().Be(original);
    }
}
