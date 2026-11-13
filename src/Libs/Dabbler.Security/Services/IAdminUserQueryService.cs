using System.Security.Claims;

namespace Dabbler.Security.Services;

/// <summary>
/// Projects the subset of user information needed by the Security Dashboard's
/// Users and Role Assignment tab.
/// </summary>
/// <param name="UserId">Stable identifier for the user (email address — matches <c>IUserRoleStore</c> key).</param>
/// <param name="DisplayName">User's display name.</param>
/// <param name="Email">User's email address.</param>
/// <param name="IsActive">Whether the user account itself is active.</param>
/// <param name="Organizations">
/// Organizations the user is an active member of, ordered by display name.
/// </param>
/// <param name="Roles">
/// The user's current effective role names sourced from
/// <see cref="IUserRoleStore"/> (baked-in admin assignments flowing through
/// the admin UI). Sorted alphabetically so rendering is stable.
/// </param>
public sealed record AdminUserRow(
    string UserId,
    string DisplayName,
    string Email,
    bool IsActive,
    IReadOnlyList<AdminUserOrganization> Organizations,
    IReadOnlyList<string> Roles);

/// <summary>
/// Projection of one organization membership for
/// <see cref="AdminUserRow.Organizations"/>.
/// </summary>
/// <param name="OrganizationId">Organization identifier.</param>
/// <param name="OrganizationKey">Organization stable key.</param>
/// <param name="OrganizationName">Organization display name.</param>
/// <param name="IsDefault">Whether this is the user's default organization.</param>
/// <param name="IsInternal">Whether this is the configured internal organization.</param>
public sealed record AdminUserOrganization(
    int OrganizationId,
    string OrganizationKey,
    string OrganizationName,
    bool IsDefault,
    bool IsInternal);

/// <summary>
/// Scope-aware query contract for the Users and Role Assignment tab.
/// Implementations inspect the current user's active role and apply the
/// UserAdmin "assigned-orgs only" scope filter at query time so the grid
/// cannot leak users outside that scope.
/// </summary>
public interface IAdminUserQueryService
{
    /// <summary>
    /// Returns the set of users visible to the currently signed-in actor,
    /// applying UserAdmin organization scoping. SystemAdmin,
    /// CrossOrgUserAdmin, and CrossOrgMonitor see the full list.
    /// </summary>
    Task<IReadOnlyList<AdminUserRow>> GetUsersAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the set of organization identifiers the currently signed-in
    /// actor is an active member of. Used by the Users tab to apply the
    /// <see cref="RoleAssignmentAuthority"/> UserAdmin org-scope rules.
    /// </summary>
    /// <remarks>
    /// SystemAdmin and CrossOrgUserAdmin still report their membership list
    /// accurately; the authority helper short-circuits the scope check for
    /// those roles. Callers should not interpret an empty list as
    /// "no privileges" without also checking the active role.
    /// </remarks>
    Task<IReadOnlySet<int>> GetActorAssignedOrganizationIdsAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the canonical active role name of the currently signed-in
    /// actor (e.g., <c>SystemAdmin</c>, <c>CrossOrgUserAdmin</c>,
    /// <c>UserAdmin</c>), or <c>null</c> when the actor has no baked-in
    /// admin role. The caller uses this to drive the authority evaluator.
    /// </summary>
    string? GetActorActiveAdminRole();

    /// <summary>
    /// Returns the stable user identifier of the currently signed-in actor
    /// (the <c>NameIdentifier</c> claim value), or an empty string if no
    /// authenticated user is in scope. Used to drive self-assignment rules
    /// in <see cref="RoleAssignmentAuthority"/>.
    /// </summary>
    string GetActorUserId();

    // ─────────────────────────────────────────────────────────────────────
    // ClaimsPrincipal-accepting overloads (2026-05-11)
    //
    // The original four methods above resolve the actor from
    // IHttpContextAccessor.HttpContext?.User. In interactive Blazor Server
    // components, HttpContext is null per Microsoft's documented stance, so
    // those methods return null/empty and the role-assignment UI fails
    // silently. Blazor components must pass the principal obtained from
    // AuthenticationStateProvider.GetAuthenticationStateAsync() into these
    // overloads instead. Non-Blazor callers (API controllers, middleware)
    // continue to use the no-arg methods above.
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc cref="GetUsersAsync(CancellationToken)" />
    /// <param name="actor">The actor's <see cref="ClaimsPrincipal"/> from <c>AuthenticationStateProvider</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<AdminUserRow>> GetUsersAsync(ClaimsPrincipal actor, CancellationToken ct = default);

    /// <inheritdoc cref="GetActorAssignedOrganizationIdsAsync(CancellationToken)" />
    /// <param name="actor">The actor's <see cref="ClaimsPrincipal"/> from <c>AuthenticationStateProvider</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlySet<int>> GetActorAssignedOrganizationIdsAsync(ClaimsPrincipal actor, CancellationToken ct = default);

    /// <inheritdoc cref="GetActorActiveAdminRole()" />
    /// <param name="actor">The actor's <see cref="ClaimsPrincipal"/> from <c>AuthenticationStateProvider</c>.</param>
    string? GetActorActiveAdminRole(ClaimsPrincipal actor);

    /// <inheritdoc cref="GetActorUserId()" />
    /// <param name="actor">The actor's <see cref="ClaimsPrincipal"/> from <c>AuthenticationStateProvider</c>.</param>
    string GetActorUserId(ClaimsPrincipal actor);

    /// <summary>
    /// Returns the organizations the granter may assign a new user to.
    /// SystemAdmin and CrossOrgUserAdmin see all active organizations;
    /// UserAdmin sees only the organizations they are an active member of.
    /// CrossOrgMonitor and actors with no active admin role get an empty list.
    /// </summary>
    Task<IReadOnlyList<AdminUserOrganization>> GetGrantableOrganizationsAsync(ClaimsPrincipal actor, CancellationToken ct = default);
}
