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

namespace Dabbler.Security.Blazor.Tests.Components.Admin.Role;

public class RoleDetailsSectionTests
{
    private static void RegisterServices(IServiceCollection svc, SecurityConfiguration? config = null)
    {
        config ??= new SecurityConfiguration
        {
            DefaultMasks =
            [
                new DefaultMaskRule { EntityKey = "Customer", ColumnKey = "SocialSecurityNumber", MaskFormat = "XXX-XX-{last4}" },
            ],
        };
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        store.Setup(s => s.SaveAsync(It.IsAny<SecurityConfiguration>(), default)).Returns(Task.CompletedTask);
        var manager = new SecurityConfigurationManager(store.Object);
        svc.AddSingleton(manager);
        svc.AddSingleton<AuthorizationStateNotifier>();
        svc.AddSingleton<IBakedInRoleProvider, PlatformBakedInRoleProvider>();

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
        };
        svc.AddSingleton(new MetadataRegistry(new ResourceRegistry(pages, entities, [], [])));
    }

    [Fact]
    public void UserTypeSelection_RendersFourSubTabs()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(
            roles: [BakedInRoles.SystemAdmin],
            configureServices: svc => RegisterServices(svc, null));

        var role = new RoleDefinition { RoleName = "BackofficeUser", Description = "Internal staff." };
        var cut = ctx.RenderComponent<RoleDetailsSection>(p => p
            .Add(c => c.Selection, RoleDetailsSection.RoleSelection.ForUserType(role)));

        cut.Markup.Should().Contain("Data Column Restrictions");
        cut.Markup.Should().Contain("Data Row Restrictions");
        cut.Markup.Should().Contain("Other UI Restrictions");
        cut.Markup.Should().Contain("Mask Exemptions");
        cut.Markup.Should().NotContain("Compiled Privileges");
    }

    [Fact]
    public void BakedInSelection_RendersMaskExemptionsAndCompiledPrivileges()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(
            roles: [BakedInRoles.SystemAdmin],
            configureServices: svc => RegisterServices(svc, null));

        var provider = new PlatformBakedInRoleProvider();
        var baked = provider.TryGetBakedInRole(BakedInRoles.SystemAdmin)!;

        var cut = ctx.RenderComponent<RoleDetailsSection>(p => p
            .Add(c => c.Selection, RoleDetailsSection.RoleSelection.ForBakedIn(baked, aliasOverride: null)));

        cut.Markup.Should().Contain("Mask Exemptions");
        cut.Markup.Should().Contain("Compiled Privileges");
        cut.Markup.Should().NotContain("Data Column Restrictions");
        cut.Markup.Should().NotContain("Data Row Restrictions");
        cut.Markup.Should().NotContain("Other UI Restrictions");
    }

    [Fact]
    public void BakedInSelection_ShowsBakedInChipInHeader()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(
            roles: [BakedInRoles.SystemAdmin],
            configureServices: svc => RegisterServices(svc, null));

        var provider = new PlatformBakedInRoleProvider();
        var baked = provider.TryGetBakedInRole(BakedInRoles.User)!;

        var cut = ctx.RenderComponent<RoleDetailsSection>(p => p
            .Add(c => c.Selection, RoleDetailsSection.RoleSelection.ForBakedIn(baked, null)));

        var header = cut.Find("[data-testid='role-details-section']");
        header.TextContent.Should().Contain("baked-in");
    }
}
