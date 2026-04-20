using Bunit;
using Dabbler.Components;
using Dabbler.Components.Data;
using Dabbler.Security.Blazor.Components.Admin.Users;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Dabbler.Security.Blazor.Tests.Components.Admin.Users;

public class UsersMasterGridTests
{
    private static readonly AdminUserRow Alice = new(
        "1", "Alice Admin", "alice@test.local", true,
        [new AdminUserOrganization(10, "internal", "Internal", true, true)],
        ["SystemAdmin"]);

    private static readonly AdminUserRow Bob = new(
        "2", "Bob Buyer", "bob@test.local", true,
        [new AdminUserOrganization(20, "customer-101", "Retail 101", true, false)],
        ["Customer"]);

    private static readonly AdminUserRow InactiveCarol = new(
        "3", "Carol Former", "carol@test.local", false,
        [new AdminUserOrganization(10, "internal", "Internal", true, true)],
        []);

    [Fact]
    public void Renders_AllRowsReturnedByQueryService()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(configureServices: services =>
        {
            RegisterQueryService(services, Alice, Bob, InactiveCarol);
        });

        var cut = ctx.RenderComponent<UsersMasterGrid>();

        cut.Markup.Should().Contain("3 of 3 users");
        cut.Find("[data-testid='users-row-1-role-SystemAdmin']").Should().NotBeNull();
        cut.Find("[data-testid='users-row-2-role-Customer']").Should().NotBeNull();
        cut.Find("[data-testid='users-row-3-no-roles']").Should().NotBeNull();
    }

    [Fact]
    public void Filter_ByEmail_NarrowsVisibleRows()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(configureServices: services =>
        {
            RegisterQueryService(services, Alice, Bob);
        });

        var cut = ctx.RenderComponent<UsersMasterGrid>();
        var filterPanel = cut.FindComponent<FilterPanel<AdminUserRow>>();

        cut.InvokeAsync(() =>
            filterPanel.Instance.Coordinator.SetFilter(
                0,
                nameof(AdminUserRow.Email),
                FilterOperators.Contains,
                "alice"));

        cut.Markup.Should().Contain("1 of 2 users");
        cut.Find("[data-testid='users-row-1-role-SystemAdmin']").Should().NotBeNull();
    }

    [Fact]
    public void Filter_ByRole_UsesAnyRoleContains()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(configureServices: services =>
        {
            RegisterQueryService(services, Alice, Bob, InactiveCarol);
        });

        var cut = ctx.RenderComponent<UsersMasterGrid>();
        var filterPanel = cut.FindComponent<FilterPanel<AdminUserRow>>();

        cut.InvokeAsync(() =>
            filterPanel.Instance.Coordinator.SetFilter(
                0,
                "ActiveRole",
                FilterOperators.Contains,
                "Customer"));

        cut.Markup.Should().Contain("1 of 3 users");
    }

    [Fact]
    public async Task RefreshWithSelectedId_RaisesSelectedUserChangedWithFreshRow()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(configureServices: services =>
        {
            RegisterQueryService(services, Alice);
        });

        AdminUserRow? received = null;
        var cut = ctx.RenderComponent<UsersMasterGrid>(parameters => parameters
            .Add(x => x.SelectedUserChanged, row => received = row));

        await cut.InvokeAsync(() => cut.Instance.RefreshAsync(Alice.UserId));

        received.Should().NotBeNull();
        received!.UserId.Should().Be(Alice.UserId);
    }

    [Fact]
    public void RefreshButton_ReloadsFromQueryService()
    {
        using var ctx = new TestContext();
        var queryMock = new Mock<IAdminUserQueryService>();
        queryMock.SetupSequence(q => q.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([Alice])
            .ReturnsAsync([Alice, Bob]);

        queryMock.Setup(q => q.GetActorActiveAdminRole()).Returns("SystemAdmin");
        queryMock.Setup(q => q.GetActorUserId()).Returns("99");
        queryMock.Setup(q => q.GetActorAssignedOrganizationIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());

        ctx.SetupForDabbler(configureServices: services =>
        {
            services.AddSingleton(queryMock.Object);
        });

        var cut = ctx.RenderComponent<UsersMasterGrid>();
        cut.Markup.Should().Contain("1 of 1 users");

        cut.Find("[data-testid='users-grid-refresh-button']").Click();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("2 of 2 users"));
    }

    [Fact]
    public void FilterChange_RaisesFiltersChanged_AndKeepsScrollRegionScopedToGrid()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(configureServices: services =>
        {
            RegisterQueryService(services, Alice, Bob, InactiveCarol);
        });

        var filterChangedCount = 0;
        var cut = ctx.RenderComponent<UsersMasterGrid>(parameters => parameters
            .Add(x => x.FiltersChanged, () =>
            {
                filterChangedCount++;
                return Task.CompletedTask;
            }));

        var filterPanel = cut.FindComponent<FilterPanel<AdminUserRow>>();
        cut.InvokeAsync(() =>
            filterPanel.Instance.Coordinator.SetFilter(
                0,
                nameof(AdminUserRow.Email),
                FilterOperators.Contains,
                "alice"));

        cut.WaitForAssertion(() => filterChangedCount.Should().Be(1));
        cut.Find("[data-testid='users-master-grid-scroll-region']").Should().NotBeNull();
    }

    private static void RegisterQueryService(IServiceCollection services, params AdminUserRow[] rows)
    {
        var mock = new Mock<IAdminUserQueryService>();
        mock.Setup(q => q.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);
        mock.Setup(q => q.GetActorActiveAdminRole()).Returns("SystemAdmin");
        mock.Setup(q => q.GetActorUserId()).Returns("99");
        mock.Setup(q => q.GetActorAssignedOrganizationIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());
        services.AddSingleton(mock.Object);
    }
}
