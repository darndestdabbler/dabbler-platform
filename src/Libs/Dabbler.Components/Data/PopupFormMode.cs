// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Specifies the layout mode for the popup form editor in <see cref="DabblerDataGrid{T}"/>.
/// </summary>
public enum PopupFormMode
{
    /// <summary>Single-column layout: one field per row, full width.</summary>
    OneColumn,

    /// <summary>Two-column layout: fields arranged in two columns with optional grouping.</summary>
    TwoColumn
}
