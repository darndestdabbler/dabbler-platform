using Dabbler.Components.Data;
using Dabbler.Platform.Stubs;
using FluentAssertions;
using Xunit;

namespace Dabbler.Components.Tests.StubPlatform;

public class StubGridDataProviderTests
{
    [Fact]
    public async Task FetchAsync_AppliesFilterSortAndPaging()
    {
        var provider = new StubGridDataProvider<SampleRow>(
        [
            new(1, "Grace", "Chicago"),
            new(2, "Ada", "Boston"),
            new(3, "Katherine", "Atlanta")
        ]);

        var response = await provider.FetchAsync(new GridDataRequest
        {
            CompositeFilter = new CompositeFilterDescriptor
            {
                Logic = CompositeFilterLogic.And,
                Filters =
                [
                    new FilterDescriptor(nameof(SampleRow.City), FilterOperators.Contains, "o")
                ]
            },
            Sorts =
            [
                new SortDescriptor(nameof(SampleRow.Name))
            ],
            PageIndex = 0,
            PageSize = 1
        });

        response.TotalCount.Should().Be(2);
        response.Items.Should().ContainSingle();
        response.Items[0].Name.Should().Be("Ada");
    }

    private sealed record SampleRow(int Id, string Name, string City);
}
