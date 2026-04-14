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

public class DabblerRowFilterEditorTests
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

    private static ResourceRegistry BuildRegistry(string entityKey) =>
        new ResourceRegistry(
            [],
            [new RegisteredEntity { Key = entityKey, DisplayName = entityKey }],
            [], []);

    [Fact]
    public void NoFilters_ShowsEmptyMessage()
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

        var cut = ctx.RenderComponent<DabblerRowFilterEditor>(p =>
            p.Add(c => c.EntityKey, "Order"));

        cut.Markup.Should().Contain("No row filters defined");
    }

    [Fact]
    public void ExistingFilters_RenderedInTable()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles = [new RoleDefinition { RoleName = "Viewer" }],
            RowFilters =
            [
                new RowFilter
                {
                    EntityKey = "Order",
                    RoleName = "Viewer",
                    FilterExpression = "OwnerId == @CurrentUserId",
                    Description = "Own orders only"
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

        var cut = ctx.RenderComponent<DabblerRowFilterEditor>(p =>
            p.Add(c => c.EntityKey, "Order"));

        cut.Markup.Should().Contain("Viewer");
        cut.Markup.Should().Contain("OwnerId == @CurrentUserId");
        cut.Markup.Should().Contain("Own orders only");
    }

    [Fact]
    public void AddFilterButton_Present()
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

        var cut = ctx.RenderComponent<DabblerRowFilterEditor>(p =>
            p.Add(c => c.EntityKey, "Order"));

        // "Add Filter" button is present and clicking it doesn't throw
        cut.Markup.Should().Contain("Add Filter");
        var action = () => cut.Find("button.mud-button-filled").Click();
        action.Should().NotThrow();
    }

    [Fact]
    public async Task DeleteFilter_RemovesAndSaves()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles = [new RoleDefinition { RoleName = "Viewer" }],
            RowFilters =
            [
                new RowFilter
                {
                    EntityKey = "Order",
                    RoleName = "Viewer",
                    FilterExpression = "Status == 'Active'",
                    Description = "Active orders only"
                }
            ]
        };
        var (manager, store) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(BuildRegistry("Order"));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerRowFilterEditor>(p =>
            p.Add(c => c.EntityKey, "Order"));

        // Click the delete button
        var deleteButtons = cut.FindAll("button.mud-icon-button");
        await cut.InvokeAsync(() => deleteButtons[1].Click()); // index 1 = delete (0 = edit)

        store.Verify(s => s.SaveAsync(
            It.Is<SecurityConfiguration>(c => !c.RowFilters.Any(f =>
                f.EntityKey == "Order" && f.RoleName == "Viewer")),
            default), Times.Once);
    }

    [Fact]
    public void FiltersForOtherEntities_NotShown()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles = [new RoleDefinition { RoleName = "Admin" }],
            RowFilters =
            [
                new RowFilter
                {
                    EntityKey = "Product",
                    RoleName = "Admin",
                    FilterExpression = "IsActive == true"
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

        var cut = ctx.RenderComponent<DabblerRowFilterEditor>(p =>
            p.Add(c => c.EntityKey, "Order"));

        // The Product entity filter should not appear when viewing Order filters
        cut.Markup.Should().NotContain("IsActive == true");
        cut.Markup.Should().Contain("No row filters defined");
    }
}
