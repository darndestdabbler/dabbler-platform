using System.Security.Claims;
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
    public async Task Renders_AllRowsReturnedByQueryService()
    {
        await using var ctx = new BunitContext();
        ctx.SetupForDabbler(configureServices: services =>
        {
            RegisterQueryService(services, Alice, Bob, InactiveCarol);
        });

        var cut = ctx.Render<UsersMasterGrid>();

        cut.Markup.Should().Contain("3 of 3 users");
        cut.Find("[data-testid='users-row-1-role-SystemAdmin']").Should().NotBeNull();
        cut.Find("[data-testid='users-row-2-role-Customer']").Should().NotBeNull();
        cut.Find("[data-testid='users-row-3-no-roles']").Should().NotBeNull();
    }

    [Fact]
    public async Task Filter_ByEmail_NarrowsVisibleRows()
    {
        await using var ctx = new BunitContext();
        ctx.SetupForDabbler(configureServices: services =>
        {
            RegisterQueryService(services, Alice, Bob);
        });

        var cut = ctx.Render<UsersMasterGrid>();
        var filterPanel = cut.FindComponent<FilterPanel<AdminUserRow>>();

        _ = cut.InvokeAsync(() =>
            filterPanel.Instance.Coordinator.SetFilter(
                0,
                nameof(AdminUserRow.Email),
                FilterOperators.Contains,
                "alice"));

        cut.Markup.Should().Contain("1 of 2 users");
        cut.Find("[data-testid='users-row-1-role-SystemAdmin']").Should().NotBeNull();
    }

    [Fact]
    public async Task Filter_ByRole_UsesAnyRoleContains()
    {
        await using var ctx = new BunitContext();
        ctx.SetupForDabbler(configureServices: services =>
        {
            RegisterQueryService(services, Alice, Bob, InactiveCarol);
        });

        var cut = ctx.Render<UsersMasterGrid>();
        var filterPanel = cut.FindComponent<FilterPanel<AdminUserRow>>();

        _ = cut.InvokeAsync(() =>
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
        await using var ctx = new BunitContext();
        ctx.SetupForDabbler(configureServices: services =>
        {
            RegisterQueryService(services, Alice);
        });

        AdminUserRow? received = null;
        var cut = ctx.Render<UsersMasterGrid>(parameters => parameters
            .Add(x => x.SelectedUserChanged, row => received = row));

        await cut.InvokeAsync(() => cut.Instance.RefreshAsync(Alice.UserId));

        received.Should().NotBeNull();
        received!.UserId.Should().Be(Alice.UserId);
    }

    [Fact]
    public async Task RefreshButton_ReloadsFromQueryService()
    {
        await using var ctx = new BunitContext();
        var queryMock = new Mock<IAdminUserQueryService>();
        queryMock.SetupSequence(q => q.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([Alice])
            .ReturnsAsync([Alice, Bob]);
        // Mirror onto the ClaimsPrincipal overload (the one components actually call).
        queryMock.SetupSequence(q => q.GetUsersAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([Alice])
            .ReturnsAsync([Alice, Bob]);

        queryMock.Setup(q => q.GetActorActiveAdminRole()).Returns("SystemAdmin");
        queryMock.Setup(q => q.GetActorUserId()).Returns("99");
        queryMock.Setup(q => q.GetActorAssignedOrganizationIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());
        queryMock.MirrorActorOverloads("SystemAdmin", "99", new HashSet<int>());

        ctx.SetupForDabbler(configureServices: services =>
        {
            services.AddSingleton(queryMock.Object);
        });

        var cut = ctx.Render<UsersMasterGrid>();
        cut.Markup.Should().Contain("1 of 1 users");

        cut.Find("[data-testid='users-grid-refresh-button']").Click();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("2 of 2 users"));
    }

    [Fact]
    public async Task FilterChange_RaisesFiltersChanged_AndKeepsScrollRegionScopedToGrid()
    {
        await using var ctx = new BunitContext();
        ctx.SetupForDabbler(configureServices: services =>
        {
            RegisterQueryService(services, Alice, Bob, InactiveCarol);
        });

        var filterChangedCount = 0;
        var cut = ctx.Render<UsersMasterGrid>(parameters => parameters
            .Add(x => x.FiltersChanged, () =>
            {
                filterChangedCount++;
                return Task.CompletedTask;
            }));

        var filterPanel = cut.FindComponent<FilterPanel<AdminUserRow>>();
        _ = cut.InvokeAsync(() =>
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
        mock.MirrorActorOverloads("SystemAdmin", "99", new HashSet<int>(), rows);
        services.AddSingleton(mock.Object);
    }
}
