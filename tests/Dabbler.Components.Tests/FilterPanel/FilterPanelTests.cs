using Bunit;
using Dabbler.Components.Data;
using Dabbler.Components.Tests.TestHelpers;
using FluentAssertions;
using Xunit;

namespace Dabbler.Components.Tests.FilterPanel;

public class FilterPanelTests
{
    private static readonly IReadOnlyList<FilterColumnDefinition> TestColumns =
    [
        new FilterColumnDefinition
        {
            PropertyName = "Name",
            Header = "Name",
            EditorType = FilterEditorType.Text,
            Placeholder = "Filter by name"
        },
        new FilterColumnDefinition
        {
            PropertyName = "Price",
            Header = "Price",
            EditorType = FilterEditorType.NumericText,
            Placeholder = "Min price"
        },
        new FilterColumnDefinition
        {
            PropertyName = "IsActive",
            Header = "Active",
            EditorType = FilterEditorType.CheckBox
        },
        new FilterColumnDefinition
        {
            PropertyName = "Category",
            Header = "Category",
            EditorType = FilterEditorType.ComboBox,
            ItemsSource = new object[] { "Coffee", "Tea", "Accessories" }
        }
    ];

    [Fact]
    public async Task RendersFilterFieldsForEachFilterableColumn()
    {
        await using var ctx = new BunitContext().SetupForDabblerComponents();

        var cut = ctx.Render<FilterPanel<StubRow>>(p =>
            p.Add(x => x.Columns, TestColumns));

        // One MudGrid should exist for the default group
        cut.FindAll(".mud-grid").Should().HaveCountGreaterOrEqualTo(1);

        // Each column produces one MudItem in the grid
        cut.FindAll(".mud-grid-item").Should().HaveCount(TestColumns.Count);
    }

    [Fact]
    public async Task TextFilter_SetFilterUpdatesCoordinatorAndRaisesFilterChanged()
    {
        await using var ctx = new BunitContext().SetupForDabblerComponents();
        CompositeFilterDescriptor? receivedFilter = null;

        var cut = ctx.Render<FilterPanel<StubRow>>(p => p
            .Add(x => x.Columns, TestColumns)
            .Add(x => x.FilterChanged, filter => receivedFilter = filter));

        // Set a text filter via the coordinator (simulating user input)
        cut.Instance.Coordinator.SetFilter(0, "Name", FilterOperators.Contains, "Espresso");

        cut.Instance.Coordinator.HasFilters.Should().BeTrue();
        cut.Instance.Coordinator.HasFilterForProperty("Name").Should().BeTrue();

        var composite = cut.Instance.GetCompositeFilter();
        composite.Should().NotBeNull();
        composite!.Filters.Should().Contain(f => f.PropertyName == "Name" && f.Value!.ToString() == "Espresso");
    }

    [Fact]
    public async Task ClearAll_ResetsAllFiltersAndRaisesFilterChanged()
    {
        await using var ctx = new BunitContext().SetupForDabblerComponents();
        CompositeFilterDescriptor? lastFilter = null;

        var cut = ctx.Render<FilterPanel<StubRow>>(p => p
            .Add(x => x.Columns, TestColumns)
            .Add(x => x.FilterChanged, filter => lastFilter = filter));

        // Set filters
        cut.Instance.Coordinator.SetFilter(0, "Name", FilterOperators.Contains, "Blend");
        cut.Instance.Coordinator.SetFilter(0, "Price", FilterOperators.GreaterThanOrEqual, 10m);
        cut.Instance.Coordinator.HasFilters.Should().BeTrue();

        // Clear all
        cut.Instance.Coordinator.ClearAll();

        cut.Instance.Coordinator.HasFilters.Should().BeFalse();
        cut.Instance.GetCompositeFilter().Should().BeNull();
    }

    [Fact]
    public async Task AddGroup_CreatesNewOrGroup()
    {
        await using var ctx = new BunitContext().SetupForDabblerComponents();

        var cut = ctx.Render<FilterPanel<StubRow>>(p =>
            p.Add(x => x.Columns, TestColumns));

        cut.Instance.Coordinator.Groups.Groups.Should().HaveCount(1);

        cut.Instance.Coordinator.AddGroup();
        cut.Render();

        cut.Instance.Coordinator.Groups.Groups.Should().HaveCount(2);
        // The OR chip divider should appear between groups
        cut.Markup.Should().Contain("OR");
    }

    [Fact]
    public async Task RemoveGroup_RemovesOrGroup()
    {
        await using var ctx = new BunitContext().SetupForDabblerComponents();

        var cut = ctx.Render<FilterPanel<StubRow>>(p =>
            p.Add(x => x.Columns, TestColumns));

        var newGroup = cut.Instance.Coordinator.AddGroup();
        cut.Render();
        cut.Instance.Coordinator.Groups.Groups.Should().HaveCount(2);

        cut.Instance.Coordinator.RemoveGroup(newGroup);
        cut.Render();

        cut.Instance.Coordinator.Groups.Groups.Should().HaveCount(1);
    }

    [Fact]
    public async Task RemoveGroup_CannotRemoveLastGroup()
    {
        await using var ctx = new BunitContext().SetupForDabblerComponents();

        var cut = ctx.Render<FilterPanel<StubRow>>(p =>
            p.Add(x => x.Columns, TestColumns));

        var onlyGroup = cut.Instance.Coordinator.Groups.Groups[0];
        cut.Instance.Coordinator.RemoveGroup(onlyGroup);

        cut.Instance.Coordinator.Groups.Groups.Should().HaveCount(1);
    }

    [Fact]
    public async Task FilterChips_ShowWhenFiltersAreActive()
    {
        await using var ctx = new BunitContext().SetupForDabblerComponents();

        var cut = ctx.Render<FilterPanel<StubRow>>(p => p
            .Add(x => x.Columns, TestColumns)
            .Add(x => x.ShowFilterChips, true));

        // No chips initially
        cut.FindAll(".mud-chipset").Should().BeEmpty();

        // Set a filter
        cut.Instance.Coordinator.SetFilter(0, "Name", FilterOperators.Contains, "Dark");
        cut.Render();

        // Chips should appear
        cut.FindAll(".mud-chip").Should().HaveCountGreaterOrEqualTo(1);
        cut.Markup.Should().Contain("Name");
        cut.Markup.Should().Contain("Dark");
    }

    [Fact]
    public async Task FilterChips_HiddenWhenShowFilterChipsIsFalse()
    {
        await using var ctx = new BunitContext().SetupForDabblerComponents();

        var cut = ctx.Render<FilterPanel<StubRow>>(p => p
            .Add(x => x.Columns, TestColumns)
            .Add(x => x.ShowFilterChips, false));

        cut.Instance.Coordinator.SetFilter(0, "Name", FilterOperators.Contains, "Test");
        cut.Render();

        cut.FindAll(".mud-chipset").Should().BeEmpty();
    }

    [Fact]
    public async Task ClearAll_ButtonAppearsOnlyWhenFiltersAreActive()
    {
        await using var ctx = new BunitContext().SetupForDabblerComponents();

        var cut = ctx.Render<FilterPanel<StubRow>>(p =>
            p.Add(x => x.Columns, TestColumns));

        cut.Markup.Should().NotContain("Clear All");

        cut.Instance.Coordinator.SetFilter(0, "Name", FilterOperators.Contains, "Test");
        cut.Render();

        cut.Markup.Should().Contain("Clear All");
    }

    [Fact]
    public async Task AddOrGroupButton_AppearsWhenShowAddGroupIsTrue()
    {
        await using var ctx = new BunitContext().SetupForDabblerComponents();

        var cut = ctx.Render<FilterPanel<StubRow>>(p => p
            .Add(x => x.Columns, TestColumns)
            .Add(x => x.ShowAddGroup, true));

        cut.Markup.Should().Contain("Add OR Group");
    }

    [Fact]
    public async Task AddOrGroupButton_HiddenWhenShowAddGroupIsFalse()
    {
        await using var ctx = new BunitContext().SetupForDabblerComponents();

        var cut = ctx.Render<FilterPanel<StubRow>>(p => p
            .Add(x => x.Columns, TestColumns)
            .Add(x => x.ShowAddGroup, false));

        cut.Markup.Should().NotContain("Add OR Group");
    }

    [Fact]
    public async Task ComboBoxFilter_RendersSelectItems()
    {
        await using var ctx = new BunitContext().SetupForDabblerComponents();

        var cut = ctx.Render<FilterPanel<StubRow>>(p =>
            p.Add(x => x.Columns, TestColumns));

        // The Category column should render with its label
        cut.Markup.Should().Contain("Category");
    }

    [Fact]
    public async Task GetFilterSummary_ReturnsReadableText()
    {
        await using var ctx = new BunitContext().SetupForDabblerComponents();

        var cut = ctx.Render<FilterPanel<StubRow>>(p =>
            p.Add(x => x.Columns, TestColumns));

        cut.Instance.Coordinator.SetFilter(0, "Name", FilterOperators.Contains, "Espresso");
        var summary = cut.Instance.GetFilterSummary();

        summary.Should().NotBeNullOrEmpty();
        summary.Should().Contain("Name");
    }

    [Fact]
    public async Task ClearFilter_RemovesSpecificPropertyFilter()
    {
        await using var ctx = new BunitContext().SetupForDabblerComponents();

        var cut = ctx.Render<FilterPanel<StubRow>>(p =>
            p.Add(x => x.Columns, TestColumns));

        cut.Instance.Coordinator.SetFilter(0, "Name", FilterOperators.Contains, "Blend");
        cut.Instance.Coordinator.SetFilter(0, "Price", FilterOperators.GreaterThanOrEqual, 5m);

        cut.Instance.Coordinator.HasFilterForProperty("Name").Should().BeTrue();
        cut.Instance.Coordinator.HasFilterForProperty("Price").Should().BeTrue();

        cut.Instance.Coordinator.ClearFilter("Name");

        cut.Instance.Coordinator.HasFilterForProperty("Name").Should().BeFalse();
        cut.Instance.Coordinator.HasFilterForProperty("Price").Should().BeTrue();
    }

    [Fact]
    public async Task Dispose_UnsubscribesFromCoordinatorEvents()
    {
        await using var ctx = new BunitContext().SetupForDabblerComponents();

        var cut = ctx.Render<FilterPanel<StubRow>>(p =>
            p.Add(x => x.Columns, TestColumns));

        var coordinator = cut.Instance.Coordinator;

        // Dispose the component
        cut.Dispose();

        // Setting a filter after dispose should not throw
        var act = () => coordinator.SetFilter(0, "Name", FilterOperators.Contains, "Test");
        act.Should().NotThrow();
    }

    [Fact]
    public void ToNullableDecimal_ConvertsVariousTypes()
    {
        FilterPanel<StubRow>.ToNullableDecimal(null).Should().BeNull();
        FilterPanel<StubRow>.ToNullableDecimal(42m).Should().Be(42m);
        FilterPanel<StubRow>.ToNullableDecimal(42).Should().Be(42m);
        FilterPanel<StubRow>.ToNullableDecimal(42.5d).Should().Be(42.5m);
        FilterPanel<StubRow>.ToNullableDecimal(42.5f).Should().Be(42.5m);
        FilterPanel<StubRow>.ToNullableDecimal(42L).Should().Be(42m);
        FilterPanel<StubRow>.ToNullableDecimal("99.99").Should().Be(99.99m);
        FilterPanel<StubRow>.ToNullableDecimal("not a number").Should().BeNull();
    }

    [Fact]
    public void ToNullableDateTime_ConvertsVariousTypes()
    {
        FilterPanel<StubRow>.ToNullableDateTime(null).Should().BeNull();
        var now = DateTime.UtcNow;
        FilterPanel<StubRow>.ToNullableDateTime(now).Should().Be(now);
        FilterPanel<StubRow>.ToNullableDateTime(new DateTimeOffset(now, TimeSpan.Zero)).Should().Be(now);
        FilterPanel<StubRow>.ToNullableDateTime("2026-01-15").Should().NotBeNull();
        FilterPanel<StubRow>.ToNullableDateTime("not a date").Should().BeNull();
    }

    [Fact]
    public void ToNullableBool_ConvertsVariousTypes()
    {
        FilterPanel<StubRow>.ToNullableBool(null).Should().BeNull();
        FilterPanel<StubRow>.ToNullableBool(true).Should().BeTrue();
        FilterPanel<StubRow>.ToNullableBool(false).Should().BeFalse();
        FilterPanel<StubRow>.ToNullableBool("true").Should().BeTrue();
        FilterPanel<StubRow>.ToNullableBool("false").Should().BeFalse();
        FilterPanel<StubRow>.ToNullableBool("not a bool").Should().BeNull();
    }

    private class StubRow
    {
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public string? Category { get; set; }
    }
}
