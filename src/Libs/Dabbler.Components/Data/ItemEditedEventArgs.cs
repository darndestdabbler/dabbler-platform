// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Event args raised after an item edit has ended (commit or cancel).
/// </summary>
/// <typeparam name="T">The type of data item.</typeparam>
public class ItemEditedEventArgs<T> : EventArgs where T : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ItemEditedEventArgs{T}"/> class.
    /// </summary>
    public ItemEditedEventArgs(T item, EditAction action)
    {
        Item = item;
        Action = action;
    }

    /// <summary>Gets the data item that was edited.</summary>
    public T Item { get; }

    /// <summary>Gets the action that occurred (commit or cancel).</summary>
    public EditAction Action { get; }
}
