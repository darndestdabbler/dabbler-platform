using Bunit;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Blazor.Components.Admin.Masks;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Configuration;
using Dabbler.Security.Registration;
using Dabbler.Security.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Entity = Dabbler.Security.Registration.RegisteredEntity;
using Field = Dabbler.Security.Registration.RegisteredField;

namespace Dabbler.Security.Blazor.Tests.Components.Admin.Masks;

public class DefaultMasksTabTests
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
                    new Field { Key = "DateOfBirth", DisplayName = "DOB" },
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
    public void Renders_HeaderAndNestedComponents()
    {
        var (ctx, _) = Build(new SecurityConfiguration());
        using var _1 = ctx;

        var cut = ctx.RenderComponent<DefaultMasksTab>();

        cut.Find("[data-testid='default-masks-tab']").Should().NotBeNull();
        cut.Find("[data-testid='sensitive-name-safety-net-container']").Should().NotBeNull();
        cut.Find("[data-testid='default-masks-grid-container']").Should().NotBeNull();
    }

    [Fact]
    public async Task SafetyNet_ClickAddToDefaultMasks_AddsRuleThroughGrid()
    {
        var config = new SecurityConfiguration();
        var (ctx, _) = Build(config);
        using var _1 = ctx;

        var cut = ctx.RenderComponent<DefaultMasksTab>();

        // Pre-condition: safety net shows the SSN column as undeclared.
        cut.Find("[data-testid='safety-net-add-Customer-SocialSecurityNumber']").Click();
        await Task.Yield();

        config.DefaultMasks.Should().ContainSingle();
        var rule = config.DefaultMasks[0];
        rule.EntityKey.Should().Be("Customer");
        rule.ColumnKey.Should().Be("SocialSecurityNumber");

        // Post-condition: the safety-net panel rescans and no longer flags the
        // just-declared column.
        cut.Render();
        cut.FindAll("[data-testid='safety-net-add-Customer-SocialSecurityNumber']").Should().BeEmpty();
        // The DOB column is still in the safety net since it was not declared.
        cut.Find("[data-testid='safety-net-add-Customer-DateOfBirth']").Should().NotBeNull();
    }

    [Fact]
    public async Task RemoveRule_RescansSafetyNet_FlagsColumnAgain()
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

        var cut = ctx.RenderComponent<DefaultMasksTab>();

        // Pre-condition: SSN column is not in the safety net (already declared).
        cut.FindAll("[data-testid='safety-net-add-Customer-SocialSecurityNumber']").Should().BeEmpty();

        cut.Find("[data-testid='default-mask-remove-Customer-SocialSecurityNumber']").Click();
        await Task.Yield();

        config.DefaultMasks.Should().BeEmpty();

        // Post-condition: SSN column is flagged again.
        cut.Render();
        cut.Find("[data-testid='safety-net-add-Customer-SocialSecurityNumber']").Should().NotBeNull();
    }

    [Fact]
    public async Task ExternalConfigChange_RescansSafetyNet_InCircuit()
    {
        var config = new SecurityConfiguration();
        var (ctx, manager) = Build(config);
        using var _1 = ctx;

        var cut = ctx.RenderComponent<DefaultMasksTab>();
        // SSN is flagged initially.
        cut.Find("[data-testid='safety-net-add-Customer-SocialSecurityNumber']").Should().NotBeNull();

        // Simulate a sibling tab (e.g., Role Master Details' Mask Exemptions
        // sub-tab) persisting a DefaultMask change.
        await cut.InvokeAsync(async () =>
        {
            config.DefaultMasks.Add(new DefaultMaskRule
            {
                EntityKey = "Customer",
                ColumnKey = "SocialSecurityNumber",
                MaskFormat = "XXX-XX-{last4}",
            });
            await manager.SaveAsync(config);
        });

        cut.FindAll("[data-testid='safety-net-add-Customer-SocialSecurityNumber']").Should().BeEmpty();
    }
}
