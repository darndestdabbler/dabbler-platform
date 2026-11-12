// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data.Metadata;

/// <summary>
/// Tracks a user's access history and pin state for an application artifact.
/// Drives the "pinned" and "recently/frequently used" sections of the top menu bar dropdowns.
/// </summary>
public class ArtifactUsageRecord
{
    /// <summary>
    /// Identifier of the user who accessed the artifact.
    /// </summary>
    public string UserId { get; set; } = "";

    /// <summary>
    /// The type of artifact (Form, Query, Report, Dashboard).
    /// </summary>
    public ArtifactType ArtifactType { get; set; }

    /// <summary>
    /// Unique identifier of the specific artifact instance.
    /// </summary>
    public string ArtifactId { get; set; } = "";

    /// <summary>
    /// Timestamp of the user's most recent access to this artifact.
    /// </summary>
    public DateTime LastAccessedAt { get; set; }

    /// <summary>
    /// Cumulative number of times the user has accessed this artifact.
    /// Used together with <see cref="LastAccessedAt"/> for frequency-weighted ranking.
    /// </summary>
    public int AccessCount { get; set; } = 0;

    /// <summary>
    /// Whether the user has pinned this artifact for quick access in the menu dropdown.
    /// </summary>
    public bool IsPinned { get; set; } = false;
}
