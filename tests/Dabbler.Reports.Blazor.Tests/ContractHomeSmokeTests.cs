using FluentAssertions;
using Xunit;

namespace Dabbler.Reports.Blazor.Tests;

public class ContractHomeSmokeTests
{
    [Fact]
    public void RetainedReportTypes_AreAvailableToTheContractHome()
    {
        var retainedTypes = new[]
        {
            typeof(Dabbler.Components.DabblerReportPage),
            typeof(ParameterPromptForm),
            typeof(ReportViewer)
        };

        retainedTypes.Should().NotContainNulls();
        retainedTypes.Should().HaveCount(3);
    }
}
