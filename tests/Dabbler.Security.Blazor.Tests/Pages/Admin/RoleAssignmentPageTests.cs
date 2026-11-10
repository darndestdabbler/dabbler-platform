using Bunit;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Data;
using Dabbler.Security.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dabbler.Security.Blazor.Tests.Pages.Admin;

public class RoleAssignmentPageTests : IDisposable
{
    private readonly TestContext _ctx;
    private readonly Mock<IUserService> _userSvcMock;

    public RoleAssignmentPageTests()
    {
        _ctx = new TestContext();
        _userSvcMock = new Mock<IUserService>();
        _ctx.SetupForDabbler(
            roles: ["UserAdministrator"],
            configureServices: s =>
            {
                s.AddSingleton(_userSvcMock.Object);
                // Use InMemory for the DbContext queries done directly in the page
                s.AddDbContext<DabblerSecurityDbContext>(opts =>
                    opts.UseInMemoryDatabase($"RoleTest_{Guid.NewGuid()}"));
            });
    }

    public void Dispose() => _ctx.Dispose();

    private DabblerSecurityDbContext GetDb() =>
        _ctx.Services.GetRequiredService<DabblerSecurityDbContext>();

    private static AppUser MakeUser(int id, params AppRole[] assignedRoles) => new()
    {
        Id = id,
        Email = $"u{id}@test.local",
        DisplayName = $"User {id}",
        IsActive = true,
        Roles = assignedRoles
            .Select(r => new AppUserRole { UserId = id, RoleId = r.Id, Role = r })
            .ToList()
    };

    private static AppRole MakeRole(int id, string name, bool isSystem = false) => new()
    {
        Id = id, Name = name, IsSystemRole = isSystem, Description = string.Empty
    };

    [Fact]
    public async Task RoleAssignmentPage_ShowsUserNameAndEmail()
    {
        var managerRole = MakeRole(1, "Manager");
        var user = MakeUser(10, managerRole);
        _userSvcMock.Setup(s => s.GetByIdCachedAsync(10)).ReturnsAsync(user);

        var db = GetDb();
        db.AppRoles.Add(managerRole);
        await db.SaveChangesAsync();

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Admin.RoleAssignmentPage>(
            p => p.Add(x => x.UserId, 10));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("User 10");
            cut.Markup.Should().Contain("u10@test.local");
        });
    }

    [Fact]
    public async Task RoleAssignmentPage_ShowsAvailableAndAssignedRoleLists()
    {
        var managerRole = MakeRole(1, "Manager");
        var viewerRole = MakeRole(2, "Viewer");
        var user = MakeUser(11, managerRole);
        _userSvcMock.Setup(s => s.GetByIdCachedAsync(11)).ReturnsAsync(user);

        var db = GetDb();
        db.AppRoles.AddRange(managerRole, viewerRole);
        await db.SaveChangesAsync();

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Admin.RoleAssignmentPage>(
            p => p.Add(x => x.UserId, 11));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Available Roles");
            cut.Markup.Should().Contain("Assigned Roles");
        });
    }

    [Fact]
    public async Task RoleAssignmentPage_AssignedRole_AppearsInAssignedList()
    {
        var managerRole = MakeRole(1, "Manager");
        var user = MakeUser(12, managerRole);
        _userSvcMock.Setup(s => s.GetByIdCachedAsync(12)).ReturnsAsync(user);

        var db = GetDb();
        db.AppRoles.Add(managerRole);
        await db.SaveChangesAsync();

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Admin.RoleAssignmentPage>(
            p => p.Add(x => x.UserId, 12));

        cut.WaitForAssertion(() =>
        {
            // Manager is assigned — should appear in "Assigned Roles" section
            cut.Markup.Should().Contain("Manager");
        });
    }

    [Fact]
    public async Task RoleAssignmentPage_UserNotFound_ShowsErrorAlert()
    {
        _userSvcMock.Setup(s => s.GetByIdCachedAsync(999)).ReturnsAsync((AppUser?)null);

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Admin.RoleAssignmentPage>(
            p => p.Add(x => x.UserId, 999));

        cut.WaitForAssertion(() =>
            cut.Markup.ToLowerInvariant().Should().Contain("not found"));
    }

    [Fact]
    public async Task RoleAssignmentPage_DoneButton_NavigatesToUserList()
    {
        var user = MakeUser(13);
        _userSvcMock.Setup(s => s.GetByIdCachedAsync(13)).ReturnsAsync(user);

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Admin.RoleAssignmentPage>(
            p => p.Add(x => x.UserId, 13));

        cut.WaitForAssertion(() => cut.FindAll("button").Should().NotBeEmpty());
        var doneButton = cut.FindAll("button")
            .FirstOrDefault(b => b.TextContent.Contains("Done"));
        doneButton?.Click();

        var navMan = _ctx.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        navMan.Uri.Should().Contain("/admin/users");
    }
}
