namespace Dabbler.Security.Blazor.Components.Restriction;

/// <summary>
/// The resolved restriction state for a single (entity, column) tuple under
/// the active user's effective roles. Combines a column's visibility,
/// editability, and masked-rendering hint into a single read-only value the
/// data-bound wrappers (<c>RestrictedFormField</c>, <c>RestrictedGridColumn</c>)
/// can apply without re-walking the configuration.
/// </summary>
/// <param name="EntityKey">
/// The entity (table) key resolved against, echoed back so consumers can
/// correlate the resolution with the inputs without holding both.
/// </param>
/// <param name="ColumnKey">The column (field) key resolved against.</param>
/// <param name="Visibility">
/// Whether the column should render at all. <see cref="ColumnVisibility.Hidden"/>
/// means the wrapper renders nothing (form field omitted, grid column omitted).
/// </param>
/// <param name="Editability">
/// Whether the column is editable when visible.
/// <see cref="ColumnEditability.ReadOnly"/> means the wrapper renders the
/// child but in a non-interactive state (form field
/// <c>Disabled</c>/<c>ReadOnly</c>; grid column with no editor).
/// </param>
/// <param name="IsMasked">
/// When <c>true</c>, the column is platform-level sensitive (declared via
/// <c>DefaultMaskRule</c>) AND no <c>MaskExemption</c> is active for any of
/// the user's effective roles. The wrapper renders a placeholder via the
/// Set D rendering hook instead of the child. When <c>false</c>, the column
/// renders its raw value or is unaffected.
/// </param>
/// <param name="MaskFormat">
/// The <c>DefaultMaskRule.MaskFormat</c> when <see cref="IsMasked"/> is
/// <c>true</c>; otherwise <see cref="string.Empty"/>. Set D consumes this
/// to render a tokenised mask; this session emits the value through the
/// hook and lets Set D wire the formatter.
/// </param>
public readonly record struct RestrictionResolution(
    string EntityKey,
    string ColumnKey,
    ColumnVisibility Visibility,
    ColumnEditability Editability,
    bool IsMasked,
    string MaskFormat)
{
    /// <summary>
    /// The unrestricted resolution: column is fully visible, editable, and
    /// not masked. Used for baked-in roles (which are unrestricted by
    /// design) and as the safe default when no user-type role applies.
    /// </summary>
    public static RestrictionResolution Unrestricted(string entityKey, string columnKey) =>
        new(entityKey, columnKey, ColumnVisibility.Visible, ColumnEditability.Editable, false, string.Empty);
}

/// <summary>
/// Whether the data-bound column should render at all under the active
/// user's effective roles.
/// </summary>
public enum ColumnVisibility
{
    /// <summary>The column renders.</summary>
    Visible = 0,

    /// <summary>The column is omitted entirely.</summary>
    Hidden = 1,
}

/// <summary>
/// Whether the visible data-bound column is editable. Independent of
/// <see cref="ColumnVisibility"/> — read-only state only matters when the
/// column is also visible.
/// </summary>
public enum ColumnEditability
{
    /// <summary>The control accepts user input.</summary>
    Editable = 0,

    /// <summary>The control renders disabled/read-only.</summary>
    ReadOnly = 1,
}
