namespace ComponentCatalogCrawler;

/// <summary>
/// One row of the component catalog. Field shape and required-property
/// rules match the W0 schema published at
/// <c>../dabbler-uat-dsl/schema/component-catalog.json</c>; the
/// <see cref="JsonCatalogAdapter"/> in that repo is the authoritative
/// consumer.
/// </summary>
public sealed record CatalogElement(
    string RoutePath,
    string Role,
    string ElementRole,
    string UserVisibleLabel,
    string? AriaLabel,
    string? TestId,
    string? HtmlTag,
    string? Scope,
    bool IsWatermarked = false);

public sealed record CatalogManifest(
    string AppName,
    string GeneratedAt,
    string SchemaVersion,
    IReadOnlyList<CatalogElement> Entries);
