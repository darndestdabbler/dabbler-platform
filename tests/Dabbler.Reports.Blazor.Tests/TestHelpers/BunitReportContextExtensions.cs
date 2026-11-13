using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Dabbler.Reports.Blazor.Tests.TestHelpers;

public static class BunitReportContextExtensions
{
    public static BunitContext SetupForDabblerReports(this BunitContext ctx)
    {
        ctx.Services.AddMudServices(cfg =>
        {
            cfg.SnackbarConfiguration.ShowTransitionDuration = 0;
            cfg.SnackbarConfiguration.HideTransitionDuration = 0;
        });

        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        ctx.Render<MudBlazor.MudPopoverProvider>();
        ctx.Render<MudBlazor.MudDialogProvider>();
        ctx.Render<MudBlazor.MudSnackbarProvider>();

        return ctx;
    }
}
