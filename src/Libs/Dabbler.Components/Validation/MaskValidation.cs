// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using MudBlazor;

namespace Dabbler.Components.Validation;

/// <summary>
/// Provides validation functions for use with <see cref="AccessPatternMask"/> and MudBlazor form components.
/// </summary>
/// <remarks>
/// <para>
/// These functions are designed to be passed to MudBlazor's <c>Validation</c> parameter on
/// <c>MudTextField</c> and other form components.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// &lt;MudTextField Mask="@_mask" Validation="@MaskValidation.IsMaskComplete(_mask)" /&gt;
/// </code>
/// </para>
/// </remarks>
public static class MaskValidation
{
    /// <summary>
    /// Returns a validation function that checks whether all required positions in an
    /// <see cref="AccessPatternMask"/> have been filled.
    /// </summary>
    /// <param name="mask">The mask to validate against.</param>
    /// <param name="errorMessage">
    /// The error message returned when validation fails.
    /// Defaults to <c>"All required fields must be filled."</c>.
    /// </param>
    /// <returns>
    /// A <c>Func&lt;string, string?&gt;</c> suitable for MudBlazor's <c>Validation</c> parameter.
    /// Returns <c>null</c> when valid, or <paramref name="errorMessage"/> when incomplete.
    /// </returns>
    public static Func<string?, string?> IsMaskComplete(
        AccessPatternMask mask,
        string errorMessage = "All required fields must be filled.")
    {
        return _ => mask.IsMaskComplete ? null : errorMessage;
    }

    /// <summary>
    /// Returns a validation function that checks whether the mask text has the expected
    /// clean text length, useful for masks where all positions must be filled.
    /// </summary>
    /// <param name="mask">The mask to validate against.</param>
    /// <param name="expectedLength">
    /// The expected length of the clean (unmasked) text.
    /// For example, a US phone number mask expects 10 digits.
    /// </param>
    /// <param name="errorMessage">
    /// The error message returned when validation fails.
    /// Defaults to <c>"Input is incomplete."</c>.
    /// </param>
    /// <returns>
    /// A <c>Func&lt;string, string?&gt;</c> suitable for MudBlazor's <c>Validation</c> parameter.
    /// Returns <c>null</c> when valid, or <paramref name="errorMessage"/> when the clean text
    /// length does not match.
    /// </returns>
    public static Func<string?, string?> HasCleanTextLength(
        IMask mask,
        int expectedLength,
        string errorMessage = "Input is incomplete.")
    {
        return _ =>
        {
            var cleanText = mask.GetCleanText();
            return cleanText?.Length == expectedLength ? null : errorMessage;
        };
    }
}
