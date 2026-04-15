using Bunit;
using Dabbler.Components.Data.Metadata;
using Dabbler.Components.Tests.TestHelpers;
using Dabbler.Platform.Stubs;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dabbler.Components.Tests.Shell;

public class DabblerShellTests
{
    [Fact]
    public void WithoutCatalogProviders_ShellKeepsCoreFrameAndOmitsTopMenu()
    {
        using var ctx = new TestContext().SetupForDabblerComponents();

        var cut = RenderShell(ctx, includeCatalogProviders: false);

        cut.FindAll(".dabbler-top-menu-bar").Should().BeEmpty();
        cut.Find("a.dabbler-skip-link").GetAttribute("href").Should().Be("#dabbler-main-content");
        cut.Find("a.dabbler-skip-link").TextContent.Should().Contain("Skip to main content");
        cut.Find("main.dabbler-shell-workspace").GetAttribute("aria-label")
            .Should().Be("Contract Shell workspace");
    }

    [Fact]
    public void WithCatalogProviders_ShellRendersTopMenu()
    {
        using var ctx = new TestContext().SetupForDabblerComponents();

        var cut = RenderShell(ctx, includeCatalogProviders: true);

        cut.FindAll(".dabbler-top-menu-bar").Should().ContainSingle();
        cut.Markup.Should().Contain("Contract Shell");
    }

    [Fact]
    public void SkipLink_UsesCurrentRouteWhenRenderedUnderNestedPath()
    {
        using var ctx = new TestContext().SetupForDabblerComponents();
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/platform/stub");

        var cut = RenderShell(ctx, includeCatalogProviders: false);

        cut.Find("a.dabbler-skip-link").GetAttribute("href")
            .Should().Be("/platform/stub#dabbler-main-content");
    }

    [Fact]
    public void ReportItems_RenderInNavigationPanelWhenProvided()
    {
        using var ctx = new TestContext().SetupForDabblerComponents();

        var cut = RenderShell(ctx, includeCatalogProviders: false, includeReportItems: true);

        cut.Markup.Should().Contain("Reports");
        cut.Markup.Should().Contain("Stub Customer Summary");
    }

    private static IRenderedComponent<DabblerShell> RenderShell(
        TestContext ctx,
        bool includeCatalogProviders,
        bool includeReportItems = false)
    {
        var definition = new StubPlatformDefinition
        {
            Entities =
            [
                new EntityMetadata
                {
                    EntityName = "Customer",
                    DisplayName = "Customers",
                    DisplayNameSingular = "Customer"
                }
            ],
            CatalogEntries =
            [
                new ArtifactCatalogEntry
                {
                    ArtifactId = "Customer",
                    ArtifactType = ArtifactType.Form,
                    Name = "Customers"
                }
            ],
            UsageRecords =
            [
                new ArtifactUsageRecord
                {
                    UserId = "contract-user",
                    ArtifactType = ArtifactType.Form,
                    ArtifactId = "Customer",
                    AccessCount = 1,
                    LastAccessedAt = DateTime.UtcNow
                }
            ],
            ReportItems =
            [
                new NavigationReportItem("customer-summary", "Stub Customer Summary")
            ]
        };

        return ctx.RenderComponent<DabblerShell>(parameters =>
        {
            parameters
                .Add(x => x.MetadataProvider, new StubEntityMetadataProvider(definition.Entities))
                .Add(x => x.NavigationDataProvider, new StubNavigationDataProvider(definition.Navigation))
                .Add(x => x.DetailFormFactory, CreateDetailFragment)
                .Add(x => x.SearchPageFactory, CreateSearchFragment)
                .Add(x => x.NewRecordFactory, CreateNewFragment)
                .Add(x => x.Title, "Contract Shell");

            if (includeReportItems)
            {
                parameters.Add(x => x.ReportItems, definition.ReportItems);
            }

            if (includeCatalogProviders)
            {
                parameters
                    .Add(x => x.CatalogProvider, new StubArtifactCatalogProvider(definition.CatalogEntries))
                    .Add(x => x.UsageTrackingProvider, new StubUsageTrackingProvider(definition.UsageRecords))
                    .Add(x => x.UserId, "contract-user");
            }
        });
    }

    private static RenderFragment CreateDetailFragment(string entityName, object primaryKey) => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddContent(1, $"{entityName}:{primaryKey}");
        builder.CloseElement();
    };

    private static RenderFragment CreateSearchFragment(string entityName, SmartFilterDefinition? _) => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddContent(1, $"{entityName} search");
        builder.CloseElement();
    };

    private static RenderFragment CreateNewFragment(string entityName) => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddContent(1, $"new {entityName}");
        builder.CloseElement();
    };
}
