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
    /// <returns>
    /// A function matching MudDataGrid's <c>Func&lt;GridState&lt;T&gt;, CancellationToken, Task&lt;GridData&lt;T&gt;&gt;&gt;</c> signature.
    /// </returns>
    public static Func<GridState<T>, CancellationToken, Task<GridData<T>>> ToServerDataFunc<T>(
        this IGridDataProvider<T> provider)
    {
        return async (gridState, cancellationToken) =>
        {
            var request = new GridDataRequest
            {
                PageIndex = gridState.Page,
                PageSize = gridState.PageSize,
                Sorts = gridState.SortDefinitions
                    .Select(s => new SortDescriptor(s.SortBy, s.Descending))
                    .ToList(),
            };

            var response = await provider.FetchAsync(request, cancellationToken);

            return new GridData<T>
            {
                Items = response.Items,
                TotalItems = response.TotalCount,
            };
        };
    }
}
