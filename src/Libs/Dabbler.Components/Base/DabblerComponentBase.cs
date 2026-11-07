// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace Dabbler.Components;

/// <summary>
/// Represents a base class for designing standalone Dabbler components.
/// </summary>
/// <remarks>
/// <para>
/// Components built inside the MudBlazor fork should inherit from <see cref="MudComponentBase"/> instead.
/// This base class is for standalone components in the <c>Dabbler.Components</c> library (e.g., Reporting Engine).
/// </para>
/// <para>
/// Provides common parameters (<see cref="Class"/>, <see cref="Style"/>, <see cref="Tag"/>, <see cref="UserAttributes"/>),
/// lifecycle tracking (<see cref="HasRendered"/>), logging, and <see cref="IAsyncDisposable"/> support.
/// </para>
/// </remarks>
public abstract class DabblerComponentBase : ComponentBase, IAsyncDisposable
{
    private ILogger? _logger;
    private bool _disposed;

    [Inject]
    private ILoggerFactory LoggerFactory { get; set; } = null!;

    /// <summary>
    /// Gets the logger for this component.
    /// </summary>
    protected ILogger Logger => _logger ??= LoggerFactory.CreateLogger(GetType());

    /// <summary>
    /// The CSS classes applied to this component.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>null</c>.  You can use spaces to separate multiple classes.
    /// Use the <see cref="Style"/> property to apply custom CSS styles.
    /// </remarks>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// The CSS styles applied to this component.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>null</c>.  Use the <see cref="Class"/> property to apply CSS classes.
    /// </remarks>
    [Parameter]
    public string? Style { get; set; }

    /// <summary>
    /// The arbitrary object to link to this component.
    /// </summary>
    /// <remarks>
    /// This property is typically used to associate additional information with this component,
    /// such as a model containing data for this component.
    /// </remarks>
    [Parameter]
    public object? Tag { get; set; }

    /// <summary>
    /// The additional HTML attributes to apply to this component.
    /// </summary>
    /// <remarks>
    /// This property is typically used to provide additional HTML attributes during rendering
    /// such as ARIA accessibility tags or a custom ID.
    /// </remarks>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object?> UserAttributes { get; set; } = new Dictionary<string, object?>();

    /// <summary>
    /// Whether the component has executed <see cref="ComponentBase.OnAfterRender"/> at least once.
    /// </summary>
    protected bool HasRendered { get; private set; }

    /// <summary>
    /// Whether JavaScript interop calls can be made.
    /// </summary>
    /// <remarks>
    /// Returns <c>true</c> after the first render has completed.
    /// </remarks>
    protected bool IsJSRuntimeAvailable => HasRendered;

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            HasRendered = true;
        }

        base.OnAfterRender(firstRender);
    }

    /// <summary>
    /// Releases resources used by this component.
    /// </summary>
    /// <remarks>
    /// Override this method to dispose of managed or unmanaged resources.
    /// Always call <c>base.DisposeAsync()</c> when overriding.
    /// </remarks>
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;
        return DisposeAsyncCore();
    }

    /// <summary>
    /// Performs application-defined cleanup of managed resources asynchronously.
    /// </summary>
    /// <remarks>
    /// Override this method in derived classes to perform custom cleanup logic.
    /// </remarks>
    protected virtual ValueTask DisposeAsyncCore()
    {
        return ValueTask.CompletedTask;
    }
}
