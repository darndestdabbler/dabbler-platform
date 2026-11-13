// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using System.Text.RegularExpressions;

namespace Dabbler.Components.Data;

/// <summary>
/// Factory methods for creating built-in <see cref="FilterValidationRule"/> instances.
/// </summary>
public static class FilterValidation
{
    /// <summary>
    /// Creates a rule that fails when the value is null or an empty string.
    /// </summary>
    public static FilterValidationRule Required(string? message = null) => new()
    {
        ErrorMessage = message ?? "This field is required.",
        ValidateFunc = (value, _) =>
        {
            if (value is null) return false;
            if (value is string s) return s.Length > 0;
            return true;
        }
    };

    /// <summary>
    /// Creates a rule that fails when a numeric value is less than <paramref name="min"/>.
    /// Non-numeric and null values pass (use <see cref="Required"/> to enforce non-null).
    /// </summary>
    public static FilterValidationRule MinValue(decimal min, string? message = null) => new()
    {
        ErrorMessage = message ?? $"Value must be at least {min}.",
        ValidateFunc = (value, _) =>
        {
            if (value is null) return true;
            try { return Convert.ToDecimal(value) >= min; }
            catch { return true; }
        }
    };

    /// <summary>
    /// Creates a rule that fails when a numeric value is greater than <paramref name="max"/>.
    /// Non-numeric and null values pass.
    /// </summary>
    public static FilterValidationRule MaxValue(decimal max, string? message = null) => new()
    {
        ErrorMessage = message ?? $"Value must be no more than {max}.",
        ValidateFunc = (value, _) =>
        {
            if (value is null) return true;
            try { return Convert.ToDecimal(value) <= max; }
            catch { return true; }
        }
    };

    /// <summary>
    /// Creates a rule that fails when the string representation of the value does not match
    /// the given regular expression <paramref name="pattern"/>.
    /// Null values pass.
    /// </summary>
    public static FilterValidationRule Regex(string pattern, string? message = null) => new()
    {
        ErrorMessage = message ?? "Invalid format.",
        ValidateFunc = (value, _) =>
        {
            if (value is null) return true;
            var s = value.ToString() ?? string.Empty;
            return System.Text.RegularExpressions.Regex.IsMatch(s, pattern);
        }
    };

    /// <summary>
    /// Creates a rule with a custom validation function.
    /// </summary>
    /// <param name="validate">
    /// Function receiving the value and property name. Returns <c>true</c> if valid.
    /// </param>
    /// <param name="message">Error message shown when the function returns <c>false</c>.</param>
    public static FilterValidationRule Custom(Func<object?, string, bool> validate, string? message = null) => new()
    {
        ErrorMessage = message ?? "Invalid value.",
        ValidateFunc = validate
    };
}
