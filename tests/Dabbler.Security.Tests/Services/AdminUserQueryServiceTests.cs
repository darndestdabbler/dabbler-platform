using System.Security.Claims;
using Dabbler.Security.Authorization;
using Dabbler.Security.Data;
using Dabbler.Security.Entities;
using Dabbler.Security.Registration;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Dabbler.Security.Tests.Services;

/// <summary>
/// Integration coverage for <see cref="AdminUserQueryService"/> against a
/// real <see cref="DabblerSecurityDbContext"/> backed by EF Core in-memory.
/// Exercises each baked-in actor's visibility (SystemAdmin and
/// CrossOrgUserAdmin/CrossOrgMonitor see all; UserAdmin scoped to shared
/// orgs; non-admin returns empty) plus the verifier-flagged scenarios where
/// UserAdmin must not learn other org memberships of users in their scope.
/// </summary>
public class AdminUserQueryServiceTests
{
    private const int InternalOrgId = 1;
    private const int RetailOrg1Id = 2;
    private const int RetailOrg2Id = 3;

    private const int SystemAdminUserId = 100;
    private const int UserAdminScopedToInternalUserId = 101;
    private const int CrossOrgMonitorUserId = 102;
    private const int InternalStaffUserId = 200;
    private const int RetailContact1UserId = 201;
    private const int RetailContact2UserId = 202;

    [Fact]
    public async Task SystemAdmin_SeesAllUsersAndAllOrganizations()
    {
        var harness = BuildSeededHarness();
        var sut = BuildSut(harness, SystemAdminUserId, BakedInRoles.SystemAdmin);

        var rows = await sut.GetUsersAsync();

        rows.Select(r => r.UserId).Should().BeEquivalentTo(
            SystemAdminUserId.ToString(),
            UserAdminScopedToInternalUserId.ToString(),
            CrossOrgMonitorUserId.ToString(),
            InternalStaffUserId.ToString(),
            RetailContact1UserId.ToString(),
            RetailContact2UserId.ToString());

        var retailContact1 = rows.Single(r => r.UserId == RetailContact1UserId.ToString());
        retailContact1.Organizations.Should().ContainSingle()
            .Which.OrganizationId.Should().Be(RetailOrg1Id);
    }

    [Fact]
    public async Task CrossOrgMonitor_SeesAllUsersDespiteNoAssignmentPrivilege()
    {
        // Verifier flagged: CrossOrgMonitor has CanSeeAllOrganizations but
        // not CanAssignUsersToRoles. The earlier implementation only matched
        // on CanAssignUsersToRoles and returned null for CrossOrgMonitor,
        // forcing the service into the shared-org branch. This regression
        // would limit Monitor's visibility, contradicting its compiled
        // privilege profile.
        var harness = BuildSeededHarness();
        var sut = BuildSut(harness, CrossOrgMonitorUserId, BakedInRoles.CrossOrgMonitor);

        var rows = await sut.GetUsersAsync();

        rows.Should().HaveCount(6);
        rows.Should().Contain(r => r.UserId == RetailContact2UserId.ToString());
    }

    [Fact]
    public async Task UserAdmin_OnlySeesUsersInSharedOrgsAndOnlyTheirSharedOrganizationMemberships()
    {
        // UserAdmin is a member of the Internal org only. Visible users must
        // share that org. The verifier flagged the org-projection leak: even
        // for the visible InternalStaff row, only the Internal org membership
        // should appear; their other orgs (none here for the current user, but
        // we add a multi-org case below) must not surface.
        var harness = BuildSeededHarness(seed =>
        {
            // Give the InternalStaff user a second membership in RetailOrg1
            // so we can prove the projection narrows to the actor's scope.
            seed.AppUserOrganizationMemberships.Add(new AppUserOrganizationMembership
            {
                UserId = InternalStaffUserId,
                OrganizationId = RetailOrg1Id,
                IsDefault = false,
                IsActive = true,
                JoinedAt = DateTime.UtcNow,
            });
        });

        var sut = BuildSut(harness, UserAdminScopedToInternalUserId, BakedInRoles.UserAdmin);
        var rows = await sut.GetUsersAsync();

        rows.Select(r => r.UserId).Should().BeEquivalentTo(
            SystemAdminUserId.ToString(),
            UserAdminScopedToInternalUserId.ToString(),
            CrossOrgMonitorUserId.ToString(),
            InternalStaffUserId.ToString());

        var staffRow = rows.Single(r => r.UserId == InternalStaffUserId.ToString());
        staffRow.Organizations.Should().ContainSingle()
            .Which.OrganizationId.Should().Be(InternalOrgId);
    }

    [Fact]
    public async Task NonAdminUser_GetsEmptyList_FailClosed()
    {
        // Verifier flagged: a user with no admin role but with org
        // memberships used to fall into the shared-org branch instead of
        // being rejected. The fix is fail-closed.
        var harness = BuildSeededHarness();
        var sut = BuildSut(harness, RetailContact1UserId, BakedInRoles.User);

        var rows = await sut.GetUsersAsync();

        rows.Should().BeEmpty();
    }

    [Fact]
    public async Task UnauthenticatedActor_GetsEmptyList()
    {
        var harness = BuildSeededHarness();
        var sut = BuildSut(harness, actorUserId: null, activeRoleName: null);

        var rows = await sut.GetUsersAsync();

        rows.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActorAssignedOrganizationIdsAsync_ReturnsActorMembershipIds()
    {
        var harness = BuildSeededHarness();
        var sut = BuildSut(harness, UserAdminScopedToInternalUserId, BakedInRoles.UserAdmin);

        var ids = await sut.GetActorAssignedOrganizationIdsAsync();

        ids.Should().BeEquivalentTo(new[] { InternalOrgId });
    }

    [Fact]
    public async Task EachPublicMethodResolvesItsOwnDbContext()
    {
        // Structural regression guard for the UAT-remediation Session 2
        // refactor. The original crash came from sibling Blazor components
        // on the Users tab awaiting a single shared scoped DbContext in
        // parallel. The fix is factory injection: each public method
        // resolves its own context from IDbContextFactory so calls cannot
        // collide.
        //
        // A behavioural concurrent-call test against the EF InMemory
        // provider would be a placebo because InMemory completes queries
        // synchronously and never genuinely overlaps. Asserting that the
        // factory is actually invoked by each public method proves the
        // wiring is in place regardless of the provider's scheduling.
        var counting = new CountingDbContextFactory(BuildSeededHarness().DbFactory);
        var sut = BuildSut(new SeededHarness(counting), SystemAdminUserId, BakedInRoles.SystemAdmin);

        await sut.GetUsersAsync();
        counting.CreateCount.Should().Be(1, "GetUsersAsync must resolve a single context via the factory.");

        await sut.GetActorAssignedOrganizationIdsAsync();
        counting.CreateCount.Should().Be(2, "GetActorAssignedOrganizationIdsAsync must resolve its own context, not reuse any shared instance.");
    }

    private sealed class CountingDbContextFactory : IDbContextFactory<DabblerSecurityDbContext>
    {
        private readonly IDbContextFactory<DabblerSecurityDbContext> _inner;

        public CountingDbContextFactory(IDbContextFactory<DabblerSecurityDbContext> inner)
        {
            _inner = inner;
        }

        public int CreateCount { get; private set; }

        public DabblerSecurityDbContext CreateDbContext()
        {
            CreateCount++;
            return _inner.CreateDbContext();
        }
    }

    private static SeededHarness BuildSeededHarness(Action<DabblerSecurityDbContext>? extraSeed = null)
    {
        var options = new DbContextOptionsBuilder<DabblerSecurityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using var seedDb = new DabblerSecurityDbContext(options);

        var now = DateTime.UtcNow;
        seedDb.AppOrganizations.AddRange(
            new AppOrganization { Id = InternalOrgId, Key = "internal", Name = "Internal Operations", IsInternal = true, IsActive = true, CreatedAt = now },
            new AppOrganization { Id = RetailOrg1Id, Key = "customer-101", Name = "Retail Business 101", IsInternal = false, IsActive = true, CreatedAt = now },
            new AppOrganization { Id = RetailOrg2Id, Key = "customer-102", Name = "Retail Business 102", IsInternal = false, IsActive = true, CreatedAt = now });

        seedDb.AppUsers.AddRange(
            new AppUser { Id = SystemAdminUserId, Email = "sysadmin@test.local", DisplayName = "Sys Admin", IsActive = true, CreatedAt = now },
            new AppUser { Id = UserAdminScopedToInternalUserId, Email = "useradmin@test.local", DisplayName = "User Admin", IsActive = true, CreatedAt = now },
            new AppUser { Id = CrossOrgMonitorUserId, Email = "monitor@test.local", DisplayName = "Cross Org Monitor", IsActive = true, CreatedAt = now },
            new AppUser { Id = InternalStaffUserId, Email = "staff@test.local", DisplayName = "Internal Staff", IsActive = true, CreatedAt = now },
            new AppUser { Id = RetailContact1UserId, Email = "contact1@test.local", DisplayName = "Retail Contact 1", IsActive = true, CreatedAt = now },
            new AppUser { Id = RetailContact2UserId, Email = "contact2@test.local", DisplayName = "Retail Contact 2", IsActive = true, CreatedAt = now });

        seedDb.AppUserOrganizationMemberships.AddRange(
            new AppUserOrganizationMembership { UserId = SystemAdminUserId, OrganizationId = InternalOrgId, IsDefault = true, IsActive = true, JoinedAt = now },
            new AppUserOrganizationMembership { UserId = UserAdminScopedToInternalUserId, OrganizationId = InternalOrgId, IsDefault = true, IsActive = true, JoinedAt = now },
            new AppUserOrganizationMembership { UserId = CrossOrgMonitorUserId, OrganizationId = InternalOrgId, IsDefault = true, IsActive = true, JoinedAt = now },
            new AppUserOrganizationMembership { UserId = InternalStaffUserId, OrganizationId = InternalOrgId, IsDefault = true, IsActive = true, JoinedAt = now },
            new AppUserOrganizationMembership { UserId = RetailContact1UserId, OrganizationId = RetailOrg1Id, IsDefault = true, IsActive = true, JoinedAt = now },
            new AppUserOrganizationMembership { UserId = RetailContact2UserId, OrganizationId = RetailOrg2Id, IsDefault = true, IsActive = true, JoinedAt = now });

        seedDb.SaveChanges();
        extraSeed?.Invoke(seedDb);
        seedDb.SaveChanges();

        return new SeededHarness(new InMemoryDbContextFactory(options));
    }

    private static AdminUserQueryService BuildSut(SeededHarness harness, int? actorUserId, string? activeRoleName)
    {
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        if (actorUserId.HasValue)
        {
            var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, actorUserId.Value.ToString()),
                    new Claim(ClaimTypes.Role, activeRoleName ?? string.Empty),
                ],
                authenticationType: "TestScheme");
            var principal = new ClaimsPrincipal(identity);
            var ctx = new DefaultHttpContext { User = principal };
            httpContextAccessor.Setup(a => a.HttpContext).Returns(ctx);
        }
        else
        {
            httpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        }

        var roleSwitcher = new Mock<IRoleSwitcher>();
        roleSwitcher.Setup(s => s.EffectiveRoles(It.IsAny<ClaimsPrincipal>()))
            .Returns(activeRoleName is null ? [] : new[] { activeRoleName });

        var bakedInRoleProvider = new PlatformBakedInRoleProvider();

        var userRoleStore = new Mock<IUserRoleStore>();
        userRoleStore.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, IReadOnlyList<string>>());

        return new AdminUserQueryService(
            httpContextAccessor.Object,
            roleSwitcher.Object,
            bakedInRoleProvider,
            userRoleStore.Object,
            harness.DbFactory);
    }

    private sealed record SeededHarness(IDbContextFactory<DabblerSecurityDbContext> DbFactory);

    private sealed class InMemoryDbContextFactory : IDbContextFactory<DabblerSecurityDbContext>
    {
        private readonly DbContextOptions<DabblerSecurityDbContext> _options;

        public InMemoryDbContextFactory(DbContextOptions<DabblerSecurityDbContext> options)
        {
            _options = options;
        }

        public DabblerSecurityDbContext CreateDbContext() => new(_options);
    }
}
