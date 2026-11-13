using Bunit;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Dabbler.Security.Blazor.Tests.Pages.Admin;

public class UserListPageTests : IAsyncLifetime
{
    private readonly BunitContext _ctx;
    private readonly Mock<IUserService> _userSvcMock;

    public UserListPageTests()
    {
        _ctx = new BunitContext();
        _userSvcMock = new Mock<IUserService>();
        _ctx.SetupForDabbler(
            roles: ["UserAdministrator"],
            configureServices: s => s.AddSingleton(_userSvcMock.Object));
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _ctx.DisposeAsync().AsTask();

    private static AppUser MakeUser(int id, string email, bool active = true, string[]? roles = null) =>
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
                .ToList()
        };

    [Fact]
    public void UserListPage_RendersHeading()
    {
        _userSvcMock.Setup(s => s.GetAllAsync()).ReturnsAsync([]);
        var cut = _ctx.Render<Dabbler.Security.Blazor.Pages.Admin.UserListPage>();
        cut.Markup.Should().Contain("User Management");
    }

    [Fact]
    public void UserListPage_ShowsUsersFromService()
    {
        var users = new List<AppUser>
        {
            MakeUser(1, "alice@test.local", active: true, roles: ["Manager"]),
            MakeUser(2, "bob@test.local",   active: false)
        };
        _userSvcMock.Setup(s => s.GetAllAsync()).ReturnsAsync(users);

        var cut = _ctx.Render<Dabbler.Security.Blazor.Pages.Admin.UserListPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("alice@test.local");
            cut.Markup.Should().Contain("bob@test.local");
        });
    }

    [Fact]
    public void UserListPage_ActiveUser_ShowsActiveChip()
    {
        _userSvcMock.Setup(s => s.GetAllAsync())
            .ReturnsAsync([MakeUser(1, "active@test.local", active: true)]);

        var cut = _ctx.Render<Dabbler.Security.Blazor.Pages.Admin.UserListPage>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Active"));
    }

    [Fact]
    public void UserListPage_InactiveUser_ShowsInactiveChip()
    {
        _userSvcMock.Setup(s => s.GetAllAsync())
            .ReturnsAsync([MakeUser(2, "inactive@test.local", active: false)]);

        var cut = _ctx.Render<Dabbler.Security.Blazor.Pages.Admin.UserListPage>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Inactive"));
    }

    [Fact]
    public void UserListPage_ShowsRoleBadgesForUser()
    {
        _userSvcMock.Setup(s => s.GetAllAsync())
            .ReturnsAsync([MakeUser(1, "admin@test.local", roles: ["UserAdministrator", "Manager"])]);

        var cut = _ctx.Render<Dabbler.Security.Blazor.Pages.Admin.UserListPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("UserAdministrator");
            cut.Markup.Should().Contain("Manager");
        });
    }

    [Fact]
    public void UserListPage_HasNewUserButton()
    {
        _userSvcMock.Setup(s => s.GetAllAsync()).ReturnsAsync([]);
        var cut = _ctx.Render<Dabbler.Security.Blazor.Pages.Admin.UserListPage>();
        cut.Markup.Should().Contain("New User");
    }

    [Fact]
    public void UserListPage_ActionButtonsHaveContextualLabels()
    {
        _userSvcMock.Setup(s => s.GetAllAsync())
            .ReturnsAsync([MakeUser(7, "alice@test.local")]);

        var cut = _ctx.Render<Dabbler.Security.Blazor.Pages.Admin.UserListPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("aria-label=\"Edit user User 7\"");
            cut.Markup.Should().Contain("aria-label=\"Manage roles for User 7\"");
        });
    }

    [Fact]
    public void UserListPage_EmptyList_RendersWithoutError()
    {
        _userSvcMock.Setup(s => s.GetAllAsync()).ReturnsAsync([]);
        var cut = _ctx.Render<Dabbler.Security.Blazor.Pages.Admin.UserListPage>();
        cut.Markup.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void UserListPage_ClickNewUser_NavigatesToCreatePage()
    {
        _userSvcMock.Setup(s => s.GetAllAsync()).ReturnsAsync([]);
        var cut = _ctx.Render<Dabbler.Security.Blazor.Pages.Admin.UserListPage>();

        cut.WaitForAssertion(() => cut.FindAll("button").Should().NotBeEmpty());
        var buttons = cut.FindAll("button");
        var newUserBtn = buttons.FirstOrDefault(b => b.TextContent.Contains("New User"));
        newUserBtn?.Click();

        var navMan = _ctx.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        navMan.Uri.Should().Contain("/admin/users/new");
    }
}
