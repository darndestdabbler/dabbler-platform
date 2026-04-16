namespace AcmeCoffeeOnline.Web.Tests;

/// <summary>
/// Verifies that the MudBlazor-native rebuild conventions are applied consistently
/// across all app pages. These are source-level structural assertions that catch
/// regression toward deleted wrappers or non-standard patterns.
/// </summary>
public sealed class MudBlazorRebuildConformanceTests
{
    // ── Layout and Shell ──────────────────────────────────────────────

    [Fact]
    public void MainLayout_UsesMudBlazorLayoutSystem()
    {
        var content = ReadAppFile("Components", "Layout", "MainLayout.razor");

        Assert.Contains("MudLayout", content);
        Assert.Contains("MudAppBar", content);
        Assert.Contains("MudDrawer", content);
        Assert.Contains("MudMainContent", content);
        Assert.Contains("MudNavMenu", content);
        Assert.Contains("MudNavLink", content);
        Assert.Contains("MudThemeProvider", content);

        // Must not use deleted shell components
        Assert.DoesNotContain("<DabblerShell", content);
        Assert.DoesNotContain("<DabblerNavigationPanel", content);
        Assert.DoesNotContain("<DabblerTabbedWorkspace", content);
        Assert.DoesNotContain("<DabblerTopMenuBar", content);
    }

    [Fact]
    public void MainLayout_HasRouteBasedNavigationLinks()
    {
        var content = ReadAppFile("Components", "Layout", "MainLayout.razor");

        // Core entity nav links should use Href routing
        Assert.Contains("Href=\"/dashboard\"", content);
        Assert.Contains("Href=\"/orders\"", content);
        Assert.Contains("Href=\"/products\"", content);
    }

    [Fact]
    public void MainLayout_IncludesMudBlazorProviders()
    {
        var content = ReadAppFile("Components", "Layout", "MainLayout.razor");

        Assert.Contains("MudPopoverProvider", content);
        Assert.Contains("MudDialogProvider", content);
        Assert.Contains("MudSnackbarProvider", content);
    }

    // ── Dashboard ─────────────────────────────────────────────────────

    [Fact]
    public void Dashboard_UsesMudBlazorCardsForNavigation()
    {
        var content = ReadAppFile("Components", "Pages", "Dashboard.razor");

        Assert.Contains("MudCard", content);
        Assert.Contains("MudCardContent", content);
        Assert.Contains("MudIcon", content);
        Assert.Contains("/orders", content);
        Assert.Contains("/products", content);
        Assert.Contains("/customers", content);

        // Must not use deleted dashboard wrappers
        Assert.DoesNotContain("<DabblerDashboardPage", content);
        Assert.DoesNotContain("<DabblerNavigationCard", content);
        Assert.DoesNotContain("<DabblerQuickLinksGroup", content);
        Assert.DoesNotContain("<DabblerEmbeddedSearch", content);
        Assert.DoesNotContain("<DabblerRecentItems", content);
        Assert.DoesNotContain("<DabblerFavorites", content);
    }

    [Fact]
    public void Dashboard_HasReportQuickLinks()
    {
        var content = ReadAppFile("Components", "Pages", "Dashboard.razor");

        Assert.Contains("/reports/orders-by-customer", content);
        Assert.Contains("/reports/sales-by-product", content);
    }

    // ── Query Pages ───────────────────────────────────────────────────

    [Theory]
    [InlineData("Products", "ProductList.razor")]
    [InlineData("Customers", "CustomerList.razor")]
    [InlineData("Orders", "OrderList.razor")]
    public void QueryPages_UseMudDataGridAndFilterPanel(string folder, string file)
    {
        var content = ReadAppFile("Components", "Pages", folder, file);

        Assert.Contains("MudDataGrid", content);
        Assert.Contains("FilterPanel", content);
        Assert.Contains("ServerData", content);

        Assert.DoesNotContain("<DabblerQueryPage", content);
        Assert.DoesNotContain("<DabblerDataGrid", content);
        Assert.DoesNotContain("<DabblerFilterGrid", content);
    }

    [Theory]
    [InlineData("Products", "ProductList.razor")]
    [InlineData("Customers", "CustomerList.razor")]
    [InlineData("Orders", "OrderList.razor")]
    public void QueryPages_HaveInlineToolbarAndErrorBanner(string folder, string file)
    {
        var content = ReadAppFile("Components", "Pages", folder, file);

        Assert.Contains("MudAlert", content);
        Assert.DoesNotContain("<DabblerPageToolbar", content);
        Assert.DoesNotContain("<DabblerPageErrorBanner", content);
    }

    [Theory]
    [InlineData("Products", "ProductList.razor")]
    [InlineData("Customers", "CustomerList.razor")]
    [InlineData("Orders", "OrderList.razor")]
    public void QueryPages_UseRowClickNavigation(string folder, string file)
    {
        var content = ReadAppFile("Components", "Pages", folder, file);

        Assert.Contains("RowClick", content);
        Assert.Contains("NavigateTo", content);
    }

    // ── Detail Pages ──────────────────────────────────────────────────

    [Theory]
    [InlineData("Products", "ProductDetail.razor")]
    [InlineData("Customers", "CustomerDetail.razor")]
    [InlineData("Orders", "OrderDetail.razor")]
    public void DetailPages_UseMudFormWithMudBlazorInputs(string folder, string file)
    {
        var content = ReadAppFile("Components", "Pages", folder, file);

        Assert.Contains("MudForm", content);

        Assert.DoesNotContain("<DabblerDetailForm", content);
        Assert.DoesNotContain("<DabblerComboBox", content);
    }

    [Theory]
    [InlineData("Products", "ProductDetail.razor")]
    [InlineData("Customers", "CustomerDetail.razor")]
    public void DetailPages_HaveSaveAndUndoActions(string folder, string file)
    {
        var content = ReadAppFile("Components", "Pages", folder, file);

        Assert.Contains("Save", content);
        Assert.Contains("Undo", content);
    }

    [Fact]
    public void OrderDetail_UseMudTabsForChildGrids()
    {
        var content = ReadAppFile("Components", "Pages", "Orders", "OrderDetail.razor");

        Assert.Contains("MudTabs", content);
        Assert.Contains("MudTabPanel", content);
        Assert.Contains("MudDataGrid", content);
    }

    // ── Admin Pages ───────────────────────────────────────────────────

    [Fact]
    public void UserManagement_UsesMudDataGridAndApiClient()
    {
        var content = ReadAppFile("Components", "Pages", "Admin", "UserManagement.razor");

        Assert.Contains("MudDataGrid", content);
        Assert.Contains("AdminUserApiClient", content);
        Assert.DoesNotContain("DabblerSecurityDbContext", content);
    }

    [Fact]
    public void ProductManagement_UsesMudDataGridAndDialogService()
    {
        var content = ReadAppFile("Components", "Pages", "Admin", "ProductManagement.razor");

        Assert.Contains("MudDataGrid", content);
        Assert.Contains("IDialogService", content);
    }

    // ── Reports ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("OrdersByCustomer.razor")]
    [InlineData("SalesByProduct.razor")]
    public void ReportPages_UseDabblerReportPageDirectly(string file)
    {
        var content = ReadAppFile("Components", "Pages", "Reports", file);

        Assert.Contains("DabblerReportPage", content);
        Assert.Contains("ShowPdfExport=\"true\"", content);
        Assert.DoesNotContain("DabblerQueryReportPage", content);
    }

    // ── Security Components ───────────────────────────────────────────

    [Fact]
    public void SecurityDashboard_UsesMudTabs()
    {
        var repoRoot = GetRepoRoot();
        var content = File.ReadAllText(Path.Combine(repoRoot,
            "src", "Libs", "Dabbler.Security.Blazor", "Components", "Admin", "DabblerSecurityDashboard.razor"));

        Assert.Contains("MudTabs", content);
        Assert.Contains("MudTabPanel", content);
    }

    [Fact]
    public void PermissionMatrices_UseMudSimpleTable()
    {
        var repoRoot = GetRepoRoot();
        var componentsDir = Path.Combine(repoRoot, "src", "Libs", "Dabbler.Security.Blazor", "Components", "Admin");

        string[] matrixFiles =
        [
            "DabblerPagePermissionMatrix.razor",
            "DabblerEntityPermissionMatrix.razor",
            "DabblerEndpointPermissionMatrix.razor"
        ];

        foreach (var file in matrixFiles)
        {
            var path = Path.Combine(componentsDir, file);
            if (!File.Exists(path)) continue;
            var content = File.ReadAllText(path);
            Assert.Contains("MudSimpleTable", content);
        }
    }

    // ── Deleted Wrapper Sweep ─────────────────────────────────────────

    [Theory]
    [InlineData("Components", "Pages", "Dashboard.razor")]
    [InlineData("Components", "Pages", "Products", "ProductList.razor")]
    [InlineData("Components", "Pages", "Products", "ProductDetail.razor")]
    [InlineData("Components", "Pages", "Customers", "CustomerList.razor")]
    [InlineData("Components", "Pages", "Customers", "CustomerDetail.razor")]
    [InlineData("Components", "Pages", "Orders", "OrderList.razor")]
    [InlineData("Components", "Pages", "Orders", "OrderDetail.razor")]
    [InlineData("Components", "Layout", "MainLayout.razor")]
    public void AppPages_DoNotReferenceDeletedWrappers(params string[] pathParts)
    {
        var content = ReadAppFile(pathParts);

        string[] deletedWrappers =
        [
            "DabblerPageToolbar",
            "DabblerPageErrorBanner",
            "DabblerQueryPage",
            "DabblerDataGrid",
            "DabblerFilterGrid",
            "DabblerDetailForm",
            "DabblerRecordPopup",
            "DabblerComboBox",
            "DabblerMultiComboBox"
        ];

        foreach (var wrapper in deletedWrappers)
        {
            Assert.DoesNotContain($"<{wrapper}", content);
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private static string ReadAppFile(params string[] pathParts)
    {
        var repoRoot = GetRepoRoot();
        var segments = new[] { repoRoot, "src", "Apps", "AcmeCoffeeOnline.Web" }
            .Concat(pathParts)
            .ToArray();
        return File.ReadAllText(Path.Combine(segments));
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
