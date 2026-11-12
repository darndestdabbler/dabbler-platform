using Dabbler.Security.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dabbler.Security.Configuration;

/// <summary>
/// Singleton that holds the in-memory SecurityConfiguration and coordinates
/// load/save operations against the backing store.
///
/// Authorization handlers resolve permissions from Current rather than
/// hitting the store on every request. Call RefreshAsync() to reload after
/// external changes.
/// </summary>
public sealed class SecurityConfigurationManager
{
    private readonly ISecurityConfigurationStore _store;
    private volatile SecurityConfiguration _current = new();
    private volatile bool _loaded;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    /// <summary>Fired after the configuration is saved or refreshed.</summary>
    public event Func<SecurityConfiguration, Task>? ConfigurationChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityConfigurationManager"/>
    /// with the specified configuration store.
    /// </summary>
    /// <param name="store">The <see cref="ISecurityConfigurationStore"/> implementation for persistence.</param>
    public SecurityConfigurationManager(ISecurityConfigurationStore store)
    {
        _store = store;
    }

    /// <summary>
    /// The current in-memory configuration. Callers should await EnsureLoadedAsync()
    /// before accessing this property on first use, or rely on the DI startup call.
    /// </summary>
    public SecurityConfiguration Current => _current;

    /// <summary>
    /// Loads the configuration from the backing store if not already loaded.
    /// Safe to call concurrently — only one load will occur.
    /// </summary>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    public async Task EnsureLoadedAsync(CancellationToken ct = default)
    {
        if (_loaded) return;

        await _loadLock.WaitAsync(ct);
        try
        {
            if (_loaded) return;
            _current = await _store.LoadAsync(ct);
            _loaded = true;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <summary>
    /// Reloads the configuration from the backing store, replacing the in-memory copy.
    /// Fires ConfigurationChanged after the refresh.
    /// </summary>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        await _loadLock.WaitAsync(ct);
        try
        {
            _current = await _store.LoadAsync(ct);
            _loaded = true;
        }
        finally
        {
            _loadLock.Release();
        }

        await RaiseChangedAsync(_current);
    }

    /// <summary>
    /// Persists the given configuration to the backing store, replaces the in-memory copy,
    /// and fires ConfigurationChanged so active Blazor circuits can re-render.
    /// </summary>
    /// <param name="configuration">The <see cref="SecurityConfiguration"/> object to persist and set as current.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    public async Task SaveAsync(SecurityConfiguration configuration, CancellationToken ct = default)
    {
        await _store.SaveAsync(configuration, ct);

        await _loadLock.WaitAsync(ct);
        try
        {
            _current = configuration;
            _loaded = true;
        }
        finally
        {
            _loadLock.Release();
        }

        await RaiseChangedAsync(configuration);
    }

    private async Task RaiseChangedAsync(SecurityConfiguration configuration)
    {
        var handler = ConfigurationChanged;
        if (handler is null) return;

        foreach (var subscriber in handler.GetInvocationList())
        {
            try
            {
                // Each subscriber is invoked in isolation to prevent one faulty handler
                // from breaking configuration propagation to other subscribers.
                await ((Func<SecurityConfiguration, Task>)subscriber).Invoke(configuration);
            }
            catch
            {
                // Isolate subscriber failures so one faulty handler cannot
                // break configuration propagation to other subscribers.
            }
        }
    }
}
