using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;

namespace AcmeCoffeeOnline.Web.Tests;

/// <summary>
/// Architecture guard for the production split: the Web app owns no EF Core
/// data access of its own. Referenced libraries (Dabbler.Security) carry
/// EF Core by design, so nothing here inspects transitive assemblies.
/// </summary>
public sealed class ProductionSplitTests
{
    [Fact]
    public void WebAssembly_DeclaresNoDbContext()
    {
        var dbContextTypes = typeof(Program).Assembly
            .GetTypes()
            .Where(type => typeof(DbContext).IsAssignableFrom(type))
            .Select(type => type.FullName)
            .ToList();

        Assert.Empty(dbContextTypes);
    }

    [Fact]
    public void WebProject_ReferencesNoEntityFrameworkCorePackage()
    {
        var project = XDocument.Load(Path.Combine(
            GetRepoRoot(), "src", "Apps", "AcmeCoffeeOnline.Web", "AcmeCoffeeOnline.Web.csproj"));

        var efPackages = project.Descendants()
            .Where(element => element.Name.LocalName == "PackageReference")
            .Select(element => (string?)element.Attribute("Include") ?? string.Empty)
            .Where(include => include.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(efPackages);
    }

    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (directory.EnumerateFiles("Dabbler.Platform.sln").Any())
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root from the current test output path.");
    }
}
