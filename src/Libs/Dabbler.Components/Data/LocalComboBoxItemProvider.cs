// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// An in-memory implementation of <see cref="IComboBoxItemProvider{T}"/> that filters a local collection.
/// Suitable for small lookup tables (up to a few hundred items).
/// </summary>
/// <typeparam name="T">The type of item in the collection.</typeparam>
public class LocalComboBoxItemProvider<T> : IComboBoxItemProvider<T>
{
    private readonly IReadOnlyList<T> _items;
    private readonly Func<T, string> _displaySelector;
    private readonly Func<T, object> _keySelector;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalComboBoxItemProvider{T}"/> class.
    /// </summary>
    /// <param name="items">The full set of items to search within. Null entries are filtered out.</param>
    /// <param name="displaySelector">
    /// A function that returns the display text for an item, used for search matching.
    /// If <c>null</c>, uses <see cref="object.ToString"/>.
    /// </param>
    /// <param name="keySelector">
    /// A function that returns the key value for an item, used by <see cref="GetByKeysAsync"/>.
    /// If <c>null</c>, the item itself is used as the key.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is null.</exception>
    public LocalComboBoxItemProvider(
        IEnumerable<T> items,
        Func<T, string>? displaySelector = null,
        Func<T, object>? keySelector = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        _items = items.Where(item => item is not null).ToList();
        _displaySelector = displaySelector ?? (item => item?.ToString() ?? string.Empty);
        _keySelector = keySelector ?? (item => item!);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<T>> SearchAsync(string? searchText, int maxResults, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrEmpty(searchText))
        {
            IReadOnlyList<T> initial = _items.Take(maxResults).ToList();
            return Task.FromResult(initial);
        }

        var results = new List<T>();
        for (int i = 0; i < _items.Count && results.Count < maxResults; i++)
        {
            var displayText = _displaySelector(_items[i]);
            if (displayText.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(_items[i]);
            }
        }

        return Task.FromResult<IReadOnlyList<T>>(results);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<T>> GetByKeysAsync(IReadOnlyList<object> keys, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keys);
        cancellationToken.ThrowIfCancellationRequested();

        if (keys.Count == 0)
            return Task.FromResult<IReadOnlyList<T>>(Array.Empty<T>());

        var keySet = new HashSet<object>(keys);
        var results = new List<T>(keys.Count);

        for (int i = 0; i < _items.Count; i++)
        {
            var itemKey = _keySelector(_items[i]);
            if (itemKey is not null && keySet.Remove(itemKey))
            {
                results.Add(_items[i]);
                if (keySet.Count == 0) break;
            }
        }

        return Task.FromResult<IReadOnlyList<T>>(results);
    }
}
