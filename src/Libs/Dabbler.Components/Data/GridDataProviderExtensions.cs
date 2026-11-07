// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using MudBlazor;

namespace Dabbler.Components.Data;

/// <summary>
/// Extension methods for adapting <see cref="IGridDataProvider{T}"/> to MudDataGrid.
/// </summary>
public static class GridDataProviderExtensions
{
    /// <summary>
    /// Creates a <c>ServerData</c> delegate compatible with <c>MudDataGrid&lt;T&gt;</c>.
    /// </summary>
    /// <typeparam name="T">The type of data item.</typeparam>
    /// <param name="provider">The grid data provider to adapt.</param>
    /// <param name="compositeFilter">
    /// Optional function that returns the current composite filter at invocation time.
    /// When provided, the filter is included in every data request.
    /// </param>
    /// <returns>
    /// A function matching MudDataGrid's <c>ServerData</c> delegate signature.
    /// </returns>
    public static Func<GridState<T>, Task<GridData<T>>> ToServerDataFunc<T>(
        this IGridDataProvider<T> provider,
        Func<CompositeFilterDescriptor?>? compositeFilter = null)
    {
        return async (gridState) =>
        {
            var request = new GridDataRequest
            {
                PageIndex = gridState.Page,
                PageSize = gridState.PageSize,
                CompositeFilter = compositeFilter?.Invoke(),
                Sorts = gridState.SortDefinitions
                    .Select(s => new SortDescriptor(s.SortBy, s.Descending))
                    .ToList(),
            };

            var response = await provider.FetchAsync(request);

            return new GridData<T>
            {
                Items = response.Items,
                TotalItems = response.TotalCount,
            };
        };
    }
}
