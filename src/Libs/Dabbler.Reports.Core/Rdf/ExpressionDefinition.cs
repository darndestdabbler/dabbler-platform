namespace Dabbler.Reports.Core.Rdf;

/// <summary>
/// Named calculated expression that can reference fields, parameters,
/// aggregate functions, and arithmetic operators.
/// </summary>
public class ExpressionDefinition
{
    /// <summary>Gets or sets the expression name for referencing from elements.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the expression text (e.g., "[Quantity] * [UnitPrice]").</summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>Gets or sets the result data type.</summary>
    public string? ResultType { get; set; }
}
