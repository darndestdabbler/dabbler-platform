using System.Reflection;
using Bunit;
using Dabbler.Components.Tests.TestHelpers;
using FluentAssertions;
using Xunit;

namespace Dabbler.Components.Tests.ComboBox;

public class DabblerComboBoxContractTests
{
    [Fact]
    public void CoreSurface_KeepsLookupAndRetainedMultiselectMembers()
    {
        var comboProperties = GetPublicPropertyNames(typeof(DabblerComboBox<string>));
        comboProperties.Should().Contain(["Value", "ValueChanged", "SearchFunc", "Columns", "Label"]);
        comboProperties.Should().NotContain([
            "AllowAddNew",
            "OnAddNewItem",
            "AddNewTemplate",
            "AddNewPromptText",
            "GroupFunc",
            "GroupSortFunc"
        ]);

        var multiComboProperties = GetPublicPropertyNames(typeof(DabblerMultiComboBox<string>));
        multiComboProperties.Should().Contain(["Values", "ValuesChanged", "SearchFunc", "Columns", "MaxSelections"]);
        multiComboProperties.Should().NotContain([
            "AllowAddNew",
            "OnAddNewItem",
            "AddNewTemplate",
            "AddNewPromptText",
            "GroupFunc",
            "GroupSortFunc"
        ]);
    }

    [Fact]
    public void RetainedMultiSelect_RendersSelectedItemsAsChips()
    {
        using var ctx = new TestContext().SetupForDabblerComponents();

        var cut = ctx.RenderComponent<DabblerMultiComboBox<string>>(parameters => parameters
            .Add(x => x.Values, new[] { "Ada", "Grace" })
            .Add(x => x.ToStringFunc, value => value)
            .Add(x => x.SearchFunc, (_, _) => Task.FromResult<IEnumerable<string>>(["Ada", "Grace", "Linus"])));

        cut.Find("[aria-label='Selected items']");
        cut.Markup.Should().Contain("Ada");
        cut.Markup.Should().Contain("Grace");
    }

    private static IReadOnlyCollection<string> GetPublicPropertyNames(Type componentType) =>
        componentType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToArray();
}
