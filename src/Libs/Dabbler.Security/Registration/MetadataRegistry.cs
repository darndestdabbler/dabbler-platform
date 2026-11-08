namespace Dabbler.Security.Registration;

/// <summary>
/// Semantic facade over <see cref="ResourceRegistry"/> that exposes the registered
/// resources in the vocabulary the security dashboard uses — tables and columns
/// (instead of entities and fields) and the pages and controls a role can be
/// restricted on.
/// </summary>
/// <remarks>
/// <para>
/// The underlying <see cref="ResourceRegistry"/> keeps the "entity / field"
/// naming from the legacy grant-based model because authorization handlers and
/// the EF store still speak that vocabulary. The restriction-based Role Master
/// Details UI, on the other hand, speaks "table / column" — matching the
/// <see cref="Configuration.ColumnRestriction"/>, <see cref="Configuration.RowRestriction"/>,
/// <see cref="Configuration.DefaultMaskRule"/>, and <see cref="Configuration.MaskExemption"/>
/// shape. This registry is a thin read-only wrapper that resolves that mismatch
/// without duplicating the registration surface.
/// </para>
/// <para>
/// Registered alongside <see cref="ResourceRegistry"/> in
/// <c>AddDabblerSecurity</c> as a singleton. Admin components inject this
/// registry for cascading Table / Column dropdowns; authorization handlers
/// keep using <see cref="ResourceRegistry"/> directly.
/// </para>
/// </remarks>
public sealed class MetadataRegistry
{
    private readonly ResourceRegistry _registry;

    /// <summary>
    /// Initializes a new instance of <see cref="MetadataRegistry"/>.
    /// </summary>
    /// <param name="registry">The underlying resource registry.</param>
    public MetadataRegistry(ResourceRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// Empty singleton for tests and no-op scenarios.
    /// </summary>
    public static readonly MetadataRegistry Empty = new(ResourceRegistry.Empty);

    /// <summary>
    /// All registered tables. Each table's <see cref="RegisteredEntity.Key"/> is
    /// the table key used by <see cref="Configuration.ColumnRestriction.EntityKey"/>,
    /// <see cref="Configuration.RowRestriction.EntityKey"/>,
    /// <see cref="Configuration.DefaultMaskRule.EntityKey"/>, and
    /// <see cref="Configuration.MaskExemption.EntityKey"/>.
    /// </summary>
    public IReadOnlyList<RegisteredEntity> Tables => _registry.Entities;

    /// <summary>
    /// Returns the registered table with the given key, or <c>null</c> when not
    /// registered. Comparison is case-insensitive.
    /// </summary>
    /// <param name="tableKey">The table (entity) key.</param>
    public RegisteredEntity? GetTable(string tableKey)
    {
        if (string.IsNullOrEmpty(tableKey))
            return null;

        foreach (var table in _registry.Entities)
        {
            if (string.Equals(table.Key, tableKey, StringComparison.OrdinalIgnoreCase))
                return table;
        }

        return null;
    }

    /// <summary>
    /// Returns the columns declared on the given table, or an empty list when
    /// the table is not registered.
    /// </summary>
    /// <param name="tableKey">The table (entity) key.</param>
    public IReadOnlyList<RegisteredField> GetColumns(string tableKey) =>
        GetTable(tableKey)?.Fields ?? [];

    /// <summary>
    /// All registered pages. Each page's <see cref="RegisteredPage.Key"/> is the
    /// page key used by <see cref="Configuration.UiRestriction.PageKey"/>.
    /// </summary>
    public IReadOnlyList<RegisteredPage> Pages => _registry.Pages;

    /// <summary>
    /// Returns the registered page with the given key, or <c>null</c> when not
    /// registered. Comparison is case-insensitive.
    /// </summary>
    public RegisteredPage? GetPage(string pageKey)
    {
        if (string.IsNullOrEmpty(pageKey))
            return null;

        foreach (var page in _registry.Pages)
        {
            if (string.Equals(page.Key, pageKey, StringComparison.OrdinalIgnoreCase))
                return page;
        }

        return null;
    }

    /// <summary>
    /// Returns the controls declared on the given page, or an empty list when
    /// the page has no registered controls. Does not include the synthetic
    /// <c>"*"</c> (Entire Page) token — callers prepend that themselves.
    /// </summary>
    /// <param name="pageKey">The page key.</param>
    public IReadOnlyList<RegisteredControl> GetControls(string pageKey)
    {
        if (string.IsNullOrEmpty(pageKey))
            return [];

        var matches = new List<RegisteredControl>();
        foreach (var control in _registry.Controls)
        {
            if (string.Equals(control.PageKey, pageKey, StringComparison.OrdinalIgnoreCase))
                matches.Add(control);
        }
        return matches;
    }

    /// <summary>
    /// The wildcard <see cref="Configuration.UiRestriction.ControlKey"/> that
    /// represents the entire page (vs. a single control on the page).
    /// </summary>
    public const string EntirePageControlKey = "*";

    /// <summary>
    /// Display label for <see cref="EntirePageControlKey"/> shown in cascading
    /// control dropdowns.
    /// </summary>
    public const string EntirePageDisplayName = "Entire Page";
}
