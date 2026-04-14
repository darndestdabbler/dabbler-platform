using System.Security.Claims;

namespace Dabbler.Security.Services;

/// <summary>
/// Scoped service (per-request in Web API; per-circuit in Blazor Server) that
/// intercepts role resolution so that a privileged user can impersonate another
/// role for testing or support workflows.
/// </summary>
/// <remarks>
/// When no switch is active, <see cref="EffectiveRoles"/> falls back to the
/// user's <see cref="ClaimTypes.Role"/> claims.
/// </remarks>
public interface IRoleSwitcher
{
    /// <summary>
    /// Gets a value indicating whether a role override is active for this scope.
    /// </summary>
    /// <value>
    /// <c>true</c> when a role switch is active; otherwise, <c>false</c>.
    /// </value>
    bool IsRoleSwitched { get; }

    /// <summary>
    /// Returns the effective roles for the user. This is the single source-of-truth
    /// used by all authorization handlers and Blazor components.
    /// </summary>
    /// <param name="user">The <see cref="ClaimsPrincipal"/> representing the current user.</param>
    /// <returns>
    /// An enumerable of role names. Returns the switched role when active;
    /// otherwise returns the user's <see cref="ClaimTypes.Role"/> claims.
    /// </returns>
    IEnumerable<string> EffectiveRoles(ClaimsPrincipal user);

    /// <summary>
    /// Sets the override role for this scope.
    /// </summary>
    /// <param name="roleName">The name of the role to switch to.</param>
    /// <remarks>
    /// The caller is responsible for verifying the switch is permitted
    /// (e.g., the user's natural role is in a pre-configured list of allowed roles).
    /// </remarks>
    void SwitchToRole(string roleName);

    /// <summary>
    /// Clears any active role switch, reverting to the user's natural role claims.
    /// </summary>
    void ClearSwitch();
}
