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
                DisplayName = "Customer",
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
    public void Renders_HeaderAndGrid_WithoutSafetyNet()
    {
        var (ctx, _) = Build(new SecurityConfiguration());
        using var _1 = ctx;

        var cut = ctx.RenderComponent<DefaultMasksTab>();

        cut.Find("[data-testid='default-masks-tab']").Should().NotBeNull();
        cut.Find("[data-testid='default-masks-grid-container']").Should().NotBeNull();

        // Safety-net feature was removed in session 6 of
        // security-dashboard-uat-remediation — the container must not appear.
        cut.FindAll("[data-testid='sensitive-name-safety-net-container']").Should().BeEmpty();
        cut.FindAll("[data-testid='sensitive-name-safety-net-panel']").Should().BeEmpty();
        cut.FindAll("[data-testid='sensitive-name-safety-net-clear']").Should().BeEmpty();
    }

}
