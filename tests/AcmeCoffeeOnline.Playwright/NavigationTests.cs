using System.Text.RegularExpressions;
using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

[Collection(PlaywrightCollection.Name)]
public sealed class NavigationTests
{
    private readonly PlaywrightFixture _fixture;

    public NavigationTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task SignInAndLoadShellAsync(IPage page, string email)
    {
        await _fixture.SeededSignInAsync(page, email);
        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await Assertions.Expect(page.GetByTestId("main-shell")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Drawer_NavigatesToDashboardAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndLoadShellAsync(page, "jdoe@acmecoffee.com");

            // Navigate away first so we can navigate back to dashboard
            await _fixture.NavigateViaDrawerAsync(page, "Orders", ".*/orders$");
            await _fixture.NavigateViaDrawerAsync(page, "Dashboard", ".*/dashboard$");

            var heading = page.GetByTestId("routed-page-content")
                .GetByRole(AriaRole.Heading);
            await Assertions.Expect(heading.First).ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task Drawer_NavigatesToOrdersAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndLoadShellAsync(page, "jdoe@acmecoffee.com");
            await _fixture.NavigateViaDrawerAsync(page, "Orders", ".*/orders$");

            await Assertions.Expect(page.GetByTestId("routed-page-content")).ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task Drawer_NavigatesToRetailBusinessesAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndLoadShellAsync(page, "jdoe@acmecoffee.com");
            await _fixture.NavigateViaDrawerAsync(page, "Retail Businesses", ".*/customers$");

            await Assertions.Expect(page.GetByTestId("routed-page-content")).ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task Drawer_NavigatesToProductsAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndLoadShellAsync(page, "jdoe@acmecoffee.com");
            await _fixture.NavigateViaDrawerAsync(page, "Products", ".*/products$");

            await Assertions.Expect(page.GetByTestId("routed-page-content")).ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task DrawerGroup_NavigatesToOrdersByRetailBusinessReportAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndLoadShellAsync(page, "jdoe@acmecoffee.com");
            await _fixture.NavigateViaDrawerGroupAsync(
                page, "Reports", "Orders by Retail Business", ".*/reports/orders-by-customer$");

            await Assertions.Expect(page.GetByTestId("routed-page-content")).ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task DrawerGroup_NavigatesToSalesByProductReportAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndLoadShellAsync(page, "jdoe@acmecoffee.com");
            await _fixture.NavigateViaDrawerGroupAsync(
                page, "Reports", "Sales by Product", ".*/reports/sales-by-product$");

            await Assertions.Expect(page.GetByTestId("routed-page-content")).ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task DrawerGroup_NavigatesToUserManagementAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            // BackofficeUserAdmin has access to User Management
            await SignInAndLoadShellAsync(page, "kbrown@acmecoffee.com");
            await _fixture.NavigateViaDrawerGroupAsync(
                page, "Admin", "User Management", ".*/admin/users$");

            await Assertions.Expect(page.GetByTestId("routed-page-content")).ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task DrawerGroup_NavigatesToProductManagementAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            // Only SystemAdmin can access Product Management
            await SignInAndLoadShellAsync(page, "admin@acmecoffee.com");
            await _fixture.NavigateViaDrawerGroupAsync(
                page, "Admin", "Product Management", ".*/admin/products$");

            await Assertions.Expect(page.GetByTestId("routed-page-content")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Product Administration" }))
                .ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task HamburgerToggle_CollapsesAndExpandsDrawerAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await SignInAndLoadShellAsync(page, "jdoe@acmecoffee.com");

            var drawer = page.GetByTestId("nav-drawer");
            var hamburger = page.Locator("button[aria-label='Toggle navigation menu']");

            // Drawer should be open initially with nav links visible
            await Assertions.Expect(drawer).ToBeVisibleAsync();
            var drawerClasses = await drawer.GetAttributeAsync("class") ?? "";
            Assert.Contains("open", drawerClasses, StringComparison.OrdinalIgnoreCase);

            // Click hamburger to toggle
            await hamburger.ClickAsync();
            await Task.Delay(500); // Animation delay: MudBlazor drawer CSS transition, not render-correlated.

            // Drawer state should change
            var toggledClasses = await drawer.GetAttributeAsync("class") ?? "";
            Assert.NotEqual(drawerClasses, toggledClasses);

            // Click hamburger again to restore
            await hamburger.ClickAsync();
            await Task.Delay(500); // Animation delay: MudBlazor drawer CSS transition, not render-correlated.

            // Drawer should return to open state
            var restoredClasses = await drawer.GetAttributeAsync("class") ?? "";
            Assert.Contains("open", restoredClasses, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }
}
