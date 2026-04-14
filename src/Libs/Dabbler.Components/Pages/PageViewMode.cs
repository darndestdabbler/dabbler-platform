// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using System.ComponentModel;

namespace Dabbler.Components;

/// <summary>
/// Specifies the active view in the compatibility-only <c>DabblerQueryReportPage&lt;T&gt;</c>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[Obsolete("PageViewMode is retained for the compatibility-only DabblerQueryReportPage<T> surface.")]
public enum PageViewMode
{
    /// <summary>Grid view showing tabular data.</summary>
    Grid,

    /// <summary>Report view showing a composed report.</summary>
    Report
}
