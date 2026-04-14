// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Event arguments raised when the user enters text that does not match any item in the list.
/// </summary>
public class ValueNotInListEventArgs : EventArgs
{
    /// <summary>
    /// Gets the text the user entered that did not match any item.
    /// </summary>
    public string EnteredText { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the event has been handled.
    /// When <c>true</c>, the control keeps the entered text.
    /// When <c>false</c> (default), the control reverts to the previously selected value's display text.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueNotInListEventArgs"/> class.
    /// </summary>
    /// <param name="enteredText">The text entered by the user.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="enteredText"/> is null.</exception>
    public ValueNotInListEventArgs(string enteredText)
    {
        EnteredText = enteredText ?? throw new ArgumentNullException(nameof(enteredText));
    }
}
