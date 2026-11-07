using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Dabbler.Security.Services;

/// <summary>
/// Default <see cref="IRenderCorrelationScope"/> implementation. Generates a
/// fresh correlation identifier when the scope is constructed (which, for
/// Blazor Server, coincides with circuit start) and regenerates on every
/// <see cref="NavigationManager.LocationChanged"/> notification when a
/// <see cref="NavigationManager"/> is registered in the active scope. In
/// non-Blazor scopes (ASP.NET Core API requests, integration tests, console
/// callers) the navigation hook is silently inactive — call
/// <see cref="ResetForRequest"/> at the request boundary instead.
/// </summary>
/// <remarks>
/// Lives in <c>Dabbler.Security</c> rather than <c>Dabbler.Security.Blazor</c>
/// because the host registers it once via <see cref="Dabbler.Security.Extensions.ServiceCollectionExtensions.AddDabblerSecurity"/>
/// and consumes it from both Blazor circuits (for navigation-driven
/// regeneration) and API request handlers (for <see cref="ResetForRequest"/>-driven
/// regeneration). The optional <see cref="NavigationManager"/> dependency is
/// resolved through the service provider and gracefully tolerated when
/// absent or uninitialized; <c>Microsoft.AspNetCore.App</c> is already a
/// framework reference for this assembly via <c>AuthenticationStateProvider</c>
/// and <c>IHttpContextAccessor</c>, so consuming <c>NavigationManager</c>
/// here does not introduce a new dependency tier.
/// </remarks>
public sealed class RenderCorrelationScope : IRenderCorrelationScope, IDisposable
{
    private readonly object _lock = new();
    private readonly HashSet<string> _emittedKeys = new(StringComparer.Ordinal);
    private NavigationManager? _navigationManager;
    private Guid _current = Guid.NewGuid();

    /// <summary>
    /// Initializes a new <see cref="RenderCorrelationScope"/>. The
    /// <see cref="NavigationManager"/> is resolved through the service
    /// provider so the same registration works in Blazor circuits (where
    /// the framework registers a circuit-scoped NavigationManager) and in
    /// API contexts (where it is either not registered or not yet
    /// initialized and the lookup either returns <c>null</c> or throws on
    /// event subscription).
    /// </summary>
    /// <param name="serviceProvider">The active service provider.</param>
    public RenderCorrelationScope(IServiceProvider serviceProvider)
    {
        _navigationManager = (NavigationManager?)serviceProvider.GetService(typeof(NavigationManager));
        if (_navigationManager is null) return;

        try
        {
            _navigationManager.LocationChanged += OnLocationChanged;
        }
        catch (InvalidOperationException)
        {
            // The NavigationManager exists in the scope but is not yet
            // initialized — typical when this scope is resolved inside a
            // non-Blazor HTTP request that does not progress to a circuit
            // (test harnesses, prerender-disabled pages, plain MVC
            // endpoints). In that case there is no navigation lifecycle
            // to subscribe to; the scope still functions as a per-scope
            // correlation source and ResetForRequest() drives any
            // request-boundary regeneration the API host arranges.
            _navigationManager = null;
        }
    }

    /// <inheritdoc />
    public Guid Current
    {
        get { lock (_lock) return _current; }
    }

    /// <inheritdoc />
    public void ResetForRequest()
    {
        lock (_lock)
        {
            _current = Guid.NewGuid();
            _emittedKeys.Clear();
        }
    }

    /// <inheritdoc />
    public bool TryRecordOnce(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        lock (_lock)
        {
            return _emittedKeys.Add(key);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_navigationManager is not null)
            _navigationManager.LocationChanged -= OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        // A new logical user-facing action begins on every navigation, so
        // the correlation refreshes and the per-correlation de-dupe set
        // resets — a sensitive read on the destination page is its own
        // event, not a repeat of a read on the previous page.
        ResetForRequest();
    }
}
