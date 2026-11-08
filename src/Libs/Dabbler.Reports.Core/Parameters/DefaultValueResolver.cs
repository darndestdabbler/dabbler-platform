using System.Globalization;

namespace Dabbler.Reports.Core.Parameters;

/// <summary>
/// Resolves default value expressions (e.g., "Today", "FirstOfMonth") to typed values.
/// Used to pre-populate report parameter editors when a
/// <see cref="Rdf.ParameterDefinition.DefaultValue"/> is set.
/// </summary>
public static class DefaultValueResolver
{
    /// <summary>
    /// Resolves a default value expression to a typed value.
    /// </summary>
    /// <param name="expression">The default value expression (e.g., "Today", "FirstOfMonth", or a literal like "42").</param>
    /// <param name="dataType">The target .NET data type name (e.g., "DateTime", "Int32", "String").</param>
    /// <returns>The resolved value, or <c>null</c> if the expression is null or empty.</returns>
    public static object? Resolve(string? expression, string dataType)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return null;

        var knownValue = ResolveKnownExpression(expression);
        if (knownValue != null)
            return knownValue;

        return ParseLiteral(expression, dataType);
    }

    private static object? ResolveKnownExpression(string expression)
    {
        return expression.Trim() switch
        {
            "Today" => DateTime.Today,
            "Now" => DateTime.Now,
            "FirstOfMonth" => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
            "LastOfMonth" => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
                .AddMonths(1).AddDays(-1),
            "FirstOfYear" => new DateTime(DateTime.Today.Year, 1, 1),
            "LastOfYear" => new DateTime(DateTime.Today.Year, 12, 31),
            _ => null,
        };
    }

    private static object? ParseLiteral(string expression, string dataType)
    {
        return dataType switch
        {
            "DateTime" => DateTime.TryParse(expression, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var dt) ? dt : null,
            "Int32" => int.TryParse(expression, CultureInfo.InvariantCulture, out var i) ? i : null,
            "Decimal" => decimal.TryParse(expression, CultureInfo.InvariantCulture, out var d) ? d : null,
            "Boolean" => bool.TryParse(expression, out var b) ? b : null,
            "String" => expression,
            _ => expression,
        };
    }
}
