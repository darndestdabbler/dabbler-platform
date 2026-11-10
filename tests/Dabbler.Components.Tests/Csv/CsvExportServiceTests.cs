using System.Text;
using Dabbler.Components.Csv;
using FluentAssertions;
using Xunit;

namespace Dabbler.Components.Tests.Csv;

/// <summary>
/// Unit coverage for the RFC 4180 CSV exporter introduced in Session 4 of the
/// <c>sensitive-field-masking-audit</c> session set. Verifies the quoting,
/// escaping, line-ending, and culture-pinning rules the Compliance Review
/// viewer depends on.
/// </summary>
public sealed class CsvExportServiceTests
{
    private readonly CsvExportService _sut = new();

    private sealed class Row
    {
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime When { get; set; }
        public string? Note { get; set; }
    }

    [Fact]
    public void Export_EmptyRows_StillEmitsHeaderRow()
    {
        var bytes = _sut.Export(
            Array.Empty<Row>(),
            new[] { new CsvColumn<Row>("Name", r => r.Name) });

        Encoding.UTF8.GetString(bytes).Should().Be("Name\r\n");
    }

    [Fact]
    public void Export_PlainValues_RendersWithoutQuotes()
    {
        var rows = new[] { new Row { Name = "Alice", Amount = 1m } };
        var csv = Encoding.UTF8.GetString(_sut.Export(rows, new[]
        {
            new CsvColumn<Row>("Name", r => r.Name),
            new CsvColumn<Row>("Amount", r => r.Amount),
        }));

        csv.Should().Be("Name,Amount\r\nAlice,1\r\n");
    }

    [Fact]
    public void Export_FieldWithComma_IsQuoted()
    {
        var rows = new[] { new Row { Name = "Smith, Bob" } };
        var csv = Encoding.UTF8.GetString(_sut.Export(rows, new[]
        {
            new CsvColumn<Row>("Name", r => r.Name),
        }));

        csv.Should().Be("Name\r\n\"Smith, Bob\"\r\n");
    }

    [Fact]
    public void Export_FieldWithEmbeddedQuote_DoublesTheQuote()
    {
        var rows = new[] { new Row { Name = "She said \"hi\"" } };
        var csv = Encoding.UTF8.GetString(_sut.Export(rows, new[]
        {
            new CsvColumn<Row>("Name", r => r.Name),
        }));

        csv.Should().Be("Name\r\n\"She said \"\"hi\"\"\"\r\n");
    }

    [Fact]
    public void Export_FieldWithLineBreak_IsQuoted()
    {
        var rows = new[] { new Row { Name = "line1\nline2" } };
        var csv = Encoding.UTF8.GetString(_sut.Export(rows, new[]
        {
            new CsvColumn<Row>("Name", r => r.Name),
        }));

        csv.Should().Be("Name\r\n\"line1\nline2\"\r\n");
    }

    [Fact]
    public void Export_NullAccessor_RendersAsEmptyCell()
    {
        var rows = new[] { new Row { Name = "Alice", Note = null } };
        var csv = Encoding.UTF8.GetString(_sut.Export(rows, new[]
        {
            new CsvColumn<Row>("Name", r => r.Name),
            new CsvColumn<Row>("Note", r => r.Note),
        }));

        csv.Should().Be("Name,Note\r\nAlice,\r\n");
    }

    [Fact]
    public void Export_DateTime_IsRenderedAsRoundTripInvariant()
    {
        var stamp = new DateTime(2026, 4, 27, 13, 45, 0, DateTimeKind.Utc);
        var rows = new[] { new Row { When = stamp } };
        var csv = Encoding.UTF8.GetString(_sut.Export(rows, new[]
        {
            new CsvColumn<Row>("When", r => r.When),
        }));

        csv.Should().Be($"When\r\n{stamp:o}\r\n");
    }

    [Fact]
    public void Export_HeaderWithComma_IsQuoted()
    {
        var csv = Encoding.UTF8.GetString(_sut.Export(
            Array.Empty<Row>(),
            new[] { new CsvColumn<Row>("First, Last", r => r.Name) }));

        csv.Should().Be("\"First, Last\"\r\n");
    }

    [Fact]
    public void Export_MultipleRowsAndColumns_PreservesOrder()
    {
        var rows = new[]
        {
            new Row { Name = "A", Amount = 1m },
            new Row { Name = "B", Amount = 2m },
            new Row { Name = "C", Amount = 3m },
        };
        var csv = Encoding.UTF8.GetString(_sut.Export(rows, new[]
        {
            new CsvColumn<Row>("Name", r => r.Name),
            new CsvColumn<Row>("Amount", r => r.Amount),
        }));

        csv.Should().Be("Name,Amount\r\nA,1\r\nB,2\r\nC,3\r\n");
    }

    [Fact]
    public void Export_ResultIsUtf8WithoutBom()
    {
        var bytes = _sut.Export(
            new[] { new Row { Name = "café" } },
            new[] { new CsvColumn<Row>("Name", r => r.Name) });

        // No UTF-8 BOM (EF/Excel-friendly callers can prepend if needed).
        bytes[0].Should().NotBe(0xEF);
    }
}
