namespace Dabbler.Reports.Core.Layout;

/// <summary>
/// Tracks running totals for detail-band elements during layout composition.
/// Each element is identified by a unique key (band name + element index).
/// </summary>
internal sealed class RunningTotalAccumulator
{
    private readonly Dictionary<string, decimal> _overAll = new();
    private readonly Dictionary<string, decimal> _overGroup = new();

    /// <summary>
    /// Accumulates a value into the running total for the given element key.
    /// Returns the new accumulated value.
    /// </summary>
    public decimal Accumulate(string elementKey, Rdf.RunningTotalMode mode, object? value)
    {
        decimal numericValue = ConvertToDecimal(value);
        switch (mode)
        {
            case Rdf.RunningTotalMode.OverAll:
                _overAll[elementKey] = _overAll.GetValueOrDefault(elementKey) + numericValue;
                return _overAll[elementKey];
            case Rdf.RunningTotalMode.OverGroup:
                _overGroup[elementKey] = _overGroup.GetValueOrDefault(elementKey) + numericValue;
                return _overGroup[elementKey];
            default:
                return numericValue;
        }
    }

    /// <summary>
    /// Resets all OverGroup accumulators at a group boundary.
    /// </summary>
    public void ResetGroup()
    {
        _overGroup.Clear();
    }

    private static decimal ConvertToDecimal(object? value)
    {
        if (value is null) return 0m;
        try { return Convert.ToDecimal(value); }
        catch { return 0m; }
    }
}
