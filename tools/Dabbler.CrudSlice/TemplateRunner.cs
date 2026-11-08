using System.Diagnostics;
using System.Text;

namespace Dabbler.CrudSlice;

/// <summary>
/// Invokes the <c>dotnet new dabbler-crud-service</c> and
/// <c>dotnet new dabbler-crud-page</c> templates as child processes against
/// the user's API and Web project roots.
/// </summary>
internal static class TemplateRunner
{
    public static void RunCrudServiceTemplate(CliOptions opts)
    {
        var args = new StringBuilder("new dabbler-crud-service");
        args.Append($" --entity {opts.Entity}");
        args.Append($" --entity-plural {opts.EntityPlural}");
        args.Append($" --api-namespace {opts.ApiNamespace}");
        args.Append($" --dto-namespace {opts.DtoNamespace}");
        args.Append($" --shared-namespace {opts.SharedNamespace}");
        args.Append($" --entity-namespace {opts.EntityNamespace}");
        args.Append($" --db-context-type {opts.DbContextType}");
        args.Append($" --db-context-namespace {opts.DbContextNamespace}");
        args.Append($" --pk-type {opts.PkType}");
        args.Append($" --schema \"{opts.SchemaPath}\"");
        if (!string.IsNullOrEmpty(opts.AuthPolicyPrefix))
            args.Append($" --auth-policy-prefix {opts.AuthPolicyPrefix}");
        if (!string.IsNullOrEmpty(opts.TemplateHive))
            args.Append($" --debug:custom-hive \"{opts.TemplateHive}\"");

        Run("dotnet", args.ToString(), opts.ApiRoot, "dabbler-crud-service");
    }

    public static void RunCrudPageTemplate(CliOptions opts)
    {
        var args = new StringBuilder("new dabbler-crud-page");
        args.Append($" --entity {opts.Entity}");
        args.Append($" --entity-plural {opts.EntityPlural}");
        args.Append($" --web-namespace {opts.WebNamespace}");
        args.Append($" --shared-namespace {opts.SharedNamespace}");
        args.Append($" --dto-namespace {opts.DtoNamespace}");
        args.Append($" --pk-type {opts.PkType}");
        args.Append($" --schema \"{opts.SchemaPath}\"");
        args.Append($" --api-base-url-config-key \"{opts.ApiBaseUrlConfigKey}\"");
        if (!string.IsNullOrEmpty(opts.TemplateHive))
            args.Append($" --debug:custom-hive \"{opts.TemplateHive}\"");

        Run("dotnet", args.ToString(), opts.WebRoot, "dabbler-crud-page");
    }

    private static void Run(string tool, string args, string workingDir, string label)
    {
        var psi = new ProcessStartInfo(tool, args)
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var p = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start: {tool} {args}");
        var sb = new StringBuilder();
        p.OutputDataReceived += (_, e) => { if (e.Data is not null) sb.AppendLine(e.Data); };
        p.ErrorDataReceived  += (_, e) => { if (e.Data is not null) sb.AppendLine(e.Data); };
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        if (!p.WaitForExit(TimeSpan.FromMinutes(5)))
        {
            try { p.Kill(entireProcessTree: true); } catch { /* best effort */ }
            throw new InvalidOperationException($"{label} timed out after 5 minutes.");
        }
        p.WaitForExit();
        var output = sb.ToString();
        Console.Out.Write(output);
        if (p.ExitCode != 0)
            throw new InvalidOperationException($"{label} exited with code {p.ExitCode}.");
    }
}
