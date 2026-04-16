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
        Assert.Contains("Search and manage distributor catalog records", dashboardPage);
        // Dashboard configuration now lives only in Dashboard.razor, not in MainLayout
        // (MainLayout no longer duplicates dashboard widget definitions after shell removal)
    }

    [Fact]
    public void QueryPages_UseMudDataGridDirectly()
    {
        // After MudBlazor-native rebuild, query pages use MudDataGrid directly
        // instead of DabblerQueryPage wrapper. CSV export will use MudDataGrid's
        // built-in export capabilities added in later sessions.
        var repoRoot = GetRepoRoot();
        var productList = ReadRepoFile(repoRoot, "src", "Apps", "AcmeCoffeeOnline.Web", "Components", "Pages", "Products", "ProductList.razor");
        var customerList = ReadRepoFile(repoRoot, "src", "Apps", "AcmeCoffeeOnline.Web", "Components", "Pages", "Customers", "CustomerList.razor");
        var orderList = ReadRepoFile(repoRoot, "src", "Apps", "AcmeCoffeeOnline.Web", "Components", "Pages", "Orders", "OrderList.razor");

        Assert.Contains("MudDataGrid", productList);
        Assert.Contains("MudDataGrid", customerList);
        Assert.Contains("MudDataGrid", orderList);
        Assert.DoesNotContain("DabblerQueryPage", productList);
        Assert.DoesNotContain("DabblerQueryPage", customerList);
        Assert.DoesNotContain("DabblerQueryPage", orderList);
    }

    [Fact]
    public void ProductCatalogRoute_UsesFocusedQueryWorkflowAlias()
    {
        var repoRoot = GetRepoRoot();
        var productCatalog = ReadRepoFile(repoRoot, "src", "Apps", "AcmeCoffeeOnline.Web", "Components", "Pages", "Products", "ProductCatalog.razor");

        Assert.Contains("@page \"/products/catalog\"", productCatalog);
        Assert.Contains("DabblerPageAuthorize(\"/products\")", productCatalog);
        Assert.Contains("<ProductList", productCatalog);
        Assert.Contains("focused distributor product workflow", productCatalog, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ProductApiClient", productCatalog);
    }

    [Fact]
    public void ReportPages_KeepPdfExportWhileAvoidingOtherCompatibilityControls()
    {
        var repoRoot = GetRepoRoot();
        var ordersByCustomer = ReadRepoFile(repoRoot, "src", "Apps", "AcmeCoffeeOnline.Web", "Components", "Pages", "Reports", "OrdersByCustomer.razor");
        var salesByProduct = ReadRepoFile(repoRoot, "src", "Apps", "AcmeCoffeeOnline.Web", "Components", "Pages", "Reports", "SalesByProduct.razor");

        Assert.Contains("ShowPdfExport=\"true\"", ordersByCustomer);
        Assert.DoesNotContain("ShowPrint", ordersByCustomer);
        Assert.DoesNotContain("ShowZoom", ordersByCustomer);
        Assert.Contains("ShowPdfExport=\"true\"", salesByProduct);
        Assert.DoesNotContain("ShowPrint", salesByProduct);
        Assert.DoesNotContain("ShowZoom", salesByProduct);
    }

    [Fact]
    public void ShellSources_RemoveCatalogCompatibilityProvidersAndMismatchedSecurityNav()
    {
        var repoRoot = GetRepoRoot();
        var mainLayout = ReadRepoFile(repoRoot, "src", "Apps", "AcmeCoffeeOnline.Web", "Components", "Layout", "MainLayout.razor");
        var program = ReadRepoFile(repoRoot, "src", "Apps", "AcmeCoffeeOnline.Web", "Program.cs");
        var dashboardProviderPath = Path.Combine(
            repoRoot,
            "src",
            "Apps",
            "AcmeCoffeeOnline.Web",
            "Data",
            "DashboardDataProvider.cs");

        Assert.DoesNotContain("@inject IArtifactCatalogProvider", mainLayout);
        Assert.DoesNotContain("@inject IUsageTrackingProvider", mainLayout);
        Assert.DoesNotContain("CatalogProvider=\"", mainLayout);
        Assert.DoesNotContain("UsageTrackingProvider=\"", mainLayout);
        Assert.DoesNotContain("NavigateTo(\"/products/catalog\")", mainLayout);
        Assert.DoesNotContain("<DabblerSecurityNav", mainLayout);
        // Products navigation now uses MudNavLink Href instead of NavigateTo
        Assert.Contains("/products", mainLayout);
        Assert.DoesNotContain("AddSingleton<IArtifactCatalogProvider", program);
        Assert.DoesNotContain("AddSingleton<IUsageTrackingProvider", program);
        Assert.False(File.Exists(dashboardProviderPath));
    }

    [Fact]
    public void AdminAndShellSources_UseApiFirstSecurityBoundaries()
    {
        var repoRoot = GetRepoRoot();
        var userManagement = ReadRepoFile(repoRoot, "src", "Apps", "AcmeCoffeeOnline.Web", "Components", "Pages", "Admin", "UserManagement.razor");
        var mainLayout = ReadRepoFile(repoRoot, "src", "Apps", "AcmeCoffeeOnline.Web", "Components", "Layout", "MainLayout.razor");

        Assert.DoesNotContain("@inject DabblerSecurityDbContext", userManagement);
        Assert.DoesNotContain("@inject DabblerSecurityDbContext", mainLayout);
        Assert.Contains("@inject AdminUserApiClient", userManagement);
        Assert.Contains("@inject ShellSessionStateLoader", mainLayout);
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
