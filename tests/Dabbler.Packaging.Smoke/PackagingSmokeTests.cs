using FluentAssertions;
using Xunit;

namespace Dabbler.Packaging.Smoke;

/// <summary>
/// Consumer smoke tests for the packaging and template paths (ledger §6.1 / A2).
///
/// Full expanded sequence (sign-in, secured page, sample report, PDF export,
/// scaffolded bUnit) requires the Dabbler.Templates package and Session 8
/// template deliverables. The complete smoke test suite will be authored
/// in Session 8/9 as part of the template smoke proof.
///
/// This file is the baseline structure that Session 8 will build on.
/// Tests marked [Skip] will be activated once the prerequisite deliverables exist.
/// </summary>
public class PackagingSmokeTests
{
    private static readonly string PackOutputDir = GetPackOutputDir();

    private static string GetPackOutputDir()
    {
        var fromEnv = Environment.GetEnvironmentVariable("DABBLER_PACK_OUTPUT");
        if (!string.IsNullOrWhiteSpace(fromEnv) && Directory.Exists(fromEnv))
            return fromEnv;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "pack-output");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "pack-output");
    }

    [SkippableFact]
    public void PackOutput_ContainsAllRequiredLibraryNupkgs()
    {
        Skip.If(!Directory.Exists(PackOutputDir),
            "Pack output directory not found. Run tools/pack.ps1 first.");

        var nupkgs = Directory.GetFiles(PackOutputDir, "*.nupkg")
            .Select(Path.GetFileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var requiredPrefixes = new[]
        {
            "Dabbler.Components.",
            "Dabbler.Components.Compat.",
            "Dabbler.Platform.Stubs.",
            "Dabbler.Platform.Stubs.Components.",
            "Dabbler.Platform.Stubs.Reports.",
            "Dabbler.Platform.Stubs.Security.",
            "Dabbler.Reports.Core.",
            "Dabbler.Reports.Export.",
            "Dabbler.Reports.Blazor.",
            "Dabbler.Security.",
            "Dabbler.Security.Blazor.",
            "Dabbler.Testing.Blazor.",
            // Session 8 deliverables
            "Dabbler.Security.DevIdp.Tool.",
            "Dabbler.Templates.",
        };

        var missing = requiredPrefixes
            .Where(prefix => !nupkgs.Any(n => n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        missing.Should().BeEmpty(
            $"All library packages must be present in pack-output/. Missing prefixes: {string.Join(", ", missing)}");
    }

    // Session 9 activates and expands these once the full consumer-validation path is proven:

    [Fact(Skip = "Session 9: validate install + build from packed Dabbler.Templates")]
    public void DabblerApp_Template_InstallsAndBuildsClean() { }

    [Fact(Skip = "Session 9: validate install + build from packed Dabbler.Templates")]
    public void DabblerMin_Template_InstallsAndBuildsClean() { }

    [Fact(Skip = "Session 9: sign-in via magic link as admin@local.test against scaffolded app")]
    public void DabblerApp_SignIn_ReachesSecuredPage() { }

    [Fact(Skip = "Session 9: sample report renders and PDF export returns non-empty bytes")]
    public void DabblerApp_SampleReport_PdfExportSucceeds() { }

    [Fact(Skip = "Session 9: scaffolded bUnit project tests all pass")]
    public void DabblerApp_ScaffoldedBUnit_AllTestsPass() { }
}
