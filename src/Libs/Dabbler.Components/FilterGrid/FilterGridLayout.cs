// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components;

/// <summary>
/// Controls the visual layout of a <see cref="DabblerFilterGrid{T}"/>.
/// </summary>
public enum FilterGridLayout
{
    /// <summary>
    /// Horizontal row layout — all filter cells in a single row per AND group.
    /// Best for data grid filter rows where width is ample.
    /// </summary>
    Horizontal,

    /// <summary>
    /// Vertical form layout — each filter cell on its own row with a label.
    /// Best for report parameter prompts or narrow panels.
    /// </summary>
    Vertical
}
