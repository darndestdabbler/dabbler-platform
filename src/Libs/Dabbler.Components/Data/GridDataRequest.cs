// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Describes a data fetch request including sort, filter, and paging parameters.
/// </summary>
/// <remarks>
/// This model is intentionally transport-agnostic. The sort and filter descriptors use
/// simple string-based representations that can be serialized over REST or gRPC and
/// translated to MudDataGrid's <c>SortDefinition</c> / <c>FilterDefinition</c> types.
/// </remarks>
public class GridDataRequest
{
    /// <summary>
    /// The sort descriptors to apply, in priority order.
    /// </summary>
    /// <remarks>
    /// Defaults to an empty list (no sorting).
    /// </remarks>
    public IReadOnlyList<SortDescriptor> Sorts { get; init; } = [];

    /// <summary>
    /// The filter descriptors to apply.
    /// </summary>
    /// <remarks>
    /// <para>Defaults to an empty list (no filtering).</para>
    /// <para>
    /// When <see cref="CompositeFilter"/> is set, it takes precedence over this property
    /// for implementations that support composite (AND/OR) filter logic.
    /// </para>
    /// </remarks>
    public IReadOnlyList<FilterDescriptor> Filters { get; init; } = [];

    /// <summary>
    /// The composite filter with AND/OR group logic.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>null</c>.  When set, takes precedence over <see cref="Filters"/>.
    /// </remarks>
    public CompositeFilterDescriptor? CompositeFilter { get; init; }

    /// <summary>
    /// The zero-based page index to retrieve.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>0</c> (first page).
    /// </remarks>
    public int PageIndex { get; init; }

    /// <summary>
    /// The number of items per page.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>50</c>.
    /// </remarks>
    public int PageSize { get; init; } = 50;

    /// <summary>
    /// The group descriptors to apply.
    /// </summary>
    /// <remarks>
    /// Defaults to an empty list (no grouping).
    /// When grouping is active, items in the response are reordered so that members of
    /// the same group are contiguous, and <see cref="GridDataResponse{T}.GroupAggregates"/>
    /// and <see cref="GridDataResponse{T}.GroupRanges"/> are populated.
    /// </remarks>
    public IReadOnlyList<GroupDescriptor> Groups { get; init; } = [];
}

/// <summary>
/// Describes a sort operation on a single property.
/// </summary>
/// <param name="PropertyName">The property name to sort by.</param>
/// <param name="Descending">Whether to sort in descending order.</param>
public record SortDescriptor(string PropertyName, bool Descending = false);

/// <summary>
/// Describes a filter operation on a single property.
/// </summary>
/// <param name="PropertyName">The property name to filter on.</param>
/// <param name="Operator">The filter operator (e.g., <c>"equals"</c>, <c>"contains"</c>).</param>
/// <param name="Value">The filter value to compare against.</param>
public record FilterDescriptor(string PropertyName, string Operator, object? Value);

/// <summary>
/// Describes a composite filter combining multiple filter descriptors with AND/OR logic.
/// </summary>
public class CompositeFilterDescriptor
{
    /// <summary>
    /// The logical operator used to combine child filters.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="CompositeFilterLogic.And"/>.
    /// </remarks>
    public CompositeFilterLogic Logic { get; init; } = CompositeFilterLogic.And;

    /// <summary>
    /// The child filter descriptors.
    /// </summary>
    public IReadOnlyList<FilterDescriptor> Filters { get; init; } = [];

    /// <summary>
    /// Nested composite filters for complex expressions (e.g., <c>(A AND B) OR (C AND D)</c>).
    /// </summary>
    /// <remarks>
    /// Defaults to an empty list.  When combined with <see cref="Filters"/>, all conditions
    /// at this level are joined by <see cref="Logic"/>.
    /// </remarks>
    public IReadOnlyList<CompositeFilterDescriptor> Children { get; init; } = [];
}

/// <summary>
/// Specifies the logical operator for combining filters in a <see cref="CompositeFilterDescriptor"/>.
/// </summary>
public enum CompositeFilterLogic
{
    /// <summary>
    /// All filters must match (logical AND).
    /// </summary>
    And,

    /// <summary>
    /// Any filter may match (logical OR).
    /// </summary>
    Or
}
