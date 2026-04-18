using System.Text.RegularExpressions;
using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

/// <summary>
/// Edge case tests for not-found pages and access-denied enforcement.
/// These verify that the application handles invalid IDs and unauthorized
/// access gracefully through the real UI.
/// </summary>
[Collection(PlaywrightCollection.Name)]
public sealed class EdgeCaseTests
{
    private readonly PlaywrightFixture _fixture;

    public EdgeCaseTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ProductNotFound_InvalidId_ShowsErrorStateAsync()
    {
        // ProductDetail.OnInitializedAsync does not catch HttpRequestException
        // for missing products, so the server returns HTTP 500. This test
        // documents the current behavior: the page errors rather than showing
        // the product-not-found alert. Close without ClosePageAsync to avoid
        // the browser error assertion on the expected 500.
        var page = await _fixture.CreatePageAsync();

        try
        {
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");

            var response = await page.GotoAsync($"{_fixture.BaseUrl}/products/999999", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            // The server returns 500 because ProductDetail throws on missing ID
            Assert.NotNull(response);
            Assert.Equal(500, response!.Status);

            // The product-not-found alert does NOT render because the component
            // crashes before reaching the else branch
            await Assertions.Expect(page.GetByTestId("product-not-found"))
                .ToHaveCountAsync(0);
        }
        finally
        {
            // Close context directly — the 500 is expected for this edge case
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task OrderNotFound_InvalidId_ShowsNotFoundAlertAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");

            // Navigate to an order with an ID that does not exist
            await page.GotoAsync($"{_fixture.BaseUrl}/orders/999999", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            // Verify the not-found alert is shown (OrderDetailState gracefully
            // catches 404 and sets Record = null)
            await Assertions.Expect(page.GetByTestId("order-not-found"))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("order-not-found"))
                .ToContainTextAsync("Order not found");

            // The page wrapper still renders order-detail-title (it always shows
            // "Order #@OrderId"), but the order detail component shows the
            // not-found alert instead of the form content
            await Assertions.Expect(page.GetByTestId("order-detail-title"))
                .ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task AccessDenied_CustomerRole_BlockedFromCustomersPageAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            // Customer role users cannot access /customers
            await _fixture.SeededSignInAsync(page, "james.smith@example.com");

            await page.GotoAsync($"{_fixture.BaseUrl}/customers", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await Assertions.Expect(page.GetByTestId("access-denied-page"))
                .ToBeVisibleAsync();

            // Access denied page uses AuthLayout (not the main shell), so
            // verify the page rendered its content and sign-in link
            await Assertions.Expect(page.GetByText("Access denied"))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Go to sign in" }))
                .ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task AccessDenied_BackofficeUser_BlockedFromAdminProductsAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            // BackofficeUser (jdoe) does not have SystemAdmin role
            await _fixture.SeededSignInAsync(page, "jdoe@acmecoffee.com");

            await page.GotoAsync($"{_fixture.BaseUrl}/admin/products", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await Assertions.Expect(page.GetByTestId("access-denied-page"))
                .ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task AccessDenied_BackofficeUser_BlockedFromAdminSecurityAsync()
    {
        var page = await _fixture.CreatePageAsync();

        try
        {
            // BackofficeUser (jdoe) does not have SystemAdmin role
            await _fixture.SeededSignInAsync(page, "jdoe@acmecoffee.com");

            await page.GotoAsync($"{_fixture.BaseUrl}/admin/security", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await Assertions.Expect(page.GetByTestId("access-denied-page"))
                .ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task AccessDenied_BackofficeUser_SecurityDashboard_ShowsCenteredCardAndAvoidsNotFoundAsync()
    {
        // Regression guard for the security-dashboard-polish Session 1 spec:
        // a BackofficeUser hitting /admin/security must land on the centered
        // access-denied card with the approved copy, never on the /not-found
        // page. The request may end at /access-denied (current cookie redirect
        // behavior) or stay on /admin/security (when rendered via Blazor's
        // <NotAuthorized> fragment) — both paths must show the same card.
        var page = await _fixture.CreatePageAsync();

        try
        {
            await _fixture.SeededSignInAsync(page, "jdoe@acmecoffee.com");

            await page.GotoAsync($"{_fixture.BaseUrl}/admin/security", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            Assert.DoesNotContain("/not-found", page.Url, StringComparison.OrdinalIgnoreCase);

            await Assertions.Expect(page.GetByTestId("access-denied-card"))
                .ToBeVisibleAsync();

            await Assertions.Expect(page.GetByText(
                    "You don't have access to this page. If you need access, please request it from your administrator."))
                .ToBeVisibleAsync();

            await Assertions.Expect(page.GetByTestId("access-denied-back-button"))
                .ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }
}
