using Bunit;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Data;
using Dabbler.Security.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Extensions;

namespace Dabbler.Security.Blazor.Tests.Pages.Admin;

/// <summary>
/// Tests for RoleListPage. The page uses DabblerSecurityDbContext directly,
/// so we provide an EF Core InMemory database seeded with test data.
/// </summary>
public class RoleListPageTests : IAsyncLifetime
{
    private readonly BunitContext _ctx;

    public RoleListPageTests()
    {
        _ctx = new BunitContext();
        _ctx.SetupForDabbler(
            roles: ["UserAdministrator"],
            configureServices: s =>
            {
                s.AddDbContext<DabblerSecurityDbContext>(opts =>
                    opts.UseInMemoryDatabase($"RoleList_{Guid.NewGuid()}"));
            });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _ctx.DisposeAsync().AsTask();

    private DabblerSecurityDbContext GetDb() =>
        _ctx.Services.GetRequiredService<DabblerSecurityDbContext>();

    private async Task SeedRolesAsync(params AppRole[] roles)
    {
        var db = GetDb();
        db.AppRoles.AddRange(roles);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task RoleListPage_RendersHeading()
    {
        await SeedRolesAsync();
        var cut = _ctx.Render<Dabbler.Security.Blazor.Pages.Admin.RoleListPage>();
        cut.Markup.Should().Contain("Role Management");
    }

    [Fact]
    public async Task RoleListPage_ShowsReadOnlyNotice()
    {
        await SeedRolesAsync();
        var cut = _ctx.Render<Dabbler.Security.Blazor.Pages.Admin.RoleListPage>();
        cut.Markup.Should().Contain("migration metadata");
    }

    [Fact]
    public async Task RoleListPage_ShowsRoleNames()
    {
        await SeedRolesAsync(
            new AppRole { Id = 1, Name = "Manager", Description = "Manages things" },
            new AppRole { Id = 2, Name = "Viewer",  Description = "Read only" }
        );

        var cut = _ctx.Render<Dabbler.Security.Blazor.Pages.Admin.RoleListPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Manager");
            cut.Markup.Should().Contain("Viewer");
        });
    }

    [Fact]
    public async Task RoleListPage_SystemRole_ShowsSystemChip()
    {
        await SeedRolesAsync(
            new AppRole { Id = 3, Name = "UserAdministrator", Description = "System role", IsSystemRole = true }
        );

        var cut = _ctx.Render<Dabbler.Security.Blazor.Pages.Admin.RoleListPage>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("System"));
    }

    [Fact]
    public async Task RoleListPage_HasViewPermissionsButton()
    {
        await SeedRolesAsync(
            new AppRole { Id = 1, Name = "Manager", Description = string.Empty }
        );

        var cut = _ctx.Render<Dabbler.Security.Blazor.Pages.Admin.RoleListPage>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("View Permissions"));
    }

    [Fact]
    public async Task RoleListPage_DeletedRolesNotShown()
    {
        await SeedRolesAsync(
            new AppRole { Id = 1, Name = "Active Role", Description = string.Empty },
            new AppRole { Id = 2, Name = "Deleted Role", Description = string.Empty, DeletedAt = DateTime.UtcNow.AddDays(-1) }
        );

        var cut = _ctx.Render<Dabbler.Security.Blazor.Pages.Admin.RoleListPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Active Role");
            cut.Markup.Should().NotContain("Deleted Role");
        });
    }

    [Fact]
    public async Task RoleListPage_ClickViewPermissions_OpensDialog()
    {
        await SeedRolesAsync(
            new AppRole { Id = 1, Name = "Manager", Description = string.Empty }
        );

        var cut = _ctx.Render<Dabbler.Security.Blazor.Pages.Admin.RoleListPage>();
        cut.WaitForAssertion(() => cut.FindAll("button").Any(b => b.TextContent.Contains("View Permissions")).Should().BeTrue());

        var btn = cut.FindAll("button").First(b => b.TextContent.Contains("View Permissions"));
        await cut.InvokeAsync(() => btn.Click());

        // Dialog Visible flag should be true after clicking — use FindComponent to check
        // rather than Markup, since MudDialog may render via portal outside cut's subtree.
        cut.WaitForAssertion(() =>
        {
            var dialog = cut.FindComponent<MudBlazor.MudDialog>();
            dialog.Instance.GetState(x => x.Visible).Should().BeTrue();
        });
    }
}
