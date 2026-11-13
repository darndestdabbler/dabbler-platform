namespace Dabbler.Reports.Core.Expressions;

/// <summary>
/// Aggregate functions available in report expressions.
/// </summary>
public enum AggregateFunction
{
    /// <summary>Sum of values.</summary>
    Sum,

    /// <summary>Count of values.</summary>
    Count,

    /// <summary>Average of values.</summary>
    Avg,

    /// <summary>Minimum value.</summary>
    Min,

    /// <summary>Maximum value.</summary>
    Max
}
