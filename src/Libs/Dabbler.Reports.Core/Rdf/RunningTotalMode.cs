namespace Dabbler.Reports.Core.Rdf;

/// <summary>
/// Specifies how a detail-band element accumulates a running total.
/// </summary>
public enum RunningTotalMode
{
    /// <summary>No running total (default).</summary>
    None,

    /// <summary>Resets at each group boundary.</summary>
    OverGroup,

    /// <summary>Accumulates across the entire report.</summary>
    OverAll
}
