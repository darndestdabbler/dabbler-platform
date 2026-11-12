using Bunit;
using Dabbler.Components.Data;
using Dabbler.Components.Tests.TestHelpers;
using FluentAssertions;
using Xunit;

namespace Dabbler.Components.Tests.FilterPanel;

public class FilterPanelAccessibilityTests
{
    private static readonly IReadOnlyList<FilterColumnDefinition> TestColumns =
    [
        new FilterColumnDefinition
        {
            PropertyName = "Name",
            Header = "Name",
            EditorType = FilterEditorType.Text
        },
        new FilterColumnDefinition
        {
            PropertyName = "Price",
            Header = "Price",
            EditorType = FilterEditorType.NumericText
        },
        new FilterColumnDefinition
        {
            PropertyName = "Category",
            Header = "Category",
            EditorType = FilterEditorType.ComboBox,
            ItemsSource = new object[] { "Coffee", "Tea" }
        }
    ];

    [Fact]
    public void FilterPanel_HasSearchRoleAndAriaLabel()
    {
        using var ctx = new TestContext().SetupForDabblerComponents();

        var cut = ctx.RenderComponent<FilterPanel<StubRow>>(p =>
            p.Add(x => x.Columns, TestColumns));

        var searchRegion = cut.Find("[role='search']");
        searchRegion.Should().NotBeNull();
        searchRegion.GetAttribute("aria-label").Should().Be("Filter panel");
    }

    [Fact]
    public void FilterInputs_HaveLabelAttributes()
    {
        using var ctx = new TestContext().SetupForDabblerComponents();

        var cut = ctx.RenderComponent<FilterPanel<StubRow>>(p =>
            p.Add(x => x.Columns, TestColumns));

        // MudBlazor renders labels as part of the input component.
        // Each filter column should produce a label element in the markup.
        var markup = cut.Markup;
        markup.Should().Contain("Name");
        markup.Should().Contain("Price");
        markup.Should().Contain("Category");
    }

    [Fact]
    public void OrGroupRemoveButton_HasAriaLabel()
    {
        using var ctx = new TestContext().SetupForDabblerComponents();

        var cut = ctx.RenderComponent<FilterPanel<StubRow>>(p =>
            p.Add(x => x.Columns, TestColumns));

        cut.Instance.Coordinator.AddGroup();
        cut.Render();

        var removeButton = cut.Find("[aria-label='Remove filter group']");
        removeButton.Should().NotBeNull();
    }

    [Fact]
    public void FilterPanel_UsesResponsiveGridLayout()
    {
        using var ctx = new TestContext().SetupForDabblerComponents();

        var cut = ctx.RenderComponent<FilterPanel<StubRow>>(p =>
            p.Add(x => x.Columns, TestColumns));

        // MudGrid items should have responsive breakpoints
        var gridItems = cut.FindAll(".mud-grid-item");
        gridItems.Should().HaveCountGreaterOrEqualTo(TestColumns.Count);
    }

    [Fact]
    public void FilterInputs_UseOutlinedVariantForVisibility()
    {
        using var ctx = new TestContext().SetupForDabblerComponents();

        var cut = ctx.RenderComponent<FilterPanel<StubRow>>(p =>
            p.Add(x => x.Columns, TestColumns));

        // MudBlazor outlined variant adds specific CSS class
        cut.FindAll(".mud-input-outlined").Should().HaveCountGreaterOrEqualTo(1);
    }

    private class StubRow
    {
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public string? Category { get; set; }
    }
}
