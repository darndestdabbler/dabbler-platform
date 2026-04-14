// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using System.Text;
using System.Text.Encodings.Web;

namespace Dabbler.Components.Data;

/// <summary>
/// Builds human-readable summaries of active filter state.
/// All methods are pure (no side effects) and suitable for unit testing.
/// </summary>
public static class FilterSummaryBuilder
{
    /// <summary>
    /// Returns a structured list of active filter conditions, one item per active filter entry.
    /// Quick-filter entries (if any) are included with <see cref="FilterSummaryItem.GroupIndex"/> = -1.
    /// Internal date-range end entries (<c>__RangeEnd</c>) are excluded.
    /// </summary>
    public static IReadOnlyList<FilterSummaryItem> GetItems(
        FilterGroupCollection groups,
        IReadOnlyList<FilterColumnDefinition> columns,
        string? quickFilterText = null)
    {
        var headerLookup = columns.ToDictionary(
            c => c.PropertyName,
            c => c.Header ?? c.PropertyName,
            StringComparer.OrdinalIgnoreCase);

        var items = new List<FilterSummaryItem>();

        if (!string.IsNullOrEmpty(quickFilterText))
        {
            items.Add(new FilterSummaryItem
            {
                ColumnHeader = "Search",
                PropertyName = "__quickfilter",
                OperatorLabel = "contains",
                FormattedValue = quickFilterText,
                GroupIndex = -1,
            });
        }

        for (int gi = 0; gi < groups.Groups.Count; gi++)
        {
            var group = groups.Groups[gi];
            // group.Entries already excludes internal __RangeEnd entries.
            foreach (var entry in group.Entries)
            {
                headerLookup.TryGetValue(entry.Key, out var header);
                items.Add(new FilterSummaryItem
                {
                    ColumnHeader = header ?? entry.Key,
                    PropertyName = entry.Key,
                    OperatorLabel = FilterOperatorInfo.GetLabel(entry.Value.Operator),
                    FormattedValue = FormatValue(entry.Value.Value),
                    GroupIndex = gi,
                });
            }
        }

        return items;
    }

    /// <summary>
    /// Returns a plain-text summary of all active filters, e.g.
    /// <c>Search contains 'John' AND (Name contains 'Smith') OR (Amount Equals '100')</c>.
    /// Returns an empty string when no filters are active.
    /// </summary>
    public static string GetSummary(
        FilterGroupCollection groups,
        IReadOnlyList<FilterColumnDefinition> columns,
        string? quickFilterText = null)
        => GetSummary(groups, columns, FilterSummaryFormat.Plain, quickFilterText);

    /// <summary>
    /// Returns a formatted summary of all active filters in the requested format.
    /// Returns an empty string when no filters are active.
    /// </summary>
    public static string GetSummary(
        FilterGroupCollection groups,
        IReadOnlyList<FilterColumnDefinition> columns,
        FilterSummaryFormat format,
        string? quickFilterText = null)
    {
        var items = GetItems(groups, columns, quickFilterText);
        if (items.Count == 0)
            return "";

        return format == FilterSummaryFormat.Html
            ? BuildHtml(items)
            : BuildPlain(items);
    }

    // ── Plain text ────────────────────────────────────────────────────

    private static string BuildPlain(IReadOnlyList<FilterSummaryItem> items)
    {
        var parts = new List<string>();

        var quickItems = items.Where(i => i.GroupIndex < 0).ToList();
        if (quickItems.Count > 0)
            parts.Add(string.Join(" AND ", quickItems.Select(PlainItem)));

        var byGroup = items
            .Where(i => i.GroupIndex >= 0)
            .GroupBy(i => i.GroupIndex)
            .OrderBy(g => g.Key)
            .Select(g => string.Join(" AND ", g.Select(PlainItem)))
            .ToList();

        if (byGroup.Count == 1)
        {
            parts.Add(byGroup[0]);
        }
        else if (byGroup.Count > 1)
        {
            parts.Add("(" + string.Join(") OR (", byGroup) + ")");
        }

        return string.Join(" AND ", parts);
    }

    private static string PlainItem(FilterSummaryItem item)
    {
        if (string.IsNullOrEmpty(item.FormattedValue))
            return $"{item.ColumnHeader} {item.OperatorLabel}";
        return $"{item.ColumnHeader} {item.OperatorLabel} '{item.FormattedValue}'";
    }

    // ── HTML ─────────────────────────────────────────────────────────

    private static string BuildHtml(IReadOnlyList<FilterSummaryItem> items)
    {
        var sb = new StringBuilder();
        sb.Append("<span class=\"filter-summary\">");

        var parts = new List<string>();

        var quickItems = items.Where(i => i.GroupIndex < 0).ToList();
        foreach (var qi in quickItems)
            parts.Add(HtmlItem(qi));

        var byGroup = items
            .Where(i => i.GroupIndex >= 0)
            .GroupBy(i => i.GroupIndex)
            .OrderBy(g => g.Key)
            .ToList();

        if (byGroup.Count == 1)
        {
            foreach (var item in byGroup[0])
                parts.Add(HtmlItem(item));
        }
        else if (byGroup.Count > 1)
        {
            var groupStrings = byGroup.Select(g =>
                "<span class=\"filter-group\">(" +
                string.Join(" <strong>AND</strong> ", g.Select(HtmlItem)) +
                ")</span>");
            parts.Add(string.Join(" <strong>OR</strong> ", groupStrings));
        }

        sb.Append(string.Join(" <strong>AND</strong> ", parts));
        sb.Append("</span>");
        return sb.ToString();
    }

    private static string HtmlItem(FilterSummaryItem item)
    {
        var header = HtmlEncode(item.ColumnHeader);
        var op = HtmlEncode(item.OperatorLabel);
        if (string.IsNullOrEmpty(item.FormattedValue))
            return $"<em>{header}</em> {op}";
        var val = HtmlEncode(item.FormattedValue);
        return $"<em>{header}</em> {op} <strong>{val}</strong>";
    }

    private static string HtmlEncode(string text)
    {
        var sb = new StringBuilder();
        HtmlEncoder.Default.Encode(new StringWriter(sb), text, 0, text.Length);
        return sb.ToString();
    }

    // ── Value formatting ──────────────────────────────────────────────

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "",
            bool b => b ? "Yes" : "No",
            DateTime dt => dt.ToString("d"),
            _ => value.ToString() ?? "",
        };
    }
}
