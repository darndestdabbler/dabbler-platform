namespace Dabbler.Security.Configuration;

/// <summary>
/// Restricts a user-type role's access to a specific UI surface — either an entire
/// page (<see cref="ControlKey"/> = <c>"*"</c>) or a single control on a page.
/// </summary>
/// <remarks>
/// <para>
/// <c>"*"</c> on <see cref="ControlKey"/> is the "Entire Page" convention. The page
/// authorization handler treats an Entire-Page <see cref="UiRestrictionKind.Hidden"/>
/// entry as a 403; an Entire-Page <see cref="UiRestrictionKind.Disabled"/> entry is
/// informational (succeed for navigation; the page itself renders its controls in a
/// read-only state).
/// </para>
/// <para>
/// User-type roles only. Absence of a <see cref="UiRestriction"/> means the role has
/// full access — user-type roles are default-permissive.
/// </para>
/// </remarks>
public sealed class UiRestriction
{
    /// <summary>
    /// The page the restriction applies to.
    /// </summary>
    public string PageKey { get; set; } = string.Empty;

    /// <summary>
    /// The control on the page that is restricted. Use <c>"*"</c> to restrict the
    /// entire page.
    /// </summary>
    public string ControlKey { get; set; } = string.Empty;

    /// <summary>
    /// How the target (page or control) is restricted for this role.
    /// </summary>
    public UiRestrictionKind Kind { get; set; } = UiRestrictionKind.Hidden;
}

/// <summary>
/// The binary shape of a <see cref="UiRestriction"/>. "Allow" is deliberately
/// absent — absence of a <see cref="UiRestriction"/> entry IS allow for user-type
/// roles.
/// </summary>
public enum UiRestrictionKind
{
    /// <summary>
    /// The target is rendered but non-interactive. Navigation to a Disabled page
    /// is permitted; the page's controls render in a read-only state.
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// The target is not rendered. Menu items for the page are suppressed; direct
    /// navigation returns a 403 response.
    /// </summary>
    Hidden = 1,
}
