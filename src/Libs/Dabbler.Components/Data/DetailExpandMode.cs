// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Controls how many rows can be expanded simultaneously in a master-detail grid.
/// </summary>
public enum DetailExpandMode
{
    /// <summary>Multiple rows can be expanded simultaneously.</summary>
    Multiple,

    /// <summary>Expanding one row collapses the previously expanded row.</summary>
    Single
}
