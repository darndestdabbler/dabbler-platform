namespace Dabbler.Security.Models;

/// <summary>
/// The discriminator for an <see cref="AppSensitiveAccessLog"/> row. Each value
/// names one of the four kinds of sensitive-field interaction the platform
/// audits.
/// </summary>
public enum SensitiveAccessEventKind
{
    /// <summary>
    /// A user changed their effective role through <c>IRoleSwitcher</c>.
    /// Reverting to the user's natural role (<c>ClearSwitch</c>) also
    /// counts as a switch event.
    /// </summary>
    RoleSwitch = 1,

    /// <summary>
    /// A user with an active mask exemption read the unmasked value of a
    /// sensitive field. Emitted at most once per
    /// (<c>CorrelationId</c>, <c>EntityKey</c>, <c>EntityId</c>,
    /// <c>ColumnKey</c>) tuple per render so a single user-facing action
    /// produces a bounded number of rows.
    /// </summary>
    FieldUnmaskedRead = 2,

    /// <summary>
    /// An authorized granter created a new <c>MaskExemption</c> through the
    /// Role Master Details — Mask Exemptions sub-tab.
    /// </summary>
    ExemptionGranted = 3,

    /// <summary>
    /// A previously granted exemption was found expired at restriction-resolution
    /// time. Emitted once per exemption per circuit / request scope so a
    /// long-lived render does not flood the audit table.
    /// </summary>
    ExemptionExpired = 4,
}

/// <summary>
/// Append-only audit row capturing one sensitive-field interaction. Backed by a
/// SQL Server temporal table so the durable history is preserved even if the
/// active row is corrected (the application never updates or deletes rows; the
/// temporal infrastructure exists for out-of-band repairs).
/// </summary>
/// <remarks>
/// Many columns are nullable because the four event kinds carry different
/// payloads. A <c>RoleSwitch</c> has no <c>EntityKey</c> / <c>ColumnKey</c>; a
/// <c>FieldUnmaskedRead</c> has no <c>TargetRoleName</c> / <c>ExpiresAt</c>. The
/// <c>EventKind</c> column is the discriminator that tells a reviewer which
/// columns to read for a given row.
/// </remarks>
public sealed class AppSensitiveAccessLog
{
    /// <summary>The surrogate primary key.</summary>
    public long Id { get; set; }

    /// <summary>The kind of sensitive-field interaction this row records.</summary>
    public SensitiveAccessEventKind EventKind { get; set; }

    /// <summary>The logical event time, in UTC.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Shared across every row emitted as part of the same Blazor circuit
    /// invocation or HTTP request. Lets a reviewer reconstruct a single
    /// user-facing action (e.g., a role switch followed immediately by a
    /// sensitive read) from the audit log without guessing from timestamps.
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>The user whose active role triggered the event.</summary>
    public int UserId { get; set; }

    /// <summary>
    /// The canonical role name (not <c>DisplayAlias</c>) the user was operating
    /// under at event time. For <c>RoleSwitch</c> events this is the role the
    /// user switched to (on switch) or the role they switched away from (on
    /// revert) — the <c>Details</c> field carries the switch / revert
    /// discriminator.
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>The active organization at event time, when the event is org-scoped.</summary>
    public int? OrganizationId { get; set; }

    /// <summary>
    /// The entity (table) key, e.g., <c>"Customer"</c>. Null for
    /// <c>RoleSwitch</c> events; populated for the other three kinds.
    /// </summary>
    public string? EntityKey { get; set; }

    /// <summary>
    /// The entity instance identifier, stringified to avoid per-entity FKs.
    /// Populated for <c>FieldUnmaskedRead</c>; usually null for the other
    /// kinds.
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// The column (field) key, e.g., <c>"SocialSecurityNumber"</c>. Null for
    /// <c>RoleSwitch</c>; populated for the other three kinds.
    /// </summary>
    public string? ColumnKey { get; set; }

    /// <summary>
    /// The role being granted or expired, populated for <c>ExemptionGranted</c>
    /// and <c>ExemptionExpired</c>.
    /// </summary>
    public string? TargetRoleName { get; set; }

    /// <summary>
    /// The user who granted the exemption (matches <c>MaskExemption.GrantedBy</c>),
    /// populated for <c>ExemptionGranted</c>.
    /// </summary>
    public int? GranterUserId { get; set; }

    /// <summary>
    /// The exemption's expiry, populated for <c>ExemptionGranted</c> (when the
    /// granter set one) and <c>ExemptionExpired</c> (always — that's the value
    /// that elapsed).
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Free-form supplementary detail, capped at 1024 characters. Used sparingly
    /// — most signal lives in the structured columns above.
    /// </summary>
    public string? Details { get; set; }

    // The temporal period columns ValidFrom / ValidTo are configured as
    // shadow properties via EF (see ModelBuilderExtensions.ConfigureAppSensitiveAccessLog)
    // — SQL Server manages their values; the application never reads them
    // through the entity surface.
}
