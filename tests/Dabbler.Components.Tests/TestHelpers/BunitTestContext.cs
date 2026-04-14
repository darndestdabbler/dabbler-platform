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

        var gridModule = ctx.JSInterop.SetupModule("./_content/Dabbler.Components/dabbler-datagrid.js");
        gridModule.SetupVoid("registerKeyInterceptor", _ => true);
        gridModule.SetupVoid("focusEditorInCell", _ => true);
        gridModule.SetupVoid("focusDetailContent", _ => true);
        gridModule.SetupVoid("focusElement", _ => true);
        gridModule.SetupVoid("applyPinOffsets", _ => true);
        gridModule.SetupVoid("unregisterKeyInterceptor", _ => true);
        gridModule.Setup<bool>("isInputFocused", _ => true).SetResult(false);
        gridModule.Setup<int>("getViewportRowCount", _ => true).SetResult(10);

        var clipboardModule = ctx.JSInterop.SetupModule("./_content/Dabbler.Components/dabbler-clipboard.js");
        clipboardModule.Setup<bool>("writeText", _ => true).SetResult(true);
        clipboardModule.Setup<string?>("readText", _ => true).SetResult(null);
        clipboardModule.SetupVoid("downloadFile", _ => true);

        ctx.RenderComponent<MudPopoverProvider>();
        ctx.RenderComponent<MudDialogProvider>();
        ctx.RenderComponent<MudSnackbarProvider>();

        return ctx;
    }
}
