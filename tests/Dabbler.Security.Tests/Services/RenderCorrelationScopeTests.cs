using Dabbler.Security.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dabbler.Security.Tests.Services;

/// <summary>
/// Unit coverage for <see cref="RenderCorrelationScope"/>: a stable Guid is
/// produced per scope, the de-dupe set obeys the correlation boundary, and a
/// navigation event regenerates both. The Blazor circuit binding is exercised
/// through a fake <see cref="NavigationManager"/> so the test does not need a
/// hosted Blazor circuit.
/// </summary>
public class RenderCorrelationScopeTests
{
    [Fact]
    public void Current_IsStableUntilReset()
    {
        var scope = new RenderCorrelationScope(EmptyServiceProvider());

        var first = scope.Current;
        var second = scope.Current;

        Assert.NotEqual(Guid.Empty, first);
        Assert.Equal(first, second);
    }

    [Fact]
    public void DifferentScopeInstances_HaveDifferentCorrelationIds()
    {
        var a = new RenderCorrelationScope(EmptyServiceProvider());
        var b = new RenderCorrelationScope(EmptyServiceProvider());

        Assert.NotEqual(a.Current, b.Current);
    }

    [Fact]
    public void ResetForRequest_GeneratesNewCorrelationAndClearsDedupe()
    {
        var scope = new RenderCorrelationScope(EmptyServiceProvider());
        var initial = scope.Current;

        Assert.True(scope.TryRecordOnce("k1"));
        Assert.False(scope.TryRecordOnce("k1"));

        scope.ResetForRequest();

        Assert.NotEqual(initial, scope.Current);
        Assert.True(scope.TryRecordOnce("k1"));
    }

    [Fact]
    public void TryRecordOnce_ReturnsTrueOnFirstSightFalseOnRepeat()
    {
        var scope = new RenderCorrelationScope(EmptyServiceProvider());

        Assert.True(scope.TryRecordOnce("a"));
        Assert.True(scope.TryRecordOnce("b"));
        Assert.False(scope.TryRecordOnce("a"));
        Assert.False(scope.TryRecordOnce("b"));
    }

    [Fact]
    public void NavigationLocationChanged_RegeneratesCorrelationAndClearsDedupe()
    {
        var nav = new TestNavigationManager();
        var sp = new ServiceCollection()
            .AddSingleton<NavigationManager>(nav)
            .BuildServiceProvider();

        var scope = new RenderCorrelationScope(sp);
        var before = scope.Current;
        Assert.True(scope.TryRecordOnce("k"));

        nav.RaiseLocationChanged("https://app.local/customers/2");

        Assert.NotEqual(before, scope.Current);
        Assert.True(scope.TryRecordOnce("k"),
            "the dedupe set must reset alongside the correlation id so the same logical key emits again");
    }

    [Fact]
    public void Dispose_UnsubscribesFromNavigation_NoFurtherRegeneration()
    {
        var nav = new TestNavigationManager();
        var sp = new ServiceCollection()
            .AddSingleton<NavigationManager>(nav)
            .BuildServiceProvider();

        var scope = new RenderCorrelationScope(sp);
        var before = scope.Current;

        scope.Dispose();
        nav.RaiseLocationChanged("https://app.local/anywhere");

        Assert.Equal(before, scope.Current);
    }

    [Fact]
    public void NoNavigationManagerRegistered_ScopeStillFunctions()
    {
        var sp = new ServiceCollection().BuildServiceProvider();

        var scope = new RenderCorrelationScope(sp);

        Assert.NotEqual(Guid.Empty, scope.Current);
        Assert.True(scope.TryRecordOnce("api-context-key"));
        Assert.False(scope.TryRecordOnce("api-context-key"));

        scope.ResetForRequest();
        Assert.True(scope.TryRecordOnce("api-context-key"));
    }

    private static IServiceProvider EmptyServiceProvider() =>
        new ServiceCollection().BuildServiceProvider();

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager()
        {
            Initialize("https://app.local/", "https://app.local/customers/1");
        }

        public void RaiseLocationChanged(string newUri)
        {
            // NavigationManager.Uri has a protected setter; the base class
            // raises LocationChanged from the parameterless overload using
            // whatever Uri currently exposes.
            Uri = newUri;
            NotifyLocationChanged(isInterceptedLink: false);
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            Uri = uri;
            NotifyLocationChanged(isInterceptedLink: false);
        }
    }
}
