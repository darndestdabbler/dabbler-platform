using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Dabbler.CrudSlice;

/// <summary>
/// Fail-fast preflight: confirms the <c>dabbler-crud-service</c> /
/// <c>dabbler-crud-page</c> <c>dotnet new</c> templates are installed before the
/// wrapper shells out to them.
/// <para>
/// Generator-acquisition decision (Session 1), Decision 5: because CrudSlice
/// shells out to the Dabbler.Templates templates, acquisition is <b>two</b>
/// installs at the same pinned <c>DabblerVersion</c> — the tool and the
/// templates. A tool-only install otherwise fails deep inside the first
/// <c>dotnet new</c> child process with an opaque "No templates found" error.
/// This preflight turns that into an actionable, version-matched
/// <c>dotnet new install Dabbler.Templates::&lt;version&gt;</c> command. The
/// <c>--skip-templates</c> escape hatch bypasses both the shell-out and this
/// check (registrations-only mode).
/// </para>
/// </summary>
internal static class TemplatePreflight
{
    private static readonly string[] RequiredTemplates =
    {
        "dabbler-crud-service",
        "dabbler-crud-page",
    };

    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> (caught by Program's outer
    /// handler → exit code 1) when any required template is not resolvable in the
    /// active hive, with the exact version-pinned install command in the message.
    /// </summary>
    public static void EnsureTemplatesInstalled(CliOptions opts)
    {
        var missing = RequiredTemplates
            .Where(t => !IsTemplateInstalled(t, opts.TemplateHive))
            .ToList();
        if (missing.Count == 0)
            return;

        var version = ToolVersion();
        var versionSuffix = version is null ? string.Empty : $"::{version}";

        var sb = new StringBuilder();
        sb.AppendLine($"Required `dotnet new` template(s) not installed: {string.Join(", ", missing)}.");
        sb.AppendLine();
        sb.AppendLine("Dabbler.CrudSlice scaffolds by invoking the Dabbler.Templates `dotnet new` templates,");
        sb.AppendLine("so they must be installed at the SAME version as this tool. Install them with:");
        sb.AppendLine();
        sb.AppendLine($"    dotnet new install Dabbler.Templates{versionSuffix}");
        sb.AppendLine();
        sb.Append("Then re-run this command. (Or pass --skip-templates to emit registration helpers only.)");
        throw new InvalidOperationException(sb.ToString());
    }

    private static bool IsTemplateInstalled(string shortName, string? templateHive)
    {
        // `dotnet new <short> -h` resolves the template and prints its help (exit 0)
        // when installed, and exits non-zero with "No templates found" when not.
        // -h requires no template parameters, so this is a cheap existence probe.
        var args = new StringBuilder($"new {shortName} -h");
        if (!string.IsNullOrEmpty(templateHive))
            args.Append($" --debug:custom-hive \"{templateHive}\"");

        var psi = new ProcessStartInfo("dotnet", args.ToString())
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        try
        {
            using var p = Process.Start(psi);
            if (p is null)
                return false;
            // Help output is small (well under the pipe buffer); drain before waiting.
            p.StandardOutput.ReadToEnd();
            p.StandardError.ReadToEnd();
            p.WaitForExit();
            return p.ExitCode == 0;
        }
        catch
        {
            // If `dotnet` itself can't be launched, let the real shell-out surface
            // the failure with its own diagnostics rather than masking it here.
            return true;
        }
    }

    private static string? ToolVersion()
    {
        var info = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrEmpty(info))
            return null;
        // Strip SourceLink "+<commit>" build metadata: "0.1.0-preview.1+abc123" -> "0.1.0-preview.1".
        var plus = info.IndexOf('+');
        return plus >= 0 ? info[..plus] : info;
    }
}
