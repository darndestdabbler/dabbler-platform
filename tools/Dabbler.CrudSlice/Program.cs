using System.Text;
using Dabbler.CrudSlice;

CliOptions opts;
try
{
    opts = CliOptions.Parse(args);
}
catch (HelpRequested)
{
    return 0;
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine();
    CliOptions.PrintUsage();
    return 2;
}

if (!Directory.Exists(opts.WebRoot))
{
    Console.Error.WriteLine($"--web-root does not exist: {opts.WebRoot}");
    return 1;
}
if (!Directory.Exists(opts.ApiRoot))
{
    Console.Error.WriteLine($"--api-root does not exist: {opts.ApiRoot}");
    return 1;
}
if (!File.Exists(opts.SchemaPath))
{
    Console.Error.WriteLine($"--schema file not found: {opts.SchemaPath}");
    return 1;
}

try
{
    // 1. Run the API-side template (service + interface + controller).
    if (!opts.SkipTemplates)
    {
        // Fail fast with the exact version-matched install command if the
        // Dabbler.Templates `dotnet new` templates are not installed — the
        // tool and templates are a two-install pair at the same DabblerVersion.
        TemplatePreflight.EnsureTemplatesInstalled(opts);

        Console.WriteLine($"[1/4] dotnet new dabbler-crud-service  (in {opts.ApiRoot})");
        TemplateRunner.RunCrudServiceTemplate(opts);

        Console.WriteLine($"[2/4] dotnet new dabbler-crud-page     (in {opts.WebRoot})");
        TemplateRunner.RunCrudPageTemplate(opts);
    }
    else
    {
        Console.WriteLine("[1/4 + 2/4] --skip-templates: leaving any existing template output in place.");
    }

    // 2. Run the schema applicator (replaces Apply-{Entity}Schema.ps1).
    if (!opts.SkipSchemaApplicator)
    {
        Console.WriteLine($"[3/4] schema applicator (cross-platform replacement for Apply-{opts.Entity}Schema.ps1)");
        SchemaApplicatorRunner.Apply(opts);
    }
    else
    {
        Console.WriteLine("[3/4] --skip-schema-applicator: leaving template's empty regions in place.");
    }

    // 3. Emit the convention-correct registration helpers.
    Console.WriteLine("[4/4] emitting registration helpers (Web + API)");
    EmitRegistrationFiles(opts);

    // 3b. Emit the {Entity}Registrations.g.cs copy-paste reference for the
    //     consumer to paste into their Api/Program.cs and Web/Program.cs
    //     AddDabblerSecurity(...) lambdas. The wrapper does NOT edit Program.cs;
    //     this is a reference file (#if false wrapped, never compiles).
    if (!opts.SkipRegistrations)
    {
        EmitRegistrationsReferenceFile(opts);
    }
    else
    {
        Console.WriteLine("   --skip-registrations: skipping {Entity}Registrations.g.cs reference emit.");
    }

    Console.WriteLine();
    Console.WriteLine($"Done. Dabbler.CrudSlice scaffolded {opts.Entity} / {opts.EntityPlural}.");
    Console.WriteLine($"  Web extension: {Path.Combine(opts.WebRoot, "Data", $"{opts.Entity}ScaffoldingExtensions.cs")}");
    Console.WriteLine($"  API extension: {Path.Combine(opts.ApiRoot, "Services", $"{opts.Entity}ApiRegistrationExtensions.cs")}");
    if (!opts.SkipRegistrations)
    {
        Console.WriteLine($"  Registrations: {Path.Combine(opts.WebRoot, "Data", $"{opts.Entity}Registrations.g.cs")} (copy-paste reference)");
    }
    Console.WriteLine();
    Console.WriteLine("Next step — wire into Program.cs:");
    Console.WriteLine($"  Web/Program.cs: builder.Services.Add{opts.Entity}Scaffolding(builder.Configuration);");
    Console.WriteLine($"  Api/Program.cs: builder.Services.Add{opts.Entity}ApiServices();");
    if (!opts.SkipRegistrations)
    {
        Console.WriteLine($"  Then copy each block from {opts.Entity}Registrations.g.cs into the matching");
        Console.WriteLine($"  lambda inside AddDabblerSecurity(...) on each side.");
    }
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"ERROR: {ex.Message}");
    return 1;
}

static void EmitRegistrationFiles(CliOptions opts)
{
    var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    var webDataDir = Path.Combine(opts.WebRoot, "Data");
    Directory.CreateDirectory(webDataDir);
    var webExtensionPath = Path.Combine(webDataDir, $"{opts.Entity}ScaffoldingExtensions.cs");
    File.WriteAllText(webExtensionPath, RegistrationEmitter.RenderWebScaffoldingExtensions(opts), utf8NoBom);
    Console.WriteLine($"   wrote {webExtensionPath} (OVERWRITE: replaces template's wrong-convention emit)");

    var apiServicesDir = Path.Combine(opts.ApiRoot, "Services");
    Directory.CreateDirectory(apiServicesDir);
    var apiExtensionPath = Path.Combine(apiServicesDir, $"{opts.Entity}ApiRegistrationExtensions.cs");
    File.WriteAllText(apiExtensionPath, RegistrationEmitter.RenderApiRegistrationExtensions(opts), utf8NoBom);
    Console.WriteLine($"   wrote {apiExtensionPath}");
}

static void EmitRegistrationsReferenceFile(CliOptions opts)
{
    var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    var schema = RegistrationReferenceEmitter.LoadSchemaOrEmpty(opts.SchemaPath);

    var webDataDir = Path.Combine(opts.WebRoot, "Data");
    Directory.CreateDirectory(webDataDir);
    var referencePath = Path.Combine(webDataDir, $"{opts.Entity}Registrations.g.cs");
    File.WriteAllText(referencePath, RegistrationReferenceEmitter.Render(opts, schema), utf8NoBom);
    Console.WriteLine($"   wrote {referencePath}  (copy-paste reference, wrapped in #if false)");
}
