using System.Security.Claims;
using Dabbler.Security.Authorization;
using Dabbler.Security.Data;
using Dabbler.Security.Entities;
using Dabbler.Security.Models;
using Dabbler.Security.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Dabbler.Security.Tests.Services;

/// <summary>
/// Unit coverage for <see cref="BackofficeUserCreationService"/> against a real
/// <see cref="DabblerSecurityDbContext"/> backed by EF Core in-memory.
/// Covers the happy path, each rejection path, and the duplicate-email guard.
/// </summary>
public class BackofficeUserCreationServiceTests
{
    private const int InternalOrgId = 1;
    private const int RetailOrgId = 2;
    private const int SystemAdminUserId = 100;
    private const int UserAdminUserId = 101;
    private const int CrossOrgMonitorUserId = 102;
    private const int BackofficeUserRoleId = 10;
    private const string BackofficeUserRoleName = "BackofficeUser";

    // ─── Happy path ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_SystemAdmin_CreatesUserMembershipRoleAndAuditRow()
    {
        var db = BuildSeededDb();
        var (sut, _) = BuildSut(db, SystemAdminUserId, BakedInRoles.SystemAdmin);

        var request = new CreateBackofficeUserRequest(
            DisplayName: "New Staff",
            Email: "new.staff@test.local",
            InitialRoleName: BackofficeUserRoleName,
            InitialOrganizationId: InternalOrgId);

        var result = await sut.CreateAsync(request, BuildPrincipal(SystemAdminUserId, BakedInRoles.SystemAdmin), "http://localhost/login");

        result.MagicLinkSent.Should().BeTrue();
        result.MagicLinkError.Should().BeNull();
        result.User.Email.Should().Be("new.staff@test.local");
        result.User.IsActive.Should().BeTrue();
        result.User.Roles.Should().ContainSingle(r => r == BackofficeUserRoleName);
        result.User.Organizations.Should().ContainSingle(o => o.OrganizationId == InternalOrgId);

        // Verify DB state
        var savedUser = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == "new.staff@test.local");
        savedUser.Should().NotBeNull();
        savedUser!.IsProvisioned.Should().BeTrue();
        savedUser.CreatedBy.Should().Be(SystemAdminUserId);

        var membership = await db.AppUserOrganizationMemberships
            .FirstOrDefaultAsync(m => m.UserId == savedUser.Id && m.OrganizationId == InternalOrgId);
        membership.Should().NotBeNull();
        membership!.IsDefault.Should().BeTrue();
        membership.JoinedBy.Should().Be(SystemAdminUserId);

        var roleAssignment = await db.AppUserRoles
            .FirstOrDefaultAsync(r => r.UserId == savedUser.Id && r.RoleId == BackofficeUserRoleId);
        roleAssignment.Should().NotBeNull();
        roleAssignment!.AssignedBy.Should().Be(SystemAdminUserId);

        var auditRow = await db.AppSensitiveAccessLogs
            .FirstOrDefaultAsync(a => a.EventKind == SensitiveAccessEventKind.UserCreated);
        auditRow.Should().NotBeNull();
        auditRow!.EntityKey.Should().Be("AppUser");
        auditRow.EntityId.Should().Be(savedUser.Id.ToString());
        auditRow.GranterUserId.Should().Be(SystemAdminUserId);
        auditRow.TargetRoleName.Should().Be(BackofficeUserRoleName);
        auditRow.RoleName.Should().Be(BakedInRoles.SystemAdmin);
    }

    // ─── UserAdmin cross-org rejection ───────────────────────────────────────

    [Fact]
    public async Task CreateAsync_UserAdmin_RejectsOutOfScopeOrg()
    {
        var db = BuildSeededDb();
        var (sut, _) = BuildSut(db, UserAdminUserId, BakedInRoles.UserAdmin);

        // UserAdmin is only a member of InternalOrg; RetailOrg is out of scope.
        var request = new CreateBackofficeUserRequest(
            DisplayName: "Retail User",
            Email: "retail.new@test.local",
            InitialRoleName: BackofficeUserRoleName,
            InitialOrganizationId: RetailOrgId);

        var act = () => sut.CreateAsync(request, BuildPrincipal(UserAdminUserId, BakedInRoles.UserAdmin), "http://localhost/login");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();

        // Nothing written to DB
        var savedUser = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == "retail.new@test.local");
        savedUser.Should().BeNull();
    }

    // ─── CrossOrgMonitor denial ───────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_CrossOrgMonitor_Denied()
    {
        var db = BuildSeededDb();
        var (sut, _) = BuildSut(db, CrossOrgMonitorUserId, BakedInRoles.CrossOrgMonitor);

        var request = new CreateBackofficeUserRequest(
            DisplayName: "Monitor Attempt",
            Email: "monitor.attempt@test.local",
            InitialRoleName: BackofficeUserRoleName,
            InitialOrganizationId: InternalOrgId);

        var act = () => sut.CreateAsync(request, BuildPrincipal(CrossOrgMonitorUserId, BakedInRoles.CrossOrgMonitor), "http://localhost/login");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();

        var savedUser = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == "monitor.attempt@test.local");
        savedUser.Should().BeNull();
    }

    // ─── Duplicate email rejection ────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_DuplicateEmail_ThrowsEmailAlreadyExistsException()
    {
        // EF Core in-memory doesn't enforce unique indexes, so we use a test-double
        // context that injects a realistic DbUpdateException on the first SaveChanges
        // that tries to persist an AppUser — the exact exception SQL Server raises
        // when IX_AppUsers_Email is violated.
        var dbOptions = BuildInMemoryOptions();
        using var seed = new DabblerSecurityDbContext(dbOptions);
        SeedStandardData(seed);
        seed.SaveChanges();

        using var serviceDb = new UniqueEmailThrowingDbContext(dbOptions);
        var (sut, _) = BuildSut(serviceDb, SystemAdminUserId, BakedInRoles.SystemAdmin);

        var request = new CreateBackofficeUserRequest(
            DisplayName: "Duplicate Attempt",
            Email: "duplicate@test.local",
            InitialRoleName: BackofficeUserRoleName,
            InitialOrganizationId: InternalOrgId);

        var act = () => sut.CreateAsync(request, BuildPrincipal(SystemAdminUserId, BakedInRoles.SystemAdmin), "http://localhost/login");

        await act.Should().ThrowAsync<EmailAlreadyExistsException>()
            .Where(ex => ex.Email.Contains("duplicate@test.local"));
    }

    /// <summary>
    /// Test-double DbContext that throws a SQL-Server-style <see cref="DbUpdateException"/>
    /// for IX_AppUsers_Email on the first call that attempts to save a new AppUser,
    /// simulating the constraint violation the real database would produce.
    /// </summary>
    private sealed class UniqueEmailThrowingDbContext(DbContextOptions<DabblerSecurityDbContext> options)
        : DabblerSecurityDbContext(options)
    {
        private bool _hasThrown;

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (!_hasThrown && ChangeTracker.Entries<AppUser>().Any(e => e.State == EntityState.Added))
            {
                _hasThrown = true;
                throw new DbUpdateException(
                    "Violation of UNIQUE KEY constraint 'IX_AppUsers_Email'.",
                    new InvalidOperationException("Cannot insert duplicate key row in object 'dbo.AppUsers'"));
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }

    // ─── Magic link failure is non-fatal ─────────────────────────────────────

    [Fact]
    public async Task CreateAsync_MagicLinkFailure_ReturnsFalseWithError_UserStillExists()
    {
        var db = BuildSeededDb();

        var magicLinkMock = new Mock<IMagicLinkService>();
        magicLinkMock.Setup(m => m.IssueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP server unavailable"));

        var (sut, _) = BuildSutWithMagicLink(db, SystemAdminUserId, BakedInRoles.SystemAdmin, magicLinkMock);

        var request = new CreateBackofficeUserRequest(
            DisplayName: "Email Fail User",
            Email: "emailfail@test.local",
            InitialRoleName: BackofficeUserRoleName,
            InitialOrganizationId: InternalOrgId);

        var result = await sut.CreateAsync(request, BuildPrincipal(SystemAdminUserId, BakedInRoles.SystemAdmin), "http://localhost/login");

        result.MagicLinkSent.Should().BeFalse();
        result.MagicLinkError.Should().Contain("SMTP server unavailable");

        // User was committed despite email failure
        var savedUser = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == "emailfail@test.local");
        savedUser.Should().NotBeNull();
        savedUser!.IsProvisioned.Should().BeTrue();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static DbContextOptions<DabblerSecurityDbContext> BuildInMemoryOptions()
        => new DbContextOptionsBuilder<DabblerSecurityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            // In-memory transactions are no-ops; suppress the EF Core error so
            // production transaction semantics are verified at integration level.
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    private static DabblerSecurityDbContext BuildSeededDb()
    {
        var options = BuildInMemoryOptions();
        using var seed = new DabblerSecurityDbContext(options);
        SeedStandardData(seed);
        seed.SaveChanges();
        return new DabblerSecurityDbContext(options);
    }

    private static void SeedStandardData(DabblerSecurityDbContext ctx)
    {
        var now = DateTime.UtcNow;

        ctx.AppOrganizations.AddRange(
            new AppOrganization { Id = InternalOrgId, Key = "internal", Name = "Internal Operations", IsInternal = true, IsActive = true, CreatedAt = now },
            new AppOrganization { Id = RetailOrgId, Key = "retail-1", Name = "Retail Business 1", IsInternal = false, IsActive = true, CreatedAt = now });

        ctx.AppRoles.Add(new AppRole
        {
            Id = BackofficeUserRoleId,
            Name = BackofficeUserRoleName,
            Description = "Backoffice user role",
            IsSystemRole = false,
            CreatedAt = now,
        });

        ctx.AppUsers.AddRange(
            new AppUser { Id = SystemAdminUserId, Email = "sysadmin@test.local", DisplayName = "Sys Admin", IsActive = true, CreatedAt = now },
            new AppUser { Id = UserAdminUserId, Email = "useradmin@test.local", DisplayName = "User Admin", IsActive = true, CreatedAt = now },
            new AppUser { Id = CrossOrgMonitorUserId, Email = "monitor@test.local", DisplayName = "Cross Org Monitor", IsActive = true, CreatedAt = now });

        ctx.AppUserOrganizationMemberships.AddRange(
            new AppUserOrganizationMembership { UserId = SystemAdminUserId, OrganizationId = InternalOrgId, IsDefault = true, IsActive = true, JoinedAt = now },
            new AppUserOrganizationMembership { UserId = UserAdminUserId, OrganizationId = InternalOrgId, IsDefault = true, IsActive = true, JoinedAt = now },
            new AppUserOrganizationMembership { UserId = CrossOrgMonitorUserId, OrganizationId = InternalOrgId, IsDefault = true, IsActive = true, JoinedAt = now });
    }

    private static (BackofficeUserCreationService sut, Mock<IMagicLinkService> magicLinkMock) BuildSut(
        DabblerSecurityDbContext db,
        int actorUserId,
        string activeRoleName)
    {
        var magicLinkMock = new Mock<IMagicLinkService>();
        magicLinkMock.Setup(m => m.IssueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MagicLinkIssueResult(true));

        return BuildSutWithMagicLink(db, actorUserId, activeRoleName, magicLinkMock);
    }

    private static (BackofficeUserCreationService sut, Mock<IMagicLinkService> magicLinkMock) BuildSutWithMagicLink(
        DabblerSecurityDbContext db,
        int actorUserId,
        string activeRoleName,
        Mock<IMagicLinkService> magicLinkMock)
    {
        var roleSwitcher = new Mock<IRoleSwitcher>();
        roleSwitcher.Setup(s => s.EffectiveRoles(It.IsAny<ClaimsPrincipal>()))
            .Returns(new[] { activeRoleName });

        var sut = new BackofficeUserCreationService(
            db,
            roleSwitcher.Object,
            new PlatformBakedInRoleProvider(),
            magicLinkMock.Object,
            NullLogger<BackofficeUserCreationService>.Instance);

        return (sut, magicLinkMock);
    }

    private static ClaimsPrincipal BuildPrincipal(int userId, string roleName)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, roleName),
            ],
            authenticationType: "TestScheme");

        return new ClaimsPrincipal(identity);
    }
}
