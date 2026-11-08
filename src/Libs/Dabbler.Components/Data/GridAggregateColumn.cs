// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Defines which aggregate to display in a group header row for a specific column.
/// </summary>
public class GridAggregateColumn
{
    /// <summary>Property name this aggregate applies to.</summary>
    public string PropertyName { get; set; } = "";

    /// <summary>Aggregate function: Sum, Average, Min, Max, Count.</summary>
    public string AggregateFunction { get; set; } = "Sum";

    /// <summary>Optional format string for the aggregate value (e.g., "C2", "N0").</summary>
    public string? Format { get; set; }

    /// <summary>Optional label text. Defaults to "{Function}:" when shown in the header.</summary>
    public string? Label { get; set; }

    /// <summary>
    /// Builds the aggregate key as used in <see cref="GroupAggregateResult.Aggregates"/>.
    /// E.g., "Salary_Sum", "Salary_Average".
    /// </summary>
    public string AggregateKey => $"{PropertyName}_{AggregateFunction}";
}
