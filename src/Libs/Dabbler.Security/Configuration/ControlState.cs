namespace Dabbler.Security.Configuration;

/// <summary>
/// The visible / interactive state that <c>DabblerSecured</c> applies to its
/// wrapped content after evaluating the active role's <see cref="UiRestriction"/>
/// list for the wrapped <c>PageKey</c> + <c>ControlKey</c>. Baked-in roles always
/// resolve to <see cref="Enabled"/>; user-type roles resolve to whichever state
/// matches their per-control restriction, or <see cref="Enabled"/> when no
/// restriction is configured (user-type roles are default-permissive).
/// </summary>
public enum ControlState
{
    /// <summary>The control is fully functional and visible.</summary>
    Enabled = 0,

    /// <summary>The control is visible but non-interactive (e.g., greyed out).</summary>
    Disabled = 1,

    /// <summary>The control is completely removed from the UI and not visible.</summary>
    Hidden = 2,
}
