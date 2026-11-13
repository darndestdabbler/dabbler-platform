using Bunit;
using Dabbler.Security.Blazor.Components.Admin.Organization;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Data;
using Dabbler.Security.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dabbler.Security.Blazor.Tests.Components.Admin.Organization;

public class OrganizationDetailsFormTests
{
    [Fact]
    public async Task DetailsForm_SaveNewOrganization_PersistsRow()
    {
        await using var ctx = new BunitContext();
        DbContextOptions<DabblerSecurityDbContext>? dbOptions = null;

        ctx.SetupForDabbler(configureServices: services =>
        {
            dbOptions = RegisterDbContext(services);
        });

        var cut = ctx.Render<OrganizationDetailsForm>(parameters => parameters
            .Add(x => x.Selection, OrganizationDetailsForm.OrganizationSelection.CreateNew()));

        cut.Find("[data-testid='organization-key-field'] input").Change("customer-202");
        cut.Find("[data-testid='organization-name-field'] input").Change("Retail 202");
        cut.Find("[data-testid='organization-save-button']").Click();

        cut.WaitForAssertion(() =>
        {
            using var verifyDb = new DabblerSecurityDbContext(dbOptions!);
            verifyDb.AppOrganizations.Any(organization => organization.Key == "customer-202").Should().BeTrue();
        });
    }

    [Fact]
    public async Task DetailsForm_ArchiveExistingOrganization_MarksRowInactive()
    {
        await using var ctx = new BunitContext();
        DbContextOptions<DabblerSecurityDbContext>? dbOptions = null;

        ctx.SetupForDabbler(configureServices: services =>
        {
            dbOptions = RegisterDbContext(services, db =>
            {
                db.AppOrganizations.Add(new AppOrganization
                {
                    Id = 5,
                    Key = "customer-303",
                    Name = "Retail 303",
                    IsInternal = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                });
            });
        });

        var cut = ctx.Render<OrganizationDetailsForm>(parameters => parameters
            .Add(x => x.Selection, OrganizationDetailsForm.OrganizationSelection.ForExisting(5)));

        cut.Find("[data-testid='organization-archive-button']").Click();

        cut.WaitForAssertion(() =>
        {
            using var verifyDb = new DabblerSecurityDbContext(dbOptions!);
            verifyDb.AppOrganizations.Single(organization => organization.Id == 5).IsActive.Should().BeFalse();
        });
    }

    [Fact]
    public async Task DetailsForm_EditExistingOrganization_PersistsChanges()
    {
        await using var ctx = new BunitContext();
        DbContextOptions<DabblerSecurityDbContext>? dbOptions = null;

        ctx.SetupForDabbler(configureServices: services =>
        {
            dbOptions = RegisterDbContext(services, db =>
            {
                db.AppOrganizations.Add(new AppOrganization
                {
                    Id = 9,
                    Key = "customer-404",
                    Name = "Original Retail 404",
                    IsInternal = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                });
            });
        });

        var cut = ctx.Render<OrganizationDetailsForm>(parameters => parameters
            .Add(x => x.Selection, OrganizationDetailsForm.OrganizationSelection.ForExisting(9)));

        cut.Find("[data-testid='organization-name-field'] input").Change("Updated Retail 404");
        cut.Find("[data-testid='organization-is-internal-field'] input").Change(true);
        cut.Find("[data-testid='organization-save-button']").Click();

        cut.WaitForAssertion(() =>
        {
            using var verifyDb = new DabblerSecurityDbContext(dbOptions!);
            var organization = verifyDb.AppOrganizations.Single(candidate => candidate.Id == 9);
            organization.Name.Should().Be("Updated Retail 404");
            organization.IsInternal.Should().BeTrue();
        });
    }

    [Fact]
    public async Task DetailsForm_DuplicateKey_WithDifferentCasing_DoesNotPersistSecondRow()
    {
        await using var ctx = new BunitContext();
        DbContextOptions<DabblerSecurityDbContext>? dbOptions = null;

        ctx.SetupForDabbler(configureServices: services =>
        {
            dbOptions = RegisterDbContext(services, db =>
            {
                db.AppOrganizations.Add(new AppOrganization
                {
                    Id = 11,
                    Key = "existing-key",
                    Name = "Existing Retail",
                    IsInternal = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                });
            });
        });

        var cut = ctx.Render<OrganizationDetailsForm>(parameters => parameters
            .Add(x => x.Selection, OrganizationDetailsForm.OrganizationSelection.CreateNew()));

        cut.Find("[data-testid='organization-key-field'] input").Change("Existing-Key");
        cut.Find("[data-testid='organization-name-field'] input").Change("Duplicate Retail");
        cut.Find("[data-testid='organization-save-button']").Click();

        cut.WaitForAssertion(() =>
        {
            using var verifyDb = new DabblerSecurityDbContext(dbOptions!);
            verifyDb.AppOrganizations.Count(organization => organization.Key == "existing-key").Should().Be(1);
        });
    }

    private static DbContextOptions<DabblerSecurityDbContext> RegisterDbContext(
        IServiceCollection services,
        Action<DabblerSecurityDbContext>? seed = null)
    {
        var options = new DbContextOptionsBuilder<DabblerSecurityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using (var seedDb = new DabblerSecurityDbContext(options))
        {
            seed?.Invoke(seedDb);
            seedDb.SaveChanges();
        }

        services.AddScoped(_ => new DabblerSecurityDbContext(options));
        services.AddSingleton<IDbContextFactory<DabblerSecurityDbContext>>(
            new TestDbContextFactory(options));
        return options;
    }

    private sealed class TestDbContextFactory : IDbContextFactory<DabblerSecurityDbContext>
    {
        private readonly DbContextOptions<DabblerSecurityDbContext> _options;

        public TestDbContextFactory(DbContextOptions<DabblerSecurityDbContext> options)
            => _options = options;

        public DabblerSecurityDbContext CreateDbContext() => new(_options);
    }
}
