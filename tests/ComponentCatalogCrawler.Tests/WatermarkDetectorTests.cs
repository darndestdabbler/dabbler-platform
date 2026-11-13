namespace ComponentCatalogCrawler.Tests;

public sealed class WatermarkDetectorTests : IDisposable
{
    private readonly string _tempDir = Directory.CreateTempSubdirectory("watermark-detector-").FullName;

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Fact]
    public void ScanWatermarkedRoutes_DirectlyInheritsTrackedComponentBase_ReturnsRoute()
    {
        WriteRazor("ProductManagement.razor",
            """
            @page "/admin/products"
            @inherits Dabbler.Testing.Blazor.Components.TrackedComponentBase
            <h1>Products</h1>
            """);

        var result = WatermarkDetector.ScanWatermarkedRoutes(_tempDir);

        Assert.Contains("/admin/products", result);
    }

    [Fact]
    public void ScanWatermarkedRoutes_NormalPage_NotReturned()
    {
        WriteRazor("Dashboard.razor",
            """
            @page "/dashboard"
            <h1>Dashboard</h1>
            """);

        var result = WatermarkDetector.ScanWatermarkedRoutes(_tempDir);

        Assert.DoesNotContain("/dashboard", result);
    }

    [Fact]
    public void ScanWatermarkedRoutes_InheritsButNoPage_NotReturned()
    {
        WriteRazor("SubComponent.razor",
            """
            @inherits Dabbler.Testing.Blazor.Components.TrackedComponentBase
            <p>Inner content</p>
            """);

        var result = WatermarkDetector.ScanWatermarkedRoutes(_tempDir);

        Assert.Empty(result);
    }

    [Fact]
    public void ScanWatermarkedRoutes_MultipleRoutesInFile_AllReturned()
    {
        WriteRazor("MultiRoute.razor",
            """
            @page "/foo"
            @page "/bar"
            @inherits Dabbler.Testing.Blazor.Components.TrackedComponentBase
            <p>Multi-route</p>
            """);

        var result = WatermarkDetector.ScanWatermarkedRoutes(_tempDir);

        Assert.Contains("/foo", result);
        Assert.Contains("/bar", result);
    }

    [Fact]
    public void ScanWatermarkedRoutes_ParameterisedRoute_NormalizesConstraint()
    {
        WriteRazor("CustomerDetail.razor",
            """
            @page "/customers/{CustomerId:int}"
            @inherits Dabbler.Testing.Blazor.Components.TrackedComponentBase
            <p>Customer detail</p>
            """);

        var result = WatermarkDetector.ScanWatermarkedRoutes(_tempDir);

        Assert.Contains("/customers/{CustomerId}", result);
        Assert.DoesNotContain("/customers/{CustomerId:int}", result);
    }

    [Fact]
    public void ScanWatermarkedRoutes_MixOfWatermarkedAndNot_OnlyWatermarkedReturned()
    {
        WriteRazor("WatermarkedPage.razor",
            """
            @page "/admin/products"
            @inherits Dabbler.Testing.Blazor.Components.TrackedComponentBase
            <h1>Products</h1>
            """);
        WriteRazor("NormalPage.razor",
            """
            @page "/dashboard"
            <h1>Dashboard</h1>
            """);

        var result = WatermarkDetector.ScanWatermarkedRoutes(_tempDir);

        Assert.Contains("/admin/products", result);
        Assert.DoesNotContain("/dashboard", result);
    }

    [Fact]
    public void ScanWatermarkedRoutes_NonExistentDirectory_ReturnsEmpty()
    {
        var result = WatermarkDetector.ScanWatermarkedRoutes(@"C:\does\not\exist\__watermark_test__");

        Assert.Empty(result);
    }

    [Fact]
    public void ScanWatermarkedRoutes_NestedSubdirectory_IsFound()
    {
        var sub = Path.Combine(_tempDir, "Admin");
        Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(sub, "ProductManagement.razor"),
            """
            @page "/admin/products"
            @inherits Dabbler.Testing.Blazor.Components.TrackedComponentBase
            <h1>Products</h1>
            """);

        var result = WatermarkDetector.ScanWatermarkedRoutes(_tempDir);

        Assert.Contains("/admin/products", result);
    }

    private void WriteRazor(string fileName, string content)
        => File.WriteAllText(Path.Combine(_tempDir, fileName), content);
}
