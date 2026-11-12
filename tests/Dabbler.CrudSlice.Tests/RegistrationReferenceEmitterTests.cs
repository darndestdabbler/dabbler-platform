using Dabbler.SchemaApplicator;
using FluentAssertions;
using Xunit;

namespace Dabbler.CrudSlice.Tests;

/// <summary>
/// Pure-substitution unit tests for <see cref="RegistrationReferenceEmitter"/>. The
/// emitter writes a copy-paste reference file containing four blocks the consumer
/// pastes into their Api/Program.cs and Web/Program.cs AddDabblerSecurity(...) lambdas.
/// Assertions verify the four block headers exist, the rendered shapes match the
/// real Supplier wiring extracted from Web/Program.cs and Api/Program.cs at spec
/// authoring time (Session 3 of crud-scaffolding-remediation, Step 1).
/// </summary>
public class RegistrationReferenceEmitterTests
{
    private static CliOptions SupplierOpts() =>
        new(
            Entity: "Supplier",
            EntityPlural: "Suppliers",
            SchemaPath: "supplier.dabbler-entity.json",
            WebRoot: "/tmp/Web",
            ApiRoot: "/tmp/Api",
            WebNamespace: "AcmeCoffeeOnline.Web",
            ApiNamespace: "AcmeCoffeeOnline.Api",
            SharedNamespace: "AcmeCoffeeOnline.Shared",
            DtoNamespace: "AcmeCoffeeOnline.Shared.DTOs",
            EntityNamespace: "AcmeCoffeeOnline.Shared.Models",
            DbContextType: "AcmeCoffeeDbContext",
            DbContextNamespace: "AcmeCoffeeOnline.Api.Data",
            PkType: "int",
            AuthPolicyPrefix: null,
            ApiBaseUrlConfigKey: "Services:ApiBaseUrl",
            ApiBaseUrlFallbackConfigKey: "ApiBaseUrl",
            ApiBaseUrlDefault: "http://localhost:5011",
            InternalApiKeyConfigKey: "DabblerSecurity:InternalApiKey",
            InternalApiKeyDefault: "dev-internal-key-acme",
            TemplateHive: null,
            SkipTemplates: false,
            SkipSchemaApplicator: false,
            SkipRegistrations: false);

    private static EntitySchema SupplierSchema() => new()
    {
        Entity = "Supplier",
        PrimaryKey = new PrimaryKey { Name = "Id", Type = "int" },
        Properties =
        {
            new EntityProperty { Name = "Id",           Type = "int" },
            new EntityProperty { Name = "Name",         Type = "string" },
            new EntityProperty { Name = "City",         Type = "string" },
            new EntityProperty { Name = "Country",      Type = "string" },
            new EntityProperty { Name = "ContactEmail", Type = "string" },
            new EntityProperty { Name = "IsActive",     Type = "bool" },
        }
    };

    [Fact]
    public void Render_includes_all_four_copy_paste_block_headers()
    {
        var rendered = RegistrationReferenceEmitter.Render(SupplierOpts(), SupplierSchema());

        rendered.Should().Contain("// ── Copy into Api/Program.cs RegisterEntities(...) ");
        rendered.Should().Contain("// ── Copy into Api/Program.cs RegisterEndpoints(...) ");
        rendered.Should().Contain("// ── Copy into Web/Program.cs RegisterPages(...) ");
        rendered.Should().Contain("// ── Copy into Web/Program.cs RegisterEndpoints(...) ");
    }

    [Fact]
    public void Render_wraps_body_in_if_false_so_file_never_compiles()
    {
        var rendered = RegistrationReferenceEmitter.Render(SupplierOpts(), SupplierSchema());

        rendered.Should().Contain("#if false",
            "the reference file must never compile even if accidentally added to a csproj compile glob");
        rendered.Should().Contain("#endif");
        // The four block headers must land inside the #if false / #endif fence.
        // Use LastIndexOf so the closing #endif directive wins over the same token
        // mentioned in the file-purpose comment at the top of the header.
        var ifIdx = rendered.LastIndexOf("#if false", StringComparison.Ordinal);
        var endIdx = rendered.LastIndexOf("#endif", StringComparison.Ordinal);
        ifIdx.Should().BeGreaterThan(0);
        endIdx.Should().BeGreaterThan(ifIdx);
        rendered.IndexOf("// ── Copy into Api/Program.cs RegisterEntities").Should().BeGreaterThan(ifIdx);
        rendered.IndexOf("// ── Copy into Web/Program.cs RegisterEndpoints").Should().BeLessThan(endIdx);
    }

    [Fact]
    public void Entity_block_matches_Api_Program_cs_94_103_shape()
    {
        var rendered = RegistrationReferenceEmitter.Render(SupplierOpts(), SupplierSchema());

        // Matches Api/Program.cs:94-103 — entity Add lambda with fields.Add(...) per property.
        rendered.Should().Contain("entities.Add(\"Supplier\", \"Supplier\", fields =>");
        rendered.Should().Contain("fields.Add(\"Name\", \"Name\");");
        rendered.Should().Contain("fields.Add(\"City\", \"City\");");
        rendered.Should().Contain("fields.Add(\"Country\", \"Country\");");
        rendered.Should().Contain("fields.Add(\"ContactEmail\", \"Contact Email\");");
        rendered.Should().Contain("fields.Add(\"IsActive\", \"Is Active\");");
        // PK is excluded — matches the Customer/Order/Supplier blocks in Api/Program.cs
        // which never list Id as a field.
        rendered.Should().NotContain("fields.Add(\"Id\",");
    }

    [Fact]
    public void Api_endpoint_block_matches_Api_Program_cs_135_139_singular_shape()
    {
        var rendered = RegistrationReferenceEmitter.Render(SupplierOpts(), SupplierSchema());

        // Matches Api/Program.cs:135-139 — singular Query{Entity} / Get{Entity}.
        rendered.Should().Contain("endpoints.Add(\"QuerySupplier\", \"Query Suppliers\", \"POST\", \"/api/suppliers/query\");");
        rendered.Should().Contain("endpoints.Add(\"GetSupplier\", \"GET Supplier by ID\", \"GET\", \"/api/suppliers/{id}\");");
        rendered.Should().Contain("endpoints.Add(\"CreateSupplier\", \"Create Supplier\", \"POST\", \"/api/suppliers\");");
        rendered.Should().Contain("endpoints.Add(\"UpdateSupplier\", \"Update Supplier\", \"PUT\", \"/api/suppliers/{id}\");");
        rendered.Should().Contain("endpoints.Add(\"DeleteSupplier\", \"Delete Supplier\", \"DELETE\", \"/api/suppliers/{id}\");");
    }

    [Fact]
    public void Web_endpoint_block_matches_Web_Program_cs_189_194_plural_byId_shape()
    {
        var rendered = RegistrationReferenceEmitter.Render(SupplierOpts(), SupplierSchema());

        // Matches Web/Program.cs:189-194 — Query{EntityPlural} / Get{Entity}ById. Note
        // the host-side identifier drift between Api and Web — see RegistrationReferenceEmitter
        // header documentation. CRUD identifiers (Create/Update/Delete) are identical across
        // both sides.
        rendered.Should().Contain("endpoints.Add(\"QuerySuppliers\", \"Query Suppliers\", \"POST\", \"/api/suppliers/query\");");
        rendered.Should().Contain("endpoints.Add(\"GetSupplierById\", \"GET Supplier by ID\", \"GET\", \"/api/suppliers/{id}\");");
        rendered.Should().Contain("endpoints.Add(\"CreateSupplier\", \"Create Supplier\", \"POST\", \"/api/suppliers\");");
        rendered.Should().Contain("endpoints.Add(\"UpdateSupplier\", \"Update Supplier\", \"PUT\", \"/api/suppliers/{id}\");");
        rendered.Should().Contain("endpoints.Add(\"DeleteSupplier\", \"Delete Supplier\", \"DELETE\", \"/api/suppliers/{id}\");");
    }

    [Fact]
    public void Page_block_matches_Web_Program_cs_supplier_page_shape()
    {
        var rendered = RegistrationReferenceEmitter.Render(SupplierOpts(), SupplierSchema());

        // Matches the Supplier line in Web/Program.cs RegisterPages(...).
        rendered.Should().Contain("pages.Add(\"/suppliers\", \"Suppliers\", \"Supplier management\");");
    }

    [Fact]
    public void Page_path_is_lowercased_plural_even_when_entity_plural_is_camel_case()
    {
        var opts = SupplierOpts() with { Entity = "PurchaseOrder", EntityPlural = "PurchaseOrders" };
        var rendered = RegistrationReferenceEmitter.Render(opts, new EntitySchema { Entity = "PurchaseOrder" });

        rendered.Should().Contain("pages.Add(\"/purchaseorders\", \"PurchaseOrders\", \"PurchaseOrder management\");");
        // API side uses singular Query{Entity}; Web side uses plural Query{EntityPlural}.
        rendered.Should().Contain("endpoints.Add(\"QueryPurchaseOrder\", \"Query PurchaseOrders\", \"POST\", \"/api/purchaseorders/query\");");
        rendered.Should().Contain("endpoints.Add(\"QueryPurchaseOrders\", \"Query PurchaseOrders\", \"POST\", \"/api/purchaseorders/query\");");
    }

    [Fact]
    public void Both_endpoint_blocks_emit_so_each_side_has_paste_targets()
    {
        var rendered = RegistrationReferenceEmitter.Render(SupplierOpts(), SupplierSchema());

        // CRUD identifiers (Create/Update/Delete) are identical across both sides — they
        // appear once per block, so each block contributes one occurrence.
        CountOccurrences(rendered, "endpoints.Add(\"CreateSupplier\"").Should().Be(2);
        CountOccurrences(rendered, "endpoints.Add(\"UpdateSupplier\"").Should().Be(2);
        CountOccurrences(rendered, "endpoints.Add(\"DeleteSupplier\"").Should().Be(2);
        // The first-two-endpoint identifiers differ per side, so each appears exactly once.
        CountOccurrences(rendered, "endpoints.Add(\"QuerySupplier\",").Should().Be(1);
        CountOccurrences(rendered, "endpoints.Add(\"QuerySuppliers\",").Should().Be(1);
        CountOccurrences(rendered, "endpoints.Add(\"GetSupplier\",").Should().Be(1);
        CountOccurrences(rendered, "endpoints.Add(\"GetSupplierById\",").Should().Be(1);
    }

    [Fact]
    public void Empty_schema_emits_an_entity_block_with_no_fields_but_remains_valid()
    {
        // EmitRegistrationsOnly path passes a stub schema with empty properties.
        var rendered = RegistrationReferenceEmitter.Render(
            SupplierOpts() with { Entity = "Widget", EntityPlural = "Widgets" },
            new EntitySchema { Entity = "Widget" });

        rendered.Should().Contain("entities.Add(\"Widget\", \"Widget\", fields =>");
        rendered.Should().Contain("{\n});",
            "the lambda body is empty when the schema has no properties — still valid C#");
        // The page + endpoint blocks do not depend on the property list and must still emit.
        rendered.Should().Contain("pages.Add(\"/widgets\", \"Widgets\", \"Widget management\");");
        // Create/Update/Delete identifiers are identical across both sides.
        rendered.Should().Contain("endpoints.Add(\"CreateWidget\", \"Create Widget\", \"POST\", \"/api/widgets\");");
        // First-two endpoint identifiers differ per side; both shapes must appear.
        rendered.Should().Contain("endpoints.Add(\"QueryWidget\", \"Query Widgets\",");
        rendered.Should().Contain("endpoints.Add(\"QueryWidgets\", \"Query Widgets\",");
        rendered.Should().Contain("endpoints.Add(\"GetWidget\", \"GET Widget by ID\",");
        rendered.Should().Contain("endpoints.Add(\"GetWidgetById\", \"GET Widget by ID\",");
    }

    [Fact]
    public void Header_documents_the_shape_source_line_ranges()
    {
        var rendered = RegistrationReferenceEmitter.Render(SupplierOpts(), SupplierSchema());

        // The reference file's header should name where the shapes were extracted from
        // so a future maintainer auditing the generator can re-verify against the host.
        rendered.Should().Contain("Api/Program.cs:94-103");
        rendered.Should().Contain("Api/Program.cs:135-139");
        rendered.Should().Contain("Web/Program.cs:155-162");
        rendered.Should().Contain("Web/Program.cs:189-194");
    }

    [Theory]
    [InlineData("Name", "Name")]
    [InlineData("FirstName", "First Name")]
    [InlineData("ContactEmail", "Contact Email")]
    [InlineData("IsActive", "Is Active")]
    [InlineData("SocialSecurityNumber", "Social Security Number")]
    [InlineData("ZipCode", "Zip Code")]
    [InlineData("URLPath", "URL Path")]
    [InlineData("",        "")]
    public void PrettifyCamelCase_splits_pascal_case_into_spaced_words(string input, string expected)
    {
        RegistrationReferenceEmitter.PrettifyCamelCase(input).Should().Be(expected);
    }

    [Fact]
    public void LoadSchemaOrEmpty_returns_empty_schema_when_file_missing()
    {
        var schema = RegistrationReferenceEmitter.LoadSchemaOrEmpty(
            Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.json"));
        schema.Properties.Should().BeEmpty();
        schema.Entity.Should().BeNull();
    }

    [Fact]
    public void LoadSchemaOrEmpty_tolerates_unparseable_json_without_throwing()
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"bad-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(tmp, "{ not valid json");
            var schema = RegistrationReferenceEmitter.LoadSchemaOrEmpty(tmp);
            schema.Properties.Should().BeEmpty();
        }
        finally
        {
            try { File.Delete(tmp); } catch { /* best effort */ }
        }
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0, idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) != -1)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }
}
