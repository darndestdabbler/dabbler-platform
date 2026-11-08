using Bunit;
using Dabbler.Reports.Blazor.Tests.TestHelpers;
using Dabbler.Reports.Core.Layout;
using Dabbler.Reports.Core.Rdf;
using FluentAssertions;
using Xunit;

namespace Dabbler.Reports.Blazor.Tests;

public class ReportViewerTests
{
    [Fact]
    public void ContractSurface_RendersAccessibleDocumentPagesAndResolvedElements()
    {
        using var ctx = new TestContext().SetupForDabblerReports();

        var cut = ctx.RenderComponent<ReportViewer>(parameters => parameters
            .Add(x => x.Report, BuildReportDefinition())
            .Add(x => x.Pages, BuildPages()));

        cut.Find(".report-viewer").GetAttribute("aria-label").Should().Be("Contract Report, 2 pages");
        cut.FindAll(".report-page").Should().HaveCount(2);
        cut.FindAll(".report-page")[0].GetAttribute("aria-label").Should().Be("Page 1 of 2");
        cut.FindAll(".report-band-bg").Should().ContainSingle();
        cut.FindAll(".report-text").Should().HaveCount(3);
        cut.Find(".report-text").GetAttribute("style").Should().Contain("font-weight:bold");
        cut.Find(".report-text").GetAttribute("style").Should().Contain("text-align:center");
        cut.Find(".report-rect").GetAttribute("style").Should().Contain("background-color:#f5f5f5");
        cut.Find(".report-line").GetAttribute("style").Should().Contain("border-top:1px solid #112233");
        cut.Find(".report-image").GetAttribute("src").Should().Be("/images/chart.png");
        cut.Find(".report-image").GetAttribute("alt").Should().BeEmpty();
        cut.Markup.Should().Contain("Nested child content");
    }

    [Fact]
    public void Scale_ChangesPageDimensionsAndTextSizing()
    {
        using var ctx = new TestContext().SetupForDabblerReports();

        var cut = ctx.RenderComponent<ReportViewer>(parameters => parameters
            .Add(x => x.Report, BuildReportDefinition())
            .Add(x => x.Pages, BuildPages())
            .Add(x => x.Scale, 1.5));

        cut.Find(".report-page").GetAttribute("style").Should().Contain("width:1224px");
        cut.Find(".report-page").GetAttribute("style").Should().Contain("height:1584px");
        cut.Find(".report-text").GetAttribute("style").Should().Contain("font-size:24pt");
    }

    private static ReportDefinition BuildReportDefinition() =>
        new()
        {
            Metadata = new ReportMetadata
            {
                Name = "Contract Report",
                PageWidth = 8.5,
                PageHeight = 11,
                DefaultFontFamily = "Segoe UI",
                DefaultFontSize = 10
            },
            Styles =
            [
                new StyleDefinition
                {
                    Name = "title",
                    FontFamily = "Segoe UI",
                    FontSize = 16,
                    IsBold = true,
                    HorizontalAlignment = HorizontalTextAlignment.Center,
                    Padding = 0.05,
                    BorderBottom = "1px solid #112233"
                },
                new StyleDefinition
                {
                    Name = "body",
                    FontSize = 10,
                    TextWrap = true
                },
                new StyleDefinition
                {
                    Name = "shape",
                    BackgroundColor = "#f5f5f5",
                    Border = "1px solid #445566"
                },
                new StyleDefinition
                {
                    Name = "line",
                    ForegroundColor = "#112233"
                }
            ]
        };

    private static IReadOnlyList<PageLayout> BuildPages()
    {
        var firstPage = new PageLayout
        {
            PageNumber = 1,
            TotalPages = 2
        };

        firstPage.BandBackgrounds.Add(new LayoutBandBackground
        {
            X = 0,
            Y = 0,
            Width = 8,
            Height = 0.6,
            Color = "#fafafa"
        });
        firstPage.Elements.Add(new LayoutElement
        {
            ElementType = ElementType.StaticText,
            Text = "Contract heading",
            X = 0.5,
            Y = 0.25,
            Width = 3,
            Height = 0.3,
            StyleName = "title"
        });
        firstPage.Elements.Add(new LayoutElement
        {
            ElementType = ElementType.Rectangle,
            X = 0.5,
            Y = 1,
            Width = 2,
            Height = 0.75,
            StyleName = "shape"
        });
        firstPage.Elements.Add(new LayoutElement
        {
            ElementType = ElementType.Line,
            X = 0.5,
            Y = 2,
            Width = 2.5,
            Height = 0.02,
            StyleName = "line"
        });
        firstPage.Elements.Add(new LayoutElement
        {
            ElementType = ElementType.Image,
            X = 0.5,
            Y = 2.5,
            Width = 1.5,
            Height = 1,
            ImageSourcePath = "/images/chart.png"
        });
        firstPage.Elements.Add(new LayoutElement
        {
            ElementType = ElementType.Subreport,
            X = 0.5,
            Y = 3.75,
            Width = 3,
            Height = 1,
            ActualHeight = 1,
            ChildElements =
            [
                new LayoutElement
                {
                    ElementType = ElementType.StaticText,
                    Text = "Nested child content",
                    X = 0.1,
                    Y = 0.1,
                    Width = 2,
                    Height = 0.25,
                    StyleName = "body"
                }
            ]
        });

        var secondPage = new PageLayout
        {
            PageNumber = 2,
            TotalPages = 2
        };
        secondPage.Elements.Add(new LayoutElement
        {
            ElementType = ElementType.StaticText,
            Text = "Second page body",
            X = 0.5,
            Y = 0.5,
            Width = 4,
            Height = 0.4,
            StyleName = "body"
        });

        return [firstPage, secondPage];
    }
}
