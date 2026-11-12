// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Specifies whether a grid column is pinned to the left, pinned to the right, or not pinned.
/// Pinned columns use <c>position: sticky</c> and remain visible during horizontal scrolling.
/// </summary>
public enum ColumnPinPosition
{
    /// <summary>The column is not pinned and scrolls normally.</summary>
    None,

    /// <summary>The column is pinned to the left edge of the grid.</summary>
    Left,

    /// <summary>The column is pinned to the right edge of the grid.</summary>
    Right
}
