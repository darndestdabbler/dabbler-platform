using Bunit;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Blazor.Components.Admin;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Configuration;
using Dabbler.Security.Registration;
using Dabbler.Security.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using FieldAccessLevel = Dabbler.Security.Configuration.FieldAccessLevel;

namespace Dabbler.Security.Blazor.Tests.Components.Admin;

public class DabblerFieldPermissionEditorTests
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

    private static ResourceRegistry BuildRegistry(string entityKey, params string[] fieldKeys) =>
        new ResourceRegistry(
            [],
            [new RegisteredEntity
            {
                Key = entityKey,
                DisplayName = entityKey,
                Fields = fieldKeys.Select(f => new RegisteredField { Key = f, DisplayName = f }).ToList()
            }],
            [], []);

    [Fact]
    public void UnknownEntity_ShowsNotRegisteredMessage()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration();
        var (manager, _) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(ResourceRegistry.Empty);
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerFieldPermissionEditor>(p =>
            p.Add(c => c.EntityKey, "NonExistent"));

        cut.Markup.Should().Contain("not registered");
    }

    [Fact]
    public void Renders_EntityFieldsAndRoles()
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
                svc.AddSingleton(BuildRegistry("Order", "OrderDate", "TotalAmount"));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerFieldPermissionEditor>(p =>
            p.Add(c => c.EntityKey, "Order"));

        cut.Markup.Should().Contain("Order");
        cut.Markup.Should().Contain("Admin");
        cut.Markup.Should().Contain("Viewer");
        cut.Markup.Should().Contain("OrderDate");
        cut.Markup.Should().Contain("TotalAmount");
    }

    [Fact]
    public void ExistingFieldPermissions_ShowCorrectState()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles = [new RoleDefinition { RoleName = "Viewer" }],
            FieldPermissions =
            [
                new FieldPermission
                {
                    EntityKey = "Order",
                    FieldKey = "Price",
                    RolePermissions =
                    [
                        new FieldRolePermission { RoleName = "Viewer", AccessLevel = FieldAccessLevel.Hidden }
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
                svc.AddSingleton(BuildRegistry("Order", "Price"));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerFieldPermissionEditor>(p =>
            p.Add(c => c.EntityKey, "Order"));

        // Hidden should appear in the rendered select
        cut.Markup.Should().Contain("Hidden");
    }

    [Fact]
    public async Task SaveButton_PersistsFieldPermissions()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles = [new RoleDefinition { RoleName = "Admin" }],
            FieldPermissions =
            [
                new FieldPermission
                {
                    EntityKey = "Product",
                    FieldKey = "CostPrice",
                    RolePermissions =
                    [
                        new FieldRolePermission { RoleName = "Admin", AccessLevel = FieldAccessLevel.ReadOnly }
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
                svc.AddSingleton(BuildRegistry("Product", "CostPrice"));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerFieldPermissionEditor>(p =>
            p.Add(c => c.EntityKey, "Product"));

        var saveButton = cut.Find("button.mud-button-filled");
        await cut.InvokeAsync(() => saveButton.Click());

        store.Verify(s => s.SaveAsync(
            It.Is<SecurityConfiguration>(c => c.FieldPermissions.Any(f =>
                f.EntityKey == "Product" && f.FieldKey == "CostPrice" &&
                f.RolePermissions.Any(r => r.RoleName == "Admin" && r.AccessLevel == FieldAccessLevel.ReadOnly))),
            default), Times.Once);
    }
}
