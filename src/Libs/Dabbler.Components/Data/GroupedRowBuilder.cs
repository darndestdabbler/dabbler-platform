// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Builds a flat list of <see cref="GridRowDescriptor{T}"/> from a <see cref="GridDataResponse{T}"/>
/// and a <see cref="GroupExpandState"/>, interleaving group headers with data rows.
/// </summary>
public static class GroupedRowBuilder
{
    /// <summary>
    /// Builds the visible row list based on current expand state.
    /// Collapsed groups show only the header; expanded groups show the header followed by
    /// their data rows (or nested group headers for multi-level grouping).
    /// When no grouping is active (<see cref="GridDataResponse{T}.GroupRanges"/> is null),
    /// returns a simple flat list of data rows.
    /// </summary>
    public static IReadOnlyList<GridRowDescriptor<T>> Build<T>(
        GridDataResponse<T> response,
        GroupExpandState expandState)
    {
        if (response.GroupRanges == null || response.GroupRanges.Count == 0)
            return BuildFlatDataRows(response.Items);

        // Build children lookup: rangeIndex → ordered list of child range indices.
        var children = new Dictionary<int, List<int>>();
        for (int i = 0; i < response.GroupRanges.Count; i++)
        {
            int parentIdx = response.GroupRanges[i].ParentRangeIndex;
            if (parentIdx >= 0)
            {
                if (!children.TryGetValue(parentIdx, out var list))
                    children[parentIdx] = list = new List<int>();
                list.Add(i);
            }
        }

        // Build aggregate lookup: (Level, KeyString) → GroupAggregateResult.
        var aggLookup = new Dictionary<(int Level, string? KeyString), GroupAggregateResult>();
        if (response.GroupAggregates != null)
        {
            foreach (var agg in response.GroupAggregates)
                aggLookup[(agg.Level, agg.Key?.ToString())] = agg;
        }

        // Collect top-level range indices (no parent).
        var topLevelIndices = new List<int>();
        for (int i = 0; i < response.GroupRanges.Count; i++)
        {
            if (response.GroupRanges[i].ParentRangeIndex == -1)
                topLevelIndices.Add(i);
        }

        var result = new List<GridRowDescriptor<T>>();
        AppendRows(topLevelIndices, response, expandState, children, aggLookup, result);
        return result;
    }

    // ── Private helpers ───────────────────────────────────────────────

    private static IReadOnlyList<GridRowDescriptor<T>> BuildFlatDataRows<T>(IReadOnlyList<T> items)
    {
        var rows = new List<GridRowDescriptor<T>>(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            rows.Add(new GridRowDescriptor<T>
            {
                RowType = GridRowType.Data,
                DataItem = items[i],
                DataIndex = i
            });
        }
        return rows;
    }

    private static void AppendRows<T>(
        List<int> rangeIndices,
        GridDataResponse<T> response,
        GroupExpandState expandState,
        Dictionary<int, List<int>> children,
        Dictionary<(int, string?), GroupAggregateResult> aggLookup,
        List<GridRowDescriptor<T>> result)
    {
        foreach (int idx in rangeIndices)
        {
            var range = response.GroupRanges![idx];
            bool isExpanded = expandState.IsExpanded(range.Level, range.GroupKey);

            aggLookup.TryGetValue((range.Level, range.GroupKey?.ToString()), out var agg);

            result.Add(new GridRowDescriptor<T>
            {
                RowType = GridRowType.GroupHeader,
                GroupKey = range.GroupKey,
                GroupLevel = range.Level,
                GroupItemCount = range.Count,
                Aggregates = agg?.Aggregates,
                IsExpanded = isExpanded
            });

            if (!isExpanded)
                continue;

            if (children.TryGetValue(idx, out var childIndices))
            {
                // Has nested groups — recurse.
                AppendRows(childIndices, response, expandState, children, aggLookup, result);
            }
            else
            {
                // Leaf group — emit the data rows for this range.
                for (int i = range.StartIndex; i < range.StartIndex + range.Count; i++)
                {
                    result.Add(new GridRowDescriptor<T>
                    {
                        RowType = GridRowType.Data,
                        DataItem = response.Items[i],
                        DataIndex = i
                    });
                }
            }
        }
    }
}
