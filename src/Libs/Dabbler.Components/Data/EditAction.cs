// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Specifies the action that triggered an edit ending.
/// </summary>
public enum EditAction
{
    /// <summary>The edit was committed.</summary>
    Commit,

    /// <summary>The edit was canceled.</summary>
    Cancel,
}
