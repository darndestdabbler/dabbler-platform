// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components;

/// <summary>
/// Defines a column in a multi-column dropdown display for
/// <see cref="DabblerComboBox{T}"/> and <see cref="DabblerMultiComboBox{T}"/>.
/// </summary>
/// <typeparam name="T">The type of item in the combo box.</typeparam>
public class ComboBoxColumn<T>
{
    /// <summary>
    /// Optional column header text. When any column has a header, a header row appears
    /// at the top of the dropdown.
    /// </summary>
    public string? Header { get; set; }

    /// <summary>
    /// Extracts the display text for this column from an item.
    /// When <c>null</c>, the column renders an empty string.
    /// </summary>
    public Func<T, string?>? ValueFunc { get; set; }

    /// <summary>
    /// CSS width of the column (e.g. <c>"200px"</c> or <c>"40%"</c>).
    /// Defaults to equal distribution when <c>null</c>.
    /// </summary>
    public string? Width { get; set; }
}
