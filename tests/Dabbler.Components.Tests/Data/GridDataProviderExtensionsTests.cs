using Dabbler.Components.Data;
using FluentAssertions;
using MudBlazor;
using Xunit;

namespace Dabbler.Components.Tests.Data;

public class GridDataProviderExtensionsTests
{
    [Fact]
    public async Task ToServerDataFunc_MapsGridStateAndPassesCancellationToken()
    {
        var provider = new RecordingProvider();
        var filter = new CompositeFilterDescriptor();
        var serverData = provider.ToServerDataFunc(() => filter);
        var state = new GridState<string>
        {
            Page = 2,
            PageSize = 25,
            SortDefinitions = [new SortDefinition<string>("Name", true, 0, s => s)],
        };
        using var cts = new CancellationTokenSource();

        var data = await serverData(state, cts.Token);

        provider.Token.Should().Be(cts.Token);
        provider.Request!.PageIndex.Should().Be(2);
        provider.Request.PageSize.Should().Be(25);
        provider.Request.CompositeFilter.Should().BeSameAs(filter);
        provider.Request.Sorts.Should().Equal(new SortDescriptor("Name", true));
        data.Items.Should().Equal("a", "b");
        data.TotalItems.Should().Be(7);
    }

    private sealed class RecordingProvider : IGridDataProvider<string>
    {
        public GridDataRequest? Request { get; private set; }

        public CancellationToken Token { get; private set; }

        public Task<GridDataResponse<string>> FetchAsync(GridDataRequest request, CancellationToken cancellationToken = default)
        {
            Request = request;
            Token = cancellationToken;
            return Task.FromResult(new GridDataResponse<string> { Items = ["a", "b"], TotalCount = 7 });
        }
    }
}
