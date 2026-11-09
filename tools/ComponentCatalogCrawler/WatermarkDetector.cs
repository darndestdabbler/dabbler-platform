using System.Text.RegularExpressions;

namespace ComponentCatalogCrawler;

/// <summary>
/// Detects which route templates are backed by a component that inherits from
/// <c>TrackedComponentBase</c>. Source-level scan: each <c>.razor</c> file is
/// checked for both a <c>@page</c> directive and an <c>@inherits</c> directive
/// that names <c>TrackedComponentBase</c>. Both must be present for the route
/// to be considered watermarked.
/// </summary>
public static class WatermarkDetector
{
    private static readonly Regex PageDirectiveRegex = new(
        """^\s*@page\s+"(?<route>[^"]+)"\s*$""",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex InheritsTrackedRegex = new(
        @"@inherits\s+\S*TrackedComponentBase\b",
        RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Returns the set of normalized route template paths (e.g.
    /// <c>/admin/products</c>, <c>/customers/{CustomerId}</c>) whose
    /// corresponding <c>.razor</c> files directly inherit
    /// <c>TrackedComponentBase</c>.
    /// </summary>
    /// <param name="pagesDirectory">Directory to scan recursively for .razor files.</param>
    public static IReadOnlySet<string> ScanWatermarkedRoutes(string pagesDirectory)
    {
        if (!Directory.Exists(pagesDirectory))
            return new HashSet<string>();

        var watermarked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in Directory.EnumerateFiles(pagesDirectory, "*.razor", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(file);

            if (!InheritsTrackedRegex.IsMatch(content))
                continue;

            foreach (Match pageMatch in PageDirectiveRegex.Matches(content))
            {
                var route = pageMatch.Groups["route"].Value;
                watermarked.Add(RouteEnumerator.NormalizeTemplatePath(route));
            }
        }

        return watermarked;
    }
}
