using FluentAssertions;
using Xunit;

namespace Dabbler.SchemaApplicator.Tests;

/// <summary>
/// Byte-identical-output proof for <see cref="SchemaApplicator"/> against the
/// PowerShell baseline. The Supplier scaffold committed under
/// <c>src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Suppliers/</c> is the
/// reference output of <c>Apply-SupplierSchema.ps1</c>; fixtures copy it
/// verbatim and the engine must reproduce it byte-for-byte from both an
/// already-populated input (idempotency) and a fresh-stamp template input.
/// </summary>
public class SchemaApplicatorTests
{
    private static readonly string FixturesDir = Path.Combine(
        Path.GetDirectoryName(typeof(SchemaApplicatorTests).Assembly.Location)!,
        "Fixtures");

    private static string ReadFixture(string name) => File.ReadAllText(Path.Combine(FixturesDir, name));

    [Fact]
    public void Apply_OnPopulatedSupplierFiles_IsIdempotent()
    {
        var schema = ReadFixture("supplier.dabbler-entity.json");
        var listInput = ReadFixture("SupplierList.razor.populated");
        var detailInput = ReadFixture("SupplierDetail.razor.populated");

        var result = SchemaApplicator.Apply(schema, "Supplier", listInput, detailInput);

        result.ListContent.Should().Be(listInput, "running on the PowerShell output should be a no-op");
        result.DetailContent.Should().Be(detailInput);
        result.GridCount.Should().Be(6);
        result.FormCount.Should().Be(5);
        result.FilterCount.Should().Be(5);
    }

    [Fact]
    public void Apply_OnFreshTemplateInput_ProducesBaselinePowerShellOutput()
    {
        var schema = ReadFixture("supplier.dabbler-entity.json");
        var listInput = ReadFixture("SupplierList.razor.template");
        var detailInput = ReadFixture("SupplierDetail.razor.template");
        var listExpected = ReadFixture("SupplierList.razor.populated");
        var detailExpected = ReadFixture("SupplierDetail.razor.populated");

        var result = SchemaApplicator.Apply(schema, "Supplier", listInput, detailInput);

        // The fresh-stamped template has LF line endings everywhere outside the
        // injected regions. The PowerShell output preserves those LF endings
        // and emits CRLF only around the BEGIN/END markers and between snippets
        // (with LF inside each snippet's body). This assertion proves the C#
        // engine matches that byte pattern.
        result.ListContent.Should().Be(listExpected);
        result.DetailContent.Should().Be(detailExpected);
    }

    [Fact]
    public void Apply_OnFreshTemplateInput_ProducesByteIdenticalUtf8()
    {
        var schema = ReadFixture("supplier.dabbler-entity.json");
        var listInput = ReadFixture("SupplierList.razor.template");
        var detailInput = ReadFixture("SupplierDetail.razor.template");

        var result = SchemaApplicator.Apply(schema, "Supplier", listInput, detailInput);

        var listBytes = System.Text.Encoding.UTF8.GetBytes(result.ListContent);
        var expectedListBytes = File.ReadAllBytes(Path.Combine(FixturesDir, "SupplierList.razor.populated"));
        listBytes.Should().Equal(expectedListBytes);

        var detailBytes = System.Text.Encoding.UTF8.GetBytes(result.DetailContent);
        var expectedDetailBytes = File.ReadAllBytes(Path.Combine(FixturesDir, "SupplierDetail.razor.populated"));
        detailBytes.Should().Equal(expectedDetailBytes);
    }

    [Fact]
    public void Apply_OmittedPrimaryKey_DefaultsToId()
    {
        var schema = """
        {
          "entity": "Foo",
          "properties": [
            { "name": "Id",   "type": "int",    "isInGrid": true, "isFilterable": true, "isReadOnly": true },
            { "name": "Name", "type": "string", "isInForm": true }
          ]
        }
        """;
        var listInput =
            "@* DABBLER-SCAFFOLD:GRID-COLUMNS:BEGIN *@\n" +
            "@* DABBLER-SCAFFOLD:GRID-COLUMNS:END *@\n" +
            "// DABBLER-SCAFFOLD:FILTER-PROPERTIES:BEGIN\n" +
            "// DABBLER-SCAFFOLD:FILTER-PROPERTIES:END\n";
        var detailInput =
            "@* DABBLER-SCAFFOLD:FORM-FIELDS:BEGIN *@\n" +
            "@* DABBLER-SCAFFOLD:FORM-FIELDS:END *@\n";

        var result = SchemaApplicator.Apply(schema, "Foo", listInput, detailInput);

        result.GridCount.Should().Be(1);
        result.FormCount.Should().Be(1, "Id is the PK so it is excluded from the form; Name remains");
        result.FilterCount.Should().Be(1);
    }

    [Fact]
    public void Apply_UnsupportedType_EmitsHandEditPlaceholder()
    {
        var schema = """
        {
          "entity": "Foo",
          "primaryKey": { "name": "Id", "type": "Guid" },
          "properties": [
            { "name": "Token", "type": "Guid", "isInForm": true }
          ]
        }
        """;
        var detailInput =
            "@* DABBLER-SCAFFOLD:FORM-FIELDS:BEGIN *@\n" +
            "@* DABBLER-SCAFFOLD:FORM-FIELDS:END *@\n";

        var result = SchemaApplicator.Apply(schema, "Foo", string.Empty, detailInput);

        result.DetailContent.Should().Contain("hand-edit required");
        result.DetailContent.Should().Contain("Token (Guid)");
        result.FormCount.Should().Be(1);
    }

    [Fact]
    public void Apply_NoMatchingProperties_EmitsEmptyStateMessage()
    {
        var schema = """
        {
          "entity": "Foo",
          "primaryKey": { "name": "Id", "type": "int" },
          "properties": [
            { "name": "Id", "type": "int", "isReadOnly": true }
          ]
        }
        """;
        var listInput =
            "@* DABBLER-SCAFFOLD:GRID-COLUMNS:BEGIN *@\n" +
            "@* DABBLER-SCAFFOLD:GRID-COLUMNS:END *@\n" +
            "// DABBLER-SCAFFOLD:FILTER-PROPERTIES:BEGIN\n" +
            "// DABBLER-SCAFFOLD:FILTER-PROPERTIES:END\n";
        var detailInput =
            "@* DABBLER-SCAFFOLD:FORM-FIELDS:BEGIN *@\n" +
            "@* DABBLER-SCAFFOLD:FORM-FIELDS:END *@\n";

        var result = SchemaApplicator.Apply(schema, "Foo", listInput, detailInput);

        result.ListContent.Should().Contain("No grid columns");
        result.ListContent.Should().Contain("No filter properties");
        result.DetailContent.Should().Contain("No form fields");
        result.GridCount.Should().Be(0);
        result.FilterCount.Should().Be(0);
        result.FormCount.Should().Be(0);
    }

    [Fact]
    public void Apply_HumanizesAndKebabsCompoundNames()
    {
        var schema = """
        {
          "entity": "Foo",
          "primaryKey": { "name": "Id", "type": "int" },
          "properties": [
            { "name": "ContactEmail", "type": "string", "isInGrid": true, "isInForm": true, "isFilterable": true }
          ]
        }
        """;
        var listInput =
            "@* DABBLER-SCAFFOLD:GRID-COLUMNS:BEGIN *@\n" +
            "@* DABBLER-SCAFFOLD:GRID-COLUMNS:END *@\n" +
            "// DABBLER-SCAFFOLD:FILTER-PROPERTIES:BEGIN\n" +
            "// DABBLER-SCAFFOLD:FILTER-PROPERTIES:END\n";
        var detailInput =
            "@* DABBLER-SCAFFOLD:FORM-FIELDS:BEGIN *@\n" +
            "@* DABBLER-SCAFFOLD:FORM-FIELDS:END *@\n";

        var result = SchemaApplicator.Apply(schema, "Foo", listInput, detailInput);

        result.ListContent.Should().Contain("Title=\"Contact Email\"");
        result.ListContent.Should().Contain("Label = \"Contact Email\"");
        result.DetailContent.Should().Contain("Label=\"Contact Email\"");
        result.DetailContent.Should().Contain("data-testid=\"field-contact-email\"");
    }
}
