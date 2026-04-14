using Bunit;
using Dabbler.Components.Data.Metadata;
using Dabbler.Components.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
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

    private static IRenderedComponent<DabblerShell> RenderShell(TestContext ctx, bool includeCatalogProviders)
    {
        return ctx.RenderComponent<DabblerShell>(parameters =>
        {
            parameters
                .Add(x => x.MetadataProvider, new StubMetadataProvider())
                .Add(x => x.NavigationDataProvider, new StubNavigationDataProvider())
                .Add(x => x.DetailFormFactory, CreateDetailFragment)
                .Add(x => x.SearchPageFactory, CreateSearchFragment)
                .Add(x => x.NewRecordFactory, CreateNewFragment)
                .Add(x => x.Title, "Contract Shell");

            if (includeCatalogProviders)
            {
                parameters
                    .Add(x => x.CatalogProvider, new StubCatalogProvider())
                    .Add(x => x.UsageTrackingProvider, new StubUsageTrackingProvider())
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

    private sealed class StubMetadataProvider : IEntityMetadataProvider
    {
        public EntityMetadata? GetEntityMetadata(string entityName) => null;

        public IReadOnlyList<EntityMetadata> GetAllEntities() => [];

        public IReadOnlyList<EntityMetadata> GetNavigableEntities() => [];
    }

    private sealed class StubNavigationDataProvider : INavigationDataProvider
    {
        public Task<int> GetSmartFilterCountAsync(string entityName, SmartFilterDefinition filter, object? context = null) =>
            Task.FromResult(0);

        public Task<IReadOnlyList<NavigationRecord>> GetSmartFilterRecordsAsync(
            string entityName,
            SmartFilterDefinition filter,
            int topN,
            object? context = null) =>
            Task.FromResult<IReadOnlyList<NavigationRecord>>([]);
    }

    private sealed class StubCatalogProvider : IArtifactCatalogProvider
    {
        public Task<IReadOnlyList<ArtifactCatalogEntry>> GetArtifactsAsync(ArtifactType artifactType, string userId) =>
            Task.FromResult<IReadOnlyList<ArtifactCatalogEntry>>([]);

        public Task<IReadOnlyList<ArtifactCatalogEntry>> SearchArtifactsAsync(
            ArtifactType artifactType,
            string userId,
            string searchText) =>
            Task.FromResult<IReadOnlyList<ArtifactCatalogEntry>>([]);
    }

    private sealed class StubUsageTrackingProvider : IUsageTrackingProvider
    {
        public Task<IReadOnlyList<ArtifactUsageRecord>> GetFrequentAsync(string userId, ArtifactType artifactType, int maxItems = 7) =>
            Task.FromResult<IReadOnlyList<ArtifactUsageRecord>>([]);

        public Task<IReadOnlyList<ArtifactUsageRecord>> GetPinnedAsync(string userId, ArtifactType artifactType) =>
            Task.FromResult<IReadOnlyList<ArtifactUsageRecord>>([]);

        public Task<IReadOnlyList<ArtifactUsageRecord>> GetRecentAsync(string userId, ArtifactType artifactType, int maxItems = 7) =>
            Task.FromResult<IReadOnlyList<ArtifactUsageRecord>>([]);

        public Task SetPinnedAsync(string userId, ArtifactType artifactType, string artifactId, bool isPinned) =>
            Task.CompletedTask;

        public Task TrackAccessAsync(string userId, ArtifactType artifactType, string artifactId) =>
            Task.CompletedTask;
    }
}
