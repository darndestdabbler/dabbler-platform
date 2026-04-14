// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components;

/// <summary>
/// Identifies the kind of content hosted in a workspace tab.
/// </summary>
public enum WorkspaceTabType
{
    /// <summary>A single-record detail/edit form.</summary>
    Detail,

    /// <summary>A search or query results grid.</summary>
    Search,

    /// <summary>A report view.</summary>
    Report,

    /// <summary>A dashboard view.</summary>
    Dashboard,

    /// <summary>An artifact catalog search page (All Forms, All Reports, etc.).</summary>
    Catalog
}
