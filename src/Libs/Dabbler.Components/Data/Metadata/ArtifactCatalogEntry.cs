// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data.Metadata;

/// <summary>
/// Describes an application artifact for catalog browsing and search.
/// Displayed in the "All Forms", "All Reports", etc. catalog search pages.
/// </summary>
public class ArtifactCatalogEntry
{
    /// <summary>
    /// Unique identifier of the artifact.
    /// </summary>
    public string ArtifactId { get; set; } = "";

    /// <summary>
    /// The type of artifact (Form, Query, Report, Dashboard).
    /// </summary>
    public ArtifactType ArtifactType { get; set; }

    /// <summary>
    /// Display name of the artifact (e.g., "Customer Entry Form").
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Optional description or purpose of the artifact.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional category for grouping in catalog views (e.g., "Sales", "Inventory").
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Optional tags for search and filtering.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Optional MudBlazor icon name displayed alongside the artifact name.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// The entity this artifact is associated with, if applicable (e.g., "Customer" for a Customer form).
    /// </summary>
    public string? EntityName { get; set; }
}
