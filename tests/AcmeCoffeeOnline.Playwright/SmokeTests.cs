using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;
using System.Text.RegularExpressions;
using Xunit.Sdk;

namespace AcmeCoffeeOnline.Playwright;

[Collection(PlaywrightCollection.Name)]
public sealed class SmokeTests
{
    private readonly PlaywrightFixture _fixture;

    public SmokeTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task HomePage_RedirectsToDashboardWithMudBlazorLayoutAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await _fixture.SeededSignInAsync(page, "jdoe@acmecoffee.com");

            await page.GotoAsync(_fixture.BaseUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            // Home page redirects to /dashboard
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/dashboard$"));

            // MudBlazor layout shell is present
            await Assertions.Expect(page.GetByTestId("main-shell"))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("link[data-dabbler-asset='dabbler-shell.css']"))
                .ToHaveCountAsync(1);
            await Assertions.Expect(page.GetByTestId("routed-page-content")
                    .GetByRole(AriaRole.Heading, new() { Name = "AcmeCoffeeDistributors Dashboard" }))
                .ToBeVisibleAsync();

            // Nav drawer and app bar are present
            await PlaywrightFixture.AssertNavDrawerVisibleAsync(page);
            await PlaywrightFixture.AssertLayoutSpacingAsync(page);
            await PlaywrightFixture.AssertAccessibilityBasicsAsync(page);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task LoginPage_RendersAnonymousAuthShellAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await page.GotoAsync($"{_fixture.BaseUrl}/login", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await Assertions.Expect(page.GetByTestId("auth-shell"))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("login-page"))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Sign In" }))
                .ToBeVisibleAsync();
            // Main layout shell should not appear on auth pages
            await Assertions.Expect(page.GetByTestId("main-shell"))
                .ToHaveCountAsync(0);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task MagicLinkFlow_SignsInRetailBusinessUserAndLoadsOrdersAsync()
    {
        const string email = "james.smith@example.com";
        var page = await _fixture.CreatePageAsync();

        try
        {
            await _fixture.ClearMagicLinksAsync(email);

            await page.GotoAsync($"{_fixture.BaseUrl}/login", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await page.GetByLabel("Email address").FillAsync(email);
            await page.GetByRole(AriaRole.Button, new() { Name = "Send sign-in link" }).ClickAsync();

            await Assertions.Expect(page.GetByText("We sent a sign-in code")).ToBeVisibleAsync();

            var magicLink = await _fixture.WaitForMagicLinkAsync(email);
            if (string.IsNullOrWhiteSpace(magicLink.LinkUrl))
            {
                throw new XunitException("Magic link payload did not include a link URL.");
            }

            Assert.Equal("MagicLink", magicLink.EmailType);

            await page.GotoAsync(magicLink.LinkUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await Assertions.Expect(page.GetByTestId("main-shell")).ToBeVisibleAsync();

            // Navigate to orders via the drawer nav menu
            await _fixture.NavigateViaDrawerAsync(page, "Orders", ".*/orders$");

            await Assertions.Expect(page.GetByTestId("access-denied-page"))
                .ToHaveCountAsync(0);

            var session = await _fixture.GetSessionInfoAsync(page);
            Assert.Contains("Customer", session.Roles ?? []);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task SeededSession_EnforcesPermissionsAndRetailBusinessOrderScopingAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await _fixture.SeededSignInAsync(page, "james.smith@example.com");

            // Customer user cannot access the Customers page
            await page.GotoAsync($"{_fixture.BaseUrl}/customers", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await Assertions.Expect(page.GetByTestId("access-denied-page")).ToBeVisibleAsync();

            var retailBusinessSession = await _fixture.GetSessionInfoAsync(page);
            Assert.Equal("james.smith@example.com", retailBusinessSession.Email);
            Assert.False(string.IsNullOrWhiteSpace(retailBusinessSession.UserId));

            // Verify API-level order scoping
            using (var ordersPayload = await _fixture.GetApiJsonAsync(
                "api/orders?page=1&pageSize=5",
                retailBusinessSession.UserId!))
            {
                var items = ordersPayload.RootElement.GetProperty("items").EnumerateArray().ToArray();
                Assert.NotEmpty(items);
                Assert.All(items, item =>
                    Assert.Equal("James Smith", item.GetProperty("customerName").GetString()));
            }

            // Switch to an internal user and verify customers page is accessible
            await _fixture.SignOutAsync(page);
            await _fixture.SeededSignInAsync(page, "jdoe@acmecoffee.com");

            // Navigate to customers via the drawer
            await _fixture.NavigateViaDrawerAsync(page, "Retail Businesses", ".*/customers$");
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task DrawerNavigation_RoutesToAllMainSectionsAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await _fixture.SeededSignInAsync(page, "jdoe@acmecoffee.com");

            await page.GotoAsync($"{_fixture.BaseUrl}/dashboard", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await Assertions.Expect(page.GetByTestId("main-shell")).ToBeVisibleAsync();

            // Navigate to each main section via the drawer
            await _fixture.NavigateViaDrawerAsync(page, "Orders", ".*/orders$");
            await _fixture.NavigateViaDrawerAsync(page, "Retail Businesses", ".*/customers$");
            await _fixture.NavigateViaDrawerAsync(page, "Products", ".*/products$");
            await _fixture.NavigateViaDrawerAsync(page, "Dashboard", ".*/dashboard$");

            // Navigate to a report via the drawer group
            await _fixture.NavigateViaDrawerGroupAsync(
                page, "Reports", "Orders by Retail Business", ".*/reports/orders-by-customer$");

            // Verify browser back navigation works with route-based nav
            await page.GoBackAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/dashboard$"));
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task AdminNavigation_UsesFocusedProductsEntryAndActualRolePermissionsAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            // kbrown is an admin (BackofficeUserAdmin) but not SystemAdmin
            await _fixture.SeededSignInAsync(page, "kbrown@acmecoffee.com");

            await page.GotoAsync(_fixture.BaseUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            // Non-system-admin should not see "Catalog" in the nav
            await Assertions.Expect(page.GetByText("Catalog", new() { Exact = true }))
                .ToHaveCountAsync(0);

            // Products page is accessible
            await _fixture.NavigateViaDrawerAsync(page, "Products", ".*/products$");

            // Products catalog is accessible
            await page.GotoAsync($"{_fixture.BaseUrl}/products/catalog", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/products/catalog$"));
            await Assertions.Expect(page.GetByTestId("access-denied-page")).ToHaveCountAsync(0);

            // User Management accessible via admin drawer group
            await _fixture.NavigateViaDrawerGroupAsync(page, "Admin", "User Management", ".*/admin/users$");

            // Product Management is reachable by BackofficeUserAdmin after the
            // Set A Session 3 handler rewrite removed the SystemAdmin-only gate.
            await page.GotoAsync($"{_fixture.BaseUrl}/admin/products", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
            await Assertions.Expect(page.GetByTestId("access-denied-page")).ToHaveCountAsync(0);

            // Security Dashboard is reachable by any baked-in role after
            // security-dashboard-uat-remediation Session 3 opened the page;
            // per-tab visibility still gates admin tabs by compiled privilege.
            await page.GotoAsync($"{_fixture.BaseUrl}/admin/security", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
            await Assertions.Expect(page.GetByTestId("access-denied-page")).ToHaveCountAsync(0);

            // Switch to SystemAdmin and verify admin sections accessible
            await _fixture.SignOutAsync(page);
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");

            await _fixture.NavigateViaDrawerGroupAsync(
                page, "Admin", "Product Management", ".*/admin/products$");
            await _fixture.NavigateViaDrawerGroupAsync(
                page, "Admin", "Security Dashboard", ".*/admin/security$");
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task DashboardCards_NavigateToEntityPagesAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await _fixture.SeededSignInAsync(page, "jdoe@acmecoffee.com");

            await page.GotoAsync($"{_fixture.BaseUrl}/dashboard", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            // Click the Orders navigation card
            await page.GetByTestId("nav-card-orders").ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/orders$"));

            // Go back and click Retail Businesses card
            await page.GoBackAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.GetByTestId("nav-card-customers").ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/customers$"));

            // Go back and click Products card
            await page.GoBackAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.GetByTestId("nav-card-products").ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/products$"));

            // Verify accessibility on last visited page
            await PlaywrightFixture.AssertAccessibilityBasicsAsync(page);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }
}
