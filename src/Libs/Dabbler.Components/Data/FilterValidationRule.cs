// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Defines a single validation rule for a filter column.
/// </summary>
/// <remarks>
/// Create instances via the <see cref="FilterValidation"/> factory class or by constructing
/// directly with a custom <see cref="ValidateFunc"/>.
/// </remarks>
public class FilterValidationRule
{
    /// <summary>
    /// The error message to display when <see cref="ValidateFunc"/> returns false.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The validation function. Receives the current filter value and the column property name.
    /// Returns <c>true</c> if the value is valid, <c>false</c> otherwise.
    /// </summary>
    public Func<object?, string, bool>? ValidateFunc { get; set; }
}
