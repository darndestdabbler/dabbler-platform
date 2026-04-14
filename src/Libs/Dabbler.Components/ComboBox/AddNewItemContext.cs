// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components;

/// <summary>
/// Context passed to <see cref="DabblerComboBox{T}.AddNewTemplate"/> when dialog mode is active.
/// The consumer renders a form using this context and calls <see cref="CompleteAsync"/> or
/// <see cref="CancelAsync"/> when done.
/// </summary>
/// <typeparam name="T">The type of item in the combo box.</typeparam>
public class AddNewItemContext<T>
{
    /// <summary>
    /// The text the user typed before clicking the add-new option.
    /// </summary>
    public string EnteredText { get; init; } = "";

    /// <summary>
    /// Call this when the user has created the new item. The combo box will select the item
    /// and close the dialog.
    /// </summary>
    public Func<T, Task> CompleteAsync { get; init; } = default!;

    /// <summary>
    /// Call this to cancel the add-new flow without selecting anything.
    /// </summary>
    public Func<Task> CancelAsync { get; init; } = default!;
}
