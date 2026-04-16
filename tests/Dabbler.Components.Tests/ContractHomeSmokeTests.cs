using Dabbler.Components.Data;
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
        // MudAutocomplete usage in app pages. FilterPanel and its supporting filter
        // model remain as preserved platform value.
        var retainedTypes = new[]
        {
            typeof(FilterPanel<>),
            typeof(FilteringCoordinator),
            typeof(FilterColumnDefinition),
            typeof(FilterGroupCollection),
            typeof(FilterGroup),
            typeof(CompositeFilterDescriptor),
            typeof(FilterDescriptor),
            typeof(FilterSummaryBuilder),
        };

        retainedTypes.Should().NotContainNulls();
        retainedTypes.Should().HaveCount(8);
    }
}
