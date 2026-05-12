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

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<object, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ExpandedHeaderText, "Roles")
            .Add(c => c.IsExpanded, true)
            .Add(c => c.GridContent, builder =>
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

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<object, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ExpandedHeaderText, "Roles")
            .Add(c => c.CollapsedSummaryText, "Role: BackofficeUser")
            .Add(c => c.CollapsedSummaryBadge, "baked-in")
            .Add(c => c.IsExpanded, false)
            .Add(c => c.GridContent, builder =>
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

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<object, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ExpandedHeaderText, "Roles")
            .Add(c => c.CollapsedSummaryText, "Role: BackofficeUser")
            .Add(c => c.IsExpanded, false)
            .Add(c => c.GridContent, builder =>
                builder.AddMarkupContent(0, "<div data-testid='inner-results'>inner</div>")));

        cut.Find("[data-testid='test-shell-summary']")
            .TextContent.Should().Contain("Role: BackofficeUser");

        cut.FindAll("[data-testid='test-shell-summary-badge']").Should().BeEmpty();
    }

    [Fact]
    public void Shell_Collapsed_BlankSummary_FallsBackToExpandedHeaderText()
    {
        using var ctx = Build();

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<object, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ExpandedHeaderText, "Roles")
            .Add(c => c.CollapsedSummaryText, string.Empty)
            .Add(c => c.IsExpanded, false)
            .Add(c => c.GridContent, builder =>
                builder.AddMarkupContent(0, "<div>inner</div>")));

        cut.Find("[data-testid='test-shell-summary']")
            .TextContent.Should().Contain("Roles");
    }

    [Fact]
    public void Shell_FilterContent_RendersInsideExpansionBody()
    {
        using var ctx = Build();

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<object, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ExpandedHeaderText, "Results")
            .Add(c => c.IsExpanded, true)
            .Add(c => c.FilterContent, builder =>
                builder.AddMarkupContent(0, "<div data-testid='filter-region'>filter</div>"))
            .Add(c => c.GridContent, builder =>
                builder.AddMarkupContent(0, "<div data-testid='inner-results'>grid</div>")));

        var body = cut.Find("[data-testid='test-shell-body']");
        body.QuerySelector("[data-testid='filter-region']").Should().NotBeNull();
        body.QuerySelector("[data-testid='inner-results']").Should().NotBeNull();
    }

    [Fact]
    public void Shell_DetailsContent_RendersOutsideExpansionPanel()
    {
        using var ctx = Build();

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<object, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ExpandedHeaderText, "Results")
            .Add(c => c.IsExpanded, true)
            .Add(c => c.GridContent, builder =>
                builder.AddMarkupContent(0, "<div data-testid='inner-results'>grid</div>"))
            .Add(c => c.DetailsContent, builder =>
                builder.AddMarkupContent(0, "<div data-testid='details-form'>details</div>")));

        // details are in their own wrapper outside the expansion panel
        cut.Find("[data-testid='test-shell-details']").Should().NotBeNull();
        cut.Find("[data-testid='test-shell-details']")
            .QuerySelector("[data-testid='details-form']").Should().NotBeNull();
    }

    [Fact]
    public void Shell_ShowGridFalse_DoesNotRenderExpansionPanel()
    {
        using var ctx = Build();

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<object, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ExpandedHeaderText, "Results")
            .Add(c => c.ShowGrid, false));

        // No expansion panel chrome rendered
        cut.FindAll("[data-testid='test-shell-body']").Should().BeEmpty();
        cut.FindAll("[data-testid='test-shell-header']").Should().BeEmpty();

        // The outer div with add-only class is present
        cut.Find("[data-testid='test-shell']").Should().NotBeNull();
    }

    [Fact]
    public void Shell_ShowAddActionFalse_RendersWithoutError()
    {
        using var ctx = Build();

        // Structural acceptance test: the Add button lives in the consumer's
        // GridContent (RoleMasterGrid), not in the shell itself for Session 1.
        var cut = ctx.RenderComponent<MasterDetailExpansionShell<object, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ShowAddAction, false));

        cut.Find("[data-testid='test-shell']").Should().NotBeNull();
    }

    [Fact]
    public void Shell_OnRowSelectedCallback_AcceptedWithoutError()
    {
        using var ctx = Build();

        // Structural acceptance test: OnRowSelected is a declared output callback;
        // the shell accepts it without rendering errors.
        var cut = ctx.RenderComponent<MasterDetailExpansionShell<object, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.OnRowSelected, EventCallback.Factory.Create<object>(this, _ => { })));

        cut.Find("[data-testid='test-shell']").Should().NotBeNull();
    }

    [Fact]
    public void Shell_Collapsed_CollapsedSummaryContent_OverridesTextAndBadge()
    {
        using var ctx = Build();

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<object, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ExpandedHeaderText, "Roles")
            .Add(c => c.CollapsedSummaryText, "Role: BackofficeUser")
            .Add(c => c.CollapsedSummaryBadge, "baked-in")
            .Add(c => c.IsExpanded, false)
            .Add(c => c.CollapsedSummaryContent, builder =>
                builder.AddMarkupContent(0, "<div data-testid='custom-summary'>Custom Summary</div>")));

        cut.Find("[data-testid='custom-summary']").TextContent.Should().Be("Custom Summary");

        // Default text + badge are suppressed by CollapsedSummaryContent
        cut.FindAll("[data-testid='test-shell-summary']").Should().BeEmpty();
        cut.FindAll("[data-testid='test-shell-summary-badge']").Should().BeEmpty();
    }

    [Fact]
    public void Shell_Collapsed_StillRendersGridContentInDom()
    {
        using var ctx = Build();

        // MudExpansionPanel hides child content via CSS, not by removing it from
        // the DOM. GridContent must remain present so component lifecycles work.
        var cut = ctx.RenderComponent<MasterDetailExpansionShell<object, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.IsExpanded, false)
            .Add(c => c.GridContent, builder =>
                builder.AddMarkupContent(0, "<div data-testid='inner-grid'>grid</div>")));

        cut.Find("[data-testid='inner-grid']").Should().NotBeNull();
    }

    // ── Session 2: ShowGrid=false / Add-Only layout tests ──────────────────

    [Fact]
    public void Shell_ShowGridFalse_DetailsContent_RenderedInMudPaper()
    {
        using var ctx = Build();

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<object, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ShowGrid, false)
            .Add(c => c.DetailsContent, builder =>
                builder.AddMarkupContent(0, "<div data-testid='inner-details'>details</div>")));

        var detailsPaper = cut.Find("[data-testid='test-shell-details']");
        detailsPaper.Should().NotBeNull();
        detailsPaper.QuerySelector("[data-testid='inner-details']").Should().NotBeNull();

        cut.FindAll("[data-testid='test-shell-header']").Should().BeEmpty();
        cut.FindAll("[data-testid='test-shell-body']").Should().BeEmpty();
    }

    [Fact]
    public void Shell_ShowGridFalse_DefaultSaveAndCancelButtons_Rendered()
    {
        using var ctx = Build();

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<object, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ShowGrid, false)
            .Add(c => c.DetailsContent, _ => { }));

        cut.Find("[data-testid='test-shell-save-button']").Should().NotBeNull();
        cut.Find("[data-testid='test-shell-cancel-button']").Should().NotBeNull();
    }

    [Fact]
    public void Shell_ShowGridFalse_SaveButton_IncrementsSessionCount()
    {
        using var ctx = Build();

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<object, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ShowGrid, false)
            .Add(c => c.DetailsContent, _ => { })
            .Add(c => c.SessionSummaryContent, (count) => builder =>
                builder.AddContent(0, $"Count: {count}"))
            .Add(c => c.OnSaveRequested, EventCallback.Factory.Create<SaveRequest<object, object>>(
                this, _ => Task.CompletedTask)));

        cut.Find("[data-testid='test-shell-save-button']").Click();

        var summary = cut.Find("[data-testid='test-shell-session-summary']");
        summary.TextContent.Should().Contain("Count: 1");
    }

    [Fact]
    public void Shell_ShowGridFalse_CancelButton_RaisesOnCancelRequested()
    {
        using var ctx = Build();
        var cancelRequested = false;

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<object, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ShowGrid, false)
            .Add(c => c.DetailsContent, _ => { })
            .Add(c => c.OnCancelRequested, EventCallback.Factory.Create(this, () =>
            {
                cancelRequested = true;
            })));

        cut.Find("[data-testid='test-shell-cancel-button']").Click();

        cancelRequested.Should().BeTrue();
    }

    [Fact]
    public void Shell_ShowGridFalse_SessionSummaryContent_NotRendered_WhenNotProvided()
    {
        using var ctx = Build();

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<object, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ShowGrid, false));

        cut.FindAll("[data-testid='test-shell-session-summary']").Should().BeEmpty();
    }
}
