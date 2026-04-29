using Dabbler.Testing.Blazor.Configuration;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using System.Collections.Concurrent;

namespace Dabbler.Testing.Blazor.Components;

/// <summary>
/// Base class for Blazor components that track async actions for deterministic render-wait synchronization in Playwright tests.
/// </summary>
public class TrackedComponentBase : ComponentBase
{
    /// <summary>
    /// JS runtime for interop calls.
    /// </summary>
    [Inject]
    protected IJSRuntime JS { get; set; } = null!;

    /// <summary>
    /// Action tracking configuration.
    /// </summary>
    [Inject]
    protected IOptions<TrackedActionsOptions> Options { get; set; } = null!;

    private readonly ConcurrentQueue<string> _pendingCompletions = new();

    /// <summary>
    /// Wraps an async action with tracking that sets DOM watermarks for test synchronization.
    /// When disabled, this is a transparent pass-through with no overhead.
    /// </summary>
    /// <param name="name">Action identifier (appears in data-actions-in-flight and data-last-action).</param>
    /// <param name="work">The async work to execute.</param>
    protected async Task TrackAction(string name, Func<Task> work)
    {
        if (!Options.Value.Enabled)
        {
            await work();
            return;
        }

        var correlationId = $"{name}-{Guid.NewGuid():N}";
        await JS.InvokeVoidAsync("dabbler.beginAction", correlationId);

        try
        {
            await work();
        }
        finally
        {
            _pendingCompletions.Enqueue(correlationId);
            RequestRender();
        }
    }

    /// <summary>
    /// Requests a re-render so <see cref="OnAfterRenderAsync"/> can drain pending completions.
    /// Virtual so test harnesses that run outside a rendering context can override with a no-op.
    /// </summary>
    protected virtual void RequestRender() => StateHasChanged();

    /// <summary>
    /// Flushes pending action completions to the JS watermark after each render.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (!Options.Value.Enabled)
            return;

        while (_pendingCompletions.TryDequeue(out var completionId))
        {
            await JS.InvokeVoidAsync("dabbler.endAction", completionId);
        }
    }
}
