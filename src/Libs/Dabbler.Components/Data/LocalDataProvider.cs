// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Reflection;
using Dabbler.Components.Utilities;

namespace Dabbler.Components.Data;

/// <summary>
/// An in-memory implementation of <see cref="IGridDataProvider{T}"/> that wraps a local collection
/// and performs sort, filter, group, and page operations on the client side.
/// </summary>
/// <typeparam name="T">The type of data item.</typeparam>
public class LocalDataProvider<T> : IGridDataProvider<T>, IDisposable
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _numericPropertyCache = new();

    private readonly IEnumerable<T> _source;
    private readonly Dictionary<string, IComparer> _customComparers = new();
    private readonly Dictionary<string, Func<T, object?>> _valueResolvers = new();
    private bool _disposed;

    /// <summary>
    /// Raised when an error occurs during data processing (e.g., missing property).
    /// </summary>
    public event EventHandler<DataProviderErrorEventArgs>? Error;

    /// <summary>
    /// Raised when the underlying source collection changes, signaling that a re-fetch may be needed.
    /// </summary>
    public event EventHandler? DataChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalDataProvider{T}"/> class.
    /// </summary>
    /// <param name="source">The collection to wrap. If it implements <see cref="INotifyCollectionChanged"/>,
    /// the provider will raise <see cref="DataChanged"/> when the collection changes.</param>
    public LocalDataProvider(IEnumerable<T> source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));

        if (source is INotifyCollectionChanged observable)
        {
            observable.CollectionChanged += OnSourceCollectionChanged;
        }
    }

    /// <summary>
    /// Registers a custom sort comparer for the specified property.
    /// </summary>
    public void SetCustomComparer(string propertyName, IComparer comparer)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        ArgumentNullException.ThrowIfNull(comparer);
        _customComparers[propertyName] = comparer;
    }

    /// <summary>
    /// Removes a custom sort comparer for the specified property.
    /// </summary>
    public void ClearCustomComparer(string propertyName)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        _customComparers.Remove(propertyName);
    }

    /// <summary>
    /// Registers a value resolver for a calculated/virtual column.
    /// </summary>
    public void SetValueResolver(string propertyName, Func<T, object?> resolver)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        ArgumentNullException.ThrowIfNull(resolver);
        _valueResolvers[propertyName] = resolver;
    }

    /// <summary>
    /// Removes a value resolver for the specified property name.
    /// </summary>
    public void ClearValueResolver(string propertyName)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        _valueResolvers.Remove(propertyName);
    }

    /// <inheritdoc />
    public Task<GridDataResponse<T>> FetchAsync(GridDataRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var items = SnapshotSource();
            items = HasCompositeFilters(request.CompositeFilter)
                ? ApplyCompositeFilter(items, request.CompositeFilter!)
                : ApplyFilters(items, request.Filters);
            var totalCount = items.Count;
            items = ApplySort(items, request.Sorts);
            var (groupAggregates, groupRanges) = ApplyGrouping(items, request.Groups, out items);

            var pageItems = ApplyPaging(items, request.PageIndex, request.PageSize);

            var response = new GridDataResponse<T>
            {
                Items = pageItems,
                TotalCount = totalCount,
                GroupAggregates = groupAggregates,
                GroupRanges = groupRanges
            };

            return Task.FromResult(response);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            OnError(new DataProviderErrorEventArgs(ex));
            return Task.FromResult(new GridDataResponse<T>());
        }
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases resources used by the provider.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing && _source is INotifyCollectionChanged observable)
        {
            observable.CollectionChanged -= OnSourceCollectionChanged;
        }
        _disposed = true;
    }

    /// <summary>
    /// Raises the <see cref="Error"/> event.
    /// </summary>
    protected virtual void OnError(DataProviderErrorEventArgs e)
    {
        Error?.Invoke(this, e);
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    // ── Pipeline Steps ──────────────────────────────────────────────────

    private List<T> SnapshotSource()
    {
        var list = new List<T>();
        foreach (var item in _source)
        {
            if (item is not null)
                list.Add(item);
        }
        return list;
    }

    /// <summary>
    /// Pre-computed filter data to avoid per-item allocations during filtering.
    /// </summary>
    private struct PreparedFilter
    {
        public FilterDescriptor Descriptor;
        public string? CachedFilterString;
        public object? CachedCoercedValue;
        public bool CoercedValueResolved;
    }

    private List<T> ApplyFilters(List<T> items, IReadOnlyList<FilterDescriptor> filters)
    {
        if (filters.Count == 0)
            return items;

        var prepared = new PreparedFilter[filters.Count];
        for (int i = 0; i < filters.Count; i++)
        {
            prepared[i].Descriptor = filters[i];

            switch (filters[i].Operator)
            {
                case FilterOperators.Contains:
                case FilterOperators.StartsWith:
                case FilterOperators.EndsWith:
                    prepared[i].CachedFilterString = filters[i].Value?.ToString();
                    break;
            }
        }

        var result = new List<T>(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            if (MatchesAllFilters(items[i], prepared))
                result.Add(items[i]);
        }
        return result;
    }

    private object? ResolveValue(T item, string propertyName)
    {
        if (_valueResolvers.TryGetValue(propertyName, out var resolver))
            return resolver(item);
        return PropertyAccessor.GetValue(item!, propertyName);
    }

    private bool MatchesAllFilters(T item, PreparedFilter[] filters)
    {
        for (int i = 0; i < filters.Length; i++)
        {
            if (!MatchesFilter(item, ref filters[i]))
                return false;
        }
        return true;
    }

    private bool MatchesFilter(T item, ref PreparedFilter pf)
    {
        var filter = pf.Descriptor;
        var value = ResolveValue(item, filter.PropertyName);

        switch (filter.Operator)
        {
            case FilterOperators.IsEmpty:
                return value is null || (value is string s && string.IsNullOrEmpty(s));

            case FilterOperators.IsNotEmpty:
                return value is not null && !(value is string s2 && string.IsNullOrEmpty(s2));

            case FilterOperators.Equals:
                if (value is null && filter.Value is null) return true;
                if (value is null || filter.Value is null) return false;
                if (value is string sv && filter.Value is string fv)
                    return string.Equals(sv, fv, StringComparison.OrdinalIgnoreCase);
                return object.Equals(value, CoerceFilterValue(filter.Value, value.GetType()));

            case FilterOperators.Contains:
                if (value is null) return false;
                var valueStr = value.ToString();
                if (valueStr is null || pf.CachedFilterString is null) return false;
                return valueStr.Contains(pf.CachedFilterString, StringComparison.OrdinalIgnoreCase);

            case FilterOperators.StartsWith:
                if (value is null) return false;
                var valStr = value.ToString();
                if (valStr is null || pf.CachedFilterString is null) return false;
                return valStr.StartsWith(pf.CachedFilterString, StringComparison.OrdinalIgnoreCase);

            case FilterOperators.EndsWith:
                if (value is null) return false;
                var endValStr = value.ToString();
                if (endValStr is null || pf.CachedFilterString is null) return false;
                return endValStr.EndsWith(pf.CachedFilterString, StringComparison.OrdinalIgnoreCase);

            case FilterOperators.GreaterThan:
            case FilterOperators.LessThan:
            case FilterOperators.GreaterThanOrEqual:
            case FilterOperators.LessThanOrEqual:
                return CompareValues(value, ref pf, filter.Operator);

            case FilterOperators.In:
                return MatchesIn(value, filter.Value);

            default:
                return false;
        }
    }

    private static bool CompareValues(object? value, ref PreparedFilter pf, string op)
    {
        if (value is null) return false;
        if (pf.Descriptor.Value is null) return false;

        if (!pf.CoercedValueResolved)
        {
            pf.CachedCoercedValue = CoerceFilterValue(pf.Descriptor.Value, value.GetType());
            pf.CoercedValueResolved = true;
        }

        if (pf.CachedCoercedValue is null) return false;

        int comparison;
        try
        {
            comparison = Comparer.Default.Compare(value, pf.CachedCoercedValue);
        }
        catch
        {
            return false;
        }

        return op switch
        {
            FilterOperators.GreaterThan => comparison > 0,
            FilterOperators.LessThan => comparison < 0,
            FilterOperators.GreaterThanOrEqual => comparison >= 0,
            FilterOperators.LessThanOrEqual => comparison <= 0,
            _ => false
        };
    }

    private static object? CoerceFilterValue(object? filterValue, Type targetType)
    {
        if (filterValue is null) return null;
        if (targetType.IsInstanceOfType(filterValue)) return filterValue;

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        try
        {
            return Convert.ChangeType(filterValue, underlyingType);
        }
        catch
        {
            return null;
        }
    }

    private static bool MatchesIn(object? value, object? filterValue)
    {
        if (filterValue is not IEnumerable collection)
            return false;

        foreach (var candidate in collection)
        {
            if (value is null && candidate is null) return true;
            if (value is null || candidate is null) continue;

            if (value is string sv && candidate is string cv)
            {
                if (string.Equals(sv, cv, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            else
            {
                var coerced = CoerceFilterValue(candidate, value.GetType());
                if (object.Equals(value, coerced))
                    return true;
            }
        }
        return false;
    }

    // ── Composite Filters ───────────────────────────────────────────────

    private static bool HasCompositeFilters(CompositeFilterDescriptor? composite)
    {
        if (composite is null) return false;
        if (composite.Filters.Count > 0) return true;
        return composite.Children.Any(c => HasCompositeFilters(c));
    }

    private List<T> ApplyCompositeFilter(List<T> items, CompositeFilterDescriptor composite)
    {
        var result = new List<T>(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            if (EvaluateComposite(items[i], composite))
                result.Add(items[i]);
        }
        return result;
    }

    private bool EvaluateComposite(T item, CompositeFilterDescriptor composite)
    {
        // Evaluate leaf filters at this level
        bool hasConditions = false;

        if (composite.Logic == CompositeFilterLogic.And)
        {
            // AND: all must match
            foreach (var filter in composite.Filters)
            {
                hasConditions = true;
                var pf = new PreparedFilter
                {
                    Descriptor = filter,
                    CachedFilterString = filter.Operator is FilterOperators.Contains
                        or FilterOperators.StartsWith or FilterOperators.EndsWith
                        ? filter.Value?.ToString() : null,
                };
                if (!MatchesFilter(item, ref pf))
                    return false;
            }
            foreach (var child in composite.Children)
            {
                hasConditions = true;
                if (!EvaluateComposite(item, child))
                    return false;
            }
            // If no conditions at all, nothing to fail → true
            return !hasConditions || true;
        }
        else
        {
            // OR: any must match
            foreach (var filter in composite.Filters)
            {
                hasConditions = true;
                var pf = new PreparedFilter
                {
                    Descriptor = filter,
                    CachedFilterString = filter.Operator is FilterOperators.Contains
                        or FilterOperators.StartsWith or FilterOperators.EndsWith
                        ? filter.Value?.ToString() : null,
                };
                if (MatchesFilter(item, ref pf))
                    return true;
            }
            foreach (var child in composite.Children)
            {
                hasConditions = true;
                if (EvaluateComposite(item, child))
                    return true;
            }
            // If no conditions at all, nothing to match → false
            return !hasConditions;
        }
    }

    // ── Sort ────────────────────────────────────────────────────────────

    private List<T> ApplySort(List<T> items, IReadOnlyList<SortDescriptor> sorts)
    {
        if (sorts.Count == 0 || items.Count <= 1)
            return items;

        // Stable sort via index tracking
        var indexed = new (T Item, int Index)[items.Count];
        for (int i = 0; i < items.Count; i++)
            indexed[i] = (items[i], i);

        var comparer = new CompositeComparer(sorts, _customComparers, _valueResolvers);

        Array.Sort(indexed, (a, b) =>
        {
            int result = comparer.Compare(a.Item, b.Item);
            return result != 0 ? result : a.Index.CompareTo(b.Index);
        });

        var sorted = new List<T>(items.Count);
        for (int i = 0; i < indexed.Length; i++)
            sorted.Add(indexed[i].Item);

        return sorted;
    }

    // ── Grouping ────────────────────────────────────────────────────────

    private static (IReadOnlyList<GroupAggregateResult>?, IReadOnlyList<GroupItemRange>?) ApplyGrouping(
        List<T> items,
        IReadOnlyList<GroupDescriptor> groups,
        out List<T> reorderedItems)
    {
        if (groups.Count == 0)
        {
            reorderedItems = items;
            return (null, null);
        }

        reorderedItems = new List<T>(items.Count);
        var aggregateResults = new List<GroupAggregateResult>();
        var ranges = new List<GroupItemRange>();

        ApplyGroupingRecursive(items, groups, 0, reorderedItems, aggregateResults, ranges, -1);

        return (aggregateResults, ranges);
    }

    private static void ApplyGroupingRecursive(
        List<T> items,
        IReadOnlyList<GroupDescriptor> groups,
        int groupLevel,
        List<T> reorderedItems,
        List<GroupAggregateResult> aggregateResults,
        List<GroupItemRange> ranges,
        int parentRangeIndex)
    {
        var groupDescriptor = groups[groupLevel];
        var nullSentinel = new object();
        var grouped = new Dictionary<object, List<T>>();
        var groupOrder = new List<object>();

        for (int i = 0; i < items.Count; i++)
        {
            var key = PropertyAccessor.GetValue(items[i]!, groupDescriptor.PropertyName) ?? nullSentinel;
            if (!grouped.TryGetValue(key, out var list))
            {
                list = new List<T>();
                grouped[key] = list;
                groupOrder.Add(key);
            }
            list.Add(items[i]);
        }

        bool hasSubGroups = groupLevel + 1 < groups.Count;

        foreach (var key in groupOrder)
        {
            var groupItems = grouped[key];
            var actualKey = ReferenceEquals(key, nullSentinel) ? null : key;
            var aggregates = ComputeAggregates(groupItems);

            int startIndex = reorderedItems.Count;

            aggregateResults.Add(new GroupAggregateResult
            {
                Key = actualKey,
                Count = groupItems.Count,
                Aggregates = aggregates,
                Level = groupLevel
            });

            int thisRangeIndex = ranges.Count;
            ranges.Add(new GroupItemRange(actualKey, startIndex, groupItems.Count, groupLevel, parentRangeIndex));

            if (hasSubGroups)
            {
                ApplyGroupingRecursive(groupItems, groups, groupLevel + 1, reorderedItems, aggregateResults, ranges, thisRangeIndex);
            }
            else
            {
                reorderedItems.AddRange(groupItems);
            }
        }
    }

    private static PropertyInfo[] GetNumericProperties(Type type)
    {
        return _numericPropertyCache.GetOrAdd(type, t =>
        {
            var allProps = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var numeric = new List<PropertyInfo>();
            for (int i = 0; i < allProps.Length; i++)
            {
                var propType = Nullable.GetUnderlyingType(allProps[i].PropertyType) ?? allProps[i].PropertyType;
                if (IsNumericType(propType))
                    numeric.Add(allProps[i]);
            }
            return numeric.ToArray();
        });
    }

    private static Dictionary<string, object?> ComputeAggregates(List<T> groupItems)
    {
        if (groupItems.Count == 0)
            return new Dictionary<string, object?>();

        var aggregates = new Dictionary<string, object?>();
        var numericProperties = GetNumericProperties(typeof(T));

        foreach (var prop in numericProperties)
        {
            var propertyName = prop.Name;
            decimal sum = 0;
            decimal min = decimal.MaxValue;
            decimal max = decimal.MinValue;
            int count = 0;

            for (int i = 0; i < groupItems.Count; i++)
            {
                var val = PropertyAccessor.GetValue(groupItems[i]!, propertyName);
                if (val is null) continue;

                decimal decVal;
                try
                {
                    decVal = Convert.ToDecimal(val);
                }
                catch
                {
                    continue;
                }

                sum += decVal;
                if (decVal < min) min = decVal;
                if (decVal > max) max = decVal;
                count++;
            }

            if (count > 0)
            {
                aggregates[$"{propertyName}_Sum"] = sum;
                aggregates[$"{propertyName}_Average"] = sum / count;
                aggregates[$"{propertyName}_Min"] = min;
                aggregates[$"{propertyName}_Max"] = max;
                aggregates[$"{propertyName}_Count"] = count;
            }
        }

        return aggregates;
    }

    private static bool IsNumericType(Type type) =>
        type == typeof(int) || type == typeof(long) || type == typeof(float) ||
        type == typeof(double) || type == typeof(decimal) || type == typeof(short) ||
        type == typeof(byte) || type == typeof(uint) || type == typeof(ulong) ||
        type == typeof(ushort) || type == typeof(sbyte);

    // ── Paging ──────────────────────────────────────────────────────────

    private static List<T> ApplyPaging(List<T> items, int pageIndex, int pageSize)
    {
        if (pageSize <= 0)
            return [];

        int skip = pageIndex * pageSize;
        if (skip >= items.Count)
            return [];

        int take = Math.Min(pageSize, items.Count - skip);
        return items.GetRange(skip, take);
    }

    // ── Inner Types ─────────────────────────────────────────────────────

    private sealed class CompositeComparer : IComparer<T>
    {
        private readonly IReadOnlyList<SortDescriptor> _sorts;
        private readonly Dictionary<string, IComparer> _customComparers;
        private readonly Dictionary<string, Func<T, object?>> _valueResolvers;

        public CompositeComparer(
            IReadOnlyList<SortDescriptor> sorts,
            Dictionary<string, IComparer> customComparers,
            Dictionary<string, Func<T, object?>> valueResolvers)
        {
            _sorts = sorts;
            _customComparers = customComparers;
            _valueResolvers = valueResolvers;
        }

        private object? Resolve(T item, string propertyName)
        {
            if (_valueResolvers.TryGetValue(propertyName, out var resolver))
                return resolver(item);
            return PropertyAccessor.GetValue(item!, propertyName);
        }

        public int Compare(T? x, T? y)
        {
            if (x is null && y is null) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            for (int i = 0; i < _sorts.Count; i++)
            {
                var sort = _sorts[i];
                var valX = Resolve(x, sort.PropertyName);
                var valY = Resolve(y, sort.PropertyName);

                int result;
                if (_customComparers.TryGetValue(sort.PropertyName, out var custom))
                {
                    result = custom.Compare(valX, valY);
                }
                else
                {
                    result = CompareWithNulls(valX, valY);
                }

                if (sort.Descending)
                    result = -result;

                if (result != 0)
                    return result;
            }

            return 0;
        }

        private static int CompareWithNulls(object? x, object? y)
        {
            // Nulls first in ascending, last in descending (negation applied by caller)
            if (x is null && y is null) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            if (x is IComparable comparableX)
                return comparableX.CompareTo(y);

            if (y is IComparable comparableY)
                return -comparableY.CompareTo(x);

            return string.Compare(x.ToString(), y.ToString(), StringComparison.Ordinal);
        }
    }
}

/// <summary>
/// Event arguments for data provider errors.
/// </summary>
public class DataProviderErrorEventArgs : EventArgs
{
    /// <summary>
    /// Gets the exception that occurred.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataProviderErrorEventArgs"/> class.
    /// </summary>
    public DataProviderErrorEventArgs(Exception exception)
    {
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }
}
