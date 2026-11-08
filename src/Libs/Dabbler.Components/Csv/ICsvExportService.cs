namespace Dabbler.Components.Csv;

/// <summary>
/// Defines a column in a CSV export — a header label plus an accessor that
/// extracts the value to render for each row.
/// </summary>
/// <typeparam name="T">The row data type.</typeparam>
/// <param name="Header">The header label rendered on the first line of the export.</param>
/// <param name="Accessor">A function that returns the unformatted value for a given row.
/// Nulls render as empty cells; non-null values are converted via
/// <see cref="object.ToString"/> with invariant culture for numeric and date types.</param>
public sealed record CsvColumn<T>(string Header, Func<T, object?> Accessor);

/// <summary>
/// Generic CSV exporter. Emits RFC 4180-compliant output (CRLF line endings,
/// double-quoted fields containing commas / quotes / line breaks, and embedded
/// quotes escaped by doubling).
/// </summary>
public interface ICsvExportService
{
    /// <summary>
    /// Renders <paramref name="rows"/> through <paramref name="columns"/> and
    /// returns the result as a UTF-8 byte buffer suitable for download.
    /// </summary>
    /// <typeparam name="T">The row data type.</typeparam>
    /// <param name="rows">The rows to export, in the order they should appear.</param>
    /// <param name="columns">The ordered column definitions; the header line and per-row
    /// projection both follow this list.</param>
    /// <returns>A UTF-8 byte buffer containing the rendered CSV (no BOM).</returns>
    byte[] Export<T>(IEnumerable<T> rows, IReadOnlyList<CsvColumn<T>> columns);
}
