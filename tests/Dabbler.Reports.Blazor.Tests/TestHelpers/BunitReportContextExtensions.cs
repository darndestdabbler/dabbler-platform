using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Dabbler.Reports.Blazor.Tests.TestHelpers;

public static class BunitReportContextExtensions
{
    public static TestContext SetupForDabblerReports(this TestContext ctx)
    {
        ctx.Services.AddMudServices(cfg =>
        {
            cfg.SnackbarConfiguration.ShowTransitionDuration = 0;
            cfg.SnackbarConfiguration.HideTransitionDuration = 0;
        });

        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        ctx.RenderComponent<MudBlazor.MudPopoverProvider>();
        ctx.RenderComponent<MudBlazor.MudDialogProvider>();
        ctx.RenderComponent<MudBlazor.MudSnackbarProvider>();

        return ctx;
    }
}
