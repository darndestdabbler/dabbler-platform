using Dabbler.Security.Authorization;

namespace Dabbler.Security.Services;

/// <summary>
/// Pure-function evaluator for the separation-of-duties rules that govern
/// who may assign which role to whom, shared between the Users and Role
/// Assignment tab and any server-side assignment service.
/// </summary>
/// <remarks>
/// <para>
/// The rules encoded here come from the <c>rearchitecting-role-administration.md</c>
/// architecture doc and the compiled <see cref="BakedInRolePrivileges"/> surface:
/// </para>
/// <list type="bullet">
///   <item><b>SystemAdmin</b> may assign any role, including the initial
///     CrossOrgUserAdmin grant.</item>
///   <item><b>CrossOrgUserAdmin</b> may assign any user-type role, UserAdmin,
///     or CrossOrgMonitor. It may self-assign CrossOrgUserAdmin for vacation
///     or illness coverage, but never SystemAdmin and never CrossOrgUserAdmin
///     to another user.</item>
///   <item><b>UserAdmin</b> may assign any role to any user in one of
///     UserAdmin's own assigned organizations. Cannot assign SystemAdmin,
///     CrossOrgUserAdmin, CrossOrgMonitor, or any other cross-org-capable
///     role, because UserAdmin is by definition organization-scoped.</item>
///   <item><b>CrossOrgMonitor</b> and user-type roles may never assign any
///     role.</item>
///   <item>A non-user-type target (SystemAdmin / CrossOrgUserAdmin /
///     CrossOrgMonitor / UserAdmin) may not accumulate additional roles,
///     except that a UserAdmin may hold UserAdmin for a different
///     organization (tracked by <paramref name="targetCurrentRoles"/>).</item>
/// </list>
/// <para>
/// No state is held; all inputs are passed explicitly. The result
/// distinguishes between "granter lacks privilege", "target is out of
/// UserAdmin's organization scope", "non-user-type role combination
/// rejected", and "allowed" so the UI can render precise feedback.
/// </para>
/// </remarks>
public static class RoleAssignmentAuthority
{
    /// <summary>
    /// Evaluates whether <paramref name="granterRoleName"/> may assign
    /// <paramref name="proposedRoleName"/> to the target user.
    /// </summary>
    /// <param name="granterRoleName">
    /// Canonical name of the granter's active baked-in role.
    /// </param>
    /// <param name="granterUserId">
    /// Stable user identifier for the granter, used to evaluate the
    /// CrossOrgUserAdmin self-grant rule. Compared case-insensitively to
    /// <paramref name="targetUserId"/>.
    /// </param>
    /// <param name="granterAssignedOrganizationIds">
    /// Organization identifiers the granter is a member of. Only consulted
    /// when the granter is <see cref="BakedInRoles.UserAdmin"/>; SystemAdmin
    /// and CrossOrgUserAdmin bypass organization scope.
    /// </param>
    /// <param name="targetUserId">
    /// Stable user identifier of the target user.
    /// </param>
    /// <param name="targetAssignedOrganizationIds">
    /// Organization identifiers the target user is a member of. For a
    /// UserAdmin granter at least one must overlap
    /// <paramref name="granterAssignedOrganizationIds"/>.
    /// </param>
    /// <param name="targetCurrentRoles">
    /// Canonical names of the target user's existing role assignments.
    /// Used to enforce the non-user-type-plus-other-role rule.
    /// </param>
    /// <param name="proposedRoleName">
    /// Canonical name of the role being assigned. May be a user-type role
    /// defined in <c>SecurityConfiguration.UserTypeRoles</c> or a baked-in
    /// role name.
    /// </param>
    public static RoleAssignmentAuthorityResult Evaluate(
        string granterRoleName,
        string granterUserId,
        IReadOnlyCollection<int> granterAssignedOrganizationIds,
        string targetUserId,
        IReadOnlyCollection<int> targetAssignedOrganizationIds,
        IReadOnlyCollection<string> targetCurrentRoles,
        string proposedRoleName)
    {
        if (string.IsNullOrWhiteSpace(granterRoleName))
            return RoleAssignmentAuthorityResult.Denied("Granter role is required.");

        if (string.IsNullOrWhiteSpace(targetUserId))
            return RoleAssignmentAuthorityResult.Denied("Target user is required.");

        if (string.IsNullOrWhiteSpace(proposedRoleName))
            return RoleAssignmentAuthorityResult.Denied("Proposed role is required.");

        if (!BakedInRoles.IsBakedIn(granterRoleName))
        {
            return RoleAssignmentAuthorityResult.Denied(
                "Only baked-in admin roles may assign roles to users.");
        }

        var granterIsSystemAdmin = IsSameRole(granterRoleName, BakedInRoles.SystemAdmin);
        var granterIsCrossOrgUserAdmin = IsSameRole(granterRoleName, BakedInRoles.CrossOrgUserAdmin);
        var granterIsUserAdmin = IsSameRole(granterRoleName, BakedInRoles.UserAdmin);

        if (!granterIsSystemAdmin && !granterIsCrossOrgUserAdmin && !granterIsUserAdmin)
        {
            return RoleAssignmentAuthorityResult.Denied(
                $"The {granterRoleName} role cannot assign roles to users.");
        }

        var proposedIsSystemAdmin = IsSameRole(proposedRoleName, BakedInRoles.SystemAdmin);
        var proposedIsCrossOrgUserAdmin = IsSameRole(proposedRoleName, BakedInRoles.CrossOrgUserAdmin);
        var proposedIsCrossOrgMonitor = IsSameRole(proposedRoleName, BakedInRoles.CrossOrgMonitor);
        var proposedIsUserAdmin = IsSameRole(proposedRoleName, BakedInRoles.UserAdmin);

        if (proposedIsSystemAdmin && !granterIsSystemAdmin)
        {
            return RoleAssignmentAuthorityResult.Denied(
                "Only SystemAdmin may assign the SystemAdmin role.");
        }

        if (proposedIsCrossOrgUserAdmin)
        {
            if (granterIsSystemAdmin)
            {
                // SystemAdmin may perform the initial CrossOrgUserAdmin assignment
                // (architecture doc #1). Falls through to the non-user-type-combination
                // guard below.
            }
            else if (granterIsCrossOrgUserAdmin)
            {
                // CrossOrgUserAdmin may self-assign CrossOrgUserAdmin for vacation
                // or illness coverage, but not to a different user.
                if (!string.Equals(granterUserId, targetUserId, StringComparison.OrdinalIgnoreCase))
                {
                    return RoleAssignmentAuthorityResult.Denied(
                        "CrossOrgUserAdmin may only self-assign CrossOrgUserAdmin for vacation coverage.");
                }
            }
            else
            {
                return RoleAssignmentAuthorityResult.Denied(
                    "Only SystemAdmin may assign CrossOrgUserAdmin to another user.");
            }
        }

        if (proposedIsCrossOrgMonitor && !(granterIsSystemAdmin || granterIsCrossOrgUserAdmin))
        {
            return RoleAssignmentAuthorityResult.Denied(
                "Only SystemAdmin or CrossOrgUserAdmin may assign CrossOrgMonitor.");
        }

        if (granterIsUserAdmin)
        {
            if (proposedIsSystemAdmin || proposedIsCrossOrgUserAdmin || proposedIsCrossOrgMonitor)
            {
                return RoleAssignmentAuthorityResult.Denied(
                    "UserAdmin cannot assign cross-organization roles.");
            }

            if (granterAssignedOrganizationIds is null || granterAssignedOrganizationIds.Count == 0)
            {
                return RoleAssignmentAuthorityResult.OutOfOrganizationScope(
                    "UserAdmin has no assigned organizations.");
            }

            if (!SharesOrganization(granterAssignedOrganizationIds, targetAssignedOrganizationIds))
            {
                return RoleAssignmentAuthorityResult.OutOfOrganizationScope(
                    "Target user is not a member of an organization the acting UserAdmin administers.");
            }
        }

        if (HasNonUserTypeRole(targetCurrentRoles, out var existingNonUserTypeRole))
        {
            // Non-user-type users may not accumulate additional roles, except that
            // UserAdmin may be held for more than one organization. Session 7's
            // role-assignment contract is global per user (via IUserRoleStore), so
            // the UserAdmin-multi-org carve-out is modelled here as "the proposed
            // role is also UserAdmin and the existing role is UserAdmin": both
            // represent UserAdmin assignments that differ by organization scope.
            var addingUserAdminOnTopOfUserAdmin =
                IsSameRole(existingNonUserTypeRole, BakedInRoles.UserAdmin)
                && proposedIsUserAdmin;

            if (!addingUserAdminOnTopOfUserAdmin)
            {
                return RoleAssignmentAuthorityResult.NonUserTypeCombination(
                    $"Target already holds the {existingNonUserTypeRole} role, which cannot be combined with another role.");
            }
        }
        else if (IsNonUserTypeRole(proposedRoleName)
                 && targetCurrentRoles.Any(role => !IsSameRole(role, proposedRoleName)))
        {
            // Flipped direction: target holds only user-type roles today, and the
            // proposed role is non-user-type. Adding a non-user-type role on top
            // of user-type roles is equally disallowed.
            return RoleAssignmentAuthorityResult.NonUserTypeCombination(
                $"Cannot assign the non-user-type role {proposedRoleName} to a user who already holds other roles.");
        }

        if (targetCurrentRoles.Any(role => IsSameRole(role, proposedRoleName)))
        {
            return RoleAssignmentAuthorityResult.Denied(
                $"Target already holds the {proposedRoleName} role.");
        }

        return RoleAssignmentAuthorityResult.Allowed();
    }

    /// <summary>
    /// Evaluates whether <paramref name="granterRoleName"/> may remove
    /// <paramref name="proposedRoleName"/> from the target user.
    /// </summary>
    /// <remarks>
    /// The rules mirror <see cref="Evaluate"/> for assignment, except the
    /// non-user-type-combination rule does not apply to removals: a user who
    /// holds only SystemAdmin may have SystemAdmin removed. Organization
    /// scope and granter-role privilege checks still apply.
    /// </remarks>
    public static RoleAssignmentAuthorityResult EvaluateRemoval(
        string granterRoleName,
        string granterUserId,
        IReadOnlyCollection<int> granterAssignedOrganizationIds,
        string targetUserId,
        IReadOnlyCollection<int> targetAssignedOrganizationIds,
        string proposedRoleName)
    {
        if (string.IsNullOrWhiteSpace(granterRoleName))
            return RoleAssignmentAuthorityResult.Denied("Granter role is required.");

        if (string.IsNullOrWhiteSpace(targetUserId))
            return RoleAssignmentAuthorityResult.Denied("Target user is required.");

        if (string.IsNullOrWhiteSpace(proposedRoleName))
            return RoleAssignmentAuthorityResult.Denied("Proposed role is required.");

        if (!BakedInRoles.IsBakedIn(granterRoleName))
        {
            return RoleAssignmentAuthorityResult.Denied(
                "Only baked-in admin roles may remove roles from users.");
        }

        var granterIsSystemAdmin = IsSameRole(granterRoleName, BakedInRoles.SystemAdmin);
        var granterIsCrossOrgUserAdmin = IsSameRole(granterRoleName, BakedInRoles.CrossOrgUserAdmin);
        var granterIsUserAdmin = IsSameRole(granterRoleName, BakedInRoles.UserAdmin);

        if (!granterIsSystemAdmin && !granterIsCrossOrgUserAdmin && !granterIsUserAdmin)
        {
            return RoleAssignmentAuthorityResult.Denied(
                $"The {granterRoleName} role cannot remove roles from users.");
        }

        var proposedIsSystemAdmin = IsSameRole(proposedRoleName, BakedInRoles.SystemAdmin);
        var proposedIsCrossOrgUserAdmin = IsSameRole(proposedRoleName, BakedInRoles.CrossOrgUserAdmin);
        var proposedIsCrossOrgMonitor = IsSameRole(proposedRoleName, BakedInRoles.CrossOrgMonitor);

        if (proposedIsSystemAdmin && !granterIsSystemAdmin)
        {
            return RoleAssignmentAuthorityResult.Denied(
                "Only SystemAdmin may remove the SystemAdmin role.");
        }

        if (proposedIsCrossOrgUserAdmin
            && !(granterIsSystemAdmin || SelfRemoval(granterIsCrossOrgUserAdmin, granterUserId, targetUserId)))
        {
            return RoleAssignmentAuthorityResult.Denied(
                "Only SystemAdmin may remove CrossOrgUserAdmin from another user.");
        }

        if (proposedIsCrossOrgMonitor && !(granterIsSystemAdmin || granterIsCrossOrgUserAdmin))
        {
            return RoleAssignmentAuthorityResult.Denied(
                "Only SystemAdmin or CrossOrgUserAdmin may remove CrossOrgMonitor.");
        }

        if (granterIsUserAdmin)
        {
            if (proposedIsSystemAdmin || proposedIsCrossOrgUserAdmin || proposedIsCrossOrgMonitor)
            {
                return RoleAssignmentAuthorityResult.Denied(
                    "UserAdmin cannot remove cross-organization roles.");
            }

            if (granterAssignedOrganizationIds is null || granterAssignedOrganizationIds.Count == 0)
            {
                return RoleAssignmentAuthorityResult.OutOfOrganizationScope(
                    "UserAdmin has no assigned organizations.");
            }

            if (!SharesOrganization(granterAssignedOrganizationIds, targetAssignedOrganizationIds))
            {
                return RoleAssignmentAuthorityResult.OutOfOrganizationScope(
                    "Target user is not a member of an organization the acting UserAdmin administers.");
            }
        }

        return RoleAssignmentAuthorityResult.Allowed();
    }

    private static bool SelfRemoval(bool granterIsCrossOrgUserAdmin, string granterUserId, string targetUserId) =>
        granterIsCrossOrgUserAdmin
        && string.Equals(granterUserId, targetUserId, StringComparison.OrdinalIgnoreCase);

    private static bool SharesOrganization(
        IReadOnlyCollection<int> left,
        IReadOnlyCollection<int>? right)
    {
        if (right is null || right.Count == 0)
            return false;

        var leftSet = left as ISet<int> ?? new HashSet<int>(left);
        foreach (var id in right)
        {
            if (leftSet.Contains(id))
                return true;
        }
        return false;
    }

    private static bool HasNonUserTypeRole(
        IReadOnlyCollection<string> roles,
        out string nonUserTypeRole)
    {
        foreach (var role in roles)
        {
            if (IsNonUserTypeRole(role))
            {
                nonUserTypeRole = role;
                return true;
            }
        }
        nonUserTypeRole = string.Empty;
        return false;
    }

    private static bool IsNonUserTypeRole(string roleName) =>
        BakedInRoles.IsBakedIn(roleName) && !IsSameRole(roleName, BakedInRoles.User);

    private static bool IsSameRole(string? left, string? right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Outcome of <see cref="RoleAssignmentAuthority.Evaluate"/> or
/// <see cref="RoleAssignmentAuthority.EvaluateRemoval"/>.
/// </summary>
public enum RoleAssignmentAuthorityOutcome
{
    /// <summary>The granter may create (or remove) the assignment as specified.</summary>
    Allowed = 0,

    /// <summary>
    /// The granter's role is not permitted to perform this assignment at
    /// all (e.g., UserAdmin trying to assign SystemAdmin, or the target
    /// already holds the proposed role).
    /// </summary>
    Denied = 1,

    /// <summary>
    /// The granter is UserAdmin and the target user is not a member of any
    /// organization the granter administers.
    /// </summary>
    OutOfOrganizationScope = 2,

    /// <summary>
    /// The proposed role would place a non-user-type role on a user who
    /// already holds another role, or assign any role on top of an existing
    /// non-user-type role.
    /// </summary>
    NonUserTypeCombination = 3,
}

/// <summary>
/// Structured result of <see cref="RoleAssignmentAuthority.Evaluate"/>.
/// </summary>
/// <param name="Outcome">The outcome of the evaluation.</param>
/// <param name="Message">
/// Human-readable explanation. Empty string when <paramref name="Outcome"/> is
/// <see cref="RoleAssignmentAuthorityOutcome.Allowed"/>; otherwise suitable for
/// a snackbar, tooltip, or inline validation message.
/// </param>
public readonly record struct RoleAssignmentAuthorityResult(
    RoleAssignmentAuthorityOutcome Outcome,
    string Message)
{
    /// <summary>Gets a value indicating whether the assignment is permitted.</summary>
    public bool IsAllowed => Outcome == RoleAssignmentAuthorityOutcome.Allowed;

    /// <summary>The shared Allowed singleton (no message).</summary>
    public static RoleAssignmentAuthorityResult Allowed() =>
        new(RoleAssignmentAuthorityOutcome.Allowed, string.Empty);

    /// <summary>Produces a generic Denied result.</summary>
    public static RoleAssignmentAuthorityResult Denied(string message) =>
        new(RoleAssignmentAuthorityOutcome.Denied, message);

    /// <summary>Produces an out-of-scope result for UserAdmin rejections.</summary>
    public static RoleAssignmentAuthorityResult OutOfOrganizationScope(string message) =>
        new(RoleAssignmentAuthorityOutcome.OutOfOrganizationScope, message);

    /// <summary>Produces a non-user-type-combination result.</summary>
    public static RoleAssignmentAuthorityResult NonUserTypeCombination(string message) =>
        new(RoleAssignmentAuthorityOutcome.NonUserTypeCombination, message);
}
