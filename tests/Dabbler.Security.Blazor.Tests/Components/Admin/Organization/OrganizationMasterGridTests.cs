using Bunit;
using Dabbler.Components;
using Dabbler.Components.Data;
using Dabbler.Security.Blazor.Components.Admin.Organization;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Data;
using Dabbler.Security.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dabbler.Security.Blazor.Tests.Components.Admin.Organization;

public class OrganizationMasterGridTests
{
    [Fact]
    public void Grid_RendersOrganizationsAndMembershipCounts()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(configureServices: services =>
        {
            RegisterDbContext(services, SeedOrganizations);
        });

        var cut = ctx.RenderComponent<OrganizationMasterGrid>();

        cut.Find("[data-testid='organization-row-1-key']").TextContent.Should().Contain("internal");
        cut.Find("[data-testid='organization-row-2-key']").TextContent.Should().Contain("customer-101");
        cut.Markup.Should().Contain("2 of 2 organizations");
        cut.Find("[data-testid='organization-row-1-membership-count']").TextContent.Should().Be("1");
    }

    [Fact]
    public void Grid_FilterByActiveStatus_NarrowsVisibleRows()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(configureServices: services =>
        {
            RegisterDbContext(services, SeedOrganizations);
        });

        var cut = ctx.RenderComponent<OrganizationMasterGrid>();
        var filterPanel = cut.FindComponent<FilterPanel<OrganizationMasterGrid.OrganizationRow>>();

        cut.InvokeAsync(() =>
            filterPanel.Instance.Coordinator.SetFilter(
                0,
                nameof(OrganizationMasterGrid.OrganizationRow.IsActive),
                FilterOperators.Equals,
                false));

        cut.Markup.Should().Contain("1 of 2 organizations");
        cut.Find("[data-testid='organization-row-2-key']").TextContent.Should().Contain("customer-101");
        var activeRow = () => cut.Find("[data-testid='organization-row-1-key']");
        activeRow.Should().Throw<Bunit.ElementNotFoundException>();
    }

    [Theory]
    [InlineData("cust", 1)]            // simple prefix match on "customer-101"
    [InlineData("customer-", 1)]       // full prefix WITH a dash must still match
    [InlineData("mer-1", 1)]           // substring spanning a dash must match
    [InlineData("cust-", 0)]           // literal "cust-" is not a substring of "customer-101"
    public void Grid_FilterByKeyContains_HandlesDashCharacter(string needle, int expectedVisibleCount)
    {
        // Session 4 of security-dashboard-uat-remediation — Finding 3 sub-issue:
        // UAT flagged that filter `Key contains 'cust-'` returned nothing while
        // `cust` did. The intended test here is that Contains is literal and
        // dash-aware, not that "cust-" is treated as a prefix. The seed is
        // "customer-101" so "cust-" is correctly absent; "customer-" and
        // "mer-1" both span the dash and must match.
        using var ctx = new TestContext();
        ctx.SetupForDabbler(configureServices: services =>
        {
            RegisterDbContext(services, SeedOrganizations);
        });

        var cut = ctx.RenderComponent<OrganizationMasterGrid>();
        var filterPanel = cut.FindComponent<FilterPanel<OrganizationMasterGrid.OrganizationRow>>();

        cut.InvokeAsync(() =>
            filterPanel.Instance.Coordinator.SetFilter(
                0,
                nameof(OrganizationMasterGrid.OrganizationRow.Key),
                FilterOperators.Contains,
                needle));

        cut.Markup.Should().Contain($"{expectedVisibleCount} of 2 organizations");
    }

    [Fact]
    public void Grid_NewOrganizationButton_RaisesCreateRequested()
    {
        using var ctx = new TestContext();
        var createRequested = false;

        ctx.SetupForDabbler(configureServices: services =>
        {
            RegisterDbContext(services, SeedOrganizations);
        });

        var cut = ctx.RenderComponent<OrganizationMasterGrid>(parameters => parameters
            .Add(x => x.CreateRequested, () => createRequested = true));

        cut.Find("[data-testid='organization-grid-add-button']").Click();

        createRequested.Should().BeTrue();
    }

    private static void RegisterDbContext(
        IServiceCollection services,
        Action<DabblerSecurityDbContext> seed)
    {
        var options = new DbContextOptionsBuilder<DabblerSecurityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using (var db = new DabblerSecurityDbContext(options))
        {
            seed(db);
            db.SaveChanges();
        }

        services.AddScoped(_ => new DabblerSecurityDbContext(options));
        services.AddSingleton<IDbContextFactory<DabblerSecurityDbContext>>(
            new TestDbContextFactory(options));
    }

    private sealed class TestDbContextFactory : IDbContextFactory<DabblerSecurityDbContext>
    {
        private readonly DbContextOptions<DabblerSecurityDbContext> _options;

        public TestDbContextFactory(DbContextOptions<DabblerSecurityDbContext> options)
            => _options = options;

        public DabblerSecurityDbContext CreateDbContext() => new(_options);
    }

    private static void SeedOrganizations(DabblerSecurityDbContext db)
    {
        db.AppOrganizations.AddRange(
            new AppOrganization
            {
                Id = 1,
                Key = "internal",
                Name = "Internal Operations",
                IsInternal = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc),
            },
            new AppOrganization
            {
                Id = 2,
                Key = "customer-101",
                Name = "Retail 101",
                IsInternal = false,
                IsActive = false,
                CreatedAt = new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc),
            });

        db.AppUsers.Add(new AppUser
        {
            Id = 7,
            Email = "org.user@test.local",
            DisplayName = "Org User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });

        db.AppUserOrganizationMemberships.Add(new AppUserOrganizationMembership
        {
            Id = 12,
            UserId = 7,
            OrganizationId = 1,
            IsDefault = true,
            IsActive = true,
            JoinedAt = DateTime.UtcNow,
        });
    }
}
