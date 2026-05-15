using System.IO.Compression;
using FluentAssertions;
using Xunit;

namespace Dabbler.PublicSurface.Tests;

/// <summary>
/// Artifact-content assertions for produced .nupkg files (ledger §6.2 / A10).
/// Run after tools/pack.ps1 has populated pack-output/.
/// Set DABBLER_PACK_OUTPUT env var to the absolute path of the pack-output directory,
/// or place it at the repository root at pack-output/ (default).
/// </summary>
public class NupkgArtifactTests
{
    private static readonly string PackOutputDir = GetPackOutputDir();

    private static string GetPackOutputDir()
    {
        var fromEnv = Environment.GetEnvironmentVariable("DABBLER_PACK_OUTPUT");
        if (!string.IsNullOrWhiteSpace(fromEnv) && Directory.Exists(fromEnv))
            return fromEnv;

        // Walk up from test assembly location to find the repo root
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

    private static IEnumerable<string> FindNupkgs(string packageId)
    {
        if (!Directory.Exists(PackOutputDir))
            return [];

        // Use a digit guard after the packageId prefix so Dabbler.Components.Compat.*.nupkg
        // does not satisfy a lookup for packageId="Dabbler.Components".
        var prefix = packageId + ".";
        return Directory.GetFiles(PackOutputDir, $"{packageId}.*.nupkg")
            .Where(f =>
            {
                var name = Path.GetFileName(f);
                return name is not null &&
                       name.Length > prefix.Length &&
                       char.IsDigit(name[prefix.Length]);
            });
    }

    private static readonly string[] PackagedLibraries =
    [
        "Dabbler.Components",
        "Dabbler.Components.Compat",
        "Dabbler.Platform.Stubs",
        "Dabbler.Platform.Stubs.Components",
        "Dabbler.Platform.Stubs.Reports",
        "Dabbler.Platform.Stubs.Security",
        "Dabbler.Reports.Core",
        "Dabbler.Reports.Export",
        "Dabbler.Reports.Blazor",
        "Dabbler.Security",
        "Dabbler.Security.Blazor",
        "Dabbler.Testing.Blazor",
    ];

    private static readonly string[] ReportsPackages =
    [
        "Dabbler.Reports.Core",
        "Dabbler.Reports.Export",
        "Dabbler.Reports.Blazor",
    ];

    public static IEnumerable<object[]> AllPackagedLibraries() =>
        PackagedLibraries.Select(id => new object[] { id });

    public static IEnumerable<object[]> ReportsPackagesOnly() =>
        ReportsPackages.Select(id => new object[] { id });

    [SkippableTheory]
    [MemberData(nameof(AllPackagedLibraries))]
    public void Nupkg_ContainsReadme(string packageId)
    {
        var nupkg = FindNupkgs(packageId).FirstOrDefault();
        Skip.If(nupkg is null, $"Pack output not found for {packageId}. Run tools/pack.ps1 first.");

        using var zip = ZipFile.OpenRead(nupkg);
        zip.Entries.Should().Contain(
            e => e.FullName == "README.md",
            $"{packageId} must contain README.md at package root");
    }

    [SkippableTheory]
    [MemberData(nameof(AllPackagedLibraries))]
    public void Nupkg_ContainsIcon(string packageId)
    {
        var nupkg = FindNupkgs(packageId).FirstOrDefault();
        Skip.If(nupkg is null, $"Pack output not found for {packageId}. Run tools/pack.ps1 first.");

        using var zip = ZipFile.OpenRead(nupkg);
        zip.Entries.Should().Contain(
            e => e.FullName == "icon.png",
            $"{packageId} must contain icon.png at package root");
    }

    [SkippableTheory]
    [MemberData(nameof(AllPackagedLibraries))]
    public void Nupkg_HasMitLicenseExpression(string packageId)
    {
        var nupkg = FindNupkgs(packageId).FirstOrDefault();
        Skip.If(nupkg is null, $"Pack output not found for {packageId}. Run tools/pack.ps1 first.");

        using var zip = ZipFile.OpenRead(nupkg);
        var nuspec = zip.Entries.FirstOrDefault(e => e.Name.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
        nuspec.Should().NotBeNull($"{packageId}.nuspec must exist");

        using var stream = nuspec!.Open();
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();

        content.Should().Contain("<license type=\"expression\">MIT</license>",
            $"{packageId} must declare MIT license expression");
    }

    [SkippableTheory]
    [MemberData(nameof(ReportsPackagesOnly))]
    public void ReportsNupkg_ContainsThirdPartyNotices(string packageId)
    {
        var nupkg = FindNupkgs(packageId).FirstOrDefault();
        Skip.If(nupkg is null, $"Pack output not found for {packageId}. Run tools/pack.ps1 first.");

        using var zip = ZipFile.OpenRead(nupkg);
        zip.Entries.Should().Contain(
            e => e.FullName == "THIRD-PARTY-NOTICES.txt",
            $"{packageId} must contain THIRD-PARTY-NOTICES.txt (DabblerPdf LGPL-2.1-only OR MPL-2.0 disclosure)");
    }

    [SkippableTheory]
    [MemberData(nameof(AllPackagedLibraries))]
    public void Nupkg_HasRepositoryMetadata(string packageId)
    {
        var nupkg = FindNupkgs(packageId).FirstOrDefault();
        Skip.If(nupkg is null, $"Pack output not found for {packageId}. Run tools/pack.ps1 first.");

        using var zip = ZipFile.OpenRead(nupkg);
        var nuspec = zip.Entries.First(e => e.Name.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));

        using var stream = nuspec.Open();
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();

        content.Should().Contain("<repository", $"{packageId} must have <repository> element");
        content.Should().Contain("darndestdabbler/dabbler-platform", $"{packageId} must link to the GitHub repo");
    }
}
