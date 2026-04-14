using Bunit;
using Dabbler.Components.Data;
using Dabbler.Components.Tests.TestHelpers;
using FluentAssertions;
using Xunit;

namespace Dabbler.Components.Tests.FilterGrid;

public class DabblerFilterGridTests
{
    [Fact]
    public void CoreSummary_TracksAndAcrossRowsAndOrAcrossGroups()
    {
        using var ctx = new TestContext().SetupForDabblerComponents();

        CompositeFilterDescriptor? latestFilter = null;
        var cut = ctx.RenderComponent<DabblerFilterGrid<SampleRow>>(parameters => parameters
            .Add(x => x.Columns, BuildColumns())
            .Add(x => x.FilterChanged, filter => latestFilter = filter));

        cut.Instance.Coordinator.SetFilter(0, "FirstName", FilterOperators.Contains, "Ada");
        cut.Instance.Coordinator.AddGroup();
        cut.Instance.Coordinator.SetFilter(1, "IsActive", FilterOperators.Equals, true);

        cut.WaitForAssertion(() =>
        {
            latestFilter.Should().NotBeNull();
            latestFilter!.Logic.Should().Be(CompositeFilterLogic.Or);
            latestFilter.Children.Should().HaveCount(2);
            cut.Instance.GetFilterSummary().Should().Contain("First Name Contains 'Ada'");
            cut.Instance.GetFilterSummary().Should().Contain("Active Equals 'Yes'");
            cut.Instance.GetFilterSummary().Should().Contain("OR");
        });
    }

    private static IReadOnlyList<FilterColumnDefinition> BuildColumns() =>
    [
        new()
        {
            Header = "First Name",
            PropertyName = "FirstName",
            EditorType = FilterEditorType.Text,
            DefaultOperator = FilterOperators.Contains
        },
        new()
        {
            Header = "Active",
            PropertyName = "IsActive",
            EditorType = FilterEditorType.CheckBox,
            DefaultOperator = FilterOperators.Equals
        }
    ];

    private sealed record SampleRow(int Id, string FirstName, bool IsActive);
}
