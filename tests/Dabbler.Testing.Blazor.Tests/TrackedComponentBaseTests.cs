using Bunit;
using Dabbler.Testing.Blazor.Components;
using Dabbler.Testing.Blazor.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Dabbler.Testing.Blazor.Tests;

// bUnit coverage for TrackedComponentBase is intentionally limited to the
// disabled-passthrough path. The enabled path (TrackAction round-tripping
// through `await JS.InvokeVoidAsync("dabbler.beginAction", ...)` from inside
// `cut.InvokeAsync`) deadlocks bUnit 2.6's renderer dispatcher even with
// JSRuntimeMode.Loose and explicit SetupVoid handlers — the testhost hangs
// indefinitely (multi-minute) at the first JS interop call. The enabled
// behaviour is fully covered by the Playwright proof test
// `ProductAdministrationWorkflowTests.E2E_WF_04_..._TogglesProductAvailability...`,
// which observes the real `data-actions-in-flight` watermark transition
// `[id]` → `[]` in a browser via `_fixture.WaitForQuietAsync(page)`. That
// test exercises the full TrackAction → JS shim → DOM watermark pipeline
// against the Blazor Server circuit, which is the deployment shape the
// library is designed for.
public sealed class TrackedComponentBaseTests : TestContext
{
    public TrackedComponentBaseTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public async Task TrackAction_WhenDisabled_IsTransparentPassThroughWithNoJsInterop()
    {
        Services.AddSingleton<IOptions<TrackedActionsOptions>>(
            Options.Create(new TrackedActionsOptions { Enabled = false }));

        var begin = JSInterop.SetupVoid("dabbler.beginAction", _ => true);
        var end = JSInterop.SetupVoid("dabbler.endAction", _ => true);

        var cut = RenderComponent<HarnessComponent>();

        var ranWork = false;
        await cut.InvokeAsync(() => cut.Instance.RunAsync("save", () =>
        {
            ranWork = true;
            return Task.CompletedTask;
        }));

        Assert.True(ranWork);
        Assert.Empty(begin.Invocations);
        Assert.Empty(end.Invocations);
    }

    public sealed class HarnessComponent : TrackedComponentBase
    {
        public Task RunAsync(string name, Func<Task> work) =>
            TrackAction(name, work);
    }
}
