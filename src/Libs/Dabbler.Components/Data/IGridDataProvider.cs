// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Abstraction over a data source that supports server-side sort, filter, and paging operations.
/// </summary>
/// <typeparam name="T">The type of data item.</typeparam>
/// <remarks>
/// <para>
/// Implementations can wrap REST APIs, gRPC services, or in-memory collections.
/// </para>
/// <para>
/// To use with <c>MudDataGrid</c>'s <c>ServerData</c> delegate, call
/// <see cref="GridDataProviderExtensions.ToServerDataFunc{T}"/> to create the adapter.
/// </para>
/// </remarks>
public interface IGridDataProvider<T>
{
    /// <summary>
    /// Fetches data according to the specified request parameters.
    /// </summary>
    /// <param name="request">The data request containing sort, filter, and paging parameters.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A response containing the requested data page and total item count.</returns>
    Task<GridDataResponse<T>> FetchAsync(GridDataRequest request, CancellationToken cancellationToken = default);
}
