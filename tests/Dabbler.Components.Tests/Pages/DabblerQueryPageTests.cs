using System.Linq.Expressions;
using Bunit;
using Dabbler.Components.Data;
using Dabbler.Components.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Xunit;

namespace Dabbler.Components.Tests.Pages;

public class DabblerQueryPageTests
{
    [Fact]
    public async Task ManualSearch_UsesCurrentFilter_AndKeepsAdvancedActionsOptIn()
    {
        using var ctx = new TestContext().SetupForDabblerComponents();

        var provider = new RecordingProvider(
        [
            new SampleRow(1, "Ada"),
            new SampleRow(2, "Grace"),
            new SampleRow(3, "Linus")
        ]);

        var cut = ctx.RenderComponent<DabblerQueryPage<SampleRow>>(parameters => parameters
            .Add(x => x.Title, "Customers")
            .Add(x => x.FilterColumns, BuildFilterColumns())
            .Add(x => x.GridColumns, BuildGridColumns())
            .Add(x => x.DataProvider, provider)
            .Add(x => x.AutoLoadOnFilterChange, false));

        cut.WaitForAssertion(() => provider.Requests.Should().HaveCount(1));
        cut.Markup.Should().NotContain("Export to CSV");
        cut.Markup.Should().NotContain("Copy to clipboard");

        cut.Instance.FilterGrid.Should().NotBeNull();
        await cut.InvokeAsync(() =>
        {
            cut.Instance.FilterGrid!.SetCellValue("Name", "Ada");
            return Task.CompletedTask;
        });

        cut.WaitForAssertion(() =>
            cut.Instance.FilterGrid!.GetFilterSummary().Should().Contain("Name Contains 'Ada'"));
        provider.Requests.Should().HaveCount(1);

        cut.FindAll("button").Single(button => button.TextContent.Contains("Search", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() =>
        {
            provider.Requests.Should().HaveCount(2);
            var compositeFilter = provider.Requests[^1].CompositeFilter;
            compositeFilter.Should().NotBeNull();
            var filter = compositeFilter!.Filters.Should().ContainSingle().Subject;
            filter.PropertyName.Should().Be("Name");
            filter.Operator.Should().Be(FilterOperators.Contains);
            filter.Value?.ToString().Should().Be("Ada");
            cut.Markup.Should().Contain("Showing 1 of 3 records");
        });
    }

    private static IReadOnlyList<FilterColumnDefinition> BuildFilterColumns() =>
    [
        new()
        {
            Header = "Name",
            PropertyName = "Name",
            EditorType = FilterEditorType.Text,
            DefaultOperator = FilterOperators.Contains,
            ShowOperator = false
        }
    ];

    private static RenderFragment BuildGridColumns() => builder =>
    {
        builder.OpenComponent<PropertyColumn<SampleRow, string>>(0);
        builder.AddAttribute(1, "Property", (Expression<Func<SampleRow, string>>)(row => row.Name));
        builder.AddAttribute(2, "Title", "Name");
        builder.CloseComponent();
    };

    private sealed record SampleRow(int Id, string Name);

    private sealed class RecordingProvider(IReadOnlyList<SampleRow> allRows) : IGridDataProvider<SampleRow>
    {
        public List<GridDataRequest> Requests { get; } = [];

        public Task<GridDataResponse<SampleRow>> FetchAsync(
            GridDataRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);

            IEnumerable<SampleRow> filteredRows = allRows;
            var nameFilter = request.CompositeFilter?.Filters.FirstOrDefault(filter =>
                string.Equals(filter.PropertyName, "Name", StringComparison.OrdinalIgnoreCase));

            if (nameFilter?.Value is string text && !string.IsNullOrWhiteSpace(text))
            {
                filteredRows = filteredRows.Where(row =>
                    row.Name.Contains(text, StringComparison.OrdinalIgnoreCase));
            }

            var materialized = filteredRows.ToList();
            return Task.FromResult(new GridDataResponse<SampleRow>
            {
                Items = materialized,
                TotalCount = materialized.Count
            });
        }
    }
}
