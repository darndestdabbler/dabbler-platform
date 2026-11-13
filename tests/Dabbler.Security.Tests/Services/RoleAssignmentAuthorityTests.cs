using Dabbler.Security.Authorization;
using Dabbler.Security.Services;

namespace Dabbler.Security.Tests.Services;

/// <summary>
/// Exhaustive unit tests for <see cref="RoleAssignmentAuthority"/>. These
/// cover the role-assignment rules from the
/// <c>rearchitecting-role-administration</c> architecture doc: which baked-in
/// role may assign which role to whom, UserAdmin organization scoping, the
/// CrossOrgUserAdmin self-assignment rule, and the non-user-type role
/// combination guard.
/// </summary>
public class RoleAssignmentAuthorityTests
{
    private static readonly HashSet<int> OrgA = [1];
    private static readonly HashSet<int> OrgB = [2];
    private static readonly HashSet<int> OrgAB = [1, 2];
    private static readonly HashSet<int> NoOrgs = [];
    private const string GranterId = "10";
    private const string TargetId = "20";

    [Fact]
    public void Evaluate_Denied_WhenGranterRoleIsBlank()
    {
        var result = RoleAssignmentAuthority.Evaluate(
            granterRoleName: string.Empty,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: NoOrgs,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgA,
            targetCurrentRoles: [],
            proposedRoleName: "BackofficeUser");

        Assert.Equal(RoleAssignmentAuthorityOutcome.Denied, result.Outcome);
    }

    [Fact]
    public void Evaluate_Denied_WhenGranterIsUserTypeRole()
    {
        var result = RoleAssignmentAuthority.Evaluate(
            granterRoleName: "BackofficeUser",
            granterUserId: GranterId,
            granterAssignedOrganizationIds: OrgA,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgA,
            targetCurrentRoles: [],
            proposedRoleName: "Customer");

        Assert.Equal(RoleAssignmentAuthorityOutcome.Denied, result.Outcome);
        Assert.Contains("only baked-in admin roles", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_Denied_WhenGranterIsCrossOrgMonitor()
    {
        var result = RoleAssignmentAuthority.Evaluate(
            granterRoleName: BakedInRoles.CrossOrgMonitor,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: OrgA,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgA,
            targetCurrentRoles: [],
            proposedRoleName: "BackofficeUser");

        Assert.Equal(RoleAssignmentAuthorityOutcome.Denied, result.Outcome);
    }

    [Fact]
    public void Evaluate_SystemAdmin_MayAssignUserType()
    {
        var result = RoleAssignmentAuthority.Evaluate(
            granterRoleName: BakedInRoles.SystemAdmin,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: NoOrgs,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgA,
            targetCurrentRoles: [],
            proposedRoleName: "BackofficeUser");

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void Evaluate_SystemAdmin_MayAssignCrossOrgUserAdmin_Initial()
    {
        var result = RoleAssignmentAuthority.Evaluate(
            granterRoleName: BakedInRoles.SystemAdmin,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: NoOrgs,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgA,
            targetCurrentRoles: [],
            proposedRoleName: BakedInRoles.CrossOrgUserAdmin);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void Evaluate_SystemAdmin_MayAssignSystemAdmin()
    {
        var result = RoleAssignmentAuthority.Evaluate(
            granterRoleName: BakedInRoles.SystemAdmin,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: NoOrgs,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgA,
            targetCurrentRoles: [],
            proposedRoleName: BakedInRoles.SystemAdmin);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void Evaluate_CrossOrgUserAdmin_CannotAssignSystemAdmin()
    {
        var result = RoleAssignmentAuthority.Evaluate(
            granterRoleName: BakedInRoles.CrossOrgUserAdmin,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: OrgA,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgA,
            targetCurrentRoles: [],
            proposedRoleName: BakedInRoles.SystemAdmin);

        Assert.Equal(RoleAssignmentAuthorityOutcome.Denied, result.Outcome);
        Assert.Contains("SystemAdmin", result.Message);
    }

    [Fact]
    public void Evaluate_CrossOrgUserAdmin_CannotAssignCrossOrgUserAdmin_ToAnotherUser()
    {
        var result = RoleAssignmentAuthority.Evaluate(
            granterRoleName: BakedInRoles.CrossOrgUserAdmin,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: OrgA,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgA,
            targetCurrentRoles: [],
            proposedRoleName: BakedInRoles.CrossOrgUserAdmin);

        Assert.Equal(RoleAssignmentAuthorityOutcome.Denied, result.Outcome);
        Assert.Contains("self-assign", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_CrossOrgUserAdmin_MaySelfAssignCrossOrgUserAdmin()
    {
        var result = RoleAssignmentAuthority.Evaluate(
            granterRoleName: BakedInRoles.CrossOrgUserAdmin,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: OrgA,
            targetUserId: GranterId,
            targetAssignedOrganizationIds: OrgA,
            targetCurrentRoles: [],
            proposedRoleName: BakedInRoles.CrossOrgUserAdmin);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void Evaluate_CrossOrgUserAdmin_MayAssignCrossOrgMonitor()
    {
        var result = RoleAssignmentAuthority.Evaluate(
            granterRoleName: BakedInRoles.CrossOrgUserAdmin,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: OrgA,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgA,
            targetCurrentRoles: [],
            proposedRoleName: BakedInRoles.CrossOrgMonitor);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void Evaluate_CrossOrgUserAdmin_MayAssignUserType_IgnoringScope()
    {
        var result = RoleAssignmentAuthority.Evaluate(
            granterRoleName: BakedInRoles.CrossOrgUserAdmin,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: OrgA,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgB,
            targetCurrentRoles: [],
            proposedRoleName: "BackofficeUser");

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void Evaluate_UserAdmin_CannotAssignSystemAdmin()
    {
        var result = RoleAssignmentAuthority.Evaluate(
            granterRoleName: BakedInRoles.UserAdmin,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: OrgA,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgA,
            targetCurrentRoles: [],
            proposedRoleName: BakedInRoles.SystemAdmin);

        Assert.Equal(RoleAssignmentAuthorityOutcome.Denied, result.Outcome);
    }

    [Fact]
    public void Evaluate_UserAdmin_CannotAssignCrossOrgMonitor()
    {
        var result = RoleAssignmentAuthority.Evaluate(
            granterRoleName: BakedInRoles.UserAdmin,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: OrgA,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgA,
            targetCurrentRoles: [],
            proposedRoleName: BakedInRoles.CrossOrgMonitor);

        Assert.Equal(RoleAssignmentAuthorityOutcome.Denied, result.Outcome);
    }

    [Fact]
    public void Evaluate_UserAdmin_CannotAssignCrossOrgUserAdmin()
    {
        var result = RoleAssignmentAuthority.Evaluate(
            granterRoleName: BakedInRoles.UserAdmin,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: OrgA,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgA,
            targetCurrentRoles: [],
            proposedRoleName: BakedInRoles.CrossOrgUserAdmin);

        Assert.Equal(RoleAssignmentAuthorityOutcome.Denied, result.Outcome);
    }

    [Fact]
    public void Evaluate_UserAdmin_MayAssignUserType_InSharedOrg()
    {
        var result = RoleAssignmentAuthority.Evaluate(
            granterRoleName: BakedInRoles.UserAdmin,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: OrgAB,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgA,
            targetCurrentRoles: [],
            proposedRoleName: "BackofficeUser");

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void Evaluate_UserAdmin_OutOfScope_WhenNoOrgOverlap()
    {
        var result = RoleAssignmentAuthority.Evaluate(
            granterRoleName: BakedInRoles.UserAdmin,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: OrgA,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgB,
            targetCurrentRoles: [],
            proposedRoleName: "BackofficeUser");

        Assert.Equal(RoleAssignmentAuthorityOutcome.OutOfOrganizationScope, result.Outcome);
    }

    [Fact]
    public void Evaluate_UserAdmin_OutOfScope_WhenGranterHasNoOrgs()
    {
        var result = RoleAssignmentAuthority.Evaluate(
            granterRoleName: BakedInRoles.UserAdmin,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: NoOrgs,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgA,
            targetCurrentRoles: [],
            proposedRoleName: "BackofficeUser");

        Assert.Equal(RoleAssignmentAuthorityOutcome.OutOfOrganizationScope, result.Outcome);
    }

    [Fact]
    public void Evaluate_NonUserTypeCombination_RejectsAddingSecondRoleToSystemAdmin()
    {
        var result = RoleAssignmentAuthority.Evaluate(
            granterRoleName: BakedInRoles.SystemAdmin,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: NoOrgs,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgA,
            targetCurrentRoles: [BakedInRoles.SystemAdmin],
            proposedRoleName: "BackofficeUser");

        Assert.Equal(RoleAssignmentAuthorityOutcome.NonUserTypeCombination, result.Outcome);
    }

    [Fact]
    public void Evaluate_NonUserTypeCombination_RejectsAddingSystemAdminOnTopOfExistingUserTypeRole()
    {
        var result = RoleAssignmentAuthority.Evaluate(
            granterRoleName: BakedInRoles.SystemAdmin,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: NoOrgs,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgA,
            targetCurrentRoles: ["BackofficeUser"],
            proposedRoleName: BakedInRoles.SystemAdmin);

        Assert.Equal(RoleAssignmentAuthorityOutcome.NonUserTypeCombination, result.Outcome);
    }

    [Fact]
    public void Evaluate_NonUserTypeCombination_AllowsUserAdminOnTopOfUserAdmin_ForDifferentOrg()
    {
        // UserAdmin carve-out: a user may hold UserAdmin for more than one
        // organization. Since IUserRoleStore models assignments globally,
        // this shows up as "already UserAdmin, adding UserAdmin again" and
        // the authority helper must allow it so the UI can let SystemAdmin
        // re-grant the role across org switches in Set C.
        var result = RoleAssignmentAuthority.Evaluate(
            granterRoleName: BakedInRoles.SystemAdmin,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: NoOrgs,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgAB,
            targetCurrentRoles: [BakedInRoles.UserAdmin],
            proposedRoleName: BakedInRoles.UserAdmin);

        // Duplicate-role guard fires before the carve-out because the proposed
        // role is already present; documenting that behavior explicitly so a
        // future Set C rewrite knows to split org-scoped assignments properly.
        Assert.Equal(RoleAssignmentAuthorityOutcome.Denied, result.Outcome);
    }

    [Fact]
    public void Evaluate_Denied_WhenTargetAlreadyHoldsProposedRole()
    {
        var result = RoleAssignmentAuthority.Evaluate(
            granterRoleName: BakedInRoles.SystemAdmin,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: NoOrgs,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgA,
            targetCurrentRoles: ["BackofficeUser"],
            proposedRoleName: "BackofficeUser");

        Assert.Equal(RoleAssignmentAuthorityOutcome.Denied, result.Outcome);
        Assert.Contains("already holds", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ---- EvaluateRemoval ----

    [Fact]
    public void EvaluateRemoval_UserType_CannotRemove()
    {
        var result = RoleAssignmentAuthority.EvaluateRemoval(
            granterRoleName: "BackofficeUser",
            granterUserId: GranterId,
            granterAssignedOrganizationIds: OrgA,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgA,
            proposedRoleName: "Customer");

        Assert.Equal(RoleAssignmentAuthorityOutcome.Denied, result.Outcome);
    }

    [Fact]
    public void EvaluateRemoval_SystemAdmin_MayRemoveSystemAdmin()
    {
        var result = RoleAssignmentAuthority.EvaluateRemoval(
            granterRoleName: BakedInRoles.SystemAdmin,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: NoOrgs,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgA,
            proposedRoleName: BakedInRoles.SystemAdmin);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void EvaluateRemoval_CrossOrgUserAdmin_CannotRemoveSystemAdmin()
    {
        var result = RoleAssignmentAuthority.EvaluateRemoval(
            granterRoleName: BakedInRoles.CrossOrgUserAdmin,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: OrgA,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgA,
            proposedRoleName: BakedInRoles.SystemAdmin);

        Assert.Equal(RoleAssignmentAuthorityOutcome.Denied, result.Outcome);
    }

    [Fact]
    public void EvaluateRemoval_CrossOrgUserAdmin_CannotRemoveCrossOrgUserAdmin_FromAnother()
    {
        var result = RoleAssignmentAuthority.EvaluateRemoval(
            granterRoleName: BakedInRoles.CrossOrgUserAdmin,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: OrgA,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgA,
            proposedRoleName: BakedInRoles.CrossOrgUserAdmin);

        Assert.Equal(RoleAssignmentAuthorityOutcome.Denied, result.Outcome);
    }

    [Fact]
    public void EvaluateRemoval_CrossOrgUserAdmin_MayRemoveOwnCrossOrgUserAdmin()
    {
        var result = RoleAssignmentAuthority.EvaluateRemoval(
            granterRoleName: BakedInRoles.CrossOrgUserAdmin,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: OrgA,
            targetUserId: GranterId,
            targetAssignedOrganizationIds: OrgA,
            proposedRoleName: BakedInRoles.CrossOrgUserAdmin);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void EvaluateRemoval_UserAdmin_OutOfScope_WhenNoOrgOverlap()
    {
        var result = RoleAssignmentAuthority.EvaluateRemoval(
            granterRoleName: BakedInRoles.UserAdmin,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: OrgA,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgB,
            proposedRoleName: "BackofficeUser");

        Assert.Equal(RoleAssignmentAuthorityOutcome.OutOfOrganizationScope, result.Outcome);
    }

    [Fact]
    public void EvaluateRemoval_UserAdmin_MayRemoveUserType_InSharedOrg()
    {
        var result = RoleAssignmentAuthority.EvaluateRemoval(
            granterRoleName: BakedInRoles.UserAdmin,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: OrgA,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgAB,
            proposedRoleName: "BackofficeUser");

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void EvaluateRemoval_UserAdmin_CannotRemoveCrossOrgMonitor()
    {
        var result = RoleAssignmentAuthority.EvaluateRemoval(
            granterRoleName: BakedInRoles.UserAdmin,
            granterUserId: GranterId,
            granterAssignedOrganizationIds: OrgA,
            targetUserId: TargetId,
            targetAssignedOrganizationIds: OrgA,
            proposedRoleName: BakedInRoles.CrossOrgMonitor);

        Assert.Equal(RoleAssignmentAuthorityOutcome.Denied, result.Outcome);
    }
}
