using System.Security.Claims;
using Dabbler.Security.Models;

namespace Dabbler.Security.Services;

/// <summary>
/// Writes <see cref="AppSensitiveAccessLog"/> rows for the four sensitive-field
/// event kinds. The auditor is the single funnel through which audit rows enter
/// the temporal table — direct DbContext writes are reserved for migrations and
/// out-of-band corrections per the principle in project-guidance.md
/// (Architecture → temporal-table auditing) and the spec's "append-only at the
/// app layer" Risk Note.
/// </summary>
public interface ISensitiveAccessAuditor
{
    /// <summary>
    /// Records a role-switch event. Fires both when a user switches into a
    /// non-natural role and when they revert (clear) back to their natural
    /// role — both transitions are reviewer-relevant.
    /// </summary>
    /// <param name="user">The principal in effect when the switch happened.</param>
    /// <param name="roleName">
    /// The canonical name of the role the user is now operating under (the
    /// switched-to role on switch; the natural role on revert).
    /// </param>
    /// <param name="details">
    /// Optional free-form supplementary text (e.g., "switch" / "revert").
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    Task AuditRoleSwitchAsync(ClaimsPrincipal user, string roleName, string? details = null, CancellationToken ct = default);

    /// <summary>
    /// Records that a granter created a new mask exemption.
    /// </summary>
    /// <param name="user">The granter principal (used for UserId / RoleName).</param>
    /// <param name="entityKey">The entity key from the granted exemption.</param>
    /// <param name="columnKey">The column key from the granted exemption.</param>
    /// <param name="targetRoleName">The role being granted the exemption.</param>
    /// <param name="granterUserId">The granter's user id (matches MaskExemption.GrantedBy).</param>
    /// <param name="expiresAt">The exemption expiry, when the granter set one.</param>
    /// <param name="details">Optional free-form supplementary text (e.g., "grant" / "renew").</param>
    /// <param name="ct">Cancellation token.</param>
    Task AuditExemptionGrantedAsync(
        ClaimsPrincipal user,
        string entityKey,
        string columnKey,
        string targetRoleName,
        int? granterUserId,
        DateTimeOffset? expiresAt,
        string? details = null,
        CancellationToken ct = default);

    /// <summary>
    /// Records that an existing mask exemption was found expired at restriction-resolution
    /// time. Callers (the binder) are responsible for ensuring this fires at most once
    /// per exemption per scope.
    /// </summary>
    /// <param name="user">The principal whose resolution surfaced the expired exemption.</param>
    /// <param name="entityKey">The entity key of the expired exemption.</param>
    /// <param name="columnKey">The column key of the expired exemption.</param>
    /// <param name="targetRoleName">The role the (now-expired) exemption was granted to.</param>
    /// <param name="expiresAt">The original expiry value that has elapsed.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AuditExemptionExpiredAsync(
        ClaimsPrincipal user,
        string entityKey,
        string columnKey,
        string targetRoleName,
        DateTimeOffset expiresAt,
        CancellationToken ct = default);

    /// <summary>
    /// Records that a user with an active exemption read the unmasked value of
    /// a sensitive field. Called by the restriction binder when it renders an
    /// unmasked value.
    /// </summary>
    /// <param name="user">The reading principal.</param>
    /// <param name="entityKey">The entity key of the read field.</param>
    /// <param name="entityId">The entity instance identifier.</param>
    /// <param name="columnKey">The column key of the read field.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AuditFieldUnmaskedReadAsync(
        ClaimsPrincipal user,
        string entityKey,
        string entityId,
        string columnKey,
        CancellationToken ct = default);

    /// <summary>
    /// Persists a fully-formed audit row. The other methods on this interface
    /// build their rows by populating the structured fields and calling
    /// through here, so callers with non-standard payloads (e.g., test fixtures,
    /// future event kinds) can write directly.
    /// </summary>
    /// <param name="row">The audit row to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AuditAsync(AppSensitiveAccessLog row, CancellationToken ct = default);
}
