using AcmeCoffeeOnline.Web.Components.Shared.DevTools;
using AcmeCoffeeOnline.Web.Configuration;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MudBlazor.Services;

namespace AcmeCoffeeOnline.Web.Tests.Components.DevTools;

public sealed class DevToolsMenuTests : TestContext
{
    public DevToolsMenuTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
    }

    [Fact]
    public void RendersNothing_WhenSeededSessionDisabled()
    {
        RegisterAuthOptions(enabled: false);

        var cut = RenderMenu();

        Assert.Empty(cut.FindAll("[data-testid='dev-tools-menu']"));
    }

    [Fact]
    public void RendersMenuRoot_WhenSeededSessionEnabled()
    {
        RegisterAuthOptions(enabled: true);

        var cut = RenderMenu();

        Assert.NotNull(cut.Find("[data-testid='dev-tools-menu']"));
    }

    // MudMenu items are rendered inside a popover that JSInterop materializes on
    // click. bUnit's Loose JS mode can't expand it, so "clicking each item opens
    // its dialog" is covered by the Session 2 Playwright smoke test and by
    // MagicLinksDialogTests, which instantiates the dialog directly via
    // IDialogService. The test below guards the gate-off branch so no menu leaks
    // into production DOM.

    private IRenderedFragment RenderMenu()
    {
        return Render(builder =>
        {
            builder.OpenComponent<MudBlazor.MudThemeProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<MudBlazor.MudPopoverProvider>(1);
            builder.CloseComponent();
            builder.OpenComponent<MudBlazor.MudDialogProvider>(2);
            builder.CloseComponent();
            builder.OpenComponent<MudBlazor.MudSnackbarProvider>(3);
            builder.CloseComponent();

            builder.OpenComponent<DevToolsMenu>(4);
            builder.CloseComponent();
        });
    }

    private void RegisterAuthOptions(bool enabled)
    {
        var options = new AppAuthenticationOptions
        {
            SeededSession = new SeededSessionOptions
            {
                Enabled = enabled,
                SharedSecret = "test-secret",
                AllowedRoles = ["SystemAdmin", "BackofficeUser", "Customer"]
            }
        };
        Services.AddSingleton<IOptions<AppAuthenticationOptions>>(Options.Create(options));
    }
}
