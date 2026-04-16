using FluentAssertions;
using Xunit;

namespace Dabbler.Components.Tests;

public class ContractHomeSmokeTests
{
    [Fact]
    public void RetainedCoreTypes_AreAvailableToTheContractHome()
    {
        // After the MudBlazor-native rebuild, the wrapper components (DabblerQueryPage,
        // DabblerDataGrid, DabblerFilterGrid, DabblerShell, DabblerDashboardPage) have
        // been removed. Remaining types are validated here as they are rebuilt in later
        // sessions or confirmed to still exist.
        var retainedTypes = new[]
        {
            typeof(FilterPanel<>),
            typeof(DabblerComboBox<>),
            typeof(DabblerDetailForm<>),
            typeof(DabblerRecordPopup<>),
        };

        retainedTypes.Should().NotContainNulls();
        retainedTypes.Should().HaveCount(4);
    }
}
