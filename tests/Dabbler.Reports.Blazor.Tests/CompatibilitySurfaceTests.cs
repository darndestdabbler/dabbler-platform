using Dabbler.Components;
using FluentAssertions;
using Xunit;

namespace Dabbler.Reports.Blazor.Tests;

public class CompatibilitySurfaceTests
{
    [Fact]
    public void ReportPage_InheritsComponentBase()
    {
        typeof(DabblerReportPage).BaseType.Should().Be(typeof(Microsoft.AspNetCore.Components.ComponentBase));
    }

    [Fact]
    public void ReportPage_ImplementsIAsyncDisposable()
    {
        typeof(DabblerReportPage).Should().Implement<IAsyncDisposable>();
    }
}
