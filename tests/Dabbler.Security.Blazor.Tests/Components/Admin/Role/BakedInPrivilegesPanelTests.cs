using Bunit;
using Dabbler.Security.Authorization;
using Dabbler.Security.Blazor.Components.Admin.Role;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Dabbler.Security.Blazor.Tests.Components.Admin.Role;

public class BakedInPrivilegesPanelTests
{
    private static BakedInRoleDefinition SystemAdminRole() =>
        new PlatformBakedInRoleProvider().TryGetBakedInRole(BakedInRoles.SystemAdmin)!;

    private static BakedInRoleDefinition UserRole() =>
        new PlatformBakedInRoleProvider().TryGetBakedInRole(BakedInRoles.User)!;

    [Fact]
    public async Task Panel_RendersRoleName_AndBakedInChip()
    {
        await using var ctx = new BunitContext();
        ctx.SetupForDabbler();

        var cut = ctx.Render<BakedInPrivilegesPanel>(parameters => parameters
            .Add(p => p.Role, SystemAdminRole())
            .Add(p => p.AliasOverride, null));

        cut.Markup.Should().Contain(BakedInRoles.SystemAdmin);
        cut.Markup.Should().Contain("baked-in");
        cut.Find("[data-testid='baked-in-privileges-title']").Should().NotBeNull();
    }

    [Fact]
    public async Task Panel_ShowsAliasOverride_WhenPresent()
    {
        await using var ctx = new BunitContext();
        ctx.SetupForDabbler();

        var cut = ctx.Render<BakedInPrivilegesPanel>(parameters => parameters
            .Add(p => p.Role, SystemAdminRole())
            .Add(p => p.AliasOverride, "Super Admin"));

        cut.Markup.Should().Contain("Super Admin");
        cut.Markup.Should().Contain("Display alias override");
    }

    [Fact]
    public async Task Panel_ShowsDefaultAlias_WhenNoOverride()
    {
        await using var ctx = new BunitContext();
        ctx.SetupForDabbler();

        var cut = ctx.Render<BakedInPrivilegesPanel>(parameters => parameters
            .Add(p => p.Role, SystemAdminRole()));

        cut.Markup.Should().Contain("Default display alias");
        cut.Find("[data-testid='baked-in-privileges-default-alias']").Should().NotBeNull();
    }

    [Fact]
    public async Task Panel_SystemAdmin_ShowsAllCapabilitiesTrue()
    {
        await using var ctx = new BunitContext();
        ctx.SetupForDabbler();

        var cut = ctx.Render<BakedInPrivilegesPanel>(parameters => parameters
            .Add(p => p.Role, SystemAdminRole()));

        // HasAllPrivileges flag renders the success icon.
        cut.Find("[data-testid='privilege-value-has-all-privileges-true']").Should().NotBeNull();
    }

    [Fact]
    public async Task Panel_User_IsUserTypeTrue_HasAllPrivilegesFalse()
    {
        await using var ctx = new BunitContext();
        ctx.SetupForDabbler();

        var cut = ctx.Render<BakedInPrivilegesPanel>(parameters => parameters
            .Add(p => p.Role, UserRole()));

        cut.Find("[data-testid='privilege-value-is-user-type-true']").Should().NotBeNull();
        cut.Find("[data-testid='privilege-value-has-all-privileges-false']").Should().NotBeNull();
    }
}
