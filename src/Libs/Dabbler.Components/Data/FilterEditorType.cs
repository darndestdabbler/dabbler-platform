// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Specifies the type of editor to display in a filter cell.
/// </summary>
public enum FilterEditorType
{
    /// <summary>A text input field.</summary>
    Text,

    /// <summary>A tri-state check box (true / false / null).</summary>
    CheckBox,

    /// <summary>A date picker.</summary>
    Date,

    /// <summary>A combo box (dropdown select).</summary>
    ComboBox,

    /// <summary>A numeric input field.</summary>
    NumericText
}
