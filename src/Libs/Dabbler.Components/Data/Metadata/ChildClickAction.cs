// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data.Metadata;

/// <summary>
/// Specifies how a child record opens when clicked in a child tab grid.
/// </summary>
public enum ChildClickAction
{
    /// <summary>
    /// Open the child record in a new workspace tab.
    /// </summary>
    OpenTab,

    /// <summary>
    /// Open the child record in a popup dialog.
    /// </summary>
    OpenPopup
}
