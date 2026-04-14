namespace AcmeCoffeeOnline.Web.Tests;

public sealed class NotesDrivenAlignmentGuardrailTests
{
    [Fact]
    public void DashboardSources_UseFocusedNavigationAndSearchDefaults()
    {
        var repoRoot = GetRepoRoot();
        var dashboardPage = ReadRepoFile(repoRoot, "src", "Apps", "AcmeCoffeeOnline.Web", "Components", "Pages", "Dashboard.razor");
        var mainLayout = ReadRepoFile(repoRoot, "src", "Apps", "AcmeCoffeeOnline.Web", "Components", "Layout", "MainLayout.razor");

        Assert.DoesNotContain("WidgetType.RecentItems", dashboardPage);
        Assert.DoesNotContain("WidgetType.Favorites", dashboardPage);
        Assert.DoesNotContain("WidgetType.RecentItems", mainLayout);
        Assert.DoesNotContain("WidgetType.Favorites", mainLayout);
        Assert.Contains("focused search", dashboardPage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("focused search", mainLayout, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Search and manage product records", dashboardPage);
        Assert.Contains("Search and manage product records", mainLayout);
    }

    [Fact]
    public void QueryPages_DoNotOptIntoCompatibilityCsvExports()
    {
        var repoRoot = GetRepoRoot();
        var productList = ReadRepoFile(repoRoot, "src", "Apps", "AcmeCoffeeOnline.Web", "Components", "Pages", "Products", "ProductList.razor");
        var customerList = ReadRepoFile(repoRoot, "src", "Apps", "AcmeCoffeeOnline.Web", "Components", "Pages", "Customers", "CustomerList.razor");
        var orderList = ReadRepoFile(repoRoot, "src", "Apps", "AcmeCoffeeOnline.Web", "Components", "Pages", "Orders", "OrderList.razor");

        Assert.DoesNotContain("ShowCsvExport", productList);
        Assert.DoesNotContain("CsvFileName", productList);
        Assert.DoesNotContain("ShowCsvExport", customerList);
        Assert.DoesNotContain("CsvFileName", customerList);
        Assert.DoesNotContain("ShowCsvExport", orderList);
        Assert.DoesNotContain("CsvFileName", orderList);
    }

    [Fact]
    public void ProductCatalogRoute_UsesFocusedQueryWorkflowAlias()
    {
        var repoRoot = GetRepoRoot();
        var productCatalog = ReadRepoFile(repoRoot, "src", "Apps", "AcmeCoffeeOnline.Web", "Components", "Pages", "Products", "ProductCatalog.razor");

        Assert.Contains("@page \"/products/catalog\"", productCatalog);
        Assert.Contains("DabblerPageAuthorize(\"/products\")", productCatalog);
        Assert.Contains("<ProductList", productCatalog);
        Assert.Contains("focused product workflow", productCatalog, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ProductApiClient", productCatalog);
    }

    [Fact]
    public void ReportPages_RelyOnRetainedDefaultViewerSurface()
    {
        var repoRoot = GetRepoRoot();
        var ordersByCustomer = ReadRepoFile(repoRoot, "src", "Apps", "AcmeCoffeeOnline.Web", "Components", "Pages", "Reports", "OrdersByCustomer.razor");
        var salesByProduct = ReadRepoFile(repoRoot, "src", "Apps", "AcmeCoffeeOnline.Web", "Components", "Pages", "Reports", "SalesByProduct.razor");

        Assert.DoesNotContain("ShowPdfExport", ordersByCustomer);
        Assert.DoesNotContain("ShowPrint", ordersByCustomer);
        Assert.DoesNotContain("ShowZoom", ordersByCustomer);
        Assert.DoesNotContain("ShowPdfExport", salesByProduct);
        Assert.DoesNotContain("ShowPrint", salesByProduct);
        Assert.DoesNotContain("ShowZoom", salesByProduct);
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

    private static string ReadRepoFile(string repoRoot, params string[] relativePath)
    {
        return File.ReadAllText(Path.Combine([repoRoot, .. relativePath]));
    }
}
