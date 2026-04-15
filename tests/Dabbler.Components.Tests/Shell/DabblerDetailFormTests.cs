using Bunit;
using Dabbler.Components.Data.Metadata;
using Dabbler.Components.Tests.TestHelpers;
using FluentAssertions;
using Xunit;

namespace Dabbler.Components.Tests.Shell;

public class DabblerDetailFormTests
{
    [Fact]
    public void CoreEditing_TracksDirtyStateAndModifiedFields()
    {
        using var ctx = new TestContext().SetupForDabblerComponents();

        var record = new SampleRecord { Id = 7, Name = "Ada" };
        var cut = ctx.RenderComponent<DabblerDetailForm<SampleRecord>>(parameters => parameters
            .Add(x => x.EntityMetadata, BuildEntityMetadata())
            .Add(x => x.Record, record));

        cut.Instance.IsDirty.Should().BeFalse();
        cut.Instance.IsValid.Should().BeTrue();
        cut.Find("button[aria-label='Save']").HasAttribute("disabled").Should().BeTrue();

        cut.FindAll("input").Last().Change("Grace");

        cut.WaitForAssertion(() =>
        {
            cut.Instance.IsDirty.Should().BeTrue();
            cut.Instance.IsValid.Should().BeTrue();
            cut.Instance.GetModifiedFields().Should().ContainKey(nameof(SampleRecord.Name))
                .WhoseValue.Should().Be("Grace");
            cut.Find("button[aria-label='Save']").HasAttribute("disabled").Should().BeFalse();
        });
    }

    [Fact]
    public void RequiredFieldValidation_RendersAlertSemanticsAndKeepsSaveDisabled()
    {
        using var ctx = new TestContext().SetupForDabblerComponents();

        var record = new SampleRecord { Id = 7, Name = string.Empty };
        var cut = ctx.RenderComponent<DabblerDetailForm<SampleRecord>>(parameters => parameters
            .Add(x => x.EntityMetadata, BuildEntityMetadata())
            .Add(x => x.Record, record));

        cut.Instance.IsValid.Should().BeFalse();
        cut.Find("button[aria-label='Save']").HasAttribute("disabled").Should().BeTrue();

        cut.WaitForAssertion(() =>
        {
            cut.Find("#field-error-Name").GetAttribute("role").Should().Be("alert");
            cut.Find("#field-error-Name").TextContent.Should().Contain("Name is required");
        });
    }

    [Fact]
    public void ReadOnlyMode_DisablesToolbarActionsAndMarksEditableFieldsReadOnly()
    {
        using var ctx = new TestContext().SetupForDabblerComponents();

        var record = new SampleRecord { Id = 7, Name = "Ada" };
        var cut = ctx.RenderComponent<DabblerDetailForm<SampleRecord>>(parameters => parameters
            .Add(x => x.EntityMetadata, BuildEntityMetadata())
            .Add(x => x.Record, record)
            .Add(x => x.IsReadOnly, true));

        cut.Find("button[aria-label='Save']").HasAttribute("disabled").Should().BeTrue();
        cut.Find("button[aria-label='Undo all changes']").HasAttribute("disabled").Should().BeTrue();
        cut.Find("button[aria-label='Delete record']").HasAttribute("disabled").Should().BeTrue();
        cut.FindAll("input").Last().GetAttribute("aria-readonly").Should().Be("true");
    }

    private static EntityMetadata BuildEntityMetadata() =>
        new()
        {
            EntityName = "SampleRecord",
            DisplayName = "Sample Records",
            DisplayNameSingular = "Sample Record",
            DetailFormFields = [nameof(SampleRecord.Id), nameof(SampleRecord.Name)],
            Fields =
            [
                new FieldMetadata
                {
                    FieldName = nameof(SampleRecord.Id),
                    DisplayLabel = "Id",
                    DataType = "int",
                    IsPrimaryKey = true
                },
                new FieldMetadata
                {
                    FieldName = nameof(SampleRecord.Name),
                    DisplayLabel = "Name",
                    DataType = "string",
                    IsRequired = true
                }
            ]
        };

    private sealed class SampleRecord
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
