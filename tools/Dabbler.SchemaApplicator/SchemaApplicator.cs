using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Dabbler.SchemaApplicator;

/// <summary>
/// Pure substitution engine — cross-platform port of
/// <c>Apply-{Entity}Schema.ps1</c>.
///
/// Byte-identical-output contract:
/// outside the BEGIN/END marker regions, the input file is preserved verbatim
/// (encoding, line endings, EOF). Inside each region, the engine emits
/// <c>BEGIN_MARKER + "\r\n" + body + "\r\n" + indent + END_MARKER</c>, where
/// <c>body</c> is the snippets joined with <c>"\r\n"</c>. Each snippet's
/// own internal newlines are <c>"\n"</c> — matching the LF line endings of
/// the original PowerShell here-strings.
/// </summary>
public static class SchemaApplicator
{
    private const string GridBegin    = "@* DABBLER-SCAFFOLD:GRID-COLUMNS:BEGIN *@";
    private const string GridEnd      = "@* DABBLER-SCAFFOLD:GRID-COLUMNS:END *@";
    private const string FormBegin    = "@* DABBLER-SCAFFOLD:FORM-FIELDS:BEGIN *@";
    private const string FormEnd      = "@* DABBLER-SCAFFOLD:FORM-FIELDS:END *@";
    private const string FilterBegin  = "// DABBLER-SCAFFOLD:FILTER-PROPERTIES:BEGIN";
    private const string FilterEnd    = "// DABBLER-SCAFFOLD:FILTER-PROPERTIES:END";

    private const string EmptyGridBody    = "        @* No grid columns — schema had no isInGrid properties. *@";
    private const string EmptyFilterBody  = "            // No filter properties — schema had no isFilterable properties.";
    private const string EmptyFormBody    = "            @* No form fields — schema had no editable isInForm properties. *@";

    public sealed record ApplyResult(string ListContent, string DetailContent, int GridCount, int FormCount, int FilterCount);

    public static ApplyResult Apply(string schemaJson, string entitySingular, string listContent, string detailContent)
    {
        var schema = JsonSerializer.Deserialize<EntitySchema>(schemaJson)
            ?? throw new InvalidOperationException("Schema JSON parsed to null.");

        if (schema.Properties is null || schema.Properties.Count == 0)
            throw new InvalidOperationException("Schema has no 'properties' array.");

        var pkName = schema.PrimaryKey?.Name is { Length: > 0 } pk ? pk : "Id";

        var grid = new List<string>();
        var form = new List<string>();
        var filter = new List<string>();

        foreach (var prop in schema.Properties)
        {
            var name = prop.Name;
            var type = prop.Type;
            var classification = ClassifyType(type);
            var label = Humanize(name);
            var kebab = Kebab(name);
            var isReadOnly = prop.IsReadOnly;
            var isSortable = prop.IsSortable ?? true;
            var isPk = string.Equals(name, pkName, StringComparison.OrdinalIgnoreCase);

            if (prop.IsInGrid)
            {
                grid.Add(isSortable
                    ? $"        <PropertyColumn Property=\"e => e.{name}\" Title=\"{label}\" />"
                    : $"        <PropertyColumn Property=\"e => e.{name}\" Title=\"{label}\" Sortable=\"false\" />");
            }

            if (prop.IsInForm && !isPk)
            {
                form.Add(EmitFormSnippet(name, type, label, kebab, isReadOnly, classification));
            }

            if (prop.IsFilterable)
            {
                var filterLine = classification switch
                {
                    "Text"    => $"            new FilterProperty<{entitySingular}Dto>.Text {{ Property = e => (object?)e.{name}, Label = \"{label}\" }},",
                    "Bool"    => $"            new FilterProperty<{entitySingular}Dto>.Bool {{ Property = e => (object?)e.{name}, Label = \"{label}\" }},",
                    "Integer" => $"            new FilterProperty<{entitySingular}Dto>.Numeric {{ Property = e => (object?)e.{name}, Label = \"{label}\" }},",
                    "Decimal" => $"            new FilterProperty<{entitySingular}Dto>.Numeric {{ Property = e => (object?)e.{name}, Label = \"{label}\" }},",
                    "Date"    => $"            new FilterProperty<{entitySingular}Dto>.Date {{ Property = e => (object?)e.{name}, Label = \"{label}\" }},",
                    _         => null,
                };
                if (filterLine is not null) filter.Add(filterLine);
            }
        }

        var gridBody = grid.Count > 0 ? string.Join("\r\n", grid) : EmptyGridBody;
        var filterBody = filter.Count > 0 ? string.Join("\r\n", filter) : EmptyFilterBody;
        var formBody = form.Count > 0 ? string.Join("\r\n", form) : EmptyFormBody;

        var newList = SetRegion(listContent, GridBegin, GridEnd, gridBody, indent: 8);
        newList = SetRegion(newList, FilterBegin, FilterEnd, filterBody, indent: 12);
        var newDetail = SetRegion(detailContent, FormBegin, FormEnd, formBody, indent: 12);

        return new ApplyResult(newList, newDetail, grid.Count, form.Count, filter.Count);
    }

    private static string EmitFormSnippet(string name, string type, string label, string kebab, bool isReadOnly, string classification)
    {
        var readOnlyLiteral = isReadOnly ? "true" : "false";

        switch (classification)
        {
            case "Text":
                return
                    "            <MudItem xs=\"12\" md=\"6\">\n" +
                    "                <MudTextField T=\"string\"\n" +
                    $"                              Value=\"@_record.{name}\"\n" +
                    $"                              ValueChanged=\"@((string v) => {{ _record.{name} = v; _session?.NotifyChanged(); }})\"\n" +
                    $"                              Label=\"{label}\"\n" +
                    "                              Variant=\"Variant.Outlined\"\n" +
                    "                              Immediate=\"true\"\n" +
                    $"                              ReadOnly=\"{readOnlyLiteral}\"\n" +
                    $"                              data-testid=\"field-{kebab}\" />\n" +
                    "            </MudItem>";

            case "Bool":
                return
                    "            <MudItem xs=\"12\" md=\"6\">\n" +
                    "                <MudCheckBox T=\"bool\"\n" +
                    $"                             Value=\"@_record.{name}\"\n" +
                    $"                             ValueChanged=\"@((bool v) => {{ _record.{name} = v; _session?.NotifyChanged(); }})\"\n" +
                    $"                             Label=\"{label}\"\n" +
                    "                             Color=\"Color.Primary\"\n" +
                    $"                             ReadOnly=\"{readOnlyLiteral}\"\n" +
                    $"                             data-testid=\"field-{kebab}\" />\n" +
                    "            </MudItem>";

            case "Integer":
            {
                var tArg = MudFieldType(type);
                return
                    "            <MudItem xs=\"12\" md=\"6\">\n" +
                    $"                <MudNumericField T=\"{tArg}\"\n" +
                    $"                                 Value=\"@_record.{name}\"\n" +
                    $"                                 ValueChanged=\"@(({tArg} v) => {{ _record.{name} = v; _session?.NotifyChanged(); }})\"\n" +
                    $"                                 Label=\"{label}\"\n" +
                    "                                 Variant=\"Variant.Outlined\"\n" +
                    "                                 Immediate=\"true\"\n" +
                    $"                                 ReadOnly=\"{readOnlyLiteral}\"\n" +
                    $"                                 data-testid=\"field-{kebab}\" />\n" +
                    "            </MudItem>";
            }

            case "Decimal":
            {
                var tArg = MudFieldType(type);
                return
                    "            <MudItem xs=\"12\" md=\"6\">\n" +
                    $"                <MudNumericField T=\"{tArg}\"\n" +
                    $"                                 Value=\"@_record.{name}\"\n" +
                    $"                                 ValueChanged=\"@(({tArg} v) => {{ _record.{name} = v; _session?.NotifyChanged(); }})\"\n" +
                    $"                                 Label=\"{label}\"\n" +
                    "                                 Variant=\"Variant.Outlined\"\n" +
                    "                                 Immediate=\"true\"\n" +
                    "                                 Format=\"N2\"\n" +
                    $"                                 ReadOnly=\"{readOnlyLiteral}\"\n" +
                    $"                                 data-testid=\"field-{kebab}\" />\n" +
                    "            </MudItem>";
            }

            case "Date":
                return
                    "            <MudItem xs=\"12\" md=\"6\">\n" +
                    $"                <MudDatePicker Date=\"@_record.{name}\"\n" +
                    $"                               DateChanged=\"@((DateTime? v) => {{ _record.{name} = v ?? default; _session?.NotifyChanged(); }})\"\n" +
                    $"                               Label=\"{label}\"\n" +
                    "                               Variant=\"Variant.Outlined\"\n" +
                    "                               DateFormat=\"MM/dd/yyyy\"\n" +
                    $"                               ReadOnly=\"{readOnlyLiteral}\"\n" +
                    $"                               data-testid=\"field-{kebab}\" />\n" +
                    "            </MudItem>";

            default:
                return
                    $"            <MudItem xs=\"12\" md=\"6\" data-testid=\"field-{kebab}\">\n" +
                    $"                @* TODO: hand-edit — schema type '{type}' is not auto-scaffolded. Add a MudBlazor field for _record.{name} and call _session?.NotifyChanged() on change. *@\n" +
                    $"                <MudText Typo=\"Typo.body2\" Color=\"Color.Warning\">{label} ({type}) — hand-edit required</MudText>\n" +
                    "            </MudItem>";
        }
    }

    private static string SetRegion(string content, string beginMarker, string endMarker, string body, int indent)
    {
        var pattern = Regex.Escape(beginMarker) + ".*?" + Regex.Escape(endMarker);
        var rx = new Regex(pattern, RegexOptions.Singleline);
        var indentSpaces = new string(' ', indent);
        var replacement = beginMarker + "\r\n" + body + "\r\n" + indentSpaces + endMarker;

        if (!rx.IsMatch(content))
        {
            // Match PowerShell behavior: warn (here, swallow — same effect as Write-Warning + skipped) and return as-is.
            return content;
        }

        return rx.Replace(content, m => replacement, count: 1);
    }

    private static string ClassifyType(string type)
    {
        if (Regex.IsMatch(type, "^(string|str|text)$", RegexOptions.IgnoreCase))            return "Text";
        if (Regex.IsMatch(type, "^(bool|boolean)$", RegexOptions.IgnoreCase))               return "Bool";
        if (Regex.IsMatch(type, "^(int|int32|short|int16)$", RegexOptions.IgnoreCase))      return "Integer";
        if (Regex.IsMatch(type, "^(long|int64)$", RegexOptions.IgnoreCase))                 return "Integer";
        if (Regex.IsMatch(type, "^(decimal|double|float|money)$", RegexOptions.IgnoreCase)) return "Decimal";
        if (Regex.IsMatch(type, "^(datetime)$", RegexOptions.IgnoreCase))                   return "Date";
        return "Unknown";
    }

    private static string MudFieldType(string type)
    {
        if (Regex.IsMatch(type, "^(int|int32|short|int16)$", RegexOptions.IgnoreCase)) return "int";
        if (Regex.IsMatch(type, "^(long|int64)$", RegexOptions.IgnoreCase))            return "long";
        if (Regex.IsMatch(type, "^(decimal|money)$", RegexOptions.IgnoreCase))         return "decimal";
        if (Regex.IsMatch(type, "^(double|float)$", RegexOptions.IgnoreCase))          return "double";
        return type;
    }

    private static string Humanize(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;
        return Regex.Replace(name, "([a-z0-9])([A-Z])", "$1 $2");
    }

    private static string Kebab(string name)
    {
        return Regex.Replace(Humanize(name), @"\s+", "-").ToLowerInvariant();
    }
}
