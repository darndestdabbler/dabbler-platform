// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Defines well-known filter operator strings for use with <see cref="FilterDescriptor"/>.
/// </summary>
public static class FilterOperators
{
    /// <summary>Exact match (case-insensitive for strings).</summary>
    public new const string Equals = "Equals";

    /// <summary>String contains (case-insensitive).</summary>
    public const string Contains = "Contains";

    /// <summary>String starts with (case-insensitive).</summary>
    public const string StartsWith = "StartsWith";

    /// <summary>String ends with (case-insensitive).</summary>
    public const string EndsWith = "EndsWith";

    /// <summary>Greater than.</summary>
    public const string GreaterThan = "GreaterThan";

    /// <summary>Less than.</summary>
    public const string LessThan = "LessThan";

    /// <summary>Greater than or equal.</summary>
    public const string GreaterThanOrEqual = "GreaterThanOrEqual";

    /// <summary>Less than or equal.</summary>
    public const string LessThanOrEqual = "LessThanOrEqual";

    /// <summary>Value is null or empty string.</summary>
    public const string IsEmpty = "IsEmpty";

    /// <summary>Value is not null and not empty string.</summary>
    public const string IsNotEmpty = "IsNotEmpty";

    /// <summary>Value is in a collection of values.</summary>
    public const string In = "In";
}
