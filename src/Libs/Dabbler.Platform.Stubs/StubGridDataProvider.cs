using Dabbler.Components.Data;

namespace Dabbler.Platform.Stubs;

/// <summary>
/// Supported stub wrapper over <see cref="LocalDataProvider{T}"/> for query and grid flows.
/// </summary>
/// <typeparam name="T">The row type.</typeparam>
public class StubGridDataProvider<T> : LocalDataProvider<T>
{
    /// <summary>
    /// Initializes a new stub provider over an in-memory item set.
    /// </summary>
    public StubGridDataProvider(IEnumerable<T> items)
        : base(items)
    {
    }
}
