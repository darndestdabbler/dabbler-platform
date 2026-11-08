using Dabbler.Security.Configuration;
using Microsoft.Extensions.Logging;

namespace Dabbler.Security.Services;

/// <summary>
/// Immutable event payload fired when the <see cref="SecurityConfiguration"/> changes.
/// </summary>
/// <param name="Configuration">The new security configuration.</param>
/// <param name="ChangedAt">The timestamp when the change was detected.</param>
public sealed record AuthorizationConfigurationChanged(
    SecurityConfiguration Configuration,
    DateTimeOffset ChangedAt);

/// <summary>
/// Singleton service that bridges <see cref="SecurityConfigurationManager.ConfigurationChanged"/>
/// (a Task-based multicast delegate) to a pub/sub pattern that Blazor components can subscribe to
/// via <c>OnInitialized</c> / <c>Dispose</c>.
/// <para>
/// Usage in a Blazor component:
/// <code>
/// @inject AuthorizationStateNotifier Notifier
/// @implements IDisposable
///
/// protected override void OnInitialized()
///     => Notifier.Subscribe(OnConfigChangedAsync);
///
/// private async Task OnConfigChangedAsync(AuthorizationConfigurationChanged evt)
///     => await InvokeAsync(StateHasChanged);
///
/// public void Dispose()
///     => Notifier.Unsubscribe(OnConfigChangedAsync);
/// </code>
/// </para>
/// </summary>
public sealed class AuthorizationStateNotifier : IDisposable
{
    private readonly SecurityConfigurationManager _manager;
    private readonly ILogger<AuthorizationStateNotifier> _logger;
    private readonly object _lock = new();
    private readonly List<Func<AuthorizationConfigurationChanged, Task>> _subscribers = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationStateNotifier"/> class.
    /// </summary>
    /// <param name="manager">The configuration manager to monitor for changes.</param>
    /// <param name="logger">The logger for this service.</param>
    public AuthorizationStateNotifier(
        SecurityConfigurationManager manager,
        ILogger<AuthorizationStateNotifier> logger)
    {
        _manager = manager;
        _logger = logger;
        _manager.ConfigurationChanged += OnConfigurationChangedAsync;
    }

    /// <summary>
    /// Registers a callback to be invoked when the configuration changes.
    /// </summary>
    /// <param name="handler">The asynchronous callback to execute.</param>
    public void Subscribe(Func<AuthorizationConfigurationChanged, Task> handler)
    {
        lock (_lock)
            _subscribers.Add(handler);
    }

    /// <summary>
    /// Removes a previously registered callback.
    /// </summary>
    /// <param name="handler">The callback to remove.</param>
    public void Unsubscribe(Func<AuthorizationConfigurationChanged, Task> handler)
    {
        lock (_lock)
            _subscribers.Remove(handler);
    }

    private async Task OnConfigurationChangedAsync(SecurityConfiguration config)
    {
        var evt = new AuthorizationConfigurationChanged(config, DateTimeOffset.UtcNow);

        List<Func<AuthorizationConfigurationChanged, Task>> snapshot;
        lock (_lock)
            snapshot = [.. _subscribers];

        foreach (var subscriber in snapshot)
        {
            try
            {
                await subscriber(evt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An AuthorizationStateNotifier subscriber threw an exception.");
            }
        }
    }

    /// <summary>
    /// Unsubscribes from the configuration changed event.
    /// </summary>
    public void Dispose()
    {
        _manager.ConfigurationChanged -= OnConfigurationChangedAsync;
    }
}
