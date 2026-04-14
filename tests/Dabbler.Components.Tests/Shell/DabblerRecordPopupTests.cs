using Bunit;
using Dabbler.Components.Data.Metadata;
using Dabbler.Components.Tests.TestHelpers;
using FluentAssertions;
using Xunit;

namespace Dabbler.Components.Tests.Shell;

public class DabblerRecordPopupTests
{
    [Fact]
    public async Task CleanClose_InvokesOnCloseCallback()
    {
        using var ctx = new TestContext().SetupForDabblerComponents();

        var record = new SampleRecord { Id = 9, Name = "Ada" };
        var closeCount = 0;
        var cut = ctx.RenderComponent<DabblerRecordPopup<SampleRecord>>(parameters => parameters
            .Add(x => x.EntityMetadata, BuildEntityMetadata())
            .Add(x => x.Record, record)
            .Add(x => x.OnClose, () => closeCount++));

        await cut.InvokeAsync(() =>
        {
            cut.Instance.Show();
            return cut.Instance.CloseAsync();
        });

        closeCount.Should().Be(1);
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
