using System.Globalization;

namespace Dabbler.Reports.Core.Parameters;

/// <summary>
/// Converts raw parameter values to typed objects based on a
/// <see cref="Rdf.ParameterDefinition.DataType"/> string.
/// Used when collecting parameter values from editors for query binding.
/// </summary>
public static class ParameterTypeConverter
{
    /// <summary>
    /// Converts a raw value to the specified data type.
    /// </summary>
    /// <param name="rawValue">The raw value from the editor control.</param>
    /// <param name="dataType">The target .NET data type name (e.g., "DateTime", "Int32", "String").</param>
    /// <returns>The converted value, or <c>null</c> if conversion fails or the input is null.</returns>
    public static object? ConvertToType(object? rawValue, string dataType)
    {
        if (rawValue == null)
            return null;

        if (IsCorrectType(rawValue, dataType))
            return rawValue;

        var stringValue = rawValue.ToString();
        if (string.IsNullOrWhiteSpace(stringValue))
            return null;

        return dataType switch
        {
            "DateTime" => DateTime.TryParse(stringValue, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var dt) ? dt : null,
            "Int32" => int.TryParse(stringValue, CultureInfo.InvariantCulture, out var i) ? i : null,
            "Decimal" => decimal.TryParse(stringValue, CultureInfo.InvariantCulture, out var d) ? d : null,
            "Boolean" => bool.TryParse(stringValue, out var b) ? b : null,
            "String" => stringValue,
            _ => rawValue,
        };
    }

    private static bool IsCorrectType(object value, string dataType)
    {
        return dataType switch
        {
            "DateTime" => value is DateTime,
            "Int32" => value is int,
            "Decimal" => value is decimal,
            "Boolean" => value is bool,
            "String" => value is string,
            _ => false,
        };
    }
}
