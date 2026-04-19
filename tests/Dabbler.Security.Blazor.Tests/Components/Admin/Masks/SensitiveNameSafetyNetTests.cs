using Bunit;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Blazor.Components.Admin.Masks;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Configuration;
using Dabbler.Security.Registration;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Entity = Dabbler.Security.Registration.RegisteredEntity;
using Field = Dabbler.Security.Registration.RegisteredField;

namespace Dabbler.Security.Blazor.Tests.Components.Admin.Masks;

public class SensitiveNameSafetyNetTests
{
    private static (TestContext Ctx, SecurityConfigurationManager Manager) Build(
        SecurityConfiguration config,
        MetadataRegistry metadata)
    {
        var ctx = new TestContext();
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        store.Setup(s => s.SaveAsync(It.IsAny<SecurityConfiguration>(), default))
             .Returns(Task.CompletedTask);
        var manager = new SecurityConfigurationManager(store.Object);

        ctx.SetupForDabbler(configureServices: svc =>
        {
            svc.AddSingleton(manager);
            svc.AddSingleton(metadata);
        });
        manager.EnsureLoadedAsync().Wait();
        return (ctx, manager);
    }

    private static MetadataRegistry BuildMetadata() =>
        new(new ResourceRegistry([], new[]
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
                    new Field { Key = "Password", DisplayName = "Password" },
                },
            },
            new Entity
            {
                Key = "Employee",
                DisplayName = "Employees",
                Fields = new[]
                {
                    new Field { Key = "Salary", DisplayName = "Annual Salary" },
                    new Field { Key = "TaxIdNumber", DisplayName = "Tax ID" },
                    new Field { Key = "CcNumber", DisplayName = "Card Number" },
                    new Field { Key = "FirstName", DisplayName = "First Name" },
                },
            },
        }, [], []));

    [Fact]
    public void NoMatches_RendersClearAlert()
    {
        var config = new SecurityConfiguration();
        var metadata = new MetadataRegistry(new ResourceRegistry([], new[]
        {
            new Entity
            {
                Key = "Product",
                DisplayName = "Products",
                Fields = new[]
                {
                    new Field { Key = "Name", DisplayName = "Name" },
                    new Field { Key = "UnitPrice", DisplayName = "Unit Price" },
                },
            },
        }, [], []));

        var (ctx, _) = Build(config, metadata);
        using var _1 = ctx;

        var cut = ctx.RenderComponent<SensitiveNameSafetyNet>(p =>
            p.Add(c => c.OnAddMatch, EventCallback.Factory.Create<SensitiveNameSafetyNet.SensitiveMatch>(this, _ => Task.CompletedTask)));

        cut.Find("[data-testid='sensitive-name-safety-net-clear']").Should().NotBeNull();
    }

    [Fact]
    public void UndeclaredSensitiveColumns_RenderInGrid()
    {
        var config = new SecurityConfiguration();
        var metadata = BuildMetadata();

        var (ctx, _) = Build(config, metadata);
        using var _1 = ctx;

        var cut = ctx.RenderComponent<SensitiveNameSafetyNet>(p =>
            p.Add(c => c.OnAddMatch, EventCallback.Factory.Create<SensitiveNameSafetyNet.SensitiveMatch>(this, _ => Task.CompletedTask)));

        cut.Find("[data-testid='sensitive-name-safety-net-panel']").Should().NotBeNull();
        cut.Find("[data-testid='safety-net-add-Customer-SocialSecurityNumber']").Should().NotBeNull();
        cut.Find("[data-testid='safety-net-add-Customer-DateOfBirth']").Should().NotBeNull();
        cut.Find("[data-testid='safety-net-add-Customer-Password']").Should().NotBeNull();
        cut.Find("[data-testid='safety-net-add-Employee-Salary']").Should().NotBeNull();
        cut.Find("[data-testid='safety-net-add-Employee-TaxIdNumber']").Should().NotBeNull();
        cut.Find("[data-testid='safety-net-add-Employee-CcNumber']").Should().NotBeNull();
    }

    [Fact]
    public void DeclaredSensitiveColumns_AreExcluded()
    {
        var config = new SecurityConfiguration
        {
            DefaultMasks =
            [
                new DefaultMaskRule { EntityKey = "Customer", ColumnKey = "SocialSecurityNumber", MaskFormat = "XXX-XX-{last4}" },
                new DefaultMaskRule { EntityKey = "Customer", ColumnKey = "Password", MaskFormat = string.Empty },
            ],
        };
        var metadata = BuildMetadata();

        var (ctx, _) = Build(config, metadata);
        using var _1 = ctx;

        var cut = ctx.RenderComponent<SensitiveNameSafetyNet>(p =>
            p.Add(c => c.OnAddMatch, EventCallback.Factory.Create<SensitiveNameSafetyNet.SensitiveMatch>(this, _ => Task.CompletedTask)));

        cut.FindAll("[data-testid='safety-net-add-Customer-SocialSecurityNumber']").Should().BeEmpty();
        cut.FindAll("[data-testid='safety-net-add-Customer-Password']").Should().BeEmpty();

        cut.Find("[data-testid='safety-net-add-Customer-DateOfBirth']").Should().NotBeNull();
        cut.Find("[data-testid='safety-net-add-Employee-Salary']").Should().NotBeNull();
    }

    [Fact]
    public void NonSensitiveColumns_AreIgnored()
    {
        var config = new SecurityConfiguration();
        var metadata = BuildMetadata();

        var (ctx, _) = Build(config, metadata);
        using var _1 = ctx;

        var cut = ctx.RenderComponent<SensitiveNameSafetyNet>(p =>
            p.Add(c => c.OnAddMatch, EventCallback.Factory.Create<SensitiveNameSafetyNet.SensitiveMatch>(this, _ => Task.CompletedTask)));

        cut.FindAll("[data-testid='safety-net-add-Customer-Email']").Should().BeEmpty();
        cut.FindAll("[data-testid='safety-net-add-Employee-FirstName']").Should().BeEmpty();
    }

    [Fact]
    public async Task ClickAddButton_RaisesOnAddMatchWithMatchDetails()
    {
        var config = new SecurityConfiguration();
        var metadata = BuildMetadata();

        var (ctx, _) = Build(config, metadata);
        using var _1 = ctx;

        SensitiveNameSafetyNet.SensitiveMatch? captured = null;
        var cut = ctx.RenderComponent<SensitiveNameSafetyNet>(p =>
            p.Add(c => c.OnAddMatch, EventCallback.Factory.Create<SensitiveNameSafetyNet.SensitiveMatch>(
                this,
                m => { captured = m; return Task.CompletedTask; })));

        cut.Find("[data-testid='safety-net-add-Customer-SocialSecurityNumber']").Click();
        await Task.Yield();

        captured.Should().NotBeNull();
        captured!.EntityKey.Should().Be("Customer");
        captured.ColumnKey.Should().Be("SocialSecurityNumber");
        captured.Pattern.Should().Be("ssn");
    }
}
