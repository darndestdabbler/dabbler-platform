using Bunit;
using Dabbler.Components.MasterDetail;
using Dabbler.Components.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dabbler.Components.Tests.MasterDetail;

public class MasterDetailExpansionShellTests
{
    private static TestContext Build()
    {
        var ctx = new TestContext();
        ctx.SetupForDabblerComponents();
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
    public void Shell_ShowAddAction_DefaultsToNoAddButton()
    {
        using var ctx = Build();

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<object, object, object>>(p => p
            .Add(c => c.TestId, "test-shell"));

        cut.FindAll("[data-testid='test-shell-add-button']").Should().BeEmpty();
    }

    [Fact]
    public void Shell_ShowAddAction_RendersAddButtonWithAddLabelAboveGrid()
    {
        using var ctx = Build();

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<object, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ShowAddAction, true)
            .Add(c => c.AddLabel, "Add Product")
            .Add(c => c.GridContent, builder =>
                builder.AddMarkupContent(0, "<div data-testid='inner-results'>grid</div>")));

        var button = cut.Find("[data-testid='test-shell-add-button']");
        button.TextContent.Should().Contain("Add Product");
        button.HasAttribute("disabled").Should().BeFalse();

        var body = cut.Find("[data-testid='test-shell-body']");
        body.InnerHtml.IndexOf("test-shell-add-button", StringComparison.Ordinal)
            .Should().BeLessThan(body.InnerHtml.IndexOf("inner-results", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Shell_AddButtonClick_ResetsSelectionThenRaisesOnAddRequestedOnce()
    {
        using var ctx = Build();
        var raised = 0;
        bool? entityDirtyWhenRaised = null;
        bool? linkDirtyWhenRaised = null;
        MasterDetailExpansionShell<TestRow, object, object>? shell = null;

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<TestRow, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ShowAddAction, true)
            .Add(c => c.AllowLinkOnly, true)
            .Add(c => c.OnAddRequested, EventCallback.Factory.Create(this, () =>
            {
                raised++;
                entityDirtyWhenRaised = shell!.DetailsContext.IsDirty;
                linkDirtyWhenRaised = shell.LinkContext.IsLinkDirty;
            })));
        shell = cut.Instance;

        await cut.InvokeAsync(() =>
        {
            shell.NotifyRowSelected(new TestRow { Id = 1 });
            shell.DetailsContext.SetDirty();
            shell.LinkContext.SetLinkDirty();
        });

        cut.Find("[data-testid='test-shell-add-button']").Click();

        raised.Should().Be(1);
        entityDirtyWhenRaised.Should().BeFalse();
        linkDirtyWhenRaised.Should().BeFalse();
        shell.IsPrimaryActionUseOnly.Should().BeFalse("the selection was cleared");
    }

    [Fact]
    public void Shell_AddButton_DisabledWhileWorking()
    {
        using var ctx = Build();

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<object, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ShowAddAction, true)
            .Add(c => c.Working, true));

        cut.Find("[data-testid='test-shell-add-button']").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Shell_ShowGridFalse_NoAddButtonEvenWithShowAddAction()
    {
        using var ctx = Build();

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<object, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ShowGrid, false)
            .Add(c => c.ShowAddAction, true)
            .Add(c => c.DetailsContent, _ => { }));

        cut.FindAll("[data-testid='test-shell-add-button']").Should().BeEmpty();
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

    // ── Session 3: Dirty state + Use/Save-and-Use label swap tests ─────────

    [Fact]
    public async Task RaiseUseRequestedAsync_InvokesOnUseRequestedCallbackWithRow()
    {
        using var ctx = Build();
        var requestedRow = new TestRow { Id = 1, Name = "Test" };
        TestRow? capturedRow = null;

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<TestRow, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ShowRowUseAction, true)
            .Add(c => c.OnUseRequested, EventCallback.Factory.Create<TestRow>(this, row =>
            {
                capturedRow = row;
            })));

        await cut.InvokeAsync(() => cut.Instance.RaiseUseRequestedAsync(requestedRow));

        capturedRow.Should().NotBeNull();
        capturedRow.Should().Be(requestedRow);
    }

    [Fact]
    public async Task PrimaryActionLabel_AllowLinkOnly_SwapsCorrectlyWithSelectionAndDirtyState()
    {
        using var ctx = Build();
        var selectedRow = new TestRow { Id = 1, Name = "Test" };

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<TestRow, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ShowRowUseAction, true)
            .Add(c => c.AllowLinkOnly, true)
            .Add(c => c.UseLabel, "Use It")
            .Add(c => c.SaveAndUseLabel, "Save & Use"));

        // 1. Initial state: no selection, not dirty -> "Save & Use" (ShowRowUseAction=true path)
        cut.Instance.PrimaryActionLabel.Should().Be("Save & Use");

        // 2. Select a row, not dirty -> "Use It"
        await cut.InvokeAsync(() => cut.Instance.NotifyRowSelected(selectedRow));
        cut.Instance.PrimaryActionLabel.Should().Be("Use It");

        // 3. Row selected AND dirty -> "Save & Use"
        await cut.InvokeAsync(() => cut.Instance.DetailsContext.SetDirty());
        cut.Instance.PrimaryActionLabel.Should().Be("Save & Use");
    }

    [Fact]
    public async Task ResetSelection_ClearsSelectionAndDirtyState()
    {
        using var ctx = Build();
        var selectedRow = new TestRow { Id = 1, Name = "Test" };

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<TestRow, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ShowRowUseAction, true)
            .Add(c => c.AllowLinkOnly, true)
            .Add(c => c.UseLabel, "Use It")
            .Add(c => c.SaveAndUseLabel, "Save & Use"));

        // Set up a selected and dirty state
        await cut.InvokeAsync(() => cut.Instance.NotifyRowSelected(selectedRow));
        await cut.InvokeAsync(() => cut.Instance.DetailsContext.SetDirty());
        cut.Instance.PrimaryActionLabel.Should().Be("Save & Use");
        cut.Instance.DetailsContext.IsDirty.Should().BeTrue();

        // Act: reset
        await cut.InvokeAsync(() => cut.Instance.ResetSelection());

        // Assert: state is cleared. With no selection, IsPrimaryActionUseOnly is false,
        // so the label falls back to SaveAndUse (ShowRowUseAction=true path).
        cut.Instance.DetailsContext.IsDirty.Should().BeFalse();
        cut.Instance.PrimaryActionLabel.Should().Be("Save & Use");
    }

    // ── Session 4: Link-data UI, read-only toggles, and multi-dirty state ─

    [Fact]
    public void Shell_ShowLinkData_RendersLinkCardWithHeading()
    {
        using var ctx = Build();

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<TestRow, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ShowLinkData, true)
            .Add(c => c.LinkDataHeading, "Line details")
            .Add(c => c.LinkDataContent, (RenderFragment)(b =>
                b.AddMarkupContent(0, "<p>some-link-content</p>")))
            .Add(c => c.DetailsContent, (RenderFragment)(b =>
                b.AddMarkupContent(0, "<div>details</div>"))));

        var card = cut.Find("[data-testid='test-shell-link-card']");
        card.Should().NotBeNull();
        card.TextContent.Should().Contain("Line details");
        card.TextContent.Should().Contain("some-link-content");
    }

    [Fact]
    public void Shell_ShowLinkData_RendersTargetCardWithHeading()
    {
        using var ctx = Build();

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<TestRow, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ShowLinkData, true)
            .Add(c => c.TargetCardHeading, "Product")
            .Add(c => c.DetailsContent, (RenderFragment)(b =>
                b.AddMarkupContent(0, "<div>details</div>"))));

        var card = cut.Find("[data-testid='test-shell-target-card']");
        card.Should().NotBeNull();
        card.TextContent.Should().Contain("Product");
    }

    [Fact]
    public void Shell_TargetEditability_ReadOnly_NoToggleButton()
    {
        using var ctx = Build();

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<TestRow, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.TargetEditability, TargetEditability.ReadOnly)
            .Add(c => c.ShowLinkData, true)
            .Add(c => c.DetailsContent, (RenderFragment)(b =>
                b.AddMarkupContent(0, "<div>details</div>"))));

        cut.FindAll("[data-testid='test-shell-edit-toggle']").Should().BeEmpty();
        cut.Instance.DetailsContext.IsTargetReadOnly.Should().BeTrue();
    }

    [Fact]
    public void Shell_TargetEditability_ReadOnlyWithToggle_ToggleButtonFlipsState_AndFiresCallback()
    {
        using var ctx = Build();
        bool? firedValue = null;

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<TestRow, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.TargetEditability, TargetEditability.ReadOnlyWithToggle)
            .Add(c => c.ShowLinkData, true)
            .Add(c => c.DetailsContent, (RenderFragment)(b =>
                b.AddMarkupContent(0, "<div>details</div>")))
            .Add(c => c.OnTargetEditingToggled, EventCallback.Factory.Create<bool>(this, captured => firedValue = captured)));

        cut.Instance.DetailsContext.IsTargetReadOnly.Should().BeTrue();

        cut.Find("[data-testid='test-shell-edit-toggle']").Click();

        cut.Instance.DetailsContext.IsTargetReadOnly.Should().BeFalse();
        firedValue.Should().BeFalse();
    }

    [Fact]
    public async Task Shell_LinkDirty_AloneFlipsPrimaryActionLabelToSaveAndUse()
    {
        using var ctx = Build();

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<TestRow, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ShowRowUseAction, true)
            .Add(c => c.AllowLinkOnly, true)
            .Add(c => c.UseLabel, "Use It")
            .Add(c => c.SaveAndUseLabel, "Save & Use")
            .Add(c => c.DetailsContent, (RenderFragment)(b =>
                b.AddMarkupContent(0, "<div>details</div>"))));

        await cut.InvokeAsync(() => cut.Instance.NotifyRowSelected(new TestRow()));
        cut.Instance.PrimaryActionLabel.Should().Be("Use It");

        await cut.InvokeAsync(() => cut.Instance.LinkContext.SetLinkDirty());
        cut.Instance.PrimaryActionLabel.Should().Be("Save & Use");
    }

    [Fact]
    public async Task Shell_ToggleWithoutChange_PreservesUseLabel()
    {
        using var ctx = Build();

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<TestRow, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.TargetEditability, TargetEditability.ReadOnlyWithToggle)
            .Add(c => c.ShowRowUseAction, true)
            .Add(c => c.AllowLinkOnly, true)
            .Add(c => c.UseLabel, "Use It")
            .Add(c => c.DetailsContent, (RenderFragment)(b =>
                b.AddMarkupContent(0, "<div>details</div>"))));

        await cut.InvokeAsync(() => cut.Instance.NotifyRowSelected(new TestRow()));
        cut.Instance.PrimaryActionLabel.Should().Be("Use It");

        cut.Find("[data-testid='test-shell-edit-toggle']").Click();

        cut.Instance.PrimaryActionLabel.Should().Be("Use It");
    }

    [Fact]
    public async Task Shell_SaveRequest_CarriesBothDirtyFlags_OnPrimaryAction()
    {
        using var ctx = Build();
        SaveRequest<object, object>? capturedRequest = null;

        var cut = ctx.RenderComponent<MasterDetailExpansionShell<TestRow, object, object>>(p => p
            .Add(c => c.TestId, "test-shell")
            .Add(c => c.ShowRowUseAction, true)
            .Add(c => c.DetailsContent, (RenderFragment)(b =>
                b.AddMarkupContent(0, "<div>details</div>")))
            .Add(c => c.OnSaveAndUseRequested, EventCallback.Factory.Create<SaveRequest<object, object>>(
                this, req => Task.FromResult(capturedRequest = req))));

        await cut.InvokeAsync(() =>
        {
            cut.Instance.NotifyRowSelected(new TestRow());
            cut.Instance.DetailsContext.SetDirty();
            cut.Instance.LinkContext.SetLinkDirty();
        });

        cut.Find("[data-testid='test-shell-primary-button']").Click();

        capturedRequest.Should().NotBeNull();
        capturedRequest!.EntityDirty.Should().BeTrue();
        capturedRequest!.LinkDirty.Should().BeTrue();
    }

    private sealed record TestRow
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }
}
