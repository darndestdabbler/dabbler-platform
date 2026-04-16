using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace Dabbler.Components.Tests.TestHelpers;

public static class BunitContextExtensions
{
    public static TestContext SetupForDabblerComponents(this TestContext ctx)
    {
        ctx.Services.AddMudServices(cfg =>
        {
            cfg.SnackbarConfiguration.ShowTransitionDuration = 0;
            cfg.SnackbarConfiguration.HideTransitionDuration = 0;
        });

        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        ctx.RenderComponent<MudPopoverProvider>();
        ctx.RenderComponent<MudDialogProvider>();
        ctx.RenderComponent<MudSnackbarProvider>();

        return ctx;
    }
}
