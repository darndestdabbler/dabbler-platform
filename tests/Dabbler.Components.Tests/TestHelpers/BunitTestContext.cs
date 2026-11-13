using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace Dabbler.Components.Tests.TestHelpers;

public static class BunitContextExtensions
{
    public static BunitContext SetupForDabblerComponents(this BunitContext ctx)
    {
        ctx.Services.AddMudServices(cfg =>
        {
            cfg.SnackbarConfiguration.ShowTransitionDuration = 0;
            cfg.SnackbarConfiguration.HideTransitionDuration = 0;
        });

        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        ctx.Render<MudPopoverProvider>();
        ctx.Render<MudDialogProvider>();
        ctx.Render<MudSnackbarProvider>();

        return ctx;
    }
}
