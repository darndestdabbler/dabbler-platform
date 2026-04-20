using Bunit;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Blazor.Components.Admin.Role;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Configuration;
using Dabbler.Security.Registration;
using Dabbler.Security.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Field = Dabbler.Security.Registration.RegisteredField;
using Entity = Dabbler.Security.Registration.RegisteredEntity;

namespace Dabbler.Security.Blazor.Tests.Components.Admin.Role;

public class ColumnRestrictionsSubTabTests
{
    private static (TestContext Ctx, SecurityConfigurationManager Manager) Build(
        RoleDefinition role,
        Action<IServiceCollection>? extra = null)
    {
        var ctx = new TestContext();
        var config = new SecurityConfiguration { UserTypeRoles = [role] };

        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        store.Setup(s => s.SaveAsync(It.IsAny<SecurityConfiguration>(), default))
             .Returns(Task.CompletedTask);
        var manager = new SecurityConfigurationManager(store.Object);

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
                Key = "Product",
                DisplayName = "Products",
                Fields = new[] { new Field { Key = "UnitPrice", DisplayName = "Unit Price" } },
            },
        };
        var metadata = new MetadataRegistry(new ResourceRegistry([], entities, [], []));

        ctx.SetupForDabbler(configureServices: svc =>
        {
            svc.AddSingleton(manager);
            svc.AddSingleton(metadata);
            extra?.Invoke(svc);
        });

        manager.EnsureLoadedAsync().Wait();
        return (ctx, manager);
    }

    [Fact]
    public void Empty_ShowsEmptyMessage()
    {
        var role = new RoleDefinition { RoleName = "BackofficeUser" };
        var (ctx, _) = Build(role);
        using var _1 = ctx;

        var cut = ctx.RenderComponent<ColumnRestrictionsSubTab>(p => p.Add(c => c.Role, role));

        cut.Find("[data-testid='column-restriction-empty']").Should().NotBeNull();
    }

    [Fact]
    public void PopulatedRole_RendersGridWithRows()
    {
        var role = new RoleDefinition
        {
            RoleName = "BackofficeUser",
            ColumnRestrictions =
            [
                new ColumnRestriction { EntityKey = "Product", ColumnKey = "UnitPrice", RestrictionKind = ColumnRestrictionKind.ViewOnly },
                new ColumnRestriction { EntityKey = "Customer", ColumnKey = "Email", RestrictionKind = ColumnRestrictionKind.Hidden },
            ],
        };
        var (ctx, _) = Build(role);
        using var _1 = ctx;

        var cut = ctx.RenderComponent<ColumnRestrictionsSubTab>(p => p.Add(c => c.Role, role));

        cut.Find("[data-testid='column-restriction-grid']").Should().NotBeNull();
        cut.Find("[data-testid='column-restriction-remove-Product-UnitPrice']").Should().NotBeNull();
        cut.Find("[data-testid='column-restriction-remove-Customer-Email']").Should().NotBeNull();
    }

    [Fact]
    public async Task ColumnDropdown_ExcludesAlreadyRestrictedColumns()
    {
        // UAT finding 5b — Session 6: columns that already carry a restriction
        // for this role must not appear in the Column dropdown so the user
        // cannot land on a silently-disabled Add button.
        var role = new RoleDefinition
        {
            RoleName = "BackofficeUser",
            ColumnRestrictions =
            [
                new ColumnRestriction { EntityKey = "Customer", ColumnKey = "Email", RestrictionKind = ColumnRestrictionKind.ViewOnly },
            ],
        };
        var (ctx, _) = Build(role);
        using var _1 = ctx;

        var cut = ctx.RenderComponent<ColumnRestrictionsSubTab>(p => p.Add(c => c.Role, role));

        // Select Table=Customer through the backing field and re-render.
        await cut.InvokeAsync(() =>
        {
            var field = cut.Instance.GetType().GetField(
                "_newTableKey",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            field.SetValue(cut.Instance, "Customer");
            cut.Render();
        });

        // Email is already restricted and must be filtered out of the
        // available-columns list, while SocialSecurityNumber (fresh pair)
        // must remain available. Inspect the backing computed property
        // rather than the popover DOM to keep the test robust against
        // MudSelect rendering quirks in bUnit.
        var availableProperty = cut.Instance.GetType().GetProperty(
            "AvailableColumns",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var available = (System.Collections.IEnumerable)availableProperty.GetValue(cut.Instance)!;
        var availableKeys = available
            .Cast<Field>()
            .Select(f => f.Key)
            .ToList();

        availableKeys.Should().NotContain("Email");
        availableKeys.Should().Contain("SocialSecurityNumber");
    }

    [Fact]
    public async Task Remove_PersistsChange_AndRaisesOnPersisted()
    {
        var role = new RoleDefinition
        {
            RoleName = "BackofficeUser",
            ColumnRestrictions =
            [
                new ColumnRestriction { EntityKey = "Product", ColumnKey = "UnitPrice", RestrictionKind = ColumnRestrictionKind.ViewOnly },
            ],
        };
        var (ctx, manager) = Build(role);
        using var _1 = ctx;

        var persistedCount = 0;
        var cut = ctx.RenderComponent<ColumnRestrictionsSubTab>(p => p
            .Add(c => c.Role, role)
            .Add(c => c.OnPersisted, () => { persistedCount++; return Task.CompletedTask; }));

        cut.Find("[data-testid='column-restriction-remove-Product-UnitPrice']").Click();

        await Task.Yield();

        role.ColumnRestrictions.Should().BeEmpty();
        persistedCount.Should().Be(1);
    }
}
