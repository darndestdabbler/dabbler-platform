using System.Data;
using Dabbler.Components.Expressions;
using Dabbler.Reports.Core.Data;
using Dabbler.Reports.Core.Rdf;

namespace Dabbler.Reports.Core.Layout;

/// <summary>
/// Composes report bands into pages, handling page break logic,
/// group break detection, keep-together rules, and expression evaluation.
/// Inspired by the JasperReports band-based layout model.
/// </summary>
/// <remarks>
/// <para>The layout algorithm uses a Y-cursor that tracks the current vertical
/// position on the page (in inches from the top margin). Bands are placed
/// sequentially, and a new page is started when the next band would overflow
/// the available space (accounting for the page footer).</para>
/// <para>
/// Band rendering order:
/// Page 1: ReportHeader → PageHeader → [GroupHeaders → Detail → GroupFooters]* → PageFooter
/// Page N: PageHeader → [GroupHeaders → Detail → GroupFooters]* → PageFooter
/// Last page: ... → ReportFooter → PageFooter
/// Empty data (page 1): ReportHeader → PageHeader → NoData → ReportFooter → PageFooter
/// </para>
/// <para>
/// The report title (ReportHeader) sits above the repeating column headers
/// (PageHeader) on page 1. On subsequent pages the PageHeader repeats alone
/// so column headers remain visible while the report title does not.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var layoutEngine = new BandLayoutEngine(reportDefinition, textMeasurer);
/// IReadOnlyList&lt;PageLayout&gt; pages = layoutEngine.Compose(dataRows);
///
/// foreach (var page in pages)
/// {
///     // Render each page's elements and band backgrounds
/// }
/// </code>
/// </example>
public class BandLayoutEngine
{
    /// <summary>
    /// Maximum allowed subreport nesting depth to prevent infinite recursion
    /// from circular subreport references.
    /// </summary>
    public const int MaxSubreportDepth = 5;

    /// <summary>
    /// Composes a report definition and its data into a sequence of pages.
    /// </summary>
    /// <param name="report">The parsed report definition.</param>
    /// <param name="data">The report data.</param>
    /// <returns>A list of composed pages ready for rendering.</returns>
    public IReadOnlyList<PageLayout> ComposePages(ReportDefinition report, DataTable data)
    {
        return ComposePages(report, data, null, null);
    }

    /// <summary>
    /// Composes a report definition and its data into a sequence of pages,
    /// with the given parameter values for expression evaluation.
    /// </summary>
    /// <param name="report">The parsed report definition.</param>
    /// <param name="data">The report data.</param>
    /// <param name="parameters">Optional parameter values for expression evaluation.</param>
    /// <returns>A list of composed pages ready for rendering.</returns>
    public IReadOnlyList<PageLayout> ComposePages(
        ReportDefinition report, DataTable data, Dictionary<string, object?>? parameters)
    {
        return ComposePages(report, data, parameters, null);
    }

    /// <summary>
    /// Composes a report definition and its data into a sequence of pages,
    /// with the given parameter values and subreport resolver for nested reports.
    /// </summary>
    /// <param name="report">The parsed report definition.</param>
    /// <param name="data">The report data.</param>
    /// <param name="parameters">Optional parameter values for expression evaluation.</param>
    /// <param name="subreportResolver">
    /// Optional resolver for loading subreport definitions and data. Required if
    /// the report contains <see cref="ElementType.Subreport"/> elements.
    /// </param>
    /// <returns>A list of composed pages ready for rendering.</returns>
    public IReadOnlyList<PageLayout> ComposePages(
        ReportDefinition report, DataTable data,
        Dictionary<string, object?>? parameters,
        ISubreportResolver? subreportResolver)
    {
        return ComposePages(report, data, parameters, subreportResolver, null);
    }

    /// <summary>
    /// Composes a report definition and its data into a sequence of pages,
    /// with the given parameter values, subreport resolver, and text measurer
    /// for CanGrow/CanShrink element support.
    /// </summary>
    /// <param name="report">The parsed report definition.</param>
    /// <param name="data">The report data.</param>
    /// <param name="parameters">Optional parameter values for expression evaluation.</param>
    /// <param name="subreportResolver">
    /// Optional resolver for loading subreport definitions and data. Required if
    /// the report contains <see cref="ElementType.Subreport"/> elements.
    /// </param>
    /// <param name="textMeasurer">
    /// Optional text measurer for computing actual text height. Required if
    /// the report contains elements with <see cref="ElementDefinition.CanGrow"/> or
    /// <see cref="ElementDefinition.CanShrink"/> enabled.
    /// </param>
    /// <returns>A list of composed pages ready for rendering.</returns>
    public IReadOnlyList<PageLayout> ComposePages(
        ReportDefinition report, DataTable data,
        Dictionary<string, object?>? parameters,
        ISubreportResolver? subreportResolver,
        ITextMeasurer? textMeasurer)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(data);

        var composer = new PageComposer(report, data, parameters, subreportResolver, textMeasurer, subreportDepth: 0);
        return composer.Compose();
    }

    /// <summary>
    /// Internal stateful composer that processes one report layout run.
    /// </summary>
    private sealed class PageComposer
    {
        private readonly ReportDefinition _report;
        private readonly DataTable _data;
        private readonly LayoutEvaluationContext _context;
        private readonly ExpressionEngine _expressionEngine;
        private readonly ISubreportResolver? _subreportResolver;
        private readonly ITextMeasurer? _textMeasurer;
        private readonly int _subreportDepth;

        // Page geometry (all in inches)
        private readonly double _pageWidth;
        private readonly double _pageHeight;
        private readonly double _marginTop;
        private readonly double _marginBottom;
        private readonly double _marginLeft;
        private readonly double _contentWidth;
        private readonly double _printableBottom; // Y position where page footer begins

        // Band lookups
        private readonly BandDefinition? _pageHeaderBand;
        private readonly BandDefinition? _pageFooterBand;
        private readonly BandDefinition? _reportHeaderBand;
        private readonly BandDefinition? _reportFooterBand;
        private readonly BandDefinition? _noDataBand;
        private readonly List<BandDefinition> _detailBands;

        // Group tracking
        private readonly List<GroupDefinition> _groups;
        private readonly Dictionary<string, BandDefinition?> _groupHeaderBands;
        private readonly Dictionary<string, BandDefinition?> _groupFooterBands;
        private readonly object?[] _currentGroupValues;

        // State
        private readonly List<PageLayout> _pages = [];
        private PageLayout _currentPage = null!;
        private double _yCursor;
        private double _pageFooterHeight;

        // Running totals and hide-duplicates tracking
        private readonly RunningTotalAccumulator _runningTotalAccumulator = new();
        private readonly DuplicateTracker _duplicateTracker = new();
        private int _detailRowIndex;

        // Deferred PageCount elements: expressions containing {PageCount} are evaluated
        // during composition when total pages is unknown. We track them here and
        // re-evaluate after all pages are composed.
        private readonly List<(LayoutElement Element, string Expression, string? FormatString, int PageNumber)>
            _deferredPageCountElements = [];

        // Cache for pre-composed subreport results. Populated by GetEffectiveBandHeight()
        // before the page break decision, consumed by ResolveSubreportContent() during EmitBand().
        // Keyed by ElementDefinition identity (each element is a unique object in the band tree).
        private readonly Dictionary<ElementDefinition, SubreportCacheEntry>
            _subreportCache = new(ReferenceEqualityComparer.Instance);

        private readonly record struct SubreportCacheEntry(
            List<LayoutElement>? ChildElements,
            double ActualHeight,
            string? ErrorText);

        // Cache for pre-measured text heights. Populated by GetEffectiveBandHeight()
        // before the page break decision, consumed by ResolveElement() during EmitBand().
        // Keyed by ElementDefinition identity. Stores resolved text and measured height.
        private readonly Dictionary<ElementDefinition, TextMeasureCacheEntry>
            _textMeasureCache = new(ReferenceEqualityComparer.Instance);

        private readonly record struct TextMeasureCacheEntry(
            string? ResolvedText,
            double MeasuredHeight);

        public PageComposer(
            ReportDefinition report, DataTable data,
            Dictionary<string, object?>? parameters,
            ISubreportResolver? subreportResolver,
            ITextMeasurer? textMeasurer,
            int subreportDepth)
        {
            _report = report;
            _data = data;
            _context = new LayoutEvaluationContext(report, parameters);
            _expressionEngine = new ExpressionEngine(_context);
            _subreportResolver = subreportResolver;
            _textMeasurer = textMeasurer;
            _subreportDepth = subreportDepth;

            // Page geometry
            var meta = report.Metadata;
            _pageWidth = meta.PageWidth;
            _pageHeight = meta.PageHeight;
            _marginTop = meta.MarginTop;
            _marginBottom = meta.MarginBottom;
            _marginLeft = meta.MarginLeft;
            _contentWidth = _pageWidth - meta.MarginLeft - meta.MarginRight;

            // Resolve bands by type
            _pageHeaderBand = FindBand(BandType.PageHeader);
            _pageFooterBand = FindBand(BandType.PageFooter);
            _reportHeaderBand = FindBand(BandType.ReportHeader);
            _reportFooterBand = FindBand(BandType.ReportFooter);
            _noDataBand = FindBand(BandType.NoData);
            _detailBands = report.Bands.Where(b => b.Type == BandType.Detail).ToList();

            _pageFooterHeight = _pageFooterBand?.Height ?? 0;
            _printableBottom = _pageHeight - _marginBottom - _pageFooterHeight;

            // Group setup
            _groups = report.Grouping;
            _groupHeaderBands = new Dictionary<string, BandDefinition?>(StringComparer.OrdinalIgnoreCase);
            _groupFooterBands = new Dictionary<string, BandDefinition?>(StringComparer.OrdinalIgnoreCase);
            _currentGroupValues = new object?[_groups.Count];

            foreach (var group in _groups)
            {
                var groupName = group.FieldName ?? group.GroupExpression ?? string.Empty;
                _groupHeaderBands[groupName] = report.Bands.FirstOrDefault(
                    b => b.Type == BandType.GroupHeader &&
                         string.Equals(b.GroupName, groupName, StringComparison.OrdinalIgnoreCase));
                _groupFooterBands[groupName] = report.Bands.FirstOrDefault(
                    b => b.Type == BandType.GroupFooter &&
                         string.Equals(b.GroupName, groupName, StringComparison.OrdinalIgnoreCase));
            }

            // Set up report-level aggregate scope
            var allRows = new DataRow[data.Rows.Count];
            data.Rows.CopyTo(allRows, 0);
            _context.SetReportRows(allRows);
        }

        public IReadOnlyList<PageLayout> Compose()
        {
            if (_data.Rows.Count == 0)
            {
                ComposeEmptyReport();
            }
            else
            {
                ComposeDataReport();
            }

            // Backfill page numbers
            var totalPages = _pages.Count;
            foreach (var page in _pages)
            {
                page.TotalPages = totalPages;
            }

            // Re-evaluate elements that reference {PageCount}, which was unknown
            // during composition. This avoids a full two-pass layout.
            if (_deferredPageCountElements.Count > 0)
            {
                _context.SetPageCount(totalPages);
                foreach (var (element, expression, formatString, pageNumber) in _deferredPageCountElements)
                {
                    _context.SetPageNumber(pageNumber);
                    try
                    {
                        var value = _expressionEngine.Evaluate(expression);
                        element.Text = FormatValue(value, formatString);
                    }
                    catch (ExpressionEvaluationException)
                    {
                        element.Text = "#ERROR";
                    }
                }
            }

            return _pages;
        }

        private void ComposeEmptyReport()
        {
            StartNewPage();
            EmitFirstPageHeaderBands();

            if (_noDataBand is not null)
                EmitBand(_noDataBand);

            if (_reportFooterBand is not null)
                EmitBand(_reportFooterBand);

            FinishPage();
        }

        private void ComposeDataReport()
        {
            StartNewPage();
            EmitFirstPageHeaderBands();

            // Process each data row
            for (int rowIndex = 0; rowIndex < _data.Rows.Count; rowIndex++)
            {
                var row = _data.Rows[rowIndex];
                _context.SetCurrentRow(row);

                // Detect group breaks
                ProcessGroupBreaks(row, rowIndex);

                // Emit detail bands
                foreach (var detailBand in _detailBands)
                {
                    EmitBandWithPageBreakCheck(detailBand);
                }
            }

            // Close all open groups (emit footers innermost-first)
            CloseAllGroups();

            // Report footer
            if (_reportFooterBand is not null)
                EmitBandWithPageBreakCheck(_reportFooterBand);

            FinishPage();
        }

        /// <summary>
        /// Detects group value changes and emits group footer/header bands as needed.
        /// </summary>
        private void ProcessGroupBreaks(DataRow row, int rowIndex)
        {
            if (_groups.Count == 0) return;

            // Determine which group level broke (outermost first)
            int breakLevel = -1;
            for (int g = 0; g < _groups.Count; g++)
            {
                var groupValue = GetGroupValue(_groups[g], row);
                if (rowIndex == 0)
                {
                    // First row — all groups start fresh
                    breakLevel = 0;
                    break;
                }
                if (!GroupValuesEqual(_currentGroupValues[g], groupValue))
                {
                    breakLevel = g;
                    break;
                }
            }

            if (breakLevel < 0) return; // No group break

            // Close inner groups first (innermost → outermost to break level)
            if (rowIndex > 0)
            {
                for (int g = _groups.Count - 1; g >= breakLevel; g--)
                {
                    EmitGroupFooter(g);
                }
            }

            // Reset running totals and duplicate tracker at group boundaries
            _runningTotalAccumulator.ResetGroup();
            _duplicateTracker.ResetForGroup();

            // Handle page break before group
            if (rowIndex > 0 && _groups[breakLevel].PageBreakBefore)
            {
                FinishPage();
                StartNewPage();
            }

            // Open new groups (outermost → innermost from break level)
            for (int g = breakLevel; g < _groups.Count; g++)
            {
                var groupValue = GetGroupValue(_groups[g], row);
                _currentGroupValues[g] = groupValue;

                // Set aggregate scope for this group (may change _currentRow internally)
                SetGroupAggregateScope(g, row);
                _context.SetCurrentRow(row);

                EmitGroupHeader(g, row);
            }
        }

        private void EmitGroupHeader(int groupIndex, DataRow firstRowInGroup)
        {
            var group = _groups[groupIndex];
            var groupName = group.FieldName ?? group.GroupExpression ?? string.Empty;
            if (!_groupHeaderBands.TryGetValue(groupName, out var headerBand) || headerBand is null)
                return;

            if (group.KeepTogether)
            {
                // Try to fit header + one detail band on current page,
                // using effective heights that account for subreport expansion
                double neededHeight = GetEffectiveBandHeight(headerBand);
                if (_detailBands.Count > 0)
                    neededHeight += GetEffectiveBandHeight(_detailBands[0]);

                if (_yCursor + neededHeight > _printableBottom && _yCursor > _marginTop)
                {
                    FinishPage();
                    StartNewPage();
                }
            }

            EmitBandWithPageBreakCheck(headerBand);
        }

        private void EmitGroupFooter(int groupIndex)
        {
            var group = _groups[groupIndex];
            var groupName = group.FieldName ?? group.GroupExpression ?? string.Empty;
            if (!_groupFooterBands.TryGetValue(groupName, out var footerBand) || footerBand is null)
                return;

            EmitBandWithPageBreakCheck(footerBand);

            if (group.PageBreakAfter)
            {
                FinishPage();
                StartNewPage();
            }
        }

        private void CloseAllGroups()
        {
            for (int g = _groups.Count - 1; g >= 0; g--)
            {
                EmitGroupFooter(g);
            }
        }

        /// <summary>
        /// Sets the aggregate scope rows for a group level, collecting all rows
        /// that share the same group values from the outermost level through this level.
        /// </summary>
        private void SetGroupAggregateScope(int groupIndex, DataRow currentRow)
        {
            var group = _groups[groupIndex];
            var groupName = group.FieldName ?? group.GroupExpression ?? string.Empty;
            var matchValue = GetGroupValue(group, currentRow);

            // Collect all rows matching group values from level 0..groupIndex
            var rows = new List<DataRow>();
            foreach (DataRow row in _data.Rows)
            {
                bool match = true;
                for (int g = 0; g <= groupIndex; g++)
                {
                    var gv = GetGroupValue(_groups[g], row);
                    if (!GroupValuesEqual(_currentGroupValues[g], gv))
                    {
                        match = false;
                        break;
                    }
                }
                if (match) rows.Add(row);
            }

            _context.SetAggregateScope(groupName, rows.ToArray());
        }

        private object? GetGroupValue(GroupDefinition group, DataRow row)
        {
            _context.SetCurrentRow(row);

            if (group.GroupExpression is not null)
            {
                try
                {
                    return _expressionEngine.Evaluate(group.GroupExpression);
                }
                catch
                {
                    return null;
                }
            }

            if (group.FieldName is not null)
            {
                try
                {
                    return _context.GetFieldValue(group.FieldName);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private static bool GroupValuesEqual(object? a, object? b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            if (a is string sa && b is string sb)
                return string.Equals(sa, sb, StringComparison.OrdinalIgnoreCase);
            return a.Equals(b);
        }

        /// <summary>
        /// Computes the effective height of a band, accounting for subreport expansion
        /// and CanGrow/CanShrink text elements. For bands without dynamic elements,
        /// returns <see cref="BandDefinition.Height"/>. For bands with subreport or
        /// CanGrow/CanShrink elements, pre-measures content (caching results for later
        /// consumption by <see cref="EmitBand"/>) and returns the effective height.
        /// </summary>
        private double GetEffectiveBandHeight(BandDefinition band)
        {
            // If band won't render, it contributes zero height
            if (!EvaluatePrintWhen(band.PrintWhenExpression))
                return 0;

            // Scan for dynamic elements — fast path if none found
            bool hasSubreports = false;
            bool hasCanGrowShrink = false;
            foreach (var element in band.Elements)
            {
                if (element.Type == ElementType.Subreport)
                    hasSubreports = true;
                else if ((element.CanGrow || element.CanShrink) && _textMeasurer is not null
                    && (element.Type == ElementType.TextField || element.Type == ElementType.StaticText))
                    hasCanGrowShrink = true;
            }

            if (!hasSubreports && !hasCanGrowShrink)
                return band.Height;

            // Compute effective bottom of every element in the band.
            // This unified approach handles subreport expansion, CanGrow, and CanShrink.
            double maxEffectiveBottom = 0;
            bool anyShrinkable = false;

            foreach (var element in band.Elements)
            {
                if (!EvaluatePrintWhen(element.PrintWhenExpression))
                    continue;

                double effectiveBottom;

                if (element.Type == ElementType.Subreport)
                {
                    PreComposeSubreportForCache(element);

                    if (_subreportCache.TryGetValue(element, out var cached) && cached.ErrorText is null)
                        effectiveBottom = element.Y + cached.ActualHeight;
                    else
                        effectiveBottom = element.Y + element.Height;
                }
                else if ((element.CanGrow || element.CanShrink) && _textMeasurer is not null
                    && (element.Type == ElementType.TextField || element.Type == ElementType.StaticText))
                {
                    PreMeasureTextForCache(element);

                    if (_textMeasureCache.TryGetValue(element, out var cached))
                    {
                        double measuredHeight = cached.MeasuredHeight;
                        if (element.CanGrow && measuredHeight > element.Height)
                            effectiveBottom = element.Y + measuredHeight;
                        else if (element.CanShrink && measuredHeight < element.Height)
                        {
                            effectiveBottom = element.Y + measuredHeight;
                            anyShrinkable = true;
                        }
                        else
                            effectiveBottom = element.Y + element.Height;
                    }
                    else
                    {
                        effectiveBottom = element.Y + element.Height;
                    }
                }
                else
                {
                    effectiveBottom = element.Y + element.Height;
                }

                if (effectiveBottom > maxEffectiveBottom)
                    maxEffectiveBottom = effectiveBottom;
            }

            // If no elements can shrink, the band never shrinks below its defined height.
            // This preserves backward compatibility: fixed-height elements anchor the band.
            if (!anyShrinkable)
                return Math.Max(maxEffectiveBottom, band.Height);

            return maxEffectiveBottom;
        }

        /// <summary>
        /// Pre-measures the text content of a CanGrow/CanShrink element and stores
        /// the result in <see cref="_textMeasureCache"/>. Resolves expressions/fields
        /// to get the text content, then measures its rendered height.
        /// </summary>
        private void PreMeasureTextForCache(ElementDefinition element)
        {
            if (_textMeasureCache.ContainsKey(element))
                return;

            // Resolve the text content
            string? text = element.Type switch
            {
                ElementType.StaticText => element.Value,
                ElementType.TextField => ResolveTextForMeasurement(element),
                _ => null
            };

            if (string.IsNullOrEmpty(text))
            {
                _textMeasureCache[element] = new TextMeasureCacheEntry(text, 0);
                return;
            }

            double measuredHeight = _textMeasurer!.MeasureTextHeight(
                text, element.StyleName, element.Width);

            _textMeasureCache[element] = new TextMeasureCacheEntry(text, measuredHeight);
        }

        /// <summary>
        /// Resolves the text content of a TextField element for measurement purposes.
        /// Similar to <see cref="ResolveTextFieldContent"/> but without layout element
        /// side effects (deferred PageCount tracking).
        /// </summary>
        private string? ResolveTextForMeasurement(ElementDefinition element)
        {
            object? value = null;

            try
            {
                if (element.Expression is not null)
                    value = _expressionEngine.Evaluate(element.Expression);
                else if (element.FieldName is not null)
                    value = _context.GetFieldValue(element.FieldName);
            }
            catch (ExpressionEvaluationException)
            {
                return "#ERROR";
            }

            if (value is null)
                return string.Empty;

            var formatString = ResolveFormatString(element);
            return FormatValue(value, formatString);
        }

        /// <summary>
        /// Emits a band, starting a new page first if there isn't enough room.
        /// Uses a small tolerance (0.001") to avoid floating-point page break errors.
        /// </summary>
        private void EmitBandWithPageBreakCheck(BandDefinition band)
        {
            const double tolerance = 0.001;

            if (band.PageBreakBefore && _yCursor > _marginTop + tolerance)
            {
                FinishPage();
                StartNewPage();
            }

            // Use effective height (accounting for subreport expansion) for the page break decision
            double effectiveHeight = GetEffectiveBandHeight(band);

            if (_yCursor + effectiveHeight > _printableBottom + tolerance && _yCursor > _marginTop + tolerance)
            {
                FinishPage();
                StartNewPage();
            }

            EmitBand(band);

            if (band.PageBreakAfter)
            {
                FinishPage();
                StartNewPage();
            }
        }

        /// <summary>
        /// Emits a band at the current Y-cursor position, resolving all elements.
        /// Computes effective band height using <see cref="GetEffectiveBandHeight"/>
        /// to account for subreport expansion and CanGrow/CanShrink text elements.
        /// </summary>
        private void EmitBand(BandDefinition band)
        {
            // Check band-level PrintWhenExpression
            if (!EvaluatePrintWhen(band.PrintWhenExpression))
                return;

            // Compute effective height — may already be cached from EmitBandWithPageBreakCheck.
            // This call is idempotent: cached entries are reused, not re-measured.
            double effectiveHeight = GetEffectiveBandHeight(band);
            double bandTop = _yCursor;

            // Emit band background if configured
            var bgColor = ResolveBandBackgroundColor(band);
            if (bgColor is not null)
            {
                _currentPage.BandBackgrounds.Add(new LayoutBandBackground
                {
                    X = _marginLeft,
                    Y = bandTop,
                    Width = _contentWidth,
                    Height = effectiveHeight,
                    Color = bgColor,
                });
            }

            foreach (var element in band.Elements)
            {
                // Check element-level PrintWhenExpression
                bool printWhenVisible = EvaluatePrintWhen(element.PrintWhenExpression);

                if (band.Type == BandType.Detail)
                {
                    var elementKey = $"{band.Name}:{band.Elements.IndexOf(element)}";

                    // Running total: accumulate even if hidden by PrintWhen
                    if (element.RunningTotal != Rdf.RunningTotalMode.None)
                    {
                        // Resolve the raw value for accumulation
                        object? rawValue = ResolveRawValue(element);
                        var accumulated = _runningTotalAccumulator.Accumulate(
                            elementKey, element.RunningTotal, rawValue);

                        if (!printWhenVisible)
                            continue;

                        var layoutElement = ResolveElement(element, bandTop);
                        // Replace the text with the accumulated value, formatted
                        var formatString = ResolveFormatString(element);
                        layoutElement.Text = FormatValue(accumulated, formatString);

                        // Apply hide duplicates to the accumulated value if enabled
                        if (element.HideDuplicates && _duplicateTracker.IsDuplicate(elementKey, layoutElement.Text))
                        {
                            layoutElement.Text = string.Empty;
                        }

                        _currentPage.Elements.Add(layoutElement);
                        continue;
                    }

                    if (!printWhenVisible)
                    {
                        // PrintWhen false: do NOT update duplicate tracker
                        continue;
                    }

                    // Hide duplicates: check after resolving and formatting
                    if (element.HideDuplicates)
                    {
                        var layoutElement = ResolveElement(element, bandTop);
                        if (_duplicateTracker.IsDuplicate(elementKey, layoutElement.Text))
                        {
                            layoutElement.Text = string.Empty;
                        }
                        _currentPage.Elements.Add(layoutElement);
                        continue;
                    }
                }
                else if (!printWhenVisible)
                {
                    continue;
                }

                {
                    var layoutElement = ResolveElement(element, bandTop);
                    _currentPage.Elements.Add(layoutElement);
                }
            }

            _yCursor += effectiveHeight;

            if (band.Type == BandType.Detail)
                _detailRowIndex++;
        }

        /// <summary>
        /// Resolves the background color for a band instance, accounting for
        /// alternate-row coloring on Detail bands.
        /// </summary>
        private string? ResolveBandBackgroundColor(BandDefinition band)
        {
            if (band.Type == BandType.Detail && band.AlternateBackgroundColor is not null)
            {
                return (_detailRowIndex % 2 == 0)
                    ? band.BackgroundColor
                    : band.AlternateBackgroundColor;
            }

            return band.BackgroundColor;
        }

        /// <summary>
        /// Resolves an <see cref="ElementDefinition"/> into a positioned <see cref="LayoutElement"/>
        /// with absolute page coordinates and evaluated text content.
        /// </summary>
        private LayoutElement ResolveElement(ElementDefinition element, double bandTopY)
        {
            var layout = new LayoutElement
            {
                X = _marginLeft + element.X,
                Y = bandTopY + element.Y,
                Width = element.Width,
                Height = element.Height,
                ElementType = element.Type,
                StyleName = element.StyleName
            };

            if (element.Type == ElementType.Subreport)
            {
                ResolveSubreportContent(element, layout);
                return layout;
            }

            if (element.Type == ElementType.Image)
            {
                ResolveImageSource(element, layout);
                return layout;
            }

            // Check text measurement cache first (populated by GetEffectiveBandHeight
            // for CanGrow/CanShrink elements)
            if (_textMeasureCache.Remove(element, out var cached))
            {
                layout.Text = cached.ResolvedText;
                double measuredHeight = cached.MeasuredHeight;

                if (element.CanGrow && measuredHeight > element.Height)
                {
                    layout.Height = measuredHeight;
                    layout.ActualHeight = measuredHeight;
                }
                else if (element.CanShrink && measuredHeight < element.Height)
                {
                    layout.Height = measuredHeight;
                    layout.ActualHeight = measuredHeight;
                }

                // For TextField, still need to handle deferred PageCount tracking
                if (element.Type == ElementType.TextField && element.Expression is not null
                    && element.Expression.Contains("PageCount", StringComparison.OrdinalIgnoreCase))
                {
                    var formatString = ResolveFormatString(element);
                    _deferredPageCountElements.Add(
                        (layout, element.Expression, formatString, _currentPage.PageNumber));
                }

                return layout;
            }

            // Resolve text content based on element type
            layout.Text = element.Type switch
            {
                ElementType.StaticText => element.Value,
                ElementType.TextField => ResolveTextFieldContent(element, layout),
                ElementType.Line or ElementType.Rectangle => element.Value,
                _ => element.Value
            };

            return layout;
        }

        /// <summary>
        /// Resolves a subreport element by loading its definition, executing a
        /// content-only layout, and embedding the resulting elements as children.
        /// </summary>
        private void ResolveSubreportContent(ElementDefinition element, LayoutElement layout)
        {
            // Check cache first — populated by GetEffectiveBandHeight() before page break decision
            if (_subreportCache.Remove(element, out var cached))
            {
                if (cached.ErrorText is not null)
                {
                    layout.Text = cached.ErrorText;
                    return;
                }

                layout.ChildElements = RepositionCachedChildren(
                    cached.ChildElements!, layout.X, layout.Y);
                layout.ActualHeight = cached.ActualHeight;
                return;
            }

            // Fallback: compose normally (for paths not going through EmitBandWithPageBreakCheck,
            // e.g., page header/footer bands or empty report composition)
            if (string.IsNullOrWhiteSpace(element.SubreportPath))
            {
                layout.Text = "#SUBREPORT:NO_PATH";
                return;
            }

            if (_subreportResolver is null)
            {
                layout.Text = "#SUBREPORT:NO_RESOLVER";
                return;
            }

            if (_subreportDepth >= MaxSubreportDepth)
            {
                layout.Text = "#SUBREPORT:MAX_DEPTH";
                return;
            }

            try
            {
                // Evaluate subreport parameters in the parent context
                var parameters = EvaluateSubreportParameters(element.SubreportParameters);

                // Resolve the subreport definition and data
                var (subReport, subData) = _subreportResolver.Resolve(
                    element.SubreportPath, parameters);

                // Compose the subreport as a content-only block (no page headers/footers)
                var childElements = ComposeSubreportContent(
                    subReport, subData, parameters, layout.X, layout.Y);

                layout.ChildElements = childElements;

                // Calculate actual height from child elements
                if (childElements.Count > 0)
                {
                    double maxBottom = 0;
                    foreach (var child in childElements)
                    {
                        double childBottom = child.Y + child.Height - layout.Y;
                        if (childBottom > maxBottom)
                            maxBottom = childBottom;
                    }
                    layout.ActualHeight = maxBottom;
                }
                else
                {
                    layout.ActualHeight = element.Height;
                }
            }
            catch (Exception)
            {
                // Subreport errors should not crash the parent report
                layout.Text = "#SUBREPORT:ERROR";
            }
        }

        /// <summary>
        /// Pre-composes a subreport element and stores the result in <see cref="_subreportCache"/>.
        /// Child elements are composed with a (0, 0) origin so they are position-independent
        /// and can be repositioned when consumed by <see cref="ResolveSubreportContent"/>.
        /// </summary>
        private void PreComposeSubreportForCache(ElementDefinition element)
        {
            if (_subreportCache.ContainsKey(element))
                return;

            if (string.IsNullOrWhiteSpace(element.SubreportPath))
            {
                _subreportCache[element] = new SubreportCacheEntry(null, 0, "#SUBREPORT:NO_PATH");
                return;
            }

            if (_subreportResolver is null)
            {
                _subreportCache[element] = new SubreportCacheEntry(null, 0, "#SUBREPORT:NO_RESOLVER");
                return;
            }

            if (_subreportDepth >= MaxSubreportDepth)
            {
                _subreportCache[element] = new SubreportCacheEntry(null, 0, "#SUBREPORT:MAX_DEPTH");
                return;
            }

            try
            {
                var parameters = EvaluateSubreportParameters(element.SubreportParameters);
                var (subReport, subData) = _subreportResolver.Resolve(
                    element.SubreportPath, parameters);

                // Compose at origin (0, 0) for position-independent caching
                var childElements = ComposeSubreportContent(
                    subReport, subData, parameters, originX: 0, originY: 0);

                double actualHeight;
                if (childElements.Count > 0)
                {
                    double maxBottom = 0;
                    foreach (var child in childElements)
                    {
                        double childBottom = child.Y + child.Height;
                        if (childBottom > maxBottom)
                            maxBottom = childBottom;
                    }
                    actualHeight = maxBottom;
                }
                else
                {
                    actualHeight = element.Height;
                }

                _subreportCache[element] = new SubreportCacheEntry(childElements, actualHeight, null);
            }
            catch (Exception)
            {
                _subreportCache[element] = new SubreportCacheEntry(null, 0, "#SUBREPORT:ERROR");
            }
        }

        /// <summary>
        /// Repositions cached child elements from origin (0, 0) to the actual
        /// subreport element position on the page. Recursively shifts nested
        /// child elements (from nested subreports).
        /// </summary>
        private static List<LayoutElement> RepositionCachedChildren(
            List<LayoutElement> children, double offsetX, double offsetY)
        {
            var result = new List<LayoutElement>(children.Count);
            foreach (var child in children)
            {
                var shifted = new LayoutElement
                {
                    X = child.X + offsetX,
                    Y = child.Y + offsetY,
                    Width = child.Width,
                    Height = child.Height,
                    Text = child.Text,
                    StyleName = child.StyleName,
                    ElementType = child.ElementType,
                    ActualHeight = child.ActualHeight,
                    ImageSourcePath = child.ImageSourcePath,
                    ImageData = child.ImageData,
                    ImageScaleMode = child.ImageScaleMode
                };
                if (child.ChildElements is { Count: > 0 })
                {
                    shifted.ChildElements = RepositionCachedChildren(
                        child.ChildElements, offsetX, offsetY);
                }
                result.Add(shifted);
            }
            return result;
        }

        /// <summary>
        /// Resolves the image source for an Image element by evaluating expressions
        /// or field bindings. Supports static file paths, data-bound field references
        /// (string paths), and binary BLOB data (byte[]).
        /// </summary>
        private void ResolveImageSource(ElementDefinition element, LayoutElement layout)
        {
            layout.ImageScaleMode = element.ImageScaleMode;

            if (string.IsNullOrWhiteSpace(element.ImageSource))
                return;

            try
            {
                // Try evaluating as an expression first (handles [FieldName] and complex expressions)
                object? value = _expressionEngine.Evaluate(element.ImageSource);

                switch (value)
                {
                    case byte[] bytes:
                        layout.ImageData = bytes;
                        break;
                    case string path when !string.IsNullOrWhiteSpace(path):
                        layout.ImageSourcePath = path;
                        break;
                    case null:
                        break;
                    default:
                        // Convert other types to string and treat as path
                        var str = value.ToString();
                        if (!string.IsNullOrWhiteSpace(str))
                            layout.ImageSourcePath = str;
                        break;
                }
            }
            catch (ExpressionEvaluationException)
            {
                // Not an expression — treat the ImageSource value as a literal file path
                layout.ImageSourcePath = element.ImageSource;
            }
        }

        /// <summary>
        /// Evaluates subreport parameter expressions in the parent report's context.
        /// </summary>
        private Dictionary<string, object?> EvaluateSubreportParameters(
            Dictionary<string, string>? parameterExpressions)
        {
            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (parameterExpressions is null)
                return result;

            foreach (var (name, expression) in parameterExpressions)
            {
                try
                {
                    result[name] = _expressionEngine.Evaluate(expression);
                }
                catch (ExpressionEvaluationException)
                {
                    result[name] = null;
                }
            }

            return result;
        }

        /// <summary>
        /// Composes a subreport's content bands into a flat list of layout elements.
        /// Subreports render content-only: no page headers/footers, no pagination.
        /// The resulting elements are positioned relative to the parent element's origin.
        /// </summary>
        private List<LayoutElement> ComposeSubreportContent(
            ReportDefinition subReport, DataTable subData,
            Dictionary<string, object?> parameters,
            double originX, double originY)
        {
            // Create a content-only report definition that strips page headers/footers
            var contentBands = subReport.Bands
                .Where(b => b.Type != BandType.PageHeader && b.Type != BandType.PageFooter)
                .ToList();

            var contentReport = new ReportDefinition
            {
                Metadata = subReport.Metadata,
                DataSources = subReport.DataSources,
                Parameters = subReport.Parameters,
                Fields = subReport.Fields,
                Sorting = subReport.Sorting,
                Grouping = subReport.Grouping,
                Bands = contentBands,
                Styles = subReport.Styles,
                Expressions = subReport.Expressions
            };

            // Use a nested PageComposer to lay out the subreport content.
            // This produces one or more "pages" but we flatten them into a single
            // stream of elements since subreports don't paginate independently.
            var subComposer = new PageComposer(
                contentReport, subData, parameters, _subreportResolver,
                _textMeasurer, _subreportDepth + 1);
            var subPages = subComposer.Compose();

            // Flatten all pages into a single list, offsetting Y coordinates
            // to stack pages sequentially and shifting to the parent element's origin.
            var childElements = new List<LayoutElement>();
            double yOffset = 0;

            foreach (var page in subPages)
            {
                foreach (var elem in page.Elements)
                {
                    // Reposition: remove subreport's own margins and add parent offset
                    var offsetElement = new LayoutElement
                    {
                        X = originX + (elem.X - subReport.Metadata.MarginLeft),
                        Y = originY + yOffset + (elem.Y - subReport.Metadata.MarginTop),
                        Width = elem.Width,
                        Height = elem.Height,
                        Text = elem.Text,
                        StyleName = elem.StyleName,
                        ElementType = elem.ElementType,
                        ChildElements = elem.ChildElements,
                        ActualHeight = elem.ActualHeight
                    };
                    childElements.Add(offsetElement);
                }

                // Stack next page's content below this page's content area
                if (page.Elements.Count > 0)
                {
                    double pageContentBottom = 0;
                    foreach (var elem in page.Elements)
                    {
                        double bottom = elem.Y - subReport.Metadata.MarginTop + elem.Height;
                        if (bottom > pageContentBottom)
                            pageContentBottom = bottom;
                    }
                    yOffset += pageContentBottom;
                }
            }

            return childElements;
        }

        /// <summary>
        /// Resolves the text content of a TextField element by evaluating its
        /// expression or field binding and applying format strings.
        /// </summary>
        private string? ResolveTextFieldContent(ElementDefinition element, LayoutElement layoutElement)
        {
            object? value = null;
            string? expression = element.Expression;

            try
            {
                if (expression is not null)
                {
                    value = _expressionEngine.Evaluate(expression);
                }
                else if (element.FieldName is not null)
                {
                    value = _context.GetFieldValue(element.FieldName);
                }
            }
            catch (ExpressionEvaluationException)
            {
                // Rendering errors should not crash the host — return error text
                return "#ERROR";
            }

            // Resolve format string: element-level overrides field-level
            var formatString = ResolveFormatString(element);

            // If the expression references PageCount, register for deferred
            // re-evaluation after all pages are composed and total is known.
            if (expression is not null &&
                expression.Contains("PageCount", StringComparison.OrdinalIgnoreCase))
            {
                _deferredPageCountElements.Add(
                    (layoutElement, expression, formatString, _currentPage.PageNumber));
            }

            if (value is null)
                return string.Empty;

            return FormatValue(value, formatString);
        }

        /// <summary>
        /// Resolves the raw (unformatted) value of a TextField element
        /// for running total accumulation.
        /// </summary>
        private object? ResolveRawValue(ElementDefinition element)
        {
            try
            {
                if (element.Expression is not null)
                    return _expressionEngine.Evaluate(element.Expression);
                if (element.FieldName is not null)
                    return _context.GetFieldValue(element.FieldName);
            }
            catch (ExpressionEvaluationException)
            {
                return null;
            }
            return null;
        }

        /// <summary>
        /// Resolves the effective format string for an element, checking
        /// the element-level override first, then the field definition.
        /// </summary>
        private string? ResolveFormatString(ElementDefinition element)
        {
            if (element.FormatString is not null)
                return element.FormatString;

            if (element.FieldName is not null)
            {
                var fieldDef = _report.Fields.FirstOrDefault(
                    f => string.Equals(f.Name, element.FieldName, StringComparison.OrdinalIgnoreCase));
                return fieldDef?.FormatString;
            }

            return null;
        }

        /// <summary>
        /// Formats a value using the given format string, or converts to string if no format.
        /// </summary>
        private static string FormatValue(object? value, string? formatString)
        {
            if (value is null)
                return string.Empty;

            if (formatString is not null && value is IFormattable formattable)
                return formattable.ToString(formatString, null);

            return value.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Evaluates a PrintWhenExpression, returning true if the band/element should render.
        /// Returns true if the expression is null or empty.
        /// </summary>
        private bool EvaluatePrintWhen(string? expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return true;

            try
            {
                var result = _expressionEngine.Evaluate(expression);
                return result switch
                {
                    bool b => b,
                    int i => i != 0,
                    double d => d != 0,
                    string s => !string.IsNullOrEmpty(s),
                    null => false,
                    _ => true
                };
            }
            catch
            {
                // If expression evaluation fails, render the band/element
                return true;
            }
        }

        private void StartNewPage()
        {
            _currentPage = new PageLayout
            {
                PageNumber = _pages.Count + 1
            };
            _pages.Add(_currentPage);

            _context.SetPageNumber(_currentPage.PageNumber);
            _yCursor = _marginTop;

            // Pages 2+ emit PageHeader at the top so column headers repeat.
            // Page 1's order (ReportHeader above PageHeader) is driven by the
            // Compose* method calling EmitFirstPageHeaderBands after StartNewPage.
            if (_currentPage.PageNumber > 1 && _pageHeaderBand is not null)
                EmitBand(_pageHeaderBand);
        }

        // On page 1, emit ReportHeader (title) above PageHeader (column headers).
        // Called once by ComposeDataReport and ComposeEmptyReport after StartNewPage.
        private void EmitFirstPageHeaderBands()
        {
            if (_reportHeaderBand is not null)
                EmitBand(_reportHeaderBand);

            if (_pageHeaderBand is not null)
                EmitBand(_pageHeaderBand);
        }

        private void FinishPage()
        {
            // Emit page footer at the bottom of the page
            if (_pageFooterBand is not null)
            {
                var savedY = _yCursor;
                _yCursor = _pageHeight - _marginBottom - _pageFooterHeight;
                EmitBand(_pageFooterBand);
                _yCursor = savedY;
            }
        }

        private BandDefinition? FindBand(BandType type)
        {
            return _report.Bands.FirstOrDefault(b => b.Type == type);
        }
    }
}
