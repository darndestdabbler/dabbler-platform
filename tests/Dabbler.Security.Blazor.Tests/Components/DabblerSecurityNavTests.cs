using Bunit;
using Dabbler.Security.Blazor.Components;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Dabbler.Security.Blazor.Tests.Components;

/// <summary>
/// Each test gets its own BunitContext so roles can differ between tests.
/// DabblerSecurityNav now uses IUserRoleStore for role-based filtering
/// instead of the declarative AuthorizeView approach.
/// </summary>
public class DabblerSecurityNavTests
{
    private static Mock<IUserRoleStore> BuildRoleStore(params string[] roles)
    {
        var mock = new Mock<IUserRoleStore>();
        mock.Setup(s => s.GetRolesAsync(It.IsAny<string>(), default))
            .ReturnsAsync((IReadOnlyList<string>)roles.ToList());
        return mock;
    }

    [Fact]
    public async Task SystemAdministrator_RendersNavGroup()
    {
        await using var ctx = new BunitContext();
        var roleStore = BuildRoleStore("SystemAdministrator");
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc => svc.AddSingleton(roleStore.Object));

        var cut = ctx.Render<DabblerSecurityNav>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Security Admin"));
    }

    [Fact]
    public async Task UserAdministrator_RendersNavGroup()
    {
        await using var ctx = new BunitContext();
        var roleStore = BuildRoleStore("UserAdministrator");
        ctx.SetupForDabbler(
            roles: ["UserAdministrator"],
            configureServices: svc => svc.AddSingleton(roleStore.Object));

        var cut = ctx.Render<DabblerSecurityNav>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Security Admin"));
    }

    [Fact]
    public async Task SystemAdministrator_ShowsSecurityDashboardLink()
    {
        await using var ctx = new BunitContext();
        var roleStore = BuildRoleStore("SystemAdministrator");
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc => svc.AddSingleton(roleStore.Object));

        var cut = ctx.Render<DabblerSecurityNav>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("/admin/security"));
    }

    [Fact]
    public async Task SystemAdministrator_RendersNavigationLandmark()
    {
        await using var ctx = new BunitContext();
        var roleStore = BuildRoleStore("SystemAdministrator");
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc => svc.AddSingleton(roleStore.Object));

        var cut = ctx.Render<DabblerSecurityNav>();

        cut.WaitForAssertion(() =>
        {
            var nav = cut.Find("nav");
            nav.GetAttribute("aria-label").Should().Be("Security administration");
        });
    }

    [Fact]
    public async Task SystemAdministrator_ShowsUsersAndAuditLinks()
    {
        await using var ctx = new BunitContext();
        var roleStore = BuildRoleStore("SystemAdministrator");
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc => svc.AddSingleton(roleStore.Object));

        var cut = ctx.Render<DabblerSecurityNav>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("/admin/users");
            cut.Markup.Should().Contain("/admin/audit");
        });
    }

    [Fact]
    public async Task NonAdminUser_RendersNothing()
    {
        await using var ctx = new BunitContext();
        var roleStore = BuildRoleStore("Viewer");
        ctx.SetupForDabbler(
            roles: ["Viewer"],
            configureServices: svc => svc.AddSingleton(roleStore.Object));

        var cut = ctx.Render<DabblerSecurityNav>();

        cut.WaitForAssertion(() =>
            cut.Markup.Trim().Should().NotContain("Security Admin"));
    }

    [Fact]
    public async Task AnonymousUser_RendersNothing()
    {
        await using var ctx = new BunitContext();
        var roleStore = BuildRoleStore(); // no roles
        ctx.SetupForDabbler(
            roles: [], // unauthenticated
            configureServices: svc => svc.AddSingleton(roleStore.Object));

        var cut = ctx.Render<DabblerSecurityNav>();

        // When unauthenticated, IsAuthenticated = false → _showSecurityAdmin stays false
        cut.Markup.Trim().Should().NotContain("Security Admin");
    }
}
