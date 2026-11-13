using System.Globalization;
using Dabbler.Reports.Core.Layout;
using Dabbler.Reports.Core.Rdf;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Dabbler.Reports.Blazor;

/// <summary>
/// Blazor component that renders composed report pages as styled HTML, applying
/// RDF-defined styles, fonts, borders, padding, and element positioning.
/// </summary>
/// <example>
/// <code>
/// &lt;ReportViewer Pages="@composedPages" Report="@reportDefinition" Scale="1.25" /&gt;
/// </code>
/// </example>
public partial class ReportViewer : ComponentBase
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>The composed pages to render.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<PageLayout>? Pages { get; set; }

    /// <summary>The report definition, used for style resolution.</summary>
    [Parameter, EditorRequired]
    public ReportDefinition? Report { get; set; }

    /// <summary>Scale factor for rendering (1.0 = 96 DPI screen pixels per inch).</summary>
    [Parameter]
    public double Scale { get; set; } = 1.0;

    private double Dpi => 96.0 * Scale;

    private Dictionary<string, StyleDefinition>? _styleMap;

    private Dictionary<string, StyleDefinition> StyleMap =>
        _styleMap ??= BuildStyleMap();

    private Dictionary<string, StyleDefinition> BuildStyleMap()
    {
        if (Report?.Styles is null)
            return new Dictionary<string, StyleDefinition>(StringComparer.OrdinalIgnoreCase);

        return Report.Styles.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _styleMap = null; // Rebuild on parameter change
    }

    private string GetViewerStyle()
    {
        return "display:flex;flex-direction:column;align-items:center;gap:16px;padding:16px;background:#e0e0e0;";
    }

    private string GetPageStyle()
    {
        var meta = Report?.Metadata ?? new ReportMetadata();
        double w = meta.PageWidth * Dpi;
        double h = meta.PageHeight * Dpi;

        return $"position:relative;width:{F(w)}px;height:{F(h)}px;background:white;box-shadow:0 2px 8px rgba(0,0,0,0.2);overflow:hidden;";
    }

    private string GetBandBackgroundStyle(LayoutBandBackground bg)
    {
        return $"position:absolute;left:{F(bg.X * Dpi)}px;top:{F(bg.Y * Dpi)}px;" +
               $"width:{F(bg.Width * Dpi)}px;height:{F(bg.Height * Dpi)}px;" +
               $"background-color:{bg.Color};";
    }

    private RenderFragment RenderElement(LayoutElement element) => builder =>
    {
        switch (element.ElementType)
        {
            case ElementType.Line:
                RenderLine(builder, element);
                break;
            case ElementType.Rectangle:
                RenderRectangle(builder, element);
                break;
            case ElementType.Image:
                RenderImage(builder, element);
                break;
            case ElementType.Subreport:
                RenderSubreport(builder, element);
                break;
            default: // TextField, StaticText
                RenderTextElement(builder, element);
                break;
        }
    };

    private void RenderTextElement(RenderTreeBuilder builder, LayoutElement element)
    {
        var style = ResolveStyle(element.StyleName);
        string css = BuildTextElementCss(element, style);

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "report-element report-text");
        builder.AddAttribute(2, "style", css);
        builder.AddContent(3, element.Text ?? string.Empty);
        builder.CloseElement();
    }

    private void RenderLine(RenderTreeBuilder builder, LayoutElement element)
    {
        var style = ResolveStyle(element.StyleName);
        string borderColor = style?.ForegroundColor ?? "#000000";
        string css = $"position:absolute;left:{F(element.X * Dpi)}px;top:{F(element.Y * Dpi)}px;" +
                     $"width:{F(element.Width * Dpi)}px;height:{F(element.Height * Dpi)}px;";

        if (element.Width >= element.Height)
        {
            // Horizontal line
            css += $"border-top:1px solid {borderColor};";
        }
        else
        {
            // Vertical line
            css += $"border-left:1px solid {borderColor};";
        }

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "report-element report-line");
        builder.AddAttribute(2, "style", css);
        builder.CloseElement();
    }

    private void RenderRectangle(RenderTreeBuilder builder, LayoutElement element)
    {
        var style = ResolveStyle(element.StyleName);
        string css = $"position:absolute;left:{F(element.X * Dpi)}px;top:{F(element.Y * Dpi)}px;" +
                     $"width:{F(element.Width * Dpi)}px;height:{F(element.Height * Dpi)}px;";

        if (style?.BackgroundColor is not null)
            css += $"background-color:{style.BackgroundColor};";

        css += BuildBorderCss(style);

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "report-element report-rect");
        builder.AddAttribute(2, "style", css);
        builder.CloseElement();
    }

    private void RenderImage(RenderTreeBuilder builder, LayoutElement element)
    {
        string css = $"position:absolute;left:{F(element.X * Dpi)}px;top:{F(element.Y * Dpi)}px;" +
                     $"width:{F(element.Width * Dpi)}px;height:{F(element.Height * Dpi)}px;" +
                     "object-fit:contain;";

        builder.OpenElement(0, "img");
        builder.AddAttribute(1, "class", "report-element report-image");
        builder.AddAttribute(2, "style", css);

        if (element.ImageData is not null)
        {
            string base64 = Convert.ToBase64String(element.ImageData);
            builder.AddAttribute(3, "src", $"data:image/png;base64,{base64}");
        }
        else if (element.ImageSourcePath is not null)
        {
            builder.AddAttribute(3, "src", element.ImageSourcePath);
        }

        builder.AddAttribute(4, "alt", "");
        builder.CloseElement();
    }

    private void RenderSubreport(RenderTreeBuilder builder, LayoutElement element)
    {
        if (element.ChildElements is null) return;

        string css = $"position:absolute;left:{F(element.X * Dpi)}px;top:{F(element.Y * Dpi)}px;" +
                     $"width:{F(element.Width * Dpi)}px;height:{F(element.ActualHeight * Dpi)}px;" +
                     "overflow:hidden;";

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "report-element report-subreport");
        builder.AddAttribute(2, "style", css);

        foreach (var child in element.ChildElements)
        {
            builder.AddContent(3, RenderElement(child));
        }

        builder.CloseElement();
    }

    private string BuildTextElementCss(LayoutElement element, StyleDefinition? style)
    {
        var parts = new List<string>(16)
        {
            "position:absolute",
            $"left:{F(element.X * Dpi)}px",
            $"top:{F(element.Y * Dpi)}px",
            $"width:{F(element.Width * Dpi)}px",
            $"height:{F(element.Height * Dpi)}px",
            "box-sizing:border-box",
            "overflow:hidden",
        };

        // Font
        string fontFamily = style?.FontFamily ?? Report?.Metadata.DefaultFontFamily ?? "Segoe UI";
        double fontSize = (style?.FontSize ?? Report?.Metadata.DefaultFontSize ?? 10) * Scale;
        parts.Add($"font-family:'{fontFamily}',sans-serif");
        parts.Add($"font-size:{F(fontSize)}pt");

        if (style?.IsBold == true) parts.Add("font-weight:bold");
        if (style?.IsItalic == true) parts.Add("font-style:italic");
        if (style?.IsUnderline == true) parts.Add("text-decoration:underline");

        // Colors
        if (style?.ForegroundColor is not null) parts.Add($"color:{style.ForegroundColor}");
        if (style?.BackgroundColor is not null) parts.Add($"background-color:{style.BackgroundColor}");

        // Alignment
        if (style?.HorizontalAlignment is not null)
        {
            parts.Add(style.HorizontalAlignment.Value switch
            {
                HorizontalTextAlignment.Center => "text-align:center",
                HorizontalTextAlignment.Right => "text-align:right",
                _ => "text-align:left",
            });
        }

        if (style?.VerticalAlignment is not null)
        {
            parts.Add("display:flex");
            parts.Add(style.VerticalAlignment.Value switch
            {
                VerticalTextAlignment.Middle => "align-items:center",
                VerticalTextAlignment.Bottom => "align-items:flex-end",
                _ => "align-items:flex-start",
            });
            // Preserve horizontal text alignment when using flex
            if (style?.HorizontalAlignment == HorizontalTextAlignment.Center)
                parts.Add("justify-content:center");
            else if (style?.HorizontalAlignment == HorizontalTextAlignment.Right)
                parts.Add("justify-content:flex-end");
        }

        // Text wrapping
        if (style?.TextWrap == true)
            parts.Add("white-space:normal;word-wrap:break-word");
        else
            parts.Add("white-space:nowrap");

        // Padding
        string padding = BuildPaddingCss(style);
        if (padding.Length > 0) parts.Add(padding);

        // Borders
        string border = BuildBorderCss(style);
        if (border.Length > 0) parts.Add(border);

        return string.Join(";", parts) + ";";
    }

    private string BuildPaddingCss(StyleDefinition? style)
    {
        if (style is null) return string.Empty;

        double? top = style.PaddingTop ?? style.Padding;
        double? bottom = style.PaddingBottom ?? style.Padding;
        double? left = style.PaddingLeft ?? style.Padding;
        double? right = style.PaddingRight ?? style.Padding;

        if (top is null && bottom is null && left is null && right is null)
            return string.Empty;

        return $"padding:{F((top ?? 0) * Dpi)}px {F((right ?? 0) * Dpi)}px " +
               $"{F((bottom ?? 0) * Dpi)}px {F((left ?? 0) * Dpi)}px";
    }

    private static string BuildBorderCss(StyleDefinition? style)
    {
        if (style is null) return string.Empty;

        // Individual borders override the shorthand
        if (style.BorderTop is not null || style.BorderBottom is not null ||
            style.BorderLeft is not null || style.BorderRight is not null)
        {
            var parts = new List<string>(4);
            if (style.BorderTop is not null) parts.Add($"border-top:{style.BorderTop}");
            if (style.BorderBottom is not null) parts.Add($"border-bottom:{style.BorderBottom}");
            if (style.BorderLeft is not null) parts.Add($"border-left:{style.BorderLeft}");
            if (style.BorderRight is not null) parts.Add($"border-right:{style.BorderRight}");
            return string.Join(";", parts);
        }

        if (style.Border is not null)
            return $"border:{style.Border}";

        return string.Empty;
    }

    private StyleDefinition? ResolveStyle(string? styleName)
    {
        if (styleName is null) return null;
        StyleMap.TryGetValue(styleName, out var style);
        return style;
    }

    private string GetReportAriaLabel()
    {
        var name = Report?.Metadata?.Name;
        var pageCount = Pages?.Count ?? 0;
        if (!string.IsNullOrEmpty(name))
            return $"{name}, {pageCount} page{(pageCount == 1 ? "" : "s")}";
        return $"Report, {pageCount} page{(pageCount == 1 ? "" : "s")}";
    }

    private static string F(double value) => value.ToString("0.##", Inv);
}
