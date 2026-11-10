// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using System.ComponentModel;
using System.Reflection;

namespace Dabbler.Components.Data;

/// <summary>
/// Manages dirty tracking, original value snapshots, IEditableObject lifecycle,
/// and batch operations for MudDataGrid editing. Wire to MudDataGrid's
/// <c>StartedEditingItem</c>, <c>CommittedItemChanges</c>, and <c>CanceledEditingItem</c> callbacks.
/// </summary>
/// <typeparam name="T">The type of data item.</typeparam>
public class GridEditingService<T> where T : class
{
    private static readonly PropertyInfo[] _writableProperties = typeof(T)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanRead && p.CanWrite)
        .ToArray();

    private readonly Dictionary<T, T> _snapshots = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<T> _dirtyItems = new(ReferenceEqualityComparer.Instance);
    private readonly Func<T, T> _cloneFunc;

    /// <summary>
    /// Initializes a new instance of the <see cref="GridEditingService{T}"/> class.
    /// </summary>
    /// <param name="cloneFunc">
    /// Optional function to create a deep copy of an item for snapshot purposes.
    /// If not provided, a shallow property-copy clone is used.
    /// </param>
    public GridEditingService(Func<T, T>? cloneFunc = null)
    {
        _cloneFunc = cloneFunc ?? ShallowClone;
    }

    /// <summary>
    /// Gets whether an item is currently being edited.
    /// </summary>
    public bool IsEditing { get; private set; }

    /// <summary>
    /// Gets the item currently being edited, or <c>null</c> if no edit is in progress.
    /// </summary>
    public T? EditingItem { get; private set; }

    /// <summary>
    /// Gets the set of items that have been modified since their last snapshot.
    /// </summary>
    public IReadOnlyCollection<T> DirtyItems => _dirtyItems;

    /// <summary>
    /// Gets the number of dirty items.
    /// </summary>
    public int DirtyCount => _dirtyItems.Count;

    /// <summary>
    /// Gets whether any items have uncommitted changes.
    /// </summary>
    public bool HasDirtyItems => _dirtyItems.Count > 0;

    /// <summary>
    /// Raised before an item edit is committed. Set <see cref="CancelEventArgs.Cancel"/>
    /// to <c>true</c> to prevent the commit.
    /// </summary>
    public event EventHandler<ItemValidatingEventArgs<T>>? ItemValidating;

    /// <summary>
    /// Raised after an item edit has been committed or canceled.
    /// </summary>
    public event EventHandler<ItemEditedEventArgs<T>>? ItemEdited;

    /// <summary>
    /// Raised when the dirty state of any item changes (item becomes dirty or is cleared).
    /// </summary>
    public event EventHandler? DirtyStateChanged;

    /// <summary>
    /// Called when MudDataGrid begins editing an item. Snapshots the original state
    /// and calls <see cref="IEditableObject.BeginEdit"/> if supported.
    /// Wire this to <c>MudDataGrid.StartedEditingItem</c>.
    /// </summary>
    public void OnStartedEditing(T item)
    {
        ArgumentNullException.ThrowIfNull(item);

        IsEditing = true;
        EditingItem = item;

        // Snapshot original state if we don't already have one.
        // Keep the first snapshot so we can revert to the true original
        // even after multiple edits to the same item.
        if (!_snapshots.ContainsKey(item))
        {
            _snapshots[item] = _cloneFunc(item);
        }

        if (item is IEditableObject editable)
            editable.BeginEdit();
    }

    /// <summary>
    /// Validates the item before commit. Returns <c>false</c> if validation was canceled.
    /// Call this from your <c>CommittedItemChanges</c> handler before accepting the commit.
    /// </summary>
    public bool Validate(T item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var args = new ItemValidatingEventArgs<T>(item);
        ItemValidating?.Invoke(this, args);
        return !args.Cancel;
    }

    /// <summary>
    /// Called when MudDataGrid has committed changes to an item.
    /// Marks the item as dirty if values differ from the snapshot,
    /// and calls <see cref="IEditableObject.EndEdit"/> if supported.
    /// Wire this to <c>MudDataGrid.CommittedItemChanges</c>.
    /// </summary>
    public void OnItemCommitted(T item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item is IEditableObject editable)
            editable.EndEdit();

        // Compare with snapshot to determine dirty state
        var wasDirty = _dirtyItems.Contains(item);
        if (_snapshots.TryGetValue(item, out var snapshot))
        {
            if (HasChanges(item, snapshot))
                _dirtyItems.Add(item);
            else
                _dirtyItems.Remove(item);
        }
        else
        {
            // No snapshot means this is a new item or was never tracked — mark dirty
            _dirtyItems.Add(item);
        }

        var dirtyChanged = wasDirty != _dirtyItems.Contains(item);

        IsEditing = false;
        EditingItem = null;

        ItemEdited?.Invoke(this, new ItemEditedEventArgs<T>(item, EditAction.Commit));

        if (dirtyChanged)
            DirtyStateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Called when MudDataGrid has canceled editing an item.
    /// Calls <see cref="IEditableObject.CancelEdit"/> if supported.
    /// Wire this to <c>MudDataGrid.CanceledEditingItem</c>.
    /// </summary>
    public void OnItemCanceled(T item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item is IEditableObject editable)
            editable.CancelEdit();

        IsEditing = false;
        EditingItem = null;

        ItemEdited?.Invoke(this, new ItemEditedEventArgs<T>(item, EditAction.Cancel));
    }

    /// <summary>
    /// Gets whether the specified item has been modified from its original snapshot.
    /// </summary>
    public bool IsItemDirty(T item) => _dirtyItems.Contains(item);

    /// <summary>
    /// Gets the original snapshot of the specified item, or <c>null</c> if no snapshot exists.
    /// </summary>
    public T? GetOriginalSnapshot(T item)
    {
        return _snapshots.TryGetValue(item, out var snapshot) ? snapshot : null;
    }

    /// <summary>
    /// Reverts a specific item to its original snapshot values.
    /// </summary>
    /// <returns><c>true</c> if the item was reverted; <c>false</c> if no snapshot exists.</returns>
    public bool RevertItem(T item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!_snapshots.TryGetValue(item, out var snapshot))
            return false;

        CopyProperties(snapshot, item);
        _snapshots.Remove(item);

        var wasDirty = _dirtyItems.Remove(item);

        if (wasDirty)
            DirtyStateChanged?.Invoke(this, EventArgs.Empty);

        return true;
    }

    /// <summary>
    /// Reverts all dirty items to their original snapshot values.
    /// </summary>
    public void RevertAll()
    {
        if (_dirtyItems.Count == 0 && _snapshots.Count == 0)
            return;

        foreach (var (item, snapshot) in _snapshots)
        {
            CopyProperties(snapshot, item);
        }

        var hadDirty = _dirtyItems.Count > 0;
        _snapshots.Clear();
        _dirtyItems.Clear();

        if (hadDirty)
            DirtyStateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Clears the dirty state for all items, accepting their current values as the new baseline.
    /// Removes all snapshots.
    /// </summary>
    public void AcceptAll()
    {
        var hadDirty = _dirtyItems.Count > 0;
        _snapshots.Clear();
        _dirtyItems.Clear();

        if (hadDirty)
            DirtyStateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Clears the dirty state for a specific item, accepting its current value as the new baseline.
    /// Removes its snapshot.
    /// </summary>
    public void AcceptItem(T item)
    {
        ArgumentNullException.ThrowIfNull(item);

        _snapshots.Remove(item);
        var wasDirty = _dirtyItems.Remove(item);

        if (wasDirty)
            DirtyStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private static bool HasChanges(T current, T snapshot)
    {
        foreach (var prop in _writableProperties)
        {
            var currentVal = prop.GetValue(current);
            var snapshotVal = prop.GetValue(snapshot);
            if (!Equals(currentVal, snapshotVal))
                return true;
        }
        return false;
    }

    private static T ShallowClone(T source)
    {
        var clone = (T)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(T));
        CopyProperties(source, clone);
        return clone;
    }

    private static void CopyProperties(T source, T target)
    {
        foreach (var prop in _writableProperties)
        {
            prop.SetValue(target, prop.GetValue(source));
        }
    }
}
