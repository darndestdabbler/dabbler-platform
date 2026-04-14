// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Microsoft.AspNetCore.Components;

namespace Dabbler.Components.Data;

/// <summary>
/// Lightweight column descriptor used by <see cref="ClipboardCoordinator"/> and
/// <see cref="CsvExporter"/> for copy, paste, and export operations.
/// </summary>
/// <remarks>
/// This decouples clipboard/export logic from MudBlazor's <c>Column&lt;T&gt;</c> type,
/// keeping the coordinators framework-agnostic and testable.
/// </remarks>
public sealed class GridColumnInfo
{
    /// <summary>
    /// The property name (or dot-notation path) to read/write on the data item.
    /// Null for computed-only columns.
    /// </summary>
    public string? PropertyName { get; init; }

    /// <summary>
    /// Display header text for the column.
    /// </summary>
    public string Header { get; init; } = "";

    /// <summary>
    /// Whether the column is read-only. Read-only columns are skipped during paste.
    /// </summary>
    public bool IsReadOnly { get; init; }

    /// <summary>
    /// Optional .NET format string (e.g., "C2", "yyyy-MM-dd") applied when
    /// formatting cell values for display, clipboard copy, or CSV export.
    /// </summary>
    public string? Format { get; init; }

    /// <summary>
    /// Optional function to compute a display value for the column.
    /// When set, this takes precedence over <see cref="PropertyName"/> for reads.
    /// </summary>
    public Func<object, object?>? ComputeValue { get; init; }

    /// <summary>
    /// Whether this column is pinned to the left, pinned to the right, or not pinned.
    /// Pinned columns use <c>position: sticky</c> and remain visible during horizontal scrolling.
    /// Can be changed at runtime via <c>DabblerDataGrid.PinColumn()</c>.
    /// </summary>
    public ColumnPinPosition PinPosition { get; set; } = ColumnPinPosition.None;

    /// <summary>
    /// Whether this field is required. When true, the popup form adds
    /// <c>aria-required="true"</c> and a <c>Required</c> attribute to auto-generated editors.
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    /// Optional custom editor template for the popup form.
    /// When set, the popup form renders this instead of an auto-generated editor.
    /// The <see cref="object"/> parameter is the item being edited.
    /// </summary>
    public RenderFragment<object>? EditTemplate { get; init; }
}
