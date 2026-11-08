// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Discriminates the visual row types that can appear in a grouped data grid.
/// </summary>
public enum GridRowType
{
    /// <summary>A regular data row containing a data item.</summary>
    Data,

    /// <summary>A group header row showing the group key, item count, and aggregate values.</summary>
    GroupHeader
}
