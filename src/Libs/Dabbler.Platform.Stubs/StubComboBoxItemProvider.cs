using Dabbler.Components.Data;

namespace Dabbler.Platform.Stubs;

/// <summary>
/// Supported stub wrapper over <see cref="LocalComboBoxItemProvider{T}"/> for lookup flows.
/// </summary>
/// <typeparam name="T">The lookup item type.</typeparam>
public sealed class StubComboBoxItemProvider<T> : LocalComboBoxItemProvider<T>
{
    /// <summary>
    /// Initializes a new stub combo-box provider over an in-memory item set.
    /// </summary>
    public StubComboBoxItemProvider(
        IEnumerable<T> items,
        Func<T, string>? displaySelector = null,
        Func<T, object>? keySelector = null)
        : base(items, displaySelector, keySelector)
    {
    }
}
