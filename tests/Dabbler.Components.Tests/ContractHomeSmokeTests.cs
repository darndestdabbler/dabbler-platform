using FluentAssertions;
using Xunit;

namespace Dabbler.Components.Tests;

public class ContractHomeSmokeTests
{
    [Fact]
    public void RetainedCoreTypes_AreAvailableToTheContractHome()
    {
        // After the MudBlazor-native rebuild, the custom wrapper components have been
        // removed. DabblerDetailForm, DabblerRecordPopup, DabblerComboBox, and
        // DabblerMultiComboBox were replaced by direct MudForm, MudDialog, and
        // MudAutocomplete usage in app pages. FilterPanel remains as preserved
        // platform value.
        var retainedTypes = new[]
        {
            typeof(FilterPanel<>),
        };

        retainedTypes.Should().NotContainNulls();
        retainedTypes.Should().HaveCount(1);
    }
}
