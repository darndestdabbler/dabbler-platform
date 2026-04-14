using Bunit;
using Dabbler.Components.Tests.TestHelpers;
using FluentAssertions;
using Xunit;

namespace Dabbler.Components.Tests.DataGrid;

public class DabblerDataGridTests
{
    [Fact]
    public void CoreWrapper_RendersAccessibleGridShell()
    {
        using var ctx = new TestContext().SetupForDabblerComponents();

        var cut = ctx.RenderComponent<DabblerDataGrid<SampleRow>>(parameters => parameters
            .Add(x => x.RowCount, 3)
            .Add(x => x.ColumnCount, 2)
            .Add(x => x.AriaLabel, "Sample rows")
            .AddChildContent("<div class=\"inner-grid\">Rows</div>"));

        var grid = cut.Find("[role='grid']");
        grid.GetAttribute("aria-label").Should().Be("Sample rows");
        grid.GetAttribute("aria-rowcount").Should().Be("3");
        grid.GetAttribute("aria-colcount").Should().Be("2");
        cut.Markup.Should().Contain("inner-grid");
    }

    private sealed record SampleRow(int Id, string Name);
}
