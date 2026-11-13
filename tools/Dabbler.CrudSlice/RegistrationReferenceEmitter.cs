using System.Reflection;
using System.Text;
using System.Text.Json;
using Dabbler.SchemaApplicator;

namespace Dabbler.CrudSlice;

/// <summary>
/// Emits a single <c>{Entity}Registrations.g.cs</c> reference file containing four
/// clearly-commented blocks the consumer copy-pastes into <c>Api/Program.cs</c> and
/// <c>Web/Program.cs</c>:
///
/// <list type="bullet">
///   <item>API entity registration (paste inside <c>options.RegisterEntities(...)</c>)</item>
///   <item>API endpoint registrations (paste inside <c>options.RegisterEndpoints(...)</c>)</item>
///   <item>Web page registration (paste inside <c>options.RegisterPages(...)</c>)</item>
///   <item>Web endpoint registrations (paste inside <c>options.RegisterEndpoints(...)</c>)</item>
/// </list>
///
/// The emitted shapes match the real Supplier wiring extracted at spec authoring time:
/// <c>Api/Program.cs:94-103</c> (entity) and <c>:135-139</c> (endpoints), and
/// <c>Web/Program.cs:155-162</c> (entity) and <c>:189-194</c> (endpoints). The API
/// and Web endpoint blocks emit per-side identifiers — API uses singular
/// <c>Query{Entity}</c> / <c>Get{Entity}</c>; Web uses <c>Query{EntityPlural}</c> /
/// <c>Get{Entity}ById</c>. This mirrors the host's pre-existing identifier drift.
///
/// The file body is wrapped in <c>#if false ... #endif</c> so it never compiles even
/// if accidentally included in a csproj's compile glob — the file is a copy-paste
/// reference, not a runtime artifact.
///
/// The wrapper does NOT edit <c>Program.cs</c> (that would be Granularity 3 — out of
/// scope per the crud-scaffolding-remediation spec's anti-patterns).
///
/// <para>
/// Block shapes are loaded from <c>Templates/*.template</c> files embedded as
/// resources (see <c>Templates/README.md</c> for the placeholder grammar). Edit
/// a <c>.template</c> file → next wrapper run picks up the change.
/// </para>
/// </summary>
internal static class RegistrationReferenceEmitter
{
    public static string Render(CliOptions opts, EntitySchema schema)
    {
        var pathPlural = opts.EntityPlural.ToLowerInvariant();
        var pkName = schema.PrimaryKey?.Name ?? "Id";
        var fieldRows = BuildFieldRows(schema, pkName);

        var sb = new StringBuilder();
        sb.Append($"// {opts.Entity}Registrations.g.cs — emitted by Dabbler.CrudSlice\n");
        sb.Append("//\n");
        sb.Append($"// Copy-paste reference for wiring the scaffolded {opts.Entity} slice into your host\n");
        sb.Append("// Program.cs files. Each block below is a self-contained chunk to paste inside the\n");
        sb.Append("// matching lambda inside AddDabblerSecurity(...). The whole file body is wrapped in\n");
        sb.Append("// #if false / #endif so it never compiles even if it lands in a csproj compile glob.\n");
        sb.Append("//\n");
        sb.Append("// Shapes match the host's real Supplier wiring:\n");
        sb.Append("//   * API entity:    Api/Program.cs:94-103\n");
        sb.Append("//   * API endpoints: Api/Program.cs:135-139\n");
        sb.Append("//   * Web page:      Web/Program.cs:155-162\n");
        sb.Append("//   * Web endpoints: Web/Program.cs:189-194\n");
        sb.Append("//\n");
        sb.Append($"// After pasting, also wire the convention-correct DI helpers from the sibling\n");
        sb.Append($"// {opts.Entity}ScaffoldingExtensions.cs / {opts.Entity}ApiRegistrationExtensions.cs files:\n");
        sb.Append($"//   Web/Program.cs: builder.Services.Add{opts.Entity}Scaffolding(builder.Configuration);\n");
        sb.Append($"//   Api/Program.cs: builder.Services.Add{opts.Entity}ApiServices();\n");
        sb.Append("\n");
        sb.Append("#if false  // copy-paste reference only — do not compile\n");
        sb.Append("\n");

        var entitySubs = new Dictionary<string, string>
        {
            ["Entity"] = opts.Entity,
            ["EntityPlural"] = opts.EntityPlural,
            ["PathPlural"] = pathPlural,
            ["FieldRows"] = RenderFieldRows(fieldRows),
        };

        sb.Append($"// ── Copy into Api/Program.cs RegisterEntities(...) ────────────────────────\n");
        sb.Append(ApplyTemplate("ApiEntityBlock", entitySubs));
        sb.Append("\n");

        sb.Append($"// ── Copy into Api/Program.cs RegisterEndpoints(...) ────────────────────────\n");
        sb.Append("// (Matches Api/Program.cs:135-139 shape — singular Query/Get identifiers.)\n");
        sb.Append(ApplyTemplate("ApiEndpointBlock", entitySubs));
        sb.Append("\n");

        sb.Append($"// ── Copy into Web/Program.cs RegisterPages(...) ────────────────────────────\n");
        sb.Append(ApplyTemplate("WebPageBlock", entitySubs));
        sb.Append("\n");

        sb.Append($"// ── Copy into Web/Program.cs RegisterEndpoints(...) ────────────────────────\n");
        sb.Append("// (Matches Web/Program.cs:189-194 shape — pre-existing identifier drift vs. Api:\n");
        sb.Append("//  Query{EntityPlural} and Get{Entity}ById on this side. Display labels + URLs are\n");
        sb.Append("//  identical to the API block; only the first two registration identifiers differ.)\n");
        sb.Append(ApplyTemplate("WebEndpointBlock", entitySubs));
        sb.Append("\n");

        sb.Append("#endif  // copy-paste reference only — do not compile\n");
        return sb.ToString();
    }

    private static string RenderFieldRows(List<(string Name, string Label)> rows)
    {
        if (rows.Count == 0) return string.Empty;
        var sb = new StringBuilder();
        foreach (var (name, label) in rows)
        {
            sb.Append(ApplyTemplate("EntityFieldRow", new Dictionary<string, string>
            {
                ["FieldName"] = name,
                ["FieldLabel"] = label,
            }));
        }
        return sb.ToString();
    }

    /// <summary>
    /// Loads a <c>Templates/{name}.template</c> embedded resource and substitutes
    /// every <c>{{Placeholder}}</c> with the matching entry in <paramref name="subs"/>.
    /// Unknown placeholders are left in place (helps surface typos during development).
    /// </summary>
    private static string ApplyTemplate(string templateName, IReadOnlyDictionary<string, string> subs)
    {
        var template = LoadTemplate(templateName);
        var sb = new StringBuilder(template);
        foreach (var (key, value) in subs)
        {
            sb.Replace("{{" + key + "}}", value);
        }
        return sb.ToString();
    }

    private static string LoadTemplate(string templateName)
    {
        var asm = typeof(RegistrationReferenceEmitter).Assembly;
        var suffix = $".Templates.{templateName}.template";
        var resourceName = asm.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(suffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"Embedded template not found: Templates/{templateName}.template");
        using var stream = asm.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        // Normalize CRLF → LF so the rendered file uses the same convention the
        // sibling RegistrationEmitter.cs already uses ("\n" only).
        return reader.ReadToEnd().Replace("\r\n", "\n");
    }

    /// <summary>
    /// Reads a <c>*.dabbler-entity.json</c> schema file. Tolerates a missing file or an
    /// empty / unparseable schema by returning an empty <see cref="EntitySchema"/> —
    /// the wrapper's <c>--skip-templates</c> emit-only mode is allowed to operate
    /// against a stub schema (the registration-reference is still useful for the
    /// copy-paste blocks that do not depend on the property list).
    /// </summary>
    public static EntitySchema LoadSchemaOrEmpty(string schemaPath)
    {
        if (string.IsNullOrEmpty(schemaPath) || !File.Exists(schemaPath))
            return new EntitySchema();
        try
        {
            var json = File.ReadAllText(schemaPath);
            return JsonSerializer.Deserialize<EntitySchema>(json) ?? new EntitySchema();
        }
        catch (JsonException)
        {
            return new EntitySchema();
        }
    }

    private static List<(string Name, string Label)> BuildFieldRows(EntitySchema schema, string pkName)
    {
        var rows = new List<(string Name, string Label)>();
        foreach (var p in schema.Properties)
        {
            if (string.IsNullOrEmpty(p.Name)) continue;
            if (string.Equals(p.Name, pkName, StringComparison.Ordinal)) continue;
            rows.Add((p.Name, PrettifyCamelCase(p.Name)));
        }
        return rows;
    }

    internal static string PrettifyCamelCase(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var sb = new StringBuilder(s.Length + 8);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (i > 0 && char.IsUpper(c))
            {
                bool prevIsLower = char.IsLower(s[i - 1]);
                bool nextIsLower = i + 1 < s.Length && char.IsLower(s[i + 1]);
                if (prevIsLower || nextIsLower)
                    sb.Append(' ');
            }
            sb.Append(c);
        }
        return sb.ToString();
    }
}
