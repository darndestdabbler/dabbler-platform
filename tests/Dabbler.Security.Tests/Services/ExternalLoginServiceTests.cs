using Dabbler.Security.Data;
using Dabbler.Security.Entities;
using Dabbler.Security.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dabbler.Security.Tests.Services;

public sealed class ExternalLoginServiceTests : IAsyncLifetime
{
    private DabblerSecurityDbContext _db = null!;
    private ExternalLoginService _sut = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<DabblerSecurityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new DabblerSecurityDbContext(options);
        await _db.Database.EnsureCreatedAsync();
        _sut = new ExternalLoginService(_db, NullLogger<ExternalLoginService>.Instance);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
    }

    // --- FindByExternalLoginAsync ---

    [Fact]
    public async Task FindByExternalLoginAsync_ExistingLogin_ReturnsUserWithRoles()
    {
        // Arrange
        var role = new AppRole { Name = "Manager", Description = "Ops" };
        _db.AppRoles.Add(role);
        await _db.SaveChangesAsync();

        var user = new AppUser
        {
            Email = "alice@contoso.local",
            DisplayName = "Alice",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

        _db.AppUserRoles.Add(new AppUserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = user.Id
        });
        _db.AppExternalLogins.Add(new AppExternalLogin
        {
            UserId = user.Id,
            Provider = "EntraId",
            ProviderSubjectId = "oid-123",
            ProviderEmail = "alice@contoso.local",
            LinkedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.FindByExternalLoginAsync("EntraId", "oid-123");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be("alice@contoso.local");
        result.Roles.Should().HaveCount(1);
        result.Roles.First().Role!.Name.Should().Be("Manager");
    }

    [Fact]
    public async Task FindByExternalLoginAsync_NotFound_ReturnsNull()
    {
        var result = await _sut.FindByExternalLoginAsync("EntraId", "nonexistent-oid");
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindByExternalLoginAsync_InactiveUser_ReturnsNull()
    {
        var user = new AppUser
        {
            Email = "inactive@contoso.local",
            DisplayName = "Inactive",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

        _db.AppExternalLogins.Add(new AppExternalLogin
        {
            UserId = user.Id,
            Provider = "EntraId",
            ProviderSubjectId = "oid-inactive",
            LinkedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _sut.FindByExternalLoginAsync("EntraId", "oid-inactive");
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindByExternalLoginAsync_DeletedUser_ReturnsNull()
    {
        var user = new AppUser
        {
            Email = "deleted@contoso.local",
            DisplayName = "Deleted",
            IsActive = true,
            DeletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

        _db.AppExternalLogins.Add(new AppExternalLogin
        {
            UserId = user.Id,
            Provider = "EntraId",
            ProviderSubjectId = "oid-deleted",
            LinkedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _sut.FindByExternalLoginAsync("EntraId", "oid-deleted");
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindByExternalLoginAsync_UpdatesLastUsedAt()
    {
        var user = new AppUser
        {
            Email = "user@contoso.local",
            DisplayName = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

        _db.AppExternalLogins.Add(new AppExternalLogin
        {
            UserId = user.Id,
            Provider = "EntraId",
            ProviderSubjectId = "oid-track",
            LinkedAt = DateTime.UtcNow,
            LastUsedAt = null
        });
        await _db.SaveChangesAsync();

        await _sut.FindByExternalLoginAsync("EntraId", "oid-track");

        var login = await _db.AppExternalLogins.FirstAsync(x => x.ProviderSubjectId == "oid-track");
        login.LastUsedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task FindByExternalLoginAsync_WrongProvider_ReturnsNull()
    {
        var user = new AppUser
        {
            Email = "user@contoso.local",
            DisplayName = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

        _db.AppExternalLogins.Add(new AppExternalLogin
        {
            UserId = user.Id,
            Provider = "Google",
            ProviderSubjectId = "google-sub-123",
            LinkedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        // Looking for EntraId but login is from Google
        var result = await _sut.FindByExternalLoginAsync("EntraId", "google-sub-123");
        result.Should().BeNull();
    }

    // --- LinkExternalLoginAsync ---

    [Fact]
    public async Task LinkExternalLoginAsync_ValidUser_CreatesLink()
    {
        var user = new AppUser
        {
            Email = "link@contoso.local",
            DisplayName = "LinkUser",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

        var login = await _sut.LinkExternalLoginAsync(user.Id, "EntraId", "oid-link", "link@contoso.local");

        login.Should().NotBeNull();
        login.UserId.Should().Be(user.Id);
        login.Provider.Should().Be("EntraId");
        login.ProviderSubjectId.Should().Be("oid-link");

        var persisted = await _db.AppExternalLogins.FirstOrDefaultAsync(x => x.ProviderSubjectId == "oid-link");
        persisted.Should().NotBeNull();
    }

    [Fact]
    public async Task LinkExternalLoginAsync_NonexistentUser_Throws()
    {
        var act = () => _sut.LinkExternalLoginAsync(999, "EntraId", "oid-ghost", null);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*999*not found*");
    }

    [Fact]
    public async Task LinkExternalLoginAsync_DeletedUser_Throws()
    {
        var user = new AppUser
        {
            Email = "del@contoso.local",
            DisplayName = "DelUser",
            IsActive = true,
            DeletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

        var act = () => _sut.LinkExternalLoginAsync(user.Id, "EntraId", "oid-del", null);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    // --- ProvisionAndLinkAsync ---

    [Fact]
    public async Task ProvisionAndLinkAsync_NewUser_CreatesUserAndLink()
    {
        var role = new AppRole { Name = "Viewer", Description = "Read-only" };
        _db.AppRoles.Add(role);
        await _db.SaveChangesAsync();

        var user = await _sut.ProvisionAndLinkAsync(
            "EntraId", "oid-new", "newuser@contoso.local", "New User", ["Viewer"]);

        user.Should().NotBeNull();
        user.Email.Should().Be("newuser@contoso.local");
        user.DisplayName.Should().Be("New User");
        user.IsActive.Should().BeTrue();
        user.Roles.Should().HaveCount(1);
        user.Roles.First().Role!.Name.Should().Be("Viewer");

        var login = await _db.AppExternalLogins
            .FirstOrDefaultAsync(x => x.ProviderSubjectId == "oid-new");
        login.Should().NotBeNull();
        login!.Provider.Should().Be("EntraId");
    }

    [Fact]
    public async Task ProvisionAndLinkAsync_ExistingEmailUser_LinksToExisting()
    {
        var existingUser = new AppUser
        {
            Email = "existing@contoso.local",
            DisplayName = "Existing",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.AppUsers.Add(existingUser);
        await _db.SaveChangesAsync();

        var user = await _sut.ProvisionAndLinkAsync(
            "EntraId", "oid-existing", "existing@contoso.local", "Existing OIDC", []);

        user.Id.Should().Be(existingUser.Id);

        var login = await _db.AppExternalLogins
            .FirstOrDefaultAsync(x => x.ProviderSubjectId == "oid-existing");
        login.Should().NotBeNull();
        login!.UserId.Should().Be(existingUser.Id);
    }

    [Fact]
    public async Task ProvisionAndLinkAsync_NullEmail_Throws()
    {
        var act = () => _sut.ProvisionAndLinkAsync(
            "EntraId", "oid-noemail", null, "No Email", []);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Email is required*");
    }

    [Fact]
    public async Task ProvisionAndLinkAsync_EmptyEmail_Throws()
    {
        var act = () => _sut.ProvisionAndLinkAsync(
            "EntraId", "oid-emptyemail", "", "Empty Email", []);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Email is required*");
    }

    [Fact]
    public async Task ProvisionAndLinkAsync_MultipleDefaultRoles_AssignsAll()
    {
        _db.AppRoles.Add(new AppRole { Name = "Manager", Description = "Manager" });
        _db.AppRoles.Add(new AppRole { Name = "Viewer", Description = "Viewer" });
        await _db.SaveChangesAsync();

        var user = await _sut.ProvisionAndLinkAsync(
            "EntraId", "oid-multi", "multi@contoso.local", "Multi", ["Manager", "Viewer"]);

        user.Roles.Should().HaveCount(2);
    }

    [Fact]
    public async Task ProvisionAndLinkAsync_NonexistentRole_SkipsGracefully()
    {
        var user = await _sut.ProvisionAndLinkAsync(
            "EntraId", "oid-badrole", "badrole@contoso.local", "BadRole", ["NonexistentRole"]);

        user.Roles.Should().BeEmpty();
    }

    // --- UnlinkExternalLoginAsync ---

    [Fact]
    public async Task UnlinkExternalLoginAsync_ExistingLogin_RemovesLink()
    {
        var user = new AppUser
        {
            Email = "unlink@contoso.local",
            DisplayName = "Unlink",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

        _db.AppExternalLogins.Add(new AppExternalLogin
        {
            UserId = user.Id,
            Provider = "EntraId",
            ProviderSubjectId = "oid-unlink",
            LinkedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        await _sut.UnlinkExternalLoginAsync(user.Id, "EntraId");

        var remaining = await _db.AppExternalLogins
            .Where(x => x.UserId == user.Id)
            .ToListAsync();
        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task UnlinkExternalLoginAsync_NonexistentLogin_Throws()
    {
        var user = new AppUser
        {
            Email = "nolink@contoso.local",
            DisplayName = "NoLink",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

        var act = () => _sut.UnlinkExternalLoginAsync(user.Id, "EntraId");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No external login*");
    }

    // --- GetExternalLoginsForUserAsync ---

    [Fact]
    public async Task GetExternalLoginsForUserAsync_MultipleLogins_ReturnsAll()
    {
        var user = new AppUser
        {
            Email = "multi@contoso.local",
            DisplayName = "Multi",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

        _db.AppExternalLogins.AddRange(
            new AppExternalLogin
            {
                UserId = user.Id,
                Provider = "EntraId",
                ProviderSubjectId = "oid-multi-1",
                LinkedAt = DateTime.UtcNow
            },
            new AppExternalLogin
            {
                UserId = user.Id,
                Provider = "Google",
                ProviderSubjectId = "google-multi-1",
                LinkedAt = DateTime.UtcNow
            }
        );
        await _db.SaveChangesAsync();

        var logins = await _sut.GetExternalLoginsForUserAsync(user.Id);

        logins.Should().HaveCount(2);
        logins.Select(l => l.Provider).Should().Contain("EntraId");
        logins.Select(l => l.Provider).Should().Contain("Google");
    }

    [Fact]
    public async Task GetExternalLoginsForUserAsync_NoLogins_ReturnsEmpty()
    {
        var user = new AppUser
        {
            Email = "lonely@contoso.local",
            DisplayName = "Lonely",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

        var logins = await _sut.GetExternalLoginsForUserAsync(user.Id);
        logins.Should().BeEmpty();
    }
}
