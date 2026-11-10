using Dabbler.Components.Data.Metadata;

namespace Dabbler.Platform.Stubs;

/// <summary>
/// Defines the reusable metadata, navigation, and optional workspace-facing
/// stub state for a stub-backed platform host.
/// </summary>
public sealed class StubPlatformDefinition
{
    /// <summary>
    /// Metadata surfaced to shell, query, and detail consumers.
    /// </summary>
    public IReadOnlyList<EntityMetadata> Entities { get; init; } = [];

    /// <summary>
    /// Navigation records keyed by entity and smart-filter name.
    /// </summary>
    public StubNavigationDataset Navigation { get; init; } = new();

    /// <summary>
    /// Optional report items shown in the shell navigation.
    /// </summary>
    public IReadOnlyList<NavigationReportItem> ReportItems { get; init; } = [];

    /// <summary>
    /// Optional home-page routing used by shell hosts.
    /// </summary>
    public HomePageConfiguration? HomePageConfiguration { get; init; }

    /// <summary>
    /// Optional dashboard definitions for shell hosts.
    /// </summary>
    public IReadOnlyDictionary<string, DashboardConfiguration> Dashboards { get; init; } =
        new Dictionary<string, DashboardConfiguration>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Optional artifact catalog entries for compatibility menu flows.
    /// </summary>
    public IReadOnlyList<ArtifactCatalogEntry> CatalogEntries { get; init; } = [];

    /// <summary>
    /// Optional usage records for compatibility menu flows.
    /// </summary>
    public IReadOnlyList<ArtifactUsageRecord> UsageRecords { get; init; } = [];
}
