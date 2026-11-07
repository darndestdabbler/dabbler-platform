using Bunit;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Authorization;
using Dabbler.Security.Blazor.Components.Admin.Documentation;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Configuration;
using Dabbler.Security.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Dabbler.Security.Blazor.Tests.Components.Admin.Documentation;

public class RoleDocumentationTabTests
{
    private static TestContext Build(SecurityConfiguration? config = null)
    {
        config ??= new SecurityConfiguration();
        var ctx = new TestContext();
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        store.Setup(s => s.SaveAsync(It.IsAny<SecurityConfiguration>(), default))
             .Returns(Task.CompletedTask);
        var manager = new SecurityConfigurationManager(store.Object);

        ctx.SetupForDabbler(configureServices: svc =>
        {
            svc.AddSingleton(manager);
            svc.AddSingleton<IBakedInRoleProvider, PlatformBakedInRoleProvider>();
        });
        manager.EnsureLoadedAsync().Wait();
        return ctx;
    }

    [Fact]
    public void Renders_AllFiveBakedInRolePanels()
    {
        using var ctx = Build();
        var cut = ctx.RenderComponent<RoleDocumentationTab>();

        cut.Find("[data-testid='role-documentation-tab']").Should().NotBeNull();
        foreach (var role in BakedInRoles.All)
        {
            cut.Find($"[data-testid='role-doc-panel-{role}']").Should().NotBeNull();
        }
    }

    [Fact]
    public void Renders_UserTypeDefaultsPanel_AndDefaultMasksPanel()
    {
        using var ctx = Build();
        var cut = ctx.RenderComponent<RoleDocumentationTab>();

        cut.Find("[data-testid='role-doc-panel-user-type-defaults']").Should().NotBeNull();
        cut.Find("[data-testid='role-doc-panel-default-masks']").Should().NotBeNull();
    }

    [Fact]
    public void SystemAdmin_CapabilitiesList_IncludesAllPrivilegesLine()
    {
        using var ctx = Build();
        var cut = ctx.RenderComponent<RoleDocumentationTab>();

        var caps = cut.Find($"[data-testid='role-doc-caps-{BakedInRoles.SystemAdmin}']");
        caps.TextContent.Should().Contain("Has every platform privilege");
        caps.TextContent.Should().Contain("Can create, edit, or archive user-type roles");
    }

    [Fact]
    public void CrossOrgMonitor_CapabilitiesList_MarksReadOnly()
    {
        using var ctx = Build();
        var cut = ctx.RenderComponent<RoleDocumentationTab>();

        var caps = cut.Find($"[data-testid='role-doc-caps-{BakedInRoles.CrossOrgMonitor}']");
        caps.TextContent.Should().Contain("Read-only");
        caps.TextContent.Should().Contain("every organization");
    }

    [Fact]
    public void User_Panel_ShowsUserTypeCapability()
    {
        using var ctx = Build();
        var cut = ctx.RenderComponent<RoleDocumentationTab>();

        var caps = cut.Find($"[data-testid='role-doc-caps-{BakedInRoles.User}']");
        caps.TextContent.Should().Contain("User-type role");
    }

    [Fact]
    public void AliasOverride_FromConfiguration_IsSurfacedForRole()
    {
        var config = new SecurityConfiguration
        {
            BakedInRoleAliasOverrides = new Dictionary<string, string>
            {
                [BakedInRoles.SystemAdmin] = "Platform Owner",
            },
        };

        using var ctx = Build(config);
        var cut = ctx.RenderComponent<RoleDocumentationTab>();

        var canonical = cut.Find($"[data-testid='role-doc-canonical-{BakedInRoles.SystemAdmin}']");
        canonical.TextContent.Should().Contain("Platform Owner");
        canonical.TextContent.Should().Contain("SystemAdmin");
    }
}
