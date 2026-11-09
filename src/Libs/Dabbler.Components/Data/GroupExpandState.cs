// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Tracks which groups are expanded in a grouped data grid.
/// Groups are identified by a composite key (level + group key value).
/// Default state is collapsed.
/// </summary>
public class GroupExpandState
{
    // When _defaultExpanded is false: _overrides contains explicitly expanded groups.
    // When _defaultExpanded is true (after ExpandAll): _overrides contains explicitly collapsed groups.
    private bool _defaultExpanded;
    private readonly HashSet<(int Level, string? KeyString)> _overrides = new();

    /// <summary>
    /// Gets whether the specified group is expanded.
    /// </summary>
    public bool IsExpanded(int level, object? groupKey)
    {
        bool overridden = _overrides.Contains((level, groupKey?.ToString()));
        return overridden ? !_defaultExpanded : _defaultExpanded;
    }

    /// <summary>
    /// Toggles the expand/collapse state of a group.
    /// </summary>
    public void Toggle(int level, object? groupKey)
    {
        var key = (level, groupKey?.ToString());
        if (!_overrides.Remove(key))
            _overrides.Add(key);
        StateChanged?.Invoke();
    }

    /// <summary>
    /// Sets the expand/collapse state of a group.
    /// </summary>
    public void SetExpanded(int level, object? groupKey, bool expanded)
    {
        bool current = IsExpanded(level, groupKey);
        if (current == expanded) return;

        var key = (level, groupKey?.ToString());
        if (!_overrides.Remove(key))
            _overrides.Add(key);
        StateChanged?.Invoke();
    }

    /// <summary>
    /// Expands all groups at all levels.
    /// </summary>
    public void ExpandAll()
    {
        _defaultExpanded = true;
        _overrides.Clear();
        StateChanged?.Invoke();
    }

    /// <summary>
    /// Collapses all groups at all levels.
    /// </summary>
    public void CollapseAll()
    {
        _defaultExpanded = false;
        _overrides.Clear();
        StateChanged?.Invoke();
    }

    /// <summary>
    /// Returns the count of currently expanded groups.
    /// When the default state is collapsed (normal mode, before <see cref="ExpandAll"/> is called),
    /// this equals the number of explicitly expanded groups.
    /// After <see cref="ExpandAll"/> is called the value is not meaningful, because the total number
    /// of groups is not known to this class — it returns 0 in that state.
    /// </summary>
    public int ExpandedCount => _defaultExpanded ? 0 : _overrides.Count;

    /// <summary>
    /// Event raised when any expand/collapse state changes.
    /// </summary>
    public event Action? StateChanged;
}
