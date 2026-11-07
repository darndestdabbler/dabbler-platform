using Bunit;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Dabbler.Security.Blazor.Tests.Pages.Admin;

public class UserEditPageTests : IDisposable
{
    private readonly TestContext _ctx;
    private readonly Mock<IUserService> _userSvcMock;
    private readonly Mock<IExternalLoginService> _extLoginSvcMock;

    public UserEditPageTests()
    {
        _ctx = new TestContext();
        _userSvcMock = new Mock<IUserService>();
        _extLoginSvcMock = new Mock<IExternalLoginService>();
        _extLoginSvcMock.Setup(s => s.GetExternalLoginsForUserAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<AppExternalLogin>());
        _ctx.SetupForDabbler(
            roles: ["UserAdministrator"],
            configureServices: s =>
            {
                s.AddSingleton(_userSvcMock.Object);
                s.AddSingleton(_extLoginSvcMock.Object);
            });
    }

    public void Dispose() => _ctx.Dispose();

    private static AppUser ExistingUser(int id = 2) => new()
    {
        Id = id,
        Email = "existing@test.local",
        DisplayName = "Existing User",
        IsActive = true,
        Roles = []
    };

    // ── Create Mode ────────────────────────────────────────────────────────

    [Fact]
    public void UserEditPage_CreateMode_ShowsEmailAndDisplayNameInputs()
    {
        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Admin.UserEditPage>();

        cut.Markup.Should().Contain("Email");
        cut.Markup.Should().Contain("Display Name");
    }

    [Fact]
    public void UserEditPage_CreateMode_DoesNotShowDeleteButton()
    {
        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Admin.UserEditPage>();

        cut.Markup.Should().NotContain("Delete");
    }

    [Fact]
    public async Task UserEditPage_CreateMode_SubmitValidForm_CallsCreateAsync()
    {
        _userSvcMock
            .Setup(s => s.CreateAsync("new@test.local", "New User", It.IsAny<int>()))
            .ReturnsAsync(new AppUser { Id = 99, Email = "new@test.local", DisplayName = "New User", Roles = [] });

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Admin.UserEditPage>();

        var inputs = cut.FindAll("input");
        // Email is first, DisplayName is second
        inputs[0].Change("new@test.local");
        inputs[1].Change("New User");

        await cut.InvokeAsync(() => cut.Find("form").Submit());

        cut.WaitForAssertion(() =>
            _userSvcMock.Verify(s => s.CreateAsync("new@test.local", "New User", It.IsAny<int>()), Times.Once));
    }

    // ── Edit Mode ──────────────────────────────────────────────────────────

    [Fact]
    public void UserEditPage_EditMode_ShowsExistingValues()
    {
        var user = ExistingUser();
        _userSvcMock.Setup(s => s.GetByIdCachedAsync(user.Id)).ReturnsAsync(user);

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Admin.UserEditPage>(
            p => p.Add(x => x.Id, user.Id));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("existing@test.local");
            cut.Markup.Should().Contain("Existing User");
        });
    }

    [Fact]
    public void UserEditPage_EditMode_ShowsDeleteButton()
    {
        var user = ExistingUser();
        _userSvcMock.Setup(s => s.GetByIdCachedAsync(user.Id)).ReturnsAsync(user);

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Admin.UserEditPage>(
            p => p.Add(x => x.Id, user.Id));

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Delete"));
    }

    [Fact]
    public void UserEditPage_EditMode_EmailInputIsDisabled()
    {
        var user = ExistingUser();
        _userSvcMock.Setup(s => s.GetByIdCachedAsync(user.Id)).ReturnsAsync(user);

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Admin.UserEditPage>(
            p => p.Add(x => x.Id, user.Id));

        cut.WaitForAssertion(() =>
        {
            // Email input should be read-only/disabled in edit mode
            var emailInput = cut.Find("input[disabled], input[readonly]");
            emailInput.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task UserEditPage_EditMode_SubmitForm_CallsUpdateAsync()
    {
        var user = ExistingUser();
        _userSvcMock.Setup(s => s.GetByIdCachedAsync(user.Id)).ReturnsAsync(user);
        _userSvcMock.Setup(s => s.UpdateAsync(user.Id, It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Admin.UserEditPage>(
            p => p.Add(x => x.Id, user.Id));

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Existing User"));

        // Change DisplayName
        var editableInputs = cut.FindAll("input:not([disabled]):not([readonly])");
        editableInputs[0].Change("Updated Name");

        await cut.InvokeAsync(() => cut.Find("form").Submit());

        cut.WaitForAssertion(() =>
            _userSvcMock.Verify(s => s.UpdateAsync(user.Id, It.IsAny<string>(), It.IsAny<bool>()), Times.Once));
    }

    [Fact]
    public void UserEditPage_NotFound_ShowsErrorOrRedirects()
    {
        _userSvcMock.Setup(s => s.GetByIdCachedAsync(999)).ReturnsAsync((AppUser?)null);

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Admin.UserEditPage>(
            p => p.Add(x => x.Id, 999));

        // After load, either shows an error message or navigates away
        cut.WaitForAssertion(() =>
        {
            var navUri = _ctx.Services
                .GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>()
                .Uri;
            (cut.Markup.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
             navUri.Contains("/admin/users"))
            .Should().BeTrue();
        });
    }
}
