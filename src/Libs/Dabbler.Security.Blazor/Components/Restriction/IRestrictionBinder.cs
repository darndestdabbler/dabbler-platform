using System.Security.Claims;
using Dabbler.Security.Configuration;

namespace Dabbler.Security.Blazor.Components.Restriction;

/// <summary>
/// Scoped service that resolves the restriction state for an
/// (entity, column) tuple under the current scope's effective roles. Used
/// by <see cref="RestrictedFormField"/> and <see cref="RestrictedGridColumn"/>
/// to decide whether a control renders, renders read-only, or renders
/// through the masked-rendering hook.
/// </summary>
/// <remarks>
/// <para>
/// The binder resolves the active <see cref="ClaimsPrincipal"/> through the
/// injected <c>AuthenticationStateProvider</c> and the active effective roles
/// through <c>IRoleSwitcher</c>. The same resolver is used by handlers and
/// other Blazor security components, so role-switch reactivity behaves
/// consistently — components that subscribe to
/// <c>AuthorizationStateNotifier</c> can call <see cref="ResolveAsync"/>
/// again on notification and re-render.
/// </para>
/// <para>
/// Multi-role aggregation is most-permissive: the column is visible if any
/// effective role can see it, editable if any effective role can edit it,
/// and unmasked if any effective role has an active mask exemption. Baked-in
/// roles are always unrestricted.
/// </para>
/// </remarks>
public interface IRestrictionBinder
{
    /// <summary>
    /// Resolves the restriction state for the given column under the
    /// current scope's effective roles, evaluated at "now"
    /// (<c>DateTimeOffset.UtcNow</c>) for mask-exemption expiry.
    /// </summary>
    /// <param name="entityKey">
    /// The entity (table) key as registered in <c>ResourceRegistry</c> /
    /// <c>MetadataRegistry</c>.
    /// </param>
    /// <param name="columnKey">The column (field) key as registered.</param>
    /// <returns>
    /// The restriction resolution. When the user is unauthenticated or no
    /// effective roles apply the resolution is unrestricted (visibility +
    /// editability default to permissive — the calling control is
    /// responsible for any additional auth gating).
    /// </returns>
    Task<RestrictionResolution> ResolveAsync(string entityKey, string columnKey);

    /// <summary>
    /// Pure resolver overload that takes the configuration and effective
    /// roles directly. Exposed so unit tests, reports, or non-Blazor
    /// callers can evaluate restrictions without spinning up the
    /// <see cref="ResolveAsync(string, string)"/> dependency graph.
    /// </summary>
    /// <param name="entityKey">The entity (table) key.</param>
    /// <param name="columnKey">The column (field) key.</param>
    /// <param name="configuration">The <see cref="SecurityConfiguration"/> snapshot.</param>
    /// <param name="effectiveRoles">
    /// The user's effective roles. Empty/null is treated as unauthenticated:
    /// the resolution is unrestricted but the caller is expected to gate.
    /// </param>
    /// <param name="evaluatedAt">
    /// The evaluation timestamp used for mask-exemption expiry. Defaults to
    /// <c>DateTimeOffset.UtcNow</c>.
    /// </param>
    RestrictionResolution Resolve(
        string entityKey,
        string columnKey,
        SecurityConfiguration configuration,
        IReadOnlyList<string> effectiveRoles,
        DateTimeOffset? evaluatedAt = null);
}
