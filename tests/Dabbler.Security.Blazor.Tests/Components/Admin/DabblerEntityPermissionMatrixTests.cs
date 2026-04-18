using Bunit;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Blazor.Components.Admin;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Configuration;
using Dabbler.Security.Registration;
using Dabbler.Security.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Dabbler.Security.Blazor.Tests.Components.Admin;

public class DabblerEntityPermissionMatrixTests
{
    private static (SecurityConfigurationManager manager, Mock<ISecurityConfigurationStore> store)
        BuildManager(SecurityConfiguration config)
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        store.Setup(s => s.SaveAsync(It.IsAny<SecurityConfiguration>(), default))
             .Returns(Task.CompletedTask);
        return (new SecurityConfigurationManager(store.Object), store);
    }

    private static ResourceRegistry BuildRegistry(params string[] entityKeys) =>
        new ResourceRegistry(
            [],
            entityKeys.Select(k => new RegisteredEntity { Key = k, DisplayName = k }).ToList(),
            [], []);

    private static ResourceRegistry BuildRegistryWithFields(string entityKey, params string[] fieldKeys) =>
        new ResourceRegistry(
            [],
            [new RegisteredEntity
            {
                Key = entityKey, DisplayName = entityKey,
                Fields = fieldKeys.Select(f => new RegisteredField { Key = f, DisplayName = f }).ToList()
            }],
            [], []);

    [Fact]
    public void Renders_RolesAndEntities()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles =
            [
                new RoleDefinition { RoleName = "Admin" },
                new RoleDefinition { RoleName = "Viewer" }
            ]
        };
        var (manager, _) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(BuildRegistry("Order", "Product"));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerEntityPermissionMatrix>();

        cut.Markup.Should().Contain("Admin");
        cut.Markup.Should().Contain("Viewer");
        cut.Markup.Should().Contain("Order");
        cut.Markup.Should().Contain("Product");
    }

    [Fact]
    public void CrudHeaders_RenderSvgImages_WithAccessibleNames()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles = [new RoleDefinition { RoleName = "Admin" }]
        };
        var (manager, _) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(BuildRegistry("Order"));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerEntityPermissionMatrix>();

        var expected = new[]
        {
            ("Create", "_content/Dabbler.Security.Blazor/create-header.svg"),
            ("Read",   "_content/Dabbler.Security.Blazor/read-header.svg"),
            ("Update", "_content/Dabbler.Security.Blazor/update-header.svg"),
            ("Delete", "_content/Dabbler.Security.Blazor/delete-header.svg"),
        };

        foreach (var (op, src) in expected)
        {
            var img = cut.FindAll($"thead th img[src='{src}']").Should().NotBeEmpty($"{op} header image should be rendered").And.Subject.First();
            img.GetAttribute("alt").Should().Be(op, $"{op} header image must expose an alt text for screen readers");
            img.GetAttribute("title").Should().Be(op, $"{op} header image must expose a title tooltip");
            img.GetAttribute("aria-label").Should().Be(op, $"{op} header image must expose an aria-label");
        }

        // The old text-only column labels should no longer appear as standalone header text.
        cut.FindAll("thead th span.mud-typography").Should().BeEmpty("CRUD header text was replaced by 45° SVG icons");
    }

    [Fact]
    public void CrudCells_AreCentered()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles = [new RoleDefinition { RoleName = "Admin" }]
        };
        var (manager, _) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(BuildRegistry("Order"));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerEntityPermissionMatrix>();

        var bodyCells = cut.FindAll("tbody td").Skip(1).Take(4).ToList(); // skip role-name td
        bodyCells.Should().HaveCount(4);
        foreach (var td in bodyCells)
        {
            (td.GetAttribute("style") ?? string.Empty).Should().Contain("text-align:center");
        }
    }

    [Fact]
    public void NoEntities_ShowsHelpMessage()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles = [new RoleDefinition { RoleName = "Admin" }]
        };
        var (manager, _) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(ResourceRegistry.Empty);
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerEntityPermissionMatrix>();

        cut.Markup.Should().Contain("No entities registered");
    }

    [Fact]
    public void ExistingEntityPermissions_CheckedOnRender()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles = [new RoleDefinition { RoleName = "Admin" }],
            EntityPermissions =
            [
                new EntityPermission
                {
                    EntityKey = "Order",
                    RolePermissions =
                    [
                        new EntityRolePermission
                        {
                            RoleName = "Admin",
                            AllowedOperations = ["Create", "Read"]
                        }
                    ]
                }
            ]
        };
        var (manager, _) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(BuildRegistry("Order"));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerEntityPermissionMatrix>();

        // Should have checkboxes; first 2 (Create+Read) checked, last 2 (Update+Delete) unchecked
        var checkboxes = cut.FindAll("input[type='checkbox']");
        checkboxes.Should().HaveCountGreaterOrEqualTo(4);
        checkboxes[0].GetAttribute("checked").Should().NotBeNull();  // Create
        checkboxes[1].GetAttribute("checked").Should().NotBeNull();  // Read
        checkboxes[2].GetAttribute("checked").Should().BeNull();     // Update
        checkboxes[3].GetAttribute("checked").Should().BeNull();     // Delete
    }

    [Fact]
    public void EntityWithFields_ShowsDrillDownButton()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles = [new RoleDefinition { RoleName = "Admin" }]
        };
        var (manager, _) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(BuildRegistryWithFields("Order", "Name", "Amount"));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerEntityPermissionMatrix>();

        // The drill-down icon button should be present (MudIconButton renders as button.mud-icon-button)
        cut.FindAll("button.mud-icon-button").Should().NotBeEmpty();
    }

    [Fact]
    public async Task DrillDownButton_TogglesDabblerFieldPermissionEditor()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles = [new RoleDefinition { RoleName = "Admin" }]
        };
        var (manager, _) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(BuildRegistryWithFields("Order", "Name", "Amount"));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerEntityPermissionMatrix>();

        // Before click — field editor not visible
        cut.HasComponent<DabblerFieldPermissionEditor>().Should().BeFalse();

        // Click the drill-down icon button (first mud-icon-button in the header area)
        var drillBtn = cut.Find("button.mud-icon-button");
        await cut.InvokeAsync(() => drillBtn.Click());

        // After click — field editor appears
        cut.HasComponent<DabblerFieldPermissionEditor>().Should().BeTrue();
    }

    [Fact]
    public async Task SaveButton_PersistsEntityPermissions()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles = [new RoleDefinition { RoleName = "Manager" }],
            EntityPermissions =
            [
                new EntityPermission
                {
                    EntityKey = "Invoice",
                    RolePermissions =
                    [
                        new EntityRolePermission { RoleName = "Manager", AllowedOperations = ["Read"] }
                    ]
                }
            ]
        };
        var (manager, store) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(BuildRegistry("Invoice"));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerEntityPermissionMatrix>();

        var saveButton = cut.Find("button.mud-button-filled");
        await cut.InvokeAsync(() => saveButton.Click());

        store.Verify(s => s.SaveAsync(
            It.Is<SecurityConfiguration>(c => c.EntityPermissions.Any(e =>
                e.EntityKey == "Invoice" &&
                e.RolePermissions.Any(r => r.RoleName == "Manager" && r.AllowedOperations.Contains("Read")))),
            default), Times.Once);
    }
}
