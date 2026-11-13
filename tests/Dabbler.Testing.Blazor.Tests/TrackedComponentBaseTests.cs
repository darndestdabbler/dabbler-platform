using Bunit;
using Dabbler.Testing.Blazor.Components;
using Dabbler.Testing.Blazor.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Xunit;

namespace Dabbler.Testing.Blazor.Tests;

// bUnit coverage for TrackedComponentBase.
//
// Disabled-path test (Enabled=false) uses bUnit and was added in Session 1.
// It remains the only bUnit test for this class because calling
// cut.InvokeAsync(() => cut.Instance.RunAsync(...)) with Enabled=true deadlocks
// bUnit 2.6's renderer dispatcher: the InvokeAsync path blocks the renderer
// context while awaiting JS.InvokeVoidAsync, and the JS completion needs the
// same context to marshal, causing a deadlock. Lifecycle-triggered tests (via
// OnParametersSetAsync) also fail because bUnit does not fully await async
// lifecycle tasks before RenderComponent returns, so assertions execute before
// the JS calls fire.
//
// Edge-case tests (exception safety and concurrent in-flight IDs) use plain
// xUnit with a RecordingJSRuntime mock, which completes JS calls synchronously
// and avoids the bUnit synchronization-context constraint. DirectHarness also
// overrides RequestRender() (introduced in Session 4) to be a no-op, since
// StateHasChanged() throws outside a rendering context.
//
// The full enabled-path behaviour (watermark transition [id] → [] in a browser)
// remains covered by the Playwright proof test E2E_WF_04.
public sealed class TrackedComponentBaseBunitTests : TestContext
{
    public TrackedComponentBaseBunitTests()
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

/// <summary>
/// Plain xUnit tests for TrackedComponentBase edge cases that require the
/// enabled path. Uses RecordingJSRuntime to avoid bUnit synchronization-context
/// constraints. DirectHarness overrides RequestRender() so StateHasChanged is
/// not called outside a rendering context.
/// </summary>
public sealed class TrackedComponentBaseDirectTests
{
    // ── Exception safety ─────────────────────────────────────────────

    [Fact]
    public async Task TrackAction_WhenEnabled_WorkThrows_StillCallsEndAction()
    {
        // Verifies that a throwing handler does not leave an orphaned
        // in-flight ID. The finally block enqueues the completion even
        // on exception; OnAfterRenderAsync (simulated by FlushAsync) drains it.
        var js = new RecordingJSRuntime();
        var opts = Options.Create(new TrackedActionsOptions { Enabled = true });
        var harness = new DirectHarness(js, opts);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.RunAsync("test", () => throw new InvalidOperationException("test exception")));

        Assert.Single(js.Calls, c => c.StartsWith("dabbler.beginAction"));

        await harness.FlushAsync();

        Assert.Single(js.Calls, c => c.StartsWith("dabbler.endAction"));
    }

    // ── JS-not-ready resilience ─────────────────────────────────────

    [Fact]
    public async Task TrackAction_WhenEnabled_JsInteropThrowsInvalidOperation_SilentlyDegradesAndRunsWork()
    {
        // Session 5 hardening: when JS interop throws InvalidOperationException
        // (e.g. during prerender or before the SignalR circuit is dispatchable),
        // TrackAction must still run work() and must not bubble the exception.
        // This prevents an early-fire of a TrackAction-wrapped event handler from
        // turning into an HTTP 500 — the failure mode that broke /admin/products
        // for non-SystemAdmin roles when wrapping ProductManagement.OnSearchAsync.
        // The watermark silently no-ops in that frame; the user-visible work
        // proceeds.
        var js = new ThrowingJSRuntime();
        var opts = Options.Create(new TrackedActionsOptions { Enabled = true });
        var harness = new DirectHarness(js, opts);

        var ranWork = false;
        await harness.RunAsync("save", () =>
        {
            ranWork = true;
            return Task.CompletedTask;
        });

        Assert.True(ranWork);
        Assert.Equal(1, js.AttemptedCalls);

        // Because the begin call failed, no completion is enqueued and
        // OnAfterRenderAsync drains nothing — i.e. no endAction either.
        await harness.FlushAsync();
        Assert.Equal(1, js.AttemptedCalls);
    }

    // ── Concurrent in-flight IDs ─────────────────────────────────────

    [Fact]
    public async Task TrackAction_WhenEnabled_ConcurrentCalls_TracksBothInFlightIds()
    {
        // Verifies that two concurrent TrackAction calls correctly maintain
        // two distinct in-flight IDs. Both beginAction and endAction must
        // fire once per call.
        var js = new RecordingJSRuntime();
        var opts = Options.Create(new TrackedActionsOptions { Enabled = true });
        var harness = new DirectHarness(js, opts);

        await Task.WhenAll(
            harness.RunAsync("action-1", () => Task.CompletedTask),
            harness.RunAsync("action-2", () => Task.CompletedTask));

        Assert.Equal(2, js.Calls.Count(c => c.StartsWith("dabbler.beginAction")));

        await harness.FlushAsync();

        Assert.Equal(2, js.Calls.Count(c => c.StartsWith("dabbler.endAction")));
    }

    // ── Test infrastructure ──────────────────────────────────────────

    private sealed class DirectHarness : TrackedComponentBase
    {
        internal DirectHarness(IJSRuntime js, IOptions<TrackedActionsOptions> opts)
        {
            JS = js;
            Options = opts;
        }

        internal Task RunAsync(string name, Func<Task> work) => TrackAction(name, work);

        internal Task FlushAsync() => OnAfterRenderAsync(firstRender: false);

        // No-op outside a render context; StateHasChanged would throw.
        protected override void RequestRender() { }
    }

    private sealed class RecordingJSRuntime : IJSRuntime
    {
        public List<string> Calls { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Calls.Add($"{identifier}({args?[0]})");
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            Calls.Add($"{identifier}({args?[0]})");
            return ValueTask.FromResult(default(TValue)!);
        }
    }

    private sealed class ThrowingJSRuntime : IJSRuntime
    {
        public int AttemptedCalls { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            AttemptedCalls++;
            throw new InvalidOperationException(
                "JavaScript interop calls cannot be issued at this time. (simulated)");
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            AttemptedCalls++;
            throw new InvalidOperationException(
                "JavaScript interop calls cannot be issued at this time. (simulated)");
        }
    }
}
