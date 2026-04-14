using FluentAssertions;
using Xunit;

namespace Dabbler.Components.Tests;

public class ContractHomeSmokeTests
{
    [Fact]
    public void RetainedCoreTypes_AreAvailableToTheContractHome()
    {
        var retainedTypes = new[]
        {
            typeof(DabblerQueryPage<>),
            typeof(DabblerDataGrid<>),
            typeof(DabblerFilterGrid<>),
            typeof(DabblerComboBox<>),
            typeof(DabblerDetailForm<>),
            typeof(DabblerRecordPopup<>),
            typeof(DabblerShell),
            typeof(DabblerDashboardPage)
        };

        retainedTypes.Should().NotContainNulls();
        retainedTypes.Should().HaveCount(8);
    }
}
