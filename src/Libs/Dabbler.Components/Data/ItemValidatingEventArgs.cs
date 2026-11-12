// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using System.ComponentModel;

namespace Dabbler.Components.Data;

/// <summary>
/// Event args raised before an item edit is committed. Can be canceled to prevent the commit.
/// </summary>
/// <typeparam name="T">The type of data item.</typeparam>
public class ItemValidatingEventArgs<T> : CancelEventArgs where T : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ItemValidatingEventArgs{T}"/> class.
    /// </summary>
    public ItemValidatingEventArgs(T item)
    {
        Item = item;
    }

    /// <summary>Gets the data item being validated.</summary>
    public T Item { get; }

    /// <summary>Gets or sets the error message if validation fails.</summary>
    public string? ErrorMessage { get; set; }
}
