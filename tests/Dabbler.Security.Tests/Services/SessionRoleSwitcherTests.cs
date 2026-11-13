using System.Security.Claims;
using Dabbler.Security.Services;

namespace Dabbler.Security.Tests.Services;

public class SessionRoleSwitcherTests
{
    private static ClaimsPrincipal UserWithRoles(params string[] roles)
    {
        var claims = roles.Select(r => new Claim(ClaimTypes.Role, r))
            .Append(new Claim(ClaimTypes.NameIdentifier, "1"))
            .ToArray();
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    [Fact]
    public void IsRoleSwitched_FalseByDefault()
    {
        var switcher = new SessionRoleSwitcher();

        switcher.IsRoleSwitched.Should().BeFalse();
    }

    [Fact]
    public void EffectiveRoles_WhenNoSwitch_ReturnsClaimsRoles()
    {
        var switcher = new SessionRoleSwitcher();
        var user = UserWithRoles("Admin", "Manager");

        var roles = switcher.EffectiveRoles(user).ToList();

        roles.Should().BeEquivalentTo(["Admin", "Manager"]);
    }

    [Fact]
    public void EffectiveRoles_AfterSwitchToRole_ReturnsOverrideOnly()
    {
        var switcher = new SessionRoleSwitcher();
        switcher.SwitchToRole("Viewer");
        var user = UserWithRoles("Admin");

        var roles = switcher.EffectiveRoles(user).ToList();

        roles.Should().ContainSingle().Which.Should().Be("Viewer");
    }

    [Fact]
    public void IsRoleSwitched_TrueAfterSwitchToRole()
    {
        var switcher = new SessionRoleSwitcher();
        switcher.SwitchToRole("Viewer");

        switcher.IsRoleSwitched.Should().BeTrue();
    }

    [Fact]
    public void ClearSwitch_ReturnsToClaimsRoles()
    {
        var switcher = new SessionRoleSwitcher();
        switcher.SwitchToRole("Viewer");
        var user = UserWithRoles("Admin");

        switcher.ClearSwitch();
        var roles = switcher.EffectiveRoles(user).ToList();

        switcher.IsRoleSwitched.Should().BeFalse();
        roles.Should().ContainSingle().Which.Should().Be("Admin");
    }

    [Fact]
    public void SwitchToRole_NullRoleName_ThrowsArgumentNullException()
    {
        var switcher = new SessionRoleSwitcher();

        var act = () => switcher.SwitchToRole(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
