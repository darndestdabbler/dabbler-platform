using Bunit;
using Dabbler.Security.Blazor.Components.Admin.Shared;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Dabbler.Security.Blazor.Tests.Components.Admin.Shared;

public class MasterDetailExpansionShellTests
{
    private static TestContext Build()
    {
        var ctx = new TestContext();
        ctx.SetupForDabbler();
        return ctx;
    }

    [Fact]
    public void Shell_Expanded_RendersExpandedHeaderText()
    {
        using var ctx = Build();

        var cut = ctx.RenderComponent<MasterDetailExpansionShell>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ExpandedHeaderText, "Roles")
            .Add(c => c.IsExpanded, true)
            .Add(c => c.ResultsContent, builder =>
                builder.AddMarkupContent(0, "<div data-testid='inner-results'>inner</div>")));

        var header = cut.Find("[data-testid='test-shell-header']");
        header.TextContent.Should().Contain("Roles");

        // The expanded header does not render the summary / badge test ids.
        cut.FindAll("[data-testid='test-shell-summary']").Should().BeEmpty();
        cut.FindAll("[data-testid='test-shell-summary-badge']").Should().BeEmpty();

        cut.Find("[data-testid='inner-results']").Should().NotBeNull();
    }

    [Fact]
    public void Shell_Collapsed_RendersSummaryTextAndOptionalBadge()
    {
        using var ctx = Build();

        var cut = ctx.RenderComponent<MasterDetailExpansionShell>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ExpandedHeaderText, "Roles")
            .Add(c => c.CollapsedSummaryText, "Role: BackofficeUser")
            .Add(c => c.CollapsedSummaryBadge, "baked-in")
            .Add(c => c.IsExpanded, false)
            .Add(c => c.ResultsContent, builder =>
                builder.AddMarkupContent(0, "<div data-testid='inner-results'>inner</div>")));

        var summary = cut.Find("[data-testid='test-shell-summary']");
        summary.TextContent.Should().Contain("Role: BackofficeUser");

        var badge = cut.Find("[data-testid='test-shell-summary-badge']");
        badge.TextContent.Should().Contain("baked-in");
    }

    [Fact]
    public void Shell_Collapsed_NoBadge_WhenBadgeParameterIsBlank()
    {
        using var ctx = Build();

        var cut = ctx.RenderComponent<MasterDetailExpansionShell>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ExpandedHeaderText, "Roles")
            .Add(c => c.CollapsedSummaryText, "Role: BackofficeUser")
            .Add(c => c.IsExpanded, false)
            .Add(c => c.ResultsContent, builder =>
                builder.AddMarkupContent(0, "<div data-testid='inner-results'>inner</div>")));

        cut.Find("[data-testid='test-shell-summary']")
            .TextContent.Should().Contain("Role: BackofficeUser");

        cut.FindAll("[data-testid='test-shell-summary-badge']").Should().BeEmpty();
    }

    [Fact]
    public void Shell_Collapsed_BlankSummary_FallsBackToExpandedHeaderText()
    {
        using var ctx = Build();

        var cut = ctx.RenderComponent<MasterDetailExpansionShell>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ExpandedHeaderText, "Roles")
            .Add(c => c.CollapsedSummaryText, string.Empty)
            .Add(c => c.IsExpanded, false)
            .Add(c => c.ResultsContent, builder =>
                builder.AddMarkupContent(0, "<div>inner</div>")));

        cut.Find("[data-testid='test-shell-summary']")
            .TextContent.Should().Contain("Roles");
    }
}
