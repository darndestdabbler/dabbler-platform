using System.Data;
using Dabbler.Reports.Core.Layout;
using Dabbler.Reports.Core.Rdf;
using FluentAssertions;
using Xunit;

namespace Dabbler.Reports.Blazor.Tests;

/// <summary>
/// Regression coverage for the Page-1 band ordering fix: on page 1, the
/// ReportHeader (title) must render above the PageHeader (column headers);
/// on subsequent pages only the PageHeader repeats at the top.
/// </summary>
public class BandLayoutEngineComposerTests
{
    private const string TitleText = "Report Title Visible On Page 1";
    private const string ColumnHeaderText = "Column Header";

    [Fact]
    public void Page1_EmitsReportHeaderAboveAndBeforePageHeader()
    {
        var engine = new BandLayoutEngine();
        var pages = engine.ComposePages(BuildSmallReport(), BuildData(rowCount: 3));

        pages.Should().HaveCountGreaterThanOrEqualTo(1);
        var firstPage = pages[0];
        firstPage.PageNumber.Should().Be(1);

        var title = firstPage.Elements.FirstOrDefault(e => e.Text == TitleText);
        var columnHeader = firstPage.Elements.FirstOrDefault(e => e.Text == ColumnHeaderText);

        title.Should().NotBeNull("ReportHeader title must appear on page 1");
        columnHeader.Should().NotBeNull("PageHeader column headers must appear on page 1");

        title!.Y.Should().BeLessThan(columnHeader!.Y,
            "title must sit above the column headers on page 1");

        var titleIndex = firstPage.Elements.IndexOf(title);
        var columnHeaderIndex = firstPage.Elements.IndexOf(columnHeader);
        titleIndex.Should().BeLessThan(columnHeaderIndex,
            "title must be emitted before column headers in the element stream");
    }

    [Fact]
    public void Page2_EmitsPageHeaderAtTopWithoutReportHeader()
    {
        var engine = new BandLayoutEngine();
        var pages = engine.ComposePages(BuildSmallReport(), BuildData(rowCount: 80));

        pages.Should().HaveCountGreaterThanOrEqualTo(2,
            "a row count of 80 at 0.25in per detail should spill onto more than one page");

        var secondPage = pages[1];
        secondPage.PageNumber.Should().Be(2);

        secondPage.Elements.Should().NotContain(e => e.Text == TitleText,
            "ReportHeader title must not repeat on page 2");

        var columnHeader = secondPage.Elements.FirstOrDefault(e => e.Text == ColumnHeaderText);
        columnHeader.Should().NotBeNull("PageHeader column headers must repeat on page 2");

        // On page 2, no ReportHeader precedes the PageHeader, so the column
        // header sits at the top of the printable area (margin top).
        columnHeader!.Y.Should().BeApproximately(
            BuildSmallReport().Metadata.MarginTop + 0.05, 0.001,
            "column header element Y on page 2 equals band top margin plus the in-band Y offset");
    }

    [Fact]
    public void EmptyReport_EmitsReportHeaderAboveAndBeforePageHeader()
    {
        var report = BuildSmallReport();
        // Add a NoData band so ComposeEmptyReport emits something meaningful.
        report.Bands.Add(new BandDefinition
        {
            Name = "noData",
            Type = BandType.NoData,
            Height = 0.3,
            Elements =
            {
                new ElementDefinition
                {
                    Type = ElementType.StaticText,
                    X = 0, Y = 0, Width = 4.0, Height = 0.25,
                    Value = "No data available"
                }
            }
        });

        var engine = new BandLayoutEngine();
        var pages = engine.ComposePages(report, BuildData(rowCount: 0));

        pages.Should().HaveCount(1);
        var page = pages[0];

        var title = page.Elements.FirstOrDefault(e => e.Text == TitleText);
        var columnHeader = page.Elements.FirstOrDefault(e => e.Text == ColumnHeaderText);
        var noData = page.Elements.FirstOrDefault(e => e.Text == "No data available");

        title.Should().NotBeNull();
        columnHeader.Should().NotBeNull();
        noData.Should().NotBeNull();

        title!.Y.Should().BeLessThan(columnHeader!.Y);
        columnHeader.Y.Should().BeLessThan(noData!.Y);

        page.Elements.IndexOf(title).Should().BeLessThan(page.Elements.IndexOf(columnHeader));
    }

    private static ReportDefinition BuildSmallReport()
    {
        return new ReportDefinition
        {
            Metadata = new ReportMetadata
            {
                Name = "Composer Band Order Test",
                PageWidth = 8.5,
                PageHeight = 11.0,
                MarginTop = 0.5,
                MarginBottom = 0.5,
                MarginLeft = 0.5,
                MarginRight = 0.5
            },
            Fields =
            {
                new FieldDefinition { Name = "Label", DataType = "String", SourceColumn = "Label" }
            },
            Bands =
            {
                new BandDefinition
                {
                    Name = "reportHeader",
                    Type = BandType.ReportHeader,
                    Height = 0.6,
                    Elements =
                    {
                        new ElementDefinition
                        {
                            Type = ElementType.StaticText,
                            X = 0, Y = 0.05, Width = 5.0, Height = 0.3,
                            Value = TitleText
                        }
                    }
                },
                new BandDefinition
                {
                    Name = "pageHeader",
                    Type = BandType.PageHeader,
                    Height = 0.3,
                    Elements =
                    {
                        new ElementDefinition
                        {
                            Type = ElementType.StaticText,
                            X = 0, Y = 0.05, Width = 3.0, Height = 0.2,
                            Value = ColumnHeaderText
                        }
                    }
                },
                new BandDefinition
                {
                    Name = "detail",
                    Type = BandType.Detail,
                    Height = 0.25,
                    Elements =
                    {
                        new ElementDefinition
                        {
                            Type = ElementType.TextField,
                            X = 0, Y = 0.02, Width = 3.0, Height = 0.2,
                            FieldName = "Label"
                        }
                    }
                }
            }
        };
    }

    private static DataTable BuildData(int rowCount)
    {
        var table = new DataTable();
        table.Columns.Add("Label", typeof(string));
        for (int i = 0; i < rowCount; i++)
            table.Rows.Add($"Row {i + 1}");
        return table;
    }
}
