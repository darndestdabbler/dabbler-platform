using Bunit;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Dabbler.Security.Blazor.Tests.Pages.Admin;

public class UserListPageExternalLoginTests : IDisposable
{
    private readonly TestContext _ctx;
    private readonly Mock<IUserService> _userSvcMock;

    public UserListPageExternalLoginTests()
    {
        _ctx = new TestContext();
        _userSvcMock = new Mock<IUserService>();
        _ctx.SetupForDabbler(
            roles: ["UserAdministrator"],
            configureServices: s => s.AddSingleton(_userSvcMock.Object));
    }

    public void Dispose() => _ctx.Dispose();

    private static AppUser MakeUser(
        int id, string email,
        bool active = true,
        string[]? roles = null,
        string[]? externalProviders = null) =>
        new()
        {
            Id = id,
            Email = email,
            DisplayName = $"User {id}",
            IsActive = active,
            Roles = (roles ?? [])
                .Select(r => new AppUserRole
                {
                    UserId = id, RoleId = 1,
                    Role = new AppRole { Id = 1, Name = r, IsSystemRole = false }
                })
                .ToList(),
            ExternalLogins = (externalProviders ?? [])
                .Select(p => new AppExternalLogin
                {
                    Id = id,
                    UserId = id,
                    Provider = p,
                    ProviderSubjectId = $"sub-{id}",
                    ProviderEmail = email,
                    LinkedAt = DateTime.UtcNow
                })
                .ToList()
        };

    [Fact]
    public void UserListPage_ShowsExternalLoginBadge_ForOidcUser()
    {
        _userSvcMock.Setup(s => s.GetAllAsync())
            .ReturnsAsync([MakeUser(1, "admin@contoso.local", externalProviders: ["EntraId"])]);

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Admin.UserListPage>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("EntraId"));
    }

    [Fact]
    public void UserListPage_ShowsMagicLinkBadge_ForMagicLinkOnlyUser()
    {
        _userSvcMock.Setup(s => s.GetAllAsync())
            .ReturnsAsync([MakeUser(1, "user@test.local")]);

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Admin.UserListPage>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Magic Link"));
    }

    [Fact]
    public void UserListPage_ShowsAuthColumnHeader()
    {
        _userSvcMock.Setup(s => s.GetAllAsync()).ReturnsAsync([]);

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Admin.UserListPage>();

        cut.Markup.Should().Contain("Auth");
    }

    [Fact]
    public void UserListPage_MultipleProviders_ShowsAllBadges()
    {
        _userSvcMock.Setup(s => s.GetAllAsync())
            .ReturnsAsync([MakeUser(1, "user@test.local", externalProviders: ["EntraId", "Google"])]);

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Admin.UserListPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("EntraId");
            cut.Markup.Should().Contain("Google");
        });
    }
}
