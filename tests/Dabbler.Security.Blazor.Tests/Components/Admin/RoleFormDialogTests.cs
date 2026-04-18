using Bunit;
using Dabbler.Security.Blazor.Components.Admin;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Dabbler.Security.Blazor.Tests.Components.Admin;

public class RoleFormDialogTests
{
    [Theory]
    [InlineData("Admin", false, true)]
    [InlineData("admin", false, true)]
    [InlineData(" Admin ", false, true)]
    [InlineData("NewRole", false, false)]
    [InlineData("Admin", true, false)]
    [InlineData("", false, false)]
    [InlineData("   ", false, false)]
    public void IsDuplicateRoleName_Matches_CaseInsensitive_And_Trims(
        string candidate,
        bool isEdit,
        bool expected)
    {
        var existing = new[] { "Admin", "Viewer" };

        RoleFormDialog.IsDuplicateRoleName(candidate, isEdit, existing)
            .Should().Be(expected);
    }

    [Fact]
    public async Task AddingDuplicateRoleName_ShowsInlineAlert_AndDoesNotCloseDialog()
    {
        using var ctx = CreateContext();

        var host = ctx.RenderComponent<RoleFormDialogHost>();
        await host.InvokeAsync(() => host.Instance.ShowAsync(
            roleName: "Admin",
            existingRoleNames: ["Admin", "Viewer"]));

        host.WaitForAssertion(() =>
            host.Find("[data-testid='role-form-save']").Should().NotBeNull());

        var saveButton = host.Find("[data-testid='role-form-save']");
        await host.InvokeAsync(() => saveButton.Click());

        host.WaitForAssertion(() =>
        {
            var alert = host.Find("[data-testid='role-form-duplicate-alert']");
            alert.TextContent.Should().Contain("A role with this name already exists.");
        });

        // Dialog is still open — the Save button is still in the DOM.
        host.FindAll("[data-testid='role-form-save']").Should().NotBeEmpty();
    }

    [Fact]
    public async Task EditingExistingRoleName_DoesNotTriggerDuplicateAlert()
    {
        using var ctx = CreateContext();

        var host = ctx.RenderComponent<RoleFormDialogHost>();
        await host.InvokeAsync(() => host.Instance.ShowAsync(
            roleName: "Admin",
            existingRoleNames: ["Admin", "Viewer"],
            isEdit: true));

        host.WaitForAssertion(() =>
            host.Find("[data-testid='role-form-save']").Should().NotBeNull());

        var saveButton = host.Find("[data-testid='role-form-save']");
        await host.InvokeAsync(() => saveButton.Click());

        host.FindAll("[data-testid='role-form-duplicate-alert']").Should().BeEmpty();
    }

    private static TestContext CreateContext()
    {
        var ctx = new TestContext();
        ctx.Services.AddMudServices(cfg =>
        {
            cfg.SnackbarConfiguration.ShowTransitionDuration = 0;
            cfg.SnackbarConfiguration.HideTransitionDuration = 0;
        });
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }
}
