// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components;

/// <summary>
/// Shared search logic used by both <see cref="DabblerComboBox{T}"/> and
/// <see cref="DabblerMultiComboBox{T}"/>. Wraps a caller-supplied search function,
/// tracks the most-recent search text, and provides not-in-list detection.
/// </summary>
/// <typeparam name="T">The type of item being searched.</typeparam>
internal sealed class ComboBoxSearchHelper<T>
{
    private Func<string?, CancellationToken, Task<IEnumerable<T>>>? _searchFunc;

    /// <summary>
    /// The most recent search text provided by the user.
    /// </summary>
    public string? CurrentSearchText { get; private set; }

    /// <summary>
    /// Updates the underlying search function. Call this when the component's
    /// <c>SearchFunc</c> parameter changes.
    /// </summary>
    public void UpdateSearchFunc(Func<string?, CancellationToken, Task<IEnumerable<T>>>? searchFunc)
        => _searchFunc = searchFunc;

    /// <summary>
    /// Wraps the underlying search function to track the current search text.
    /// Use this as the <c>SearchFunc</c> for <see cref="MudBlazor.MudAutocomplete{T}"/>.
    /// </summary>
    public async Task<IEnumerable<T>> SearchAsync(string? text, CancellationToken ct)
    {
        CurrentSearchText = text;
        if (_searchFunc is null) return [];
        return await _searchFunc(text, ct);
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="inputText"/> is non-empty and differs
    /// from <paramref name="selectedText"/> (ordinal comparison).
    /// </summary>
    public static bool IsNotInList(string? inputText, string? selectedText) =>
        !string.IsNullOrEmpty(inputText) &&
        !string.Equals(inputText, selectedText, StringComparison.Ordinal);
}
