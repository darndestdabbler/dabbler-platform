namespace Dabbler.Reports.Core.Data;

/// <summary>
/// Holds a resolved parameter name and its typed value,
/// ready for binding to a report query.
/// </summary>
public class ParameterValue
{
    /// <summary>Gets the parameter name (matches <see cref="Rdf.ParameterDefinition.Name"/>).</summary>
    public string Name { get; }

    /// <summary>Gets the resolved parameter value, or null if the parameter was left empty.</summary>
    public object? Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ParameterValue"/> class.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The resolved value.</param>
    public ParameterValue(string name, object? value)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
        Value = value;
    }
}
