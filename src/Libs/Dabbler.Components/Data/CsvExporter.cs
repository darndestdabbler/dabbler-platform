// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using System.Text;

namespace Dabbler.Components.Data;

/// <summary>
/// Exports grid data to CSV format (RFC 4180) or tab-delimited text for clipboard.
/// Pure C# — no framework dependencies.
/// </summary>
public static class CsvExporter
{
    /// <summary>
    /// Exports the specified items and columns to CSV format and writes to the given stream.
    /// </summary>
    /// <param name="items">The data items to export (in display order).</param>
    /// <param name="columns">The columns to export.</param>
    /// <param name="output">The stream to write CSV data to.</param>
    public static async Task ExportAsync(
        IReadOnlyList<object> items,
        IReadOnlyList<GridColumnInfo> columns,
        Stream output)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(output);

        using var writer = new StreamWriter(output, new UTF8Encoding(false), leaveOpen: true);

        // Write header row
        for (int i = 0; i < columns.Count; i++)
        {
            if (i > 0) await writer.WriteAsync(',');
            await writer.WriteAsync(EscapeCsvField(columns[i].Header));
        }
        await writer.WriteLineAsync();

        // Write data rows
        for (int row = 0; row < items.Count; row++)
        {
            var item = items[row];
            for (int col = 0; col < columns.Count; col++)
            {
                if (col > 0) await writer.WriteAsync(',');
                string value = ClipboardCoordinator.GetCellDisplayValue(item, columns[col]);
                await writer.WriteAsync(EscapeCsvField(value));
            }
            await writer.WriteLineAsync();
        }

        await writer.FlushAsync();
    }

    /// <summary>
    /// Formats the specified items and columns as tab-delimited text for clipboard operations.
    /// </summary>
    /// <param name="items">The data items to format.</param>
    /// <param name="columns">The columns to include.</param>
    /// <param name="includeHeaders">Whether to include a header row.</param>
    /// <returns>Tab-delimited text suitable for clipboard paste into spreadsheets.</returns>
    public static string FormatAsTabDelimited(
        IReadOnlyList<object> items,
        IReadOnlyList<GridColumnInfo> columns,
        bool includeHeaders = true)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(columns);

        var sb = new StringBuilder();

        if (includeHeaders)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                if (i > 0) sb.Append('\t');
                sb.Append(columns[i].Header);
            }
            sb.AppendLine();
        }

        for (int row = 0; row < items.Count; row++)
        {
            var item = items[row];
            for (int col = 0; col < columns.Count; col++)
            {
                if (col > 0) sb.Append('\t');
                sb.Append(ClipboardCoordinator.GetCellDisplayValue(item, columns[col]));
            }
            if (row < items.Count - 1)
                sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escapes a field value for CSV per RFC 4180.
    /// Fields containing commas, quotes, or newlines are enclosed in double quotes.
    /// Double quotes within the field are escaped by doubling them.
    /// </summary>
    public static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}
