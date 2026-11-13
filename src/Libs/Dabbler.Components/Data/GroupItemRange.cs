// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Describes a contiguous range of items belonging to a single group in the result set.
/// </summary>
/// <param name="GroupKey">The group key value.</param>
/// <param name="StartIndex">The zero-based start index in the result items list.</param>
/// <param name="Count">The number of items in this range.</param>
/// <param name="Level">The zero-based grouping level.</param>
/// <param name="ParentRangeIndex">The index of the parent range, or -1 for top-level groups.</param>
public record GroupItemRange(object? GroupKey, int StartIndex, int Count, int Level, int ParentRangeIndex);
