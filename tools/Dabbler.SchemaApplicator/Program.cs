using System.Text;
using Dabbler.SchemaApplicator;

// Cross-platform replacement for Apply-{Entity}Schema.ps1.
//
// Usage:
//   Dabbler.SchemaApplicator --entity <Singular> --directory <razor-folder> [--schema <path>]
//
// Defaults (matching the PowerShell behavior):
//   - <razor-folder> contains {Entity}List.razor and {Entity}Detail.razor.
//   - --schema defaults to <razor-folder>/{entity-lower}.dabbler-entity.json,
//     then <razor-folder>/{Entity}.dabbler-entity.json, then their --project-root
//     equivalents (the closest ancestor containing a *.csproj).

string? entity = null;
string? directory = null;
string? schemaPath = null;
string? projectRoot = null;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--entity" when i + 1 < args.Length:
            entity = args[++i];
            break;
        case "--directory" when i + 1 < args.Length:
            directory = args[++i];
            break;
        case "--schema" when i + 1 < args.Length:
            schemaPath = args[++i];
            break;
        case "--project-root" when i + 1 < args.Length:
            projectRoot = args[++i];
            break;
        case "-h":
        case "--help":
            PrintUsage();
            return 0;
        default:
            Console.Error.WriteLine($"Unknown or incomplete argument: {args[i]}");
            PrintUsage();
            return 2;
    }
}

if (string.IsNullOrEmpty(entity))
{
    Console.Error.WriteLine("Required argument missing: --entity <Singular>");
    PrintUsage();
    return 2;
}
if (string.IsNullOrEmpty(directory))
{
    Console.Error.WriteLine("Required argument missing: --directory <razor-folder>");
    PrintUsage();
    return 2;
}
if (!Directory.Exists(directory))
{
    Console.Error.WriteLine($"Directory does not exist: {directory}");
    return 1;
}

projectRoot ??= FindProjectRoot(directory) ?? directory;

if (string.IsNullOrEmpty(schemaPath))
{
    schemaPath = ResolveDefaultSchemaPath(entity, directory, projectRoot)
        ?? throw new FileNotFoundException(
            $"Schema file not found. Pass --schema <path>, or place the schema at one of: " +
            string.Join(", ", DefaultSchemaCandidates(entity, directory, projectRoot)));
}

if (!File.Exists(schemaPath))
{
    Console.Error.WriteLine($"Schema file not found at: {schemaPath}");
    return 1;
}

var listPath = Path.Combine(directory, $"{entity}List.razor");
var detailPath = Path.Combine(directory, $"{entity}Detail.razor");
if (!File.Exists(listPath))   { Console.Error.WriteLine($"Expected file not found: {listPath}"); return 1; }
if (!File.Exists(detailPath)) { Console.Error.WriteLine($"Expected file not found: {detailPath}"); return 1; }

Console.WriteLine($"Apply-{entity}Schema: reading {schemaPath}");

var schemaJson = File.ReadAllText(schemaPath);
var listContent = File.ReadAllText(listPath);
var detailContent = File.ReadAllText(detailPath);

var result = SchemaApplicator.Apply(schemaJson, entity, listContent, detailContent);

var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
File.WriteAllText(listPath, result.ListContent, utf8NoBom);
File.WriteAllText(detailPath, result.DetailContent, utf8NoBom);

Console.WriteLine($"Apply-{entity}Schema: wrote {result.GridCount} grid columns, {result.FormCount} form fields, {result.FilterCount} filter properties.");
return 0;

static void PrintUsage()
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  Dabbler.SchemaApplicator --entity <Singular> --directory <razor-folder> [--schema <path>] [--project-root <path>]");
}

static string? FindProjectRoot(string start)
{
    var dir = new DirectoryInfo(start);
    while (dir is not null)
    {
        if (dir.EnumerateFiles("*.csproj").Any()) return dir.FullName;
        dir = dir.Parent;
    }
    return null;
}

static IEnumerable<string> DefaultSchemaCandidates(string entity, string directory, string projectRoot)
{
    var lower = entity.ToLowerInvariant();
    yield return Path.Combine(directory, $"{lower}.dabbler-entity.json");
    yield return Path.Combine(directory, $"{entity}.dabbler-entity.json");
    yield return Path.Combine(projectRoot, $"{lower}.dabbler-entity.json");
    yield return Path.Combine(projectRoot, $"{entity}.dabbler-entity.json");
}

static string? ResolveDefaultSchemaPath(string entity, string directory, string projectRoot)
{
    foreach (var candidate in DefaultSchemaCandidates(entity, directory, projectRoot))
    {
        if (File.Exists(candidate)) return candidate;
    }
    return null;
}
