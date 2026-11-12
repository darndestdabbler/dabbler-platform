using Dabbler.Reports.Core.Layout;
using Dabbler.Reports.Core.Rdf;
using DabblerPdf.Document;
using DabblerPdf.Pdf;
using DabblerPdf.Text;
using DabblerPdf.Utilities;

namespace Dabbler.Reports.Export;

/// <summary>
/// Renders a single <see cref="PageLayout"/> onto an DabblerPdf <see cref="PdfContentByte"/> surface.
/// Converts inch-based coordinates from the layout engine to PDF points (72 DPI)
/// using bottom-left-up PDF coordinates.
/// </summary>
internal sealed class PdfPageRenderer
{
    private readonly PdfStyleResolver _styleResolver;
    private readonly ReportMetadata _metadata;

    public PdfPageRenderer(PdfStyleResolver styleResolver, ReportMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(styleResolver);
        ArgumentNullException.ThrowIfNull(metadata);
        _styleResolver = styleResolver;
        _metadata = metadata;
    }

    public double PageWidthPoints => CoordinateTranslator.InchesToPoints(_metadata.PageWidth);
    public double PageHeightPoints => CoordinateTranslator.InchesToPoints(_metadata.PageHeight);

    /// <summary>
    /// Renders the specified page layout onto the given PdfContentByte surface.
    /// </summary>
    public void RenderPage(PdfContentByte cb, PageLayout page)
    {
        ArgumentNullException.ThrowIfNull(cb);
        ArgumentNullException.ThrowIfNull(page);

        // Draw white page background
        cb.SetColorFill(BaseColor.White);
        cb.Rectangle(0, 0, PageWidthPoints, PageHeightPoints);
        cb.Fill();

        // Draw band backgrounds (behind elements)
        foreach (var bg in page.BandBackgrounds)
        {
            var color = _styleResolver.GetColor(bg.Color);
            if (color is not null)
            {
                var (bx, by) = CoordinateTranslator.FromTopLeftInches(
                    bg.X, bg.Y, bg.Height, _metadata.PageHeight);
                cb.SetColorFill(color);
                cb.Rectangle(bx, by,
                    CoordinateTranslator.InchesToPoints(bg.Width),
                    CoordinateTranslator.InchesToPoints(bg.Height));
                cb.Fill();
            }
        }

        // Render each element
        foreach (var element in page.Elements)
        {
            RenderElement(cb, element);
        }
    }

    private void RenderElement(PdfContentByte cb, LayoutElement element)
    {
        try
        {
            switch (element.ElementType)
            {
                case ElementType.StaticText:
                case ElementType.TextField:
                    RenderTextElement(cb, element);
                    break;
                case ElementType.Line:
                    RenderLineElement(cb, element);
                    break;
                case ElementType.Rectangle:
                    RenderRectangleElement(cb, element);
                    break;
                case ElementType.Image:
                    RenderImageElement(cb, element);
                    break;
                case ElementType.Subreport:
                    RenderSubreportElement(cb, element);
                    break;
            }
        }
        catch
        {
            // Rendering errors should not propagate — silently skip the element.
        }
    }

    private void RenderTextElement(PdfContentByte cb, LayoutElement element)
    {
        var text = element.Text ?? string.Empty;
        if (text.Length == 0) return;

        var style = _styleResolver.FindStyle(element.StyleName);
        var font = _styleResolver.GetFont(style);
        var fontSize = _styleResolver.GetFontSize(style);
        var padding = PdfStyleResolver.GetPaddingPoints(style);

        var (x, y) = CoordinateTranslator.FromTopLeftInches(
            element.X, element.Y, element.Height, _metadata.PageHeight);
        var w = CoordinateTranslator.InchesToPoints(element.Width);
        var h = CoordinateTranslator.InchesToPoints(element.Height);

        // Draw background
        var bgColor = _styleResolver.GetColor(style?.BackgroundColor);
        if (bgColor is not null)
        {
            cb.SetColorFill(bgColor);
            cb.Rectangle(x, y, w, h);
            cb.Fill();
        }

        // Draw borders
        DrawBorders(cb, x, y, w, h, style);

        // Calculate text area within padding
        var textX = x + padding.Left;
        var textY = y + padding.Bottom; // PDF Y is bottom-up, so bottom padding is at the base
        var textW = Math.Max(1, w - padding.Left - padding.Right);
        var textH = Math.Max(1, h - padding.Top - padding.Bottom);

        var fgColor = _styleResolver.GetColor(style?.ForegroundColor) ?? BaseColor.Black;

        if (style?.TextWrap == true)
        {
            RenderWrappedText(cb, text, font, fontSize, fgColor,
                textX, textY, textW, textH, style);
        }
        else
        {
            RenderSingleLineText(cb, text, font, fontSize, fgColor,
                textX, textY, textW, textH, style);
        }
    }

    private void RenderSingleLineText(
        PdfContentByte cb, string text, BaseFont font, double fontSize,
        BaseColor color, double x, double y, double w, double h,
        StyleDefinition? style)
    {
        var textWidth = font.GetWidthPoint(text, fontSize);
        var ascent = font.GetFontDescriptor(BaseFont.ASCENT, fontSize);
        var descent = font.GetFontDescriptor(BaseFont.DESCENT, fontSize);
        var lineHeight = ascent - descent;

        // Horizontal alignment
        double textX = style?.HorizontalAlignment switch
        {
            HorizontalTextAlignment.Center => x + (w - textWidth) / 2,
            HorizontalTextAlignment.Right => x + w - textWidth,
            _ => x
        };

        // Vertical alignment — position baseline
        double textY = style?.VerticalAlignment switch
        {
            VerticalTextAlignment.Middle => y + (h - lineHeight) / 2 - descent,
            VerticalTextAlignment.Bottom => y - descent,
            _ => y + h - ascent // Top: baseline is ascent below the top of the box
        };

        cb.BeginText();
        cb.SetFontAndSize(font, fontSize);
        cb.SetColorFill(color);
        cb.SetTextMatrix(textX, textY);
        cb.ShowText(text);
        cb.EndText();
    }

    private void RenderWrappedText(
        PdfContentByte cb, string text, BaseFont font, double fontSize,
        BaseColor color, double x, double y, double w, double h,
        StyleDefinition? style)
    {
        var ascent = font.GetFontDescriptor(BaseFont.ASCENT, fontSize);
        var descent = font.GetFontDescriptor(BaseFont.DESCENT, fontSize);
        var lineHeight = ascent - descent;

        // The same wrap OpenPdfTextMeasurer counts with, so a grown field holds every line
        var lines = TextLayout.WrapText(text, font, fontSize, w);

        // Start from the top of the text area
        double currentY = y + h - ascent; // top of box, baseline at ascent below

        cb.BeginText();
        cb.SetFontAndSize(font, fontSize);
        cb.SetColorFill(color);

        foreach (var line in lines)
        {
            if (currentY < y + descent) break; // Clip at bottom of text area

            double lineX = style?.HorizontalAlignment switch
            {
                HorizontalTextAlignment.Center => x + (w - font.GetWidthPoint(line, fontSize)) / 2,
                HorizontalTextAlignment.Right => x + w - font.GetWidthPoint(line, fontSize),
                _ => x
            };

            cb.SetTextMatrix(lineX, currentY);
            cb.ShowText(line);
            currentY -= lineHeight;
        }

        cb.EndText();
    }

    private void RenderLineElement(PdfContentByte cb, LayoutElement element)
    {
        var style = _styleResolver.FindStyle(element.StyleName);
        var (x, y) = CoordinateTranslator.FromTopLeftInches(
            element.X, element.Y, element.Height, _metadata.PageHeight);
        var w = CoordinateTranslator.InchesToPoints(element.Width);
        var h = CoordinateTranslator.InchesToPoints(element.Height);

        ApplyLinePen(cb, style, element.Text);

        if (w >= h)
        {
            // Horizontal line centered vertically
            var midY = y + h / 2;
            cb.MoveTo(x, midY);
            cb.LineTo(x + w, midY);
        }
        else
        {
            // Vertical line centered horizontally
            var midX = x + w / 2;
            cb.MoveTo(midX, y);
            cb.LineTo(midX, y + h);
        }

        cb.Stroke();
    }

    private void RenderRectangleElement(PdfContentByte cb, LayoutElement element)
    {
        var style = _styleResolver.FindStyle(element.StyleName);
        var (x, y) = CoordinateTranslator.FromTopLeftInches(
            element.X, element.Y, element.Height, _metadata.PageHeight);
        var w = CoordinateTranslator.InchesToPoints(element.Width);
        var h = CoordinateTranslator.InchesToPoints(element.Height);

        var fill = _styleResolver.GetColor(style?.BackgroundColor);
        if (fill is not null)
        {
            cb.SetColorFill(fill);
            cb.Rectangle(x, y, w, h);
            cb.Fill();
        }

        ApplyLinePen(cb, style, element.Text);
        cb.Rectangle(x, y, w, h);
        cb.Stroke();
    }

    private void RenderImageElement(PdfContentByte cb, LayoutElement element)
    {
        var (x, y) = CoordinateTranslator.FromTopLeftInches(
            element.X, element.Y, element.Height, _metadata.PageHeight);
        var w = CoordinateTranslator.InchesToPoints(element.Width);
        var h = CoordinateTranslator.InchesToPoints(element.Height);

        DabblerPdf.Image.Image? image = null;
        try
        {
            image = LoadImage(element);
        }
        catch
        {
            // Image loading failures should not crash rendering
        }

        if (image is null)
        {
            RenderImagePlaceholder(cb, x, y, w, h, element.ImageSourcePath);
            return;
        }

        // Draw background/borders from style
        var style = _styleResolver.FindStyle(element.StyleName);
        var bgColor = _styleResolver.GetColor(style?.BackgroundColor);
        if (bgColor is not null)
        {
            cb.SetColorFill(bgColor);
            cb.Rectangle(x, y, w, h);
            cb.Fill();
        }
        DrawBorders(cb, x, y, w, h, style);

        // Calculate image rect based on scale mode
        var (ix, iy, iw, ih) = CalculateImageRect(
            element.ImageScaleMode, image.PixelWidth, image.PixelHeight,
            x, y, w, h);

        // Use transformation matrix to place and scale the image
        // Matrix: [width 0 0 height x y]
        cb.AddImage(image, iw, 0, 0, ih, ix, iy);
    }

    private static DabblerPdf.Image.Image? LoadImage(LayoutElement element)
    {
        if (element.ImageData is { Length: > 0 })
        {
            return DabblerPdf.Image.Image.GetInstance(element.ImageData);
        }

        if (!string.IsNullOrWhiteSpace(element.ImageSourcePath))
        {
            if (!File.Exists(element.ImageSourcePath))
                return null;

            return DabblerPdf.Image.Image.GetInstance(element.ImageSourcePath);
        }

        return null;
    }

    internal static (double X, double Y, double Width, double Height) CalculateImageRect(
        ImageScaleMode scaleMode, double imageWidth, double imageHeight,
        double x, double y, double w, double h)
    {
        switch (scaleMode)
        {
            case ImageScaleMode.Stretch:
                return (x, y, w, h);

            case ImageScaleMode.ActualSize:
                return (x, y + (h - imageHeight), imageWidth, imageHeight);

            case ImageScaleMode.ScaleToFit:
            default:
            {
                double scaleX = w / imageWidth;
                double scaleY = h / imageHeight;
                double scale = Math.Min(scaleX, scaleY);
                double scaledW = imageWidth * scale;
                double scaledH = imageHeight * scale;
                double offsetX = x + (w - scaledW) / 2;
                double offsetY = y + (h - scaledH) / 2;
                return (offsetX, offsetY, scaledW, scaledH);
            }
        }
    }

    private void RenderImagePlaceholder(
        PdfContentByte cb, double x, double y, double w, double h, string? sourcePath)
    {
        // Gray border with light gray fill
        cb.SetColorStroke(BaseColor.DarkGray);
        cb.SetLineWidth(1);
        cb.SetColorFill(BaseColor.LightGray);
        cb.Rectangle(x, y, w, h);
        cb.FillStroke();

        var label = string.IsNullOrWhiteSpace(sourcePath)
            ? "[Image]"
            : $"[Image: {Path.GetFileName(sourcePath)}]";
        var font = _styleResolver.GetFont(null);
        var fontSize = _styleResolver.GetFontSize(null);
        var textWidth = font.GetWidthPoint(label, fontSize);
        var ascent = font.GetFontDescriptor(BaseFont.ASCENT, fontSize);
        var descent = font.GetFontDescriptor(BaseFont.DESCENT, fontSize);
        var lineHeight = ascent - descent;

        cb.BeginText();
        cb.SetFontAndSize(font, fontSize);
        cb.SetColorFill(BaseColor.Black);
        cb.SetTextMatrix(x + (w - textWidth) / 2, y + (h - lineHeight) / 2 - descent);
        cb.ShowText(label);
        cb.EndText();
    }

    private void RenderSubreportElement(PdfContentByte cb, LayoutElement element)
    {
        if (element.ChildElements is null || element.ChildElements.Count == 0)
        {
            if (!string.IsNullOrEmpty(element.Text))
            {
                RenderTextElement(cb, element);
            }
            return;
        }

        foreach (var child in element.ChildElements)
        {
            RenderElement(cb, child);
        }
    }

    /// <summary>
    /// Applies stroke settings for a line/rectangle from style border, element color, or default.
    /// </summary>
    private void ApplyLinePen(PdfContentByte cb, StyleDefinition? style, string? valueColor)
    {
        var border = _styleResolver.ParseBorder(style?.Border);
        if (border.HasValue)
        {
            cb.SetLineWidth(border.Value.Width);
            cb.SetColorStroke(border.Value.Color);
            if (border.Value.DashPattern is not null)
                cb.SetLineDash(border.Value.DashPattern, 0);
            return;
        }

        if (!string.IsNullOrWhiteSpace(valueColor))
        {
            var color = _styleResolver.GetColor(valueColor);
            if (color is not null)
            {
                cb.SetLineWidth(1);
                cb.SetColorStroke(color);
                return;
            }
        }

        // Default: 1pt solid black
        cb.SetLineWidth(1);
        cb.SetColorStroke(BaseColor.Black);
    }

    private void DrawBorders(PdfContentByte cb, double x, double y, double w, double h, StyleDefinition? style)
    {
        if (style is null) return;

        DrawSingleBorder(cb, style.BorderTop ?? style.Border, x, y + h, x + w, y + h);
        DrawSingleBorder(cb, style.BorderBottom ?? style.Border, x, y, x + w, y);
        DrawSingleBorder(cb, style.BorderLeft ?? style.Border, x, y, x, y + h);
        DrawSingleBorder(cb, style.BorderRight ?? style.Border, x + w, y, x + w, y + h);
    }

    private void DrawSingleBorder(PdfContentByte cb, string? borderSpec, double x1, double y1, double x2, double y2)
    {
        var border = _styleResolver.ParseBorder(borderSpec);
        if (!border.HasValue) return;

        cb.SaveState();
        cb.SetLineWidth(border.Value.Width);
        cb.SetColorStroke(border.Value.Color);
        if (border.Value.DashPattern is not null)
            cb.SetLineDash(border.Value.DashPattern, 0);
        cb.MoveTo(x1, y1);
        cb.LineTo(x2, y2);
        cb.Stroke();
        cb.RestoreState();
    }
}
