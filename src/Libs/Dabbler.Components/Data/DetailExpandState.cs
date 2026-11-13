// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Tracks which rows have their detail/subgrid expanded.
/// Rows are identified by keys of type <typeparamref name="TKey"/>.
/// </summary>
/// <typeparam name="TKey">The key type used to identify rows.</typeparam>
public class DetailExpandState<TKey> where TKey : notnull
{
    private readonly HashSet<TKey> _expanded = new();

    /// <summary>
    /// Expand mode: <see cref="DetailExpandMode.Single"/> (only one row expanded at a time)
    /// or <see cref="DetailExpandMode.Multiple"/>.
    /// </summary>
    public DetailExpandMode Mode { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="DetailExpandState{TKey}"/>.
    /// </summary>
    /// <param name="mode">The expand mode. Defaults to <see cref="DetailExpandMode.Multiple"/>.</param>
    public DetailExpandState(DetailExpandMode mode = DetailExpandMode.Multiple)
    {
        Mode = mode;
    }

    /// <summary>
    /// Gets whether the row with the specified key is expanded.
    /// </summary>
    public bool IsExpanded(TKey key) => _expanded.Contains(key);

    /// <summary>
    /// Toggles the expand/collapse state of the row with the specified key.
    /// In <see cref="DetailExpandMode.Single"/> mode, expanding a row collapses all others.
    /// </summary>
    public void Toggle(TKey key)
    {
        if (_expanded.Contains(key))
        {
            _expanded.Remove(key);
        }
        else
        {
            if (Mode == DetailExpandMode.Single)
                _expanded.Clear();
            _expanded.Add(key);
        }

        StateChanged?.Invoke();
    }

    /// <summary>
    /// Sets the expand/collapse state of the row with the specified key.
    /// In <see cref="DetailExpandMode.Single"/> mode, expanding a row collapses all others.
    /// </summary>
    public void SetExpanded(TKey key, bool expanded)
    {
        bool changed;

        if (expanded)
        {
            if (Mode == DetailExpandMode.Single && !_expanded.Contains(key))
                _expanded.Clear();

            changed = _expanded.Add(key);
        }
        else
        {
            changed = _expanded.Remove(key);
        }

        if (changed)
            StateChanged?.Invoke();
    }

    /// <summary>
    /// Collapses all expanded rows. Does not raise <see cref="StateChanged"/> if nothing was expanded.
    /// </summary>
    public void CollapseAll()
    {
        if (_expanded.Count > 0)
        {
            _expanded.Clear();
            StateChanged?.Invoke();
        }
    }

    /// <summary>
    /// Gets the number of currently expanded rows.
    /// </summary>
    public int ExpandedCount => _expanded.Count;

    /// <summary>
    /// Event raised when any expand/collapse state changes.
    /// </summary>
    public event Action? StateChanged;
}
