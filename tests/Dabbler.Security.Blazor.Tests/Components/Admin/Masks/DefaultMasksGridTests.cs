using Bunit;
using Dabbler.Components.Data;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Blazor.Components.Admin.Masks;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Configuration;
using Dabbler.Security.Registration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Entity = Dabbler.Security.Registration.RegisteredEntity;
using Field = Dabbler.Security.Registration.RegisteredField;

namespace Dabbler.Security.Blazor.Tests.Components.Admin.Masks;

public class DefaultMasksGridTests
{
    private static (TestContext Ctx, SecurityConfigurationManager Manager) Build(
        SecurityConfiguration config)
    {
        var ctx = new TestContext();
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        store.Setup(s => s.SaveAsync(It.IsAny<SecurityConfiguration>(), default))
             .Returns(Task.CompletedTask);
        var manager = new SecurityConfigurationManager(store.Object);

        var metadata = new MetadataRegistry(new ResourceRegistry([], new[]
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
                Key = "Employee",
                DisplayName = "Employees",
                Fields = new[]
                {
                    new Field { Key = "Salary", DisplayName = "Salary" },
                },
            },
        }, [], []));

        ctx.SetupForDabbler(configureServices: svc =>
        {
            svc.AddSingleton(manager);
            svc.AddSingleton(metadata);
        });
        manager.EnsureLoadedAsync().Wait();
        return (ctx, manager);
    }

    [Fact]
    public void Empty_ShowsEmptyMessage()
    {
        var (ctx, _) = Build(new SecurityConfiguration());
        using var _1 = ctx;

        var cut = ctx.RenderComponent<DefaultMasksGrid>();

        cut.Find("[data-testid='default-mask-empty']").Should().NotBeNull();
    }

    [Fact]
    public void WithRows_RendersGridAndCount()
    {
        var config = new SecurityConfiguration
        {
            DefaultMasks =
            [
                new DefaultMaskRule { EntityKey = "Customer", ColumnKey = "SocialSecurityNumber", MaskFormat = "XXX-XX-{last4}" },
                new DefaultMaskRule { EntityKey = "Employee", ColumnKey = "Salary", MaskFormat = string.Empty },
            ],
        };
        var (ctx, _) = Build(config);
        using var _1 = ctx;

        var cut = ctx.RenderComponent<DefaultMasksGrid>();

        cut.Find("[data-testid='default-mask-grid']").Should().NotBeNull();
        cut.Find("[data-testid='default-mask-remove-Customer-SocialSecurityNumber']").Should().NotBeNull();
        cut.Find("[data-testid='default-mask-remove-Employee-Salary']").Should().NotBeNull();
        cut.Find("[data-testid='default-mask-count']").TextContent.Should().Contain("2 of 2");
    }

    [Fact]
    public async Task Remove_PersistsAndRaisesOnPersisted()
    {
        var config = new SecurityConfiguration
        {
            DefaultMasks =
            [
                new DefaultMaskRule { EntityKey = "Customer", ColumnKey = "SocialSecurityNumber", MaskFormat = "XXX-XX-{last4}" },
            ],
        };
        var (ctx, _) = Build(config);
        using var _1 = ctx;

        var persistedCount = 0;
        var cut = ctx.RenderComponent<DefaultMasksGrid>(p =>
            p.Add(c => c.OnPersisted, () => { persistedCount++; return Task.CompletedTask; }));

        cut.Find("[data-testid='default-mask-remove-Customer-SocialSecurityNumber']").Click();
        await Task.Yield();

        config.DefaultMasks.Should().BeEmpty();
        persistedCount.Should().Be(1);
    }

    [Fact]
    public async Task AddFromSafetyNet_PrefillsAndPersistsNewRule()
    {
        var config = new SecurityConfiguration();
        var (ctx, _) = Build(config);
        using var _1 = ctx;

        var cut = ctx.RenderComponent<DefaultMasksGrid>();

        await cut.InvokeAsync(() => cut.Instance.AddFromSafetyNetAsync("Customer", "SocialSecurityNumber"));

        config.DefaultMasks.Should().ContainSingle();
        var rule = config.DefaultMasks[0];
        rule.EntityKey.Should().Be("Customer");
        rule.ColumnKey.Should().Be("SocialSecurityNumber");
        rule.MaskFormat.Should().Be(string.Empty);
    }

    [Fact]
    public async Task Filter_ByTableKey_NarrowsVisibleRows()
    {
        var config = new SecurityConfiguration
        {
            DefaultMasks =
            [
                new DefaultMaskRule { EntityKey = "Customer", ColumnKey = "SocialSecurityNumber", MaskFormat = "XXX-XX-{last4}" },
                new DefaultMaskRule { EntityKey = "Customer", ColumnKey = "Email", MaskFormat = string.Empty },
                new DefaultMaskRule { EntityKey = "Employee", ColumnKey = "Salary", MaskFormat = string.Empty },
            ],
        };
        var (ctx, _) = Build(config);
        using var _1 = ctx;

        var cut = ctx.RenderComponent<DefaultMasksGrid>();

        // Sanity: unfiltered state shows all three rows.
        cut.Find("[data-testid='default-mask-count']").TextContent.Should().Contain("3 of 3");
        cut.Find("[data-testid='default-mask-remove-Customer-SocialSecurityNumber']").Should().NotBeNull();
        cut.Find("[data-testid='default-mask-remove-Employee-Salary']").Should().NotBeNull();

        // Apply a Table filter via the OnFilterChanged callback (same entry
        // point the FilterPanel uses at runtime).
        var descriptor = new CompositeFilterDescriptor
        {
            Filters =
            [
                new FilterDescriptor(nameof(DefaultMaskRule.EntityKey), FilterOperators.Contains, "Customer"),
            ],
        };
        await cut.InvokeAsync(() => cut.Instance.OnFilterChanged(descriptor));

        cut.Find("[data-testid='default-mask-count']").TextContent.Should().Contain("2 of 3");
        cut.Find("[data-testid='default-mask-remove-Customer-SocialSecurityNumber']").Should().NotBeNull();
        cut.Find("[data-testid='default-mask-remove-Customer-Email']").Should().NotBeNull();
        cut.FindAll("[data-testid='default-mask-remove-Employee-Salary']").Should().BeEmpty();

        // Clearing the filter restores the full set.
        await cut.InvokeAsync(() => cut.Instance.OnFilterChanged(null));
        cut.Find("[data-testid='default-mask-count']").TextContent.Should().Contain("3 of 3");
    }

    [Fact]
    public async Task AddFromSafetyNet_IsIdempotent_DoesNotDuplicateExistingRule()
    {
        var config = new SecurityConfiguration
        {
            DefaultMasks =
            [
                new DefaultMaskRule { EntityKey = "Customer", ColumnKey = "SocialSecurityNumber", MaskFormat = "XXX-XX-{last4}" },
            ],
        };
        var (ctx, _) = Build(config);
        using var _1 = ctx;

        var cut = ctx.RenderComponent<DefaultMasksGrid>();

        await cut.InvokeAsync(() => cut.Instance.AddFromSafetyNetAsync("Customer", "SocialSecurityNumber"));

        config.DefaultMasks.Should().ContainSingle();
        config.DefaultMasks[0].MaskFormat.Should().Be("XXX-XX-{last4}");
    }
}
