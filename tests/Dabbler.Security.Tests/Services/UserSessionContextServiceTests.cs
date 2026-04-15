using System.Security.Claims;
using Dabbler.Security.Data;
using Dabbler.Security.Entities;
using Dabbler.Security.Options;
using Dabbler.Security.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dabbler.Security.Tests.Services;

public sealed class UserSessionContextServiceTests : IDisposable
{
    private readonly DabblerSecurityDbContext _db;
    private readonly UserSessionContextService _service;

    public UserSessionContextServiceTests()
    {
        var options = new DbContextOptionsBuilder<DabblerSecurityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new DabblerSecurityDbContext(options);
        _service = new UserSessionContextService(
            _db,
            Microsoft.Extensions.Options.Options.Create(new DabblerSecurityOptions
            {
                DefaultOrganizationKey = "internal",
                DefaultOrganizationName = "Internal Operations"
            }));
    }

    [Fact]
    public async Task BuildAsync_UsesDefaultOrganizationWhenNoOverrideIsRequested()
    {
        var (user, internalOrganization, retailOrganization) = await SeedDualMembershipUserAsync();

        var sessionContext = await _service.BuildAsync(user.Id);

        sessionContext.Should().NotBeNull();
        sessionContext!.ActiveOrganizationId.Should().Be(internalOrganization.Id);
        sessionContext.ActiveOrganizationKey.Should().Be("internal");
        sessionContext.ActiveCustomerId.Should().BeNull();
        sessionContext.EffectiveRoles.Should().ContainSingle().Which.Should().Be("BackofficeUser");
        sessionContext.OrganizationMemberships.Should().HaveCount(2);
        sessionContext.OrganizationMemberships.Should().Contain(organization => organization.OrganizationId == retailOrganization.Id);
    }

    [Fact]
    public async Task BuildAsync_UsesRequestedRetailOrganizationAndBridgesCustomerId()
    {
        var (user, _, retailOrganization) = await SeedDualMembershipUserAsync();

        var sessionContext = await _service.BuildAsync(user.Id, retailOrganization.Id);
        var principal = await _service.CreatePrincipalAsync(user.Id, requestedOrganizationId: retailOrganization.Id);

        sessionContext.Should().NotBeNull();
        sessionContext!.ActiveOrganizationId.Should().Be(retailOrganization.Id);
        sessionContext.ActiveOrganizationKey.Should().Be("customer-42");
        sessionContext.ActiveCustomerId.Should().Be(42);
        sessionContext.EffectiveRoles.Should().ContainSingle().Which.Should().Be("Customer");

        principal.Should().NotBeNull();
        principal!.FindAll(ClaimTypes.Role).Select(claim => claim.Value).Should().BeEquivalentTo(["Customer"]);
        principal.FindFirstValue("CustomerId").Should().Be("42");
        principal.FindFirstValue(UserSessionContextConstants.ActiveOrganizationKeyClaimType).Should().Be("customer-42");
    }

    [Fact]
    public async Task BuildAsync_SystemAdminCanTargetAnOrganizationWithoutMembership()
    {
        var systemAdminRole = new AppRole { Id = 1, Name = "SystemAdmin", IsSystemRole = true, CreatedAt = DateTime.UtcNow };
        var internalOrganization = new AppOrganization
        {
            Id = 1,
            Key = "internal",
            Name = "Internal Operations",
            IsInternal = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var retailOrganization = new AppOrganization
        {
            Id = 2,
            Key = "customer-77",
            Name = "Retail Business 077",
            IsInternal = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var user = new AppUser
        {
            Id = 11,
            Email = "admin@test.local",
            DisplayName = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.AppRoles.Add(systemAdminRole);
        _db.AppOrganizations.AddRange(internalOrganization, retailOrganization);
        _db.AppUsers.Add(user);
        _db.AppUserOrganizationMemberships.Add(new AppUserOrganizationMembership
        {
            Id = 5,
            UserId = user.Id,
            OrganizationId = internalOrganization.Id,
            IsDefault = true,
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        });
        _db.AppUserRoles.Add(new AppUserRole
        {
            UserId = user.Id,
            RoleId = systemAdminRole.Id,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = user.Id
        });
        await _db.SaveChangesAsync();

        var sessionContext = await _service.BuildAsync(user.Id, retailOrganization.Id);

        sessionContext.Should().NotBeNull();
        sessionContext!.ActiveOrganizationId.Should().Be(retailOrganization.Id);
        sessionContext.ActiveOrganizationMembershipId.Should().BeNull();
        sessionContext.ActiveCustomerId.Should().Be(77);
        sessionContext.EffectiveRoles.Should().Contain("SystemAdmin");
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task<(AppUser User, AppOrganization InternalOrganization, AppOrganization RetailOrganization)> SeedDualMembershipUserAsync()
    {
        var internalOrganization = new AppOrganization
        {
            Id = 1,
            Key = "internal",
            Name = "Internal Operations",
            IsInternal = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var retailOrganization = new AppOrganization
        {
            Id = 2,
            Key = "customer-42",
            Name = "Retail Business 042",
            IsInternal = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var internalRole = new AppRole { Id = 1, Name = "BackofficeUser", CreatedAt = DateTime.UtcNow, IsSystemRole = true };
        var retailRole = new AppRole { Id = 2, Name = "Customer", CreatedAt = DateTime.UtcNow, IsSystemRole = true };
        var user = new AppUser
        {
            Id = 7,
            Email = "casey@test.local",
            DisplayName = "Casey Hart",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var internalMembership = new AppUserOrganizationMembership
        {
            Id = 10,
            UserId = user.Id,
            OrganizationId = internalOrganization.Id,
            IsDefault = true,
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        };
        var retailMembership = new AppUserOrganizationMembership
        {
            Id = 11,
            UserId = user.Id,
            OrganizationId = retailOrganization.Id,
            IsDefault = false,
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        };

        _db.AppOrganizations.AddRange(internalOrganization, retailOrganization);
        _db.AppRoles.AddRange(internalRole, retailRole);
        _db.AppUsers.Add(user);
        _db.AppUserOrganizationMemberships.AddRange(internalMembership, retailMembership);
        _db.AppUserRoles.AddRange(
            new AppUserRole
            {
                UserId = user.Id,
                RoleId = internalRole.Id,
                OrganizationMembershipId = internalMembership.Id,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = user.Id
            },
            new AppUserRole
            {
                UserId = user.Id,
                RoleId = retailRole.Id,
                OrganizationMembershipId = retailMembership.Id,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = user.Id
            });

        await _db.SaveChangesAsync();

        return (user, internalOrganization, retailOrganization);
    }
}
