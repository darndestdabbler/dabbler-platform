using System.Globalization;
using System.Text;

namespace Dabbler.Components.Csv;

/// <summary>
/// Default <see cref="ICsvExportService"/> implementation. Stateless and safe to
/// register as a singleton.
/// </summary>
public sealed class CsvExportService : ICsvExportService
{
    /// <inheritdoc />
    public byte[] Export<T>(IEnumerable<T> rows, IReadOnlyList<CsvColumn<T>> columns)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(columns);

        var sb = new StringBuilder();

        for (var i = 0; i < columns.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(QuoteIfNeeded(columns[i].Header));
        }
        sb.Append("\r\n");

        foreach (var row in rows)
        {
            for (var i = 0; i < columns.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(QuoteIfNeeded(Render(columns[i].Accessor(row))));
            }
            sb.Append("\r\n");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string Render(object? value)
    {
        return value switch
        {
            null => string.Empty,
            string s => s,
            DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString("o", CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
    }

    private static string QuoteIfNeeded(string value)
    {
        // RFC 4180: quote when the field contains a comma, quote, CR, or LF.
        // Embedded quotes are escaped by doubling.
        var needsQuote = false;
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c == ',' || c == '"' || c == '\r' || c == '\n')
            {
                needsQuote = true;
                break;
            }
        }
        if (!needsQuote) return value;

        var sb = new StringBuilder(value.Length + 2);
        sb.Append('"');
        foreach (var c in value)
        {
            if (c == '"') sb.Append('"');
            sb.Append(c);
        }
        sb.Append('"');
        return sb.ToString();
    }
}
