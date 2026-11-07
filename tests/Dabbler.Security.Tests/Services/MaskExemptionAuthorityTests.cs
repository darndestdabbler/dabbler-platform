using Dabbler.Security.Authorization;
using Dabbler.Security.Services;

namespace Dabbler.Security.Tests.Services;

/// <summary>
/// Unit tests for <see cref="MaskExemptionAuthority.Evaluate"/> covering the
/// separation-of-duties rules described on <see cref="Dabbler.Security.Configuration.MaskExemption"/>.
/// </summary>
public class MaskExemptionAuthorityTests
{
    private const string SysAdminId = "sysadmin@test.local";
    private const string CrossOrgAdminId = "crossorg@test.local";

    [Theory]
    [InlineData(BakedInRoles.SystemAdmin)]
    [InlineData(BakedInRoles.CrossOrgUserAdmin)]
    public void SystemAdminOrCrossOrgUserAdmin_MayGrant_UserTypeTarget_WithoutExpiry(string granterRole)
    {
        var result = MaskExemptionAuthority.Evaluate(
            granterRoleName: granterRole,
            granterUserId: SysAdminId,
            targetRoleName: "BackofficeUser",
            targetUserId: null,
            expiresAt: null);

        result.IsAllowed.Should().BeTrue();
    }

    [Theory]
    [InlineData(BakedInRoles.SystemAdmin)]
    [InlineData(BakedInRoles.CrossOrgUserAdmin)]
    public void SystemAdminOrCrossOrgUserAdmin_MayGrant_UserAdminTarget_WithoutExpiry(string granterRole)
    {
        var result = MaskExemptionAuthority.Evaluate(
            granterRoleName: granterRole,
            granterUserId: SysAdminId,
            targetRoleName: BakedInRoles.UserAdmin,
            targetUserId: null,
            expiresAt: null);

        result.IsAllowed.Should().BeTrue();
    }

    [Theory]
    [InlineData(BakedInRoles.UserAdmin)]
    [InlineData(BakedInRoles.CrossOrgMonitor)]
    [InlineData(BakedInRoles.User)]
    public void NonPrivilegedGranter_UserTypeTarget_IsDenied(string granterRole)
    {
        var result = MaskExemptionAuthority.Evaluate(
            granterRoleName: granterRole,
            granterUserId: SysAdminId,
            targetRoleName: "BackofficeUser",
            targetUserId: null,
            expiresAt: null);

        result.IsAllowed.Should().BeFalse();
        result.Outcome.Should().Be(MaskExemptionAuthorityOutcome.Denied);
    }

    [Theory]
    [InlineData(BakedInRoles.SystemAdmin)]
    public void SystemAdmin_CannotGrant_SystemAdminTarget(string granterRole)
    {
        var result = MaskExemptionAuthority.Evaluate(
            granterRoleName: granterRole,
            granterUserId: SysAdminId,
            targetRoleName: BakedInRoles.SystemAdmin,
            targetUserId: null,
            expiresAt: DateTimeOffset.UtcNow.AddDays(14));

        result.Outcome.Should().Be(MaskExemptionAuthorityOutcome.Denied);
    }

    [Fact]
    public void CrossOrgUserAdmin_MayGrant_SystemAdminTarget_WithExpiry()
    {
        var result = MaskExemptionAuthority.Evaluate(
            granterRoleName: BakedInRoles.CrossOrgUserAdmin,
            granterUserId: CrossOrgAdminId,
            targetRoleName: BakedInRoles.SystemAdmin,
            targetUserId: null,
            expiresAt: DateTimeOffset.UtcNow.AddDays(14));

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void CrossOrgUserAdmin_CannotGrant_SystemAdminTarget_WithoutExpiry()
    {
        var result = MaskExemptionAuthority.Evaluate(
            granterRoleName: BakedInRoles.CrossOrgUserAdmin,
            granterUserId: CrossOrgAdminId,
            targetRoleName: BakedInRoles.SystemAdmin,
            targetUserId: null,
            expiresAt: null);

        result.Outcome.Should().Be(MaskExemptionAuthorityOutcome.ExpiryRequired);
    }

    [Fact]
    public void CrossOrgUserAdmin_MayGrant_CrossOrgMonitorTarget_WithExpiry()
    {
        var result = MaskExemptionAuthority.Evaluate(
            granterRoleName: BakedInRoles.CrossOrgUserAdmin,
            granterUserId: CrossOrgAdminId,
            targetRoleName: BakedInRoles.CrossOrgMonitor,
            targetUserId: null,
            expiresAt: DateTimeOffset.UtcNow.AddDays(7));

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void CrossOrgUserAdmin_CannotGrant_CrossOrgMonitorTarget_WithoutExpiry()
    {
        var result = MaskExemptionAuthority.Evaluate(
            granterRoleName: BakedInRoles.CrossOrgUserAdmin,
            granterUserId: CrossOrgAdminId,
            targetRoleName: BakedInRoles.CrossOrgMonitor,
            targetUserId: null,
            expiresAt: null);

        result.Outcome.Should().Be(MaskExemptionAuthorityOutcome.ExpiryRequired);
    }

    [Fact]
    public void CrossOrgUserAdmin_MaySelfGrant_OwnRole_WithExpiry()
    {
        var result = MaskExemptionAuthority.Evaluate(
            granterRoleName: BakedInRoles.CrossOrgUserAdmin,
            granterUserId: CrossOrgAdminId,
            targetRoleName: BakedInRoles.CrossOrgUserAdmin,
            targetUserId: CrossOrgAdminId,
            expiresAt: DateTimeOffset.UtcNow.AddDays(14));

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void CrossOrgUserAdmin_CannotSelfGrant_OwnRole_WithoutExpiry()
    {
        var result = MaskExemptionAuthority.Evaluate(
            granterRoleName: BakedInRoles.CrossOrgUserAdmin,
            granterUserId: CrossOrgAdminId,
            targetRoleName: BakedInRoles.CrossOrgUserAdmin,
            targetUserId: CrossOrgAdminId,
            expiresAt: null);

        result.Outcome.Should().Be(MaskExemptionAuthorityOutcome.ExpiryRequired);
    }

    [Fact]
    public void CrossOrgUserAdmin_CannotGrant_CrossOrgUserAdminTarget_ToOtherUser()
    {
        var result = MaskExemptionAuthority.Evaluate(
            granterRoleName: BakedInRoles.CrossOrgUserAdmin,
            granterUserId: CrossOrgAdminId,
            targetRoleName: BakedInRoles.CrossOrgUserAdmin,
            targetUserId: "other@test.local",
            expiresAt: DateTimeOffset.UtcNow.AddDays(14));

        result.Outcome.Should().Be(MaskExemptionAuthorityOutcome.Denied);
    }

    [Fact]
    public void SystemAdmin_CannotGrant_CrossOrgUserAdminTarget()
    {
        var result = MaskExemptionAuthority.Evaluate(
            granterRoleName: BakedInRoles.SystemAdmin,
            granterUserId: SysAdminId,
            targetRoleName: BakedInRoles.CrossOrgUserAdmin,
            targetUserId: SysAdminId,
            expiresAt: DateTimeOffset.UtcNow.AddDays(14));

        // Self-grant only rule: SystemAdmin is a different user and a different role.
        result.Outcome.Should().Be(MaskExemptionAuthorityOutcome.Denied);
    }

    [Fact]
    public void UserTypeRoleGranter_IsAlwaysDenied()
    {
        var result = MaskExemptionAuthority.Evaluate(
            granterRoleName: "BackofficeUser",
            granterUserId: "backoffice@test.local",
            targetRoleName: "Customer",
            targetUserId: null,
            expiresAt: DateTimeOffset.UtcNow.AddDays(1));

        result.Outcome.Should().Be(MaskExemptionAuthorityOutcome.Denied);
    }

    [Fact]
    public void EmptyGranterRole_IsDenied()
    {
        var result = MaskExemptionAuthority.Evaluate(
            granterRoleName: string.Empty,
            granterUserId: SysAdminId,
            targetRoleName: "BackofficeUser",
            targetUserId: null,
            expiresAt: null);

        result.Outcome.Should().Be(MaskExemptionAuthorityOutcome.Denied);
    }

    [Fact]
    public void EmptyTargetRole_IsDenied()
    {
        var result = MaskExemptionAuthority.Evaluate(
            granterRoleName: BakedInRoles.SystemAdmin,
            granterUserId: SysAdminId,
            targetRoleName: string.Empty,
            targetUserId: null,
            expiresAt: null);

        result.Outcome.Should().Be(MaskExemptionAuthorityOutcome.Denied);
    }

    [Fact]
    public void UserBakedInTarget_IsTreatedAsUserType()
    {
        // BakedInRoles.User is the only baked-in role that IS user-type, so
        // it follows the user-type rule set (expiry optional).
        var result = MaskExemptionAuthority.Evaluate(
            granterRoleName: BakedInRoles.SystemAdmin,
            granterUserId: SysAdminId,
            targetRoleName: BakedInRoles.User,
            targetUserId: null,
            expiresAt: null);

        result.IsAllowed.Should().BeTrue();
    }
}
