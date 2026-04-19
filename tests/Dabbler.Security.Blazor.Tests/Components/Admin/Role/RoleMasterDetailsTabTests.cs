using Bunit;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Authorization;
using Dabbler.Security.Blazor.Components.Admin.Role;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Configuration;
using Dabbler.Security.Registration;
using Dabbler.Security.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Field = Dabbler.Security.Registration.RegisteredField;
using Entity = Dabbler.Security.Registration.RegisteredEntity;
using Page = Dabbler.Security.Registration.RegisteredPage;
using Control = Dabbler.Security.Registration.RegisteredControl;

namespace Dabbler.Security.Blazor.Tests.Components.Admin.Role;

/// <summary>
/// Tests for <see cref="RoleMasterDetailsTab"/>. They wire the full DI graph
/// the tab expects (config manager, baked-in provider, metadata registry) so
/// the grid, details section, and sub-tabs render without stubbing the
/// collaborators.
/// </summary>
public class RoleMasterDetailsTabTests
{
    private static SecurityConfiguration SeedConfiguration(
        params RoleDefinition[] userTypeRoles) =>
        new()
        {
            UserTypeRoles = userTypeRoles.ToList(),
            DefaultMasks =
            [
                new DefaultMaskRule { EntityKey = "Customer", ColumnKey = "SocialSecurityNumber", MaskFormat = "XXX-XX-{last4}" },
            ],
        };

    private static MetadataRegistry SeedMetadata()
    {
        var pages = new[] { new Page { Key = "/orders", DisplayName = "Orders" } };
        var entities = new[]
        {
            new Entity
            {
                Key = "Customer",
                DisplayName = "Customers",
                Fields = new[]
                {
                    new Field { Key = "Email", DisplayName = "Email" },
                    new Field { Key = "SocialSecurityNumber", DisplayName = "SSN" },
                },
            },
            new Entity
            {
                Key = "Order",
                DisplayName = "Orders",
                Fields = new[] { new Field { Key = "Status", DisplayName = "Status" } },
            },
        };
        var controls = new[]
        {
            new Control { Key = "DeleteOrderButton", PageKey = "/orders", DisplayName = "Delete order" },
        };

        var resource = new ResourceRegistry(pages, entities, [], controls);
        return new MetadataRegistry(resource);
    }

    private static void RegisterServices(
        IServiceCollection svc,
        SecurityConfiguration config,
        MetadataRegistry? metadata = null)
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        store.Setup(s => s.SaveAsync(It.IsAny<SecurityConfiguration>(), default))
             .Returns(Task.CompletedTask);
        var manager = new SecurityConfigurationManager(store.Object);

        svc.AddSingleton(manager);
        svc.AddSingleton<AuthorizationStateNotifier>();
        svc.AddSingleton<IBakedInRoleProvider, PlatformBakedInRoleProvider>();
        svc.AddSingleton(metadata ?? SeedMetadata());
    }

    [Fact]
    public void RoleMasterDetailsTab_RendersAllBakedInAndUserTypeRoles()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(
            roles: [BakedInRoles.SystemAdmin],
            configureServices: svc => RegisterServices(
                svc,
                SeedConfiguration(
                    new RoleDefinition { RoleName = "BackofficeUser", Description = "Internal staff." },
                    new RoleDefinition { RoleName = "Customer", Description = "External customer." })));

        var cut = ctx.RenderComponent<RoleMasterDetailsTab>();

        cut.Find("[data-testid='role-row-SystemAdmin']").Should().NotBeNull();
        cut.Find("[data-testid='role-row-CrossOrgUserAdmin']").Should().NotBeNull();
        cut.Find("[data-testid='role-row-CrossOrgMonitor']").Should().NotBeNull();
        cut.Find("[data-testid='role-row-UserAdmin']").Should().NotBeNull();
        cut.Find("[data-testid='role-row-User']").Should().NotBeNull();
        cut.Find("[data-testid='role-row-BackofficeUser']").Should().NotBeNull();
        cut.Find("[data-testid='role-row-Customer']").Should().NotBeNull();
    }

    [Fact]
    public void RoleMasterDetailsTab_BakedInRows_CarryBakedInChip()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(
            roles: [BakedInRoles.SystemAdmin],
            configureServices: svc => RegisterServices(svc, SeedConfiguration()));

        var cut = ctx.RenderComponent<RoleMasterDetailsTab>();

        cut.Find("[data-testid='role-row-SystemAdmin-baked-in-chip']").Should().NotBeNull();
    }

    [Fact]
    public void RoleMasterDetailsTab_UserTypeRows_DoNotCarryBakedInChip()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(
            roles: [BakedInRoles.SystemAdmin],
            configureServices: svc => RegisterServices(
                svc,
                SeedConfiguration(new RoleDefinition { RoleName = "BackofficeUser" })));

        var cut = ctx.RenderComponent<RoleMasterDetailsTab>();

        var action = () => cut.Find("[data-testid='role-row-BackofficeUser-baked-in-chip']");
        action.Should().Throw<Bunit.ElementNotFoundException>();
    }

    [Fact]
    public void RoleMasterDetailsTab_SystemAdmin_SeesAddAndArchiveAffordances()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(
            roles: [BakedInRoles.SystemAdmin],
            configureServices: svc => RegisterServices(
                svc,
                SeedConfiguration(new RoleDefinition { RoleName = "BackofficeUser" })));

        var cut = ctx.RenderComponent<RoleMasterDetailsTab>();

        // Add button is enabled for SystemAdmin.
        var addButton = cut.Find("[data-testid='role-master-add-button']");
        addButton.HasAttribute("disabled").Should().BeFalse();

        // Archive shown only on the user-type row, not on baked-in rows.
        cut.Find("[data-testid='role-row-BackofficeUser-archive']").Should().NotBeNull();
        var systemAdminArchive = () => cut.Find("[data-testid='role-row-SystemAdmin-archive']");
        systemAdminArchive.Should().Throw<Bunit.ElementNotFoundException>();
    }

    [Fact]
    public void RoleMasterDetailsTab_NonSystemAdmin_AddButtonIsDisabled()
    {
        // Note: in production the dashboard gates Role Master Details so only
        // SystemAdmin can reach this tab. CanManageRoles is still evaluated
        // here to make the affordance state self-documenting.
        using var ctx = new TestContext();
        ctx.SetupForDabbler(
            roles: [BakedInRoles.UserAdmin],
            configureServices: svc => RegisterServices(svc, SeedConfiguration()));

        var cut = ctx.RenderComponent<RoleMasterDetailsTab>();

        var addButton = cut.Find("[data-testid='role-master-add-button']");
        addButton.HasAttribute("disabled").Should().BeTrue();
    }
}
