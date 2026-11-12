using System.Diagnostics;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Dabbler.PublicSurface.Tests;

/// <summary>
/// Smoke checks for the dabbler-devidp CLI tool (S4 regression gate).
/// Tests are skipped when the tool has not been built.
/// </summary>
public class DevIdpToolTests
{
    private static readonly string? ToolDllPath = FindDevIdpDll();

    private static string? FindDevIdpDll()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var toolBin = Path.Combine(dir.FullName, "tools", "Dabbler.Security.DevIdp.Tool", "bin");
            if (Directory.Exists(toolBin))
            {
                return Directory.GetFiles(toolBin, "Dabbler.Security.DevIdp.Tool.dll", SearchOption.AllDirectories)
                    .FirstOrDefault();
            }
            dir = dir.Parent;
        }
        return null;
    }

    [SkippableFact]
    public void DevIdpTool_Help_ExitsZeroWithUsageText()
    {
        Skip.If(ToolDllPath is null,
            "DevIdp tool DLL not found. Run 'dotnet build tools/Dabbler.Security.DevIdp.Tool' first.");

        var (exitCode, output) = RunProcess("dotnet", $"\"{ToolDllPath}\" --help");

        exitCode.Should().Be(0, $"dabbler-devidp --help should exit 0. Output:\n{output}");
        output.Should().ContainEquivalentOf("usage",
            because: "dabbler-devidp --help must print usage text");
        output.Should().ContainEquivalentOf("dabbler-devidp",
            because: "dabbler-devidp --help must identify the tool name");
    }

    [SkippableFact]
    public void DevIdpTool_HelpShortFlag_ExitsZeroWithUsageText()
    {
        Skip.If(ToolDllPath is null,
            "DevIdp tool DLL not found. Run 'dotnet build tools/Dabbler.Security.DevIdp.Tool' first.");

        var (exitCode, output) = RunProcess("dotnet", $"\"{ToolDllPath}\" -h");

        exitCode.Should().Be(0, $"dabbler-devidp -h should exit 0. Output:\n{output}");
        output.Should().ContainEquivalentOf("usage",
            because: "dabbler-devidp -h must print usage text");
    }

    [SkippableFact]
    public void DevIdpTool_HelpQuestionMark_ExitsZeroWithUsageText()
    {
        Skip.If(ToolDllPath is null,
            "DevIdp tool DLL not found. Run 'dotnet build tools/Dabbler.Security.DevIdp.Tool' first.");

        var (exitCode, output) = RunProcess("dotnet", $"\"{ToolDllPath}\" -?");

        exitCode.Should().Be(0, $"dabbler-devidp -? should exit 0. Output:\n{output}");
        output.Should().ContainEquivalentOf("usage",
            because: "dabbler-devidp -? must print usage text");
    }

    private static (int exitCode, string output) RunProcess(string fileName, string arguments)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(psi)!;
        var sb = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) sb.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) sb.AppendLine(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        bool exited = process.WaitForExit(TimeSpan.FromSeconds(30));
        if (!exited)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best-effort */ }
            process.WaitForExit(TimeSpan.FromSeconds(5));
            return (-1, $"TIMEOUT after 30s. Partial output:\n{sb}");
        }

        process.WaitForExit();
        return (process.ExitCode, sb.ToString());
    }
}
