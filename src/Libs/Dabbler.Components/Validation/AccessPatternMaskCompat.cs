// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

// Compatibility stub for MudBlazor.AccessPatternMask which was part of the local
// MudBlazor fork but is not in the published NuGet package.

namespace MudBlazor;

/// <summary>
/// A mask that uses a pattern string to define allowed and required character positions.
/// </summary>
public class AccessPatternMask : PatternMask
{
    /// <summary>
    /// Gets the access mask pattern string.
    /// </summary>
    public string AccessMask { get; }

    /// <summary>
    /// Gets a value indicating whether all required positions in the mask have been filled.
    /// </summary>
    public bool IsMaskComplete => Text?.Length == Mask?.Length;

    /// <summary>
    /// Initializes a new instance of <see cref="AccessPatternMask"/> with the specified pattern.
    /// </summary>
    /// <param name="accessMask">The mask pattern string.</param>
    public AccessPatternMask(string accessMask) : base(accessMask)
    {
        AccessMask = accessMask;
    }
}
