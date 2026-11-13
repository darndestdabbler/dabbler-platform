using Dabbler.Security.Authorization;
using Dabbler.Security.Services;

namespace Dabbler.Security.Tests.Services;

public class OrganizationScopeFilterTests
{
    private readonly OrganizationScopeFilter _sut = new();
    private readonly IBakedInRoleProvider _bakedInRoleProvider = new PlatformBakedInRoleProvider();

    [Theory]
    [InlineData(BakedInRoles.SystemAdmin, true)]
    [InlineData(BakedInRoles.CrossOrgUserAdmin, true)]
    [InlineData(BakedInRoles.CrossOrgMonitor, true)]
    [InlineData(BakedInRoles.UserAdmin, false)]
    [InlineData(BakedInRoles.User, false)]
    [InlineData("BackofficeUser", false)]
    public void CanBypassOrganizationScope_ReturnsExpectedValue(string roleName, bool expected)
    {
        var actual = _sut.CanBypassOrganizationScope([roleName], _bakedInRoleProvider);

        actual.Should().Be(expected);
    }

    [Fact]
    public void CanBypassOrganizationScope_MixedEffectiveRoles_ReturnsTrueWhenAnyRoleCanSeeAllOrganizations()
    {
        var actual = _sut.CanBypassOrganizationScope(
            [BakedInRoles.UserAdmin, BakedInRoles.CrossOrgMonitor],
            _bakedInRoleProvider);

        actual.Should().BeTrue();
    }

    [Fact]
    public void DeferredReason_ExplainsSetCDependency()
    {
        OrganizationScopeFilter.DeferredReason.Should().Contain("Set C");
        OrganizationScopeFilter.DeferredReason.Should().Contain("OrganizationId");
    }
}
