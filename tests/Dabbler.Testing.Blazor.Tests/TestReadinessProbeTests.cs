using Bunit;
using Dabbler.Testing.Blazor.Components;
using Dabbler.Testing.Blazor.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Dabbler.Testing.Blazor.Tests;

public sealed class TestReadinessProbeTests : BunitContext
{
    public TestReadinessProbeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Probe_WhenEnabled_FiresSetReadyOnFirstRenderOnly()
    {
        Services.AddSingleton<IOptions<TrackedActionsOptions>>(
            Options.Create(new TrackedActionsOptions { Enabled = true }));

        var setReady = JSInterop.SetupVoid("dabbler.setReady");

        var cut = Render<TestReadinessProbe>();
        Assert.Single(setReady.Invocations);

        cut.Render();
        cut.Render();
        Assert.Single(setReady.Invocations);
    }

    [Fact]
    public void Probe_WhenDisabled_DoesNotFireSetReady()
    {
        Services.AddSingleton<IOptions<TrackedActionsOptions>>(
            Options.Create(new TrackedActionsOptions { Enabled = false }));

        var setReady = JSInterop.SetupVoid("dabbler.setReady");

        Render<TestReadinessProbe>();

        Assert.Empty(setReady.Invocations);
    }
}
