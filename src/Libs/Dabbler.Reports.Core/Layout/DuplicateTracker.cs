namespace Dabbler.Reports.Core.Layout;

/// <summary>
/// Tracks the last displayed value for Hide Duplicates elements during layout composition.
/// Suppresses consecutive duplicate values by comparing formatted strings.
/// </summary>
internal sealed class DuplicateTracker
{
    private readonly Dictionary<string, string?> _lastValues = new();

    /// <summary>
    /// Checks whether the current value is a duplicate of the last displayed value
    /// for the given element key. Updates the tracker if the value is new.
    /// </summary>
    /// <returns>True if the value should be hidden (it's a duplicate).</returns>
    public bool IsDuplicate(string elementKey, string? currentValue)
    {
        bool isDuplicate = _lastValues.TryGetValue(elementKey, out var lastValue)
                           && string.Equals(lastValue, currentValue, StringComparison.Ordinal);

        if (!isDuplicate)
        {
            _lastValues[elementKey] = currentValue;
        }

        return isDuplicate;
    }

    /// <summary>
    /// Resets tracking at group boundaries so the first row of each group
    /// always shows the value, matching Access behavior.
    /// </summary>
    public void ResetForGroup()
    {
        _lastValues.Clear();
    }
}
