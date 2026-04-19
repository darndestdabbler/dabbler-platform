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
