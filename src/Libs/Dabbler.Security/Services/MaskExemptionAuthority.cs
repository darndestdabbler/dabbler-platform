using Dabbler.Security.Authorization;
using Dabbler.Security.Configuration;

namespace Dabbler.Security.Services;

/// <summary>
/// Pure-function evaluator for the separation-of-duties authority rules on
/// <see cref="MaskExemption"/> rows, shared between the Role Master Details
/// Mask Exemptions sub-tab and the (Set D) exemption-granting service.
/// </summary>
/// <remarks>
/// <para>
/// The rules encoded here come from the <see cref="MaskExemption"/> xml-doc and
/// the <c>rearchitecting-role-administration.md</c> architecture doc:
/// </para>
/// <list type="bullet">
///   <item>Exemptions targeting user-type roles or <see cref="BakedInRoles.UserAdmin"/>:
///     may be granted by <see cref="BakedInRoles.SystemAdmin"/> or
///     <see cref="BakedInRoles.CrossOrgUserAdmin"/>, with or without an expiry.</item>
///   <item>Exemptions targeting <see cref="BakedInRoles.SystemAdmin"/> or
///     <see cref="BakedInRoles.CrossOrgMonitor"/>: granted only by
///     <see cref="BakedInRoles.CrossOrgUserAdmin"/> and <b>must</b> have a
///     non-null <see cref="MaskExemption.ExpiresAt"/>.</item>
///   <item>Exemptions targeting <see cref="BakedInRoles.CrossOrgUserAdmin"/>:
///     self-granted only (granter user identity matches the exemption target) and
///     <b>must</b> have a non-null <see cref="MaskExemption.ExpiresAt"/>.</item>
/// </list>
/// <para>
/// No state is held; all inputs are passed per call. The result distinguishes
/// between "denied because of granter role", "expiry required but missing", and
/// "allowed" so the UI can render precise feedback.
/// </para>
/// </remarks>
public static class MaskExemptionAuthority
{
    /// <summary>
    /// Evaluates whether the given granter may grant an exemption for the given
    /// target role + optional expiry, returning a structured verdict.
    /// </summary>
    /// <param name="granterRoleName">
    /// Canonical name of the granter's active role. Must be a baked-in role
    /// (<see cref="BakedInRoles.SystemAdmin"/>, <see cref="BakedInRoles.CrossOrgUserAdmin"/>, …).
    /// User-type roles can never grant exemptions.
    /// </param>
    /// <param name="granterUserId">
    /// Stable user identifier for the granter, used to evaluate the
    /// self-grant rule for <see cref="BakedInRoles.CrossOrgUserAdmin"/>.
    /// Compared case-insensitively to <paramref name="targetUserId"/>.
    /// </param>
    /// <param name="targetRoleName">
    /// Canonical name of the role being exempted. May be a user-type role name
    /// or any baked-in role name.
    /// </param>
    /// <param name="targetUserId">
    /// Optional stable user identifier of the specific user the exemption
    /// targets. Currently only consulted when the exemption targets
    /// <see cref="BakedInRoles.CrossOrgUserAdmin"/> (self-grant rule). Null
    /// when the exemption is role-wide rather than scoped to a single user.
    /// </param>
    /// <param name="expiresAt">
    /// The exemption's optional <see cref="MaskExemption.ExpiresAt"/>.
    /// Null = permanent.
    /// </param>
    public static MaskExemptionAuthorityResult Evaluate(
        string granterRoleName,
        string granterUserId,
        string targetRoleName,
        string? targetUserId,
        DateTimeOffset? expiresAt)
    {
        if (string.IsNullOrWhiteSpace(granterRoleName))
            return MaskExemptionAuthorityResult.Denied("Granter role is required.");

        if (string.IsNullOrWhiteSpace(targetRoleName))
            return MaskExemptionAuthorityResult.Denied("Target role is required.");

        var grantsFromBakedIn = BakedInRoles.IsBakedIn(granterRoleName);
        if (!grantsFromBakedIn)
        {
            return MaskExemptionAuthorityResult.Denied(
                "Only baked-in roles may grant mask exemptions.");
        }

        if (IsUserTypeOrUserAdminTarget(targetRoleName))
        {
            var granterIsSysAdmin = IsSameRole(granterRoleName, BakedInRoles.SystemAdmin);
            var granterIsCrossOrgUserAdmin = IsSameRole(granterRoleName, BakedInRoles.CrossOrgUserAdmin);

            if (!granterIsSysAdmin && !granterIsCrossOrgUserAdmin)
            {
                return MaskExemptionAuthorityResult.Denied(
                    "Exemptions targeting user-type roles or UserAdmin may only be granted by SystemAdmin or CrossOrgUserAdmin.");
            }

            // No expiry required for user-type / UserAdmin targets.
            return MaskExemptionAuthorityResult.Allowed();
        }

        if (IsSameRole(targetRoleName, BakedInRoles.SystemAdmin)
            || IsSameRole(targetRoleName, BakedInRoles.CrossOrgMonitor))
        {
            if (!IsSameRole(granterRoleName, BakedInRoles.CrossOrgUserAdmin))
            {
                return MaskExemptionAuthorityResult.Denied(
                    "Exemptions targeting SystemAdmin or CrossOrgMonitor may only be granted by CrossOrgUserAdmin.");
            }

            if (expiresAt is null)
            {
                return MaskExemptionAuthorityResult.ExpiryRequired(
                    "Exemptions targeting SystemAdmin or CrossOrgMonitor must specify an expiration.");
            }

            return MaskExemptionAuthorityResult.Allowed();
        }

        if (IsSameRole(targetRoleName, BakedInRoles.CrossOrgUserAdmin))
        {
            if (!IsSameRole(granterRoleName, BakedInRoles.CrossOrgUserAdmin))
            {
                return MaskExemptionAuthorityResult.Denied(
                    "Exemptions targeting CrossOrgUserAdmin are self-granted only.");
            }

            if (targetUserId is null
                || !string.Equals(granterUserId, targetUserId, StringComparison.OrdinalIgnoreCase))
            {
                return MaskExemptionAuthorityResult.Denied(
                    "CrossOrgUserAdmin may self-grant an exemption but may not grant one to another user.");
            }

            if (expiresAt is null)
            {
                return MaskExemptionAuthorityResult.ExpiryRequired(
                    "Self-granted CrossOrgUserAdmin exemptions must specify an expiration.");
            }

            return MaskExemptionAuthorityResult.Allowed();
        }

        // Any other baked-in role (e.g., a host-registered non-user-type role)
        // is treated the same as SystemAdmin/CrossOrgMonitor targeting above:
        // only CrossOrgUserAdmin may grant, and expiry is required. This keeps
        // the default closed for future-added baked-in roles.
        if (!IsSameRole(granterRoleName, BakedInRoles.CrossOrgUserAdmin))
        {
            return MaskExemptionAuthorityResult.Denied(
                "Exemptions targeting non-user-type baked-in roles may only be granted by CrossOrgUserAdmin.");
        }
        if (expiresAt is null)
        {
            return MaskExemptionAuthorityResult.ExpiryRequired(
                "Exemptions targeting non-user-type baked-in roles must specify an expiration.");
        }
        return MaskExemptionAuthorityResult.Allowed();
    }

    private static bool IsUserTypeOrUserAdminTarget(string targetRoleName)
    {
        if (IsSameRole(targetRoleName, BakedInRoles.UserAdmin))
            return true;

        // User-type roles are anything that is not one of the four non-user-type
        // baked-in roles (SystemAdmin / CrossOrgUserAdmin / CrossOrgMonitor /
        // UserAdmin). The baked-in BakedInRoles.User is itself user-type.
        if (!BakedInRoles.IsBakedIn(targetRoleName))
            return true;

        return IsSameRole(targetRoleName, BakedInRoles.User);
    }

    private static bool IsSameRole(string? left, string? right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Outcome of <see cref="MaskExemptionAuthority.Evaluate"/>.
/// </summary>
public enum MaskExemptionAuthorityOutcome
{
    /// <summary>The granter may create or keep the exemption as specified.</summary>
    Allowed = 0,

    /// <summary>
    /// The granter's role or identity is not permitted to grant this exemption
    /// at all, regardless of whether an expiry is supplied.
    /// </summary>
    Denied = 1,

    /// <summary>
    /// The granter's role would be permitted, but an expiry is required for
    /// this target-role class and the supplied <c>ExpiresAt</c> is null.
    /// </summary>
    ExpiryRequired = 2,
}

/// <summary>
/// Structured result of a <see cref="MaskExemptionAuthority.Evaluate"/> call.
/// </summary>
/// <param name="Outcome">The outcome of the evaluation.</param>
/// <param name="Message">
/// Human-readable explanation. Empty string when <paramref name="Outcome"/> is
/// <see cref="MaskExemptionAuthorityOutcome.Allowed"/>; otherwise suitable for
/// a snackbar or inline validation message.
/// </param>
public readonly record struct MaskExemptionAuthorityResult(
    MaskExemptionAuthorityOutcome Outcome,
    string Message)
{
    /// <summary>Gets a value indicating whether the exemption is permitted.</summary>
    public bool IsAllowed => Outcome == MaskExemptionAuthorityOutcome.Allowed;

    /// <summary>The shared Allowed singleton (no message).</summary>
    public static MaskExemptionAuthorityResult Allowed() =>
        new(MaskExemptionAuthorityOutcome.Allowed, string.Empty);

    /// <summary>Produces a Denied result with the given message.</summary>
    public static MaskExemptionAuthorityResult Denied(string message) =>
        new(MaskExemptionAuthorityOutcome.Denied, message);

    /// <summary>Produces an ExpiryRequired result with the given message.</summary>
    public static MaskExemptionAuthorityResult ExpiryRequired(string message) =>
        new(MaskExemptionAuthorityOutcome.ExpiryRequired, message);
}
