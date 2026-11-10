using System.Text;
using Dabbler.SchemaApplicator;

namespace Dabbler.CrudSlice;

/// <summary>
/// Invokes the <see cref="SchemaApplicator"/> in-process to fill the schema-driven
/// GRID-COLUMNS / FORM-FIELDS / FILTER-PROPERTIES regions of the dabbler-crud-page
/// template's emitted <c>{Entity}List.razor</c> and <c>{Entity}Detail.razor</c>.
/// </summary>
internal static class SchemaApplicatorRunner
{
    public static void Apply(CliOptions opts)
    {
        var pagesDir = Path.Combine(opts.WebRoot, "Components", "Pages", opts.EntityPlural);
        var listPath = Path.Combine(pagesDir, $"{opts.Entity}List.razor");
        var detailPath = Path.Combine(pagesDir, $"{opts.Entity}Detail.razor");

        if (!File.Exists(listPath))
            throw new FileNotFoundException(
                $"Schema applicator: expected list file not found at {listPath}. " +
                "Did the dabbler-crud-page template run? (Pass --skip-templates only when the slice is already on disk.)");
        if (!File.Exists(detailPath))
            throw new FileNotFoundException(
                $"Schema applicator: expected detail file not found at {detailPath}.");
        if (!File.Exists(opts.SchemaPath))
            throw new FileNotFoundException($"Schema file not found at {opts.SchemaPath}.");

        var schemaJson = File.ReadAllText(opts.SchemaPath);
        var listContent = File.ReadAllText(listPath);
        var detailContent = File.ReadAllText(detailPath);

        var result = SchemaApplicator.SchemaApplicator.Apply(schemaJson, opts.Entity, listContent, detailContent);

        var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        File.WriteAllText(listPath, result.ListContent, utf8NoBom);
        File.WriteAllText(detailPath, result.DetailContent, utf8NoBom);

        Console.WriteLine(
            $"Apply{opts.Entity}Schema: wrote {result.GridCount} grid columns, " +
            $"{result.FormCount} form fields, {result.FilterCount} filter properties.");
    }
}
