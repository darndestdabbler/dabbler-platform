// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Output format for <see cref="FilterSummaryBuilder"/> summary strings.
/// </summary>
public enum FilterSummaryFormat
{
    /// <summary>Plain text with no markup.</summary>
    Plain,

    /// <summary>HTML with semantic elements for highlighting.</summary>
    Html,
}
