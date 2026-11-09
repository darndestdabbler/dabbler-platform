using System.Text.RegularExpressions;
using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;
using Xunit.Sdk;

namespace AcmeCoffeeOnline.Playwright;

[Collection(PlaywrightCollection.Name)]
public sealed class CustomerWorkflowTests
{
    private readonly PlaywrightFixture _fixture;

    public CustomerWorkflowTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task E2E_WF_03_MagicLinkCustomer_OpensScopedOrderDetailAndBlocksForeignOrderAsync()
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

            await Assertions.Expect(page.GetByText("We sent a sign-in code"))
                .ToBeVisibleAsync();

            var magicLink = await _fixture.WaitForMagicLinkAsync(email);
            if (string.IsNullOrWhiteSpace(magicLink.LinkUrl))
            {
                throw new XunitException("Magic link payload did not include a link URL.");
            }

            await page.GotoAsync(magicLink.LinkUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await Assertions.Expect(page.GetByTestId("main-shell"))
                .ToBeVisibleAsync();

            var session = await _fixture.GetSessionInfoAsync(page);
            Assert.False(string.IsNullOrWhiteSpace(session.UserId));
            Assert.Contains("Customer", session.Roles ?? []);

            var activeOrganizationId = int.TryParse(session.ActiveOrganizationId, out var parsedOrganizationId)
                ? parsedOrganizationId
                : (int?)null;

            var visibleOrders = await _fixture.GetOrdersAsync(session.UserId!, activeOrganizationId, pageSize: 10);
            var visibleOrder = visibleOrders.FirstOrDefault()
                ?? throw new XunitException("The signed-in customer did not have any visible orders.");

            var verificationContext = await _fixture.GetCustomerOrderVerificationContextAsync(
                session.UserId!,
                visibleOrder.CustomerId,
                activeOrganizationId);

            // Navigate to orders via drawer
            await _fixture.NavigateViaDrawerAsync(page, "Orders", ".*/orders$");

            // Click the order detail link in the grid
            await Assertions.Expect(page.GetByTestId($"order-detail-link-{verificationContext.VisibleOrder.Id}"))
                .ToBeVisibleAsync();

            await page.GetByTestId($"order-detail-link-{verificationContext.VisibleOrder.Id}").ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await Assertions.Expect(page)
                .ToHaveURLAsync(new Regex($".*/orders/{verificationContext.VisibleOrder.Id}$"));
            await Assertions.Expect(page.GetByTestId("order-detail-page"))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("order-detail-title"))
                .ToHaveTextAsync($"Order #{verificationContext.VisibleOrder.Id}");
            await Assertions.Expect(page.GetByTestId("order-not-found"))
                .ToHaveCountAsync(0);

            // Verify accessibility on order detail page
            await PlaywrightFixture.AssertAccessibilityBasicsAsync(page);

            var verifiedVisibleOrder = await _fixture.GetOrderByIdAsync(
                session.UserId!,
                verificationContext.VisibleOrder.Id,
                activeOrganizationId);

            Assert.Equal(verificationContext.VisibleOrder.Id, verifiedVisibleOrder.Id);
            Assert.Equal(verificationContext.VisibleOrder.CustomerId, verifiedVisibleOrder.CustomerId);

            // Foreign order should show not-found
            await page.GotoAsync($"{_fixture.BaseUrl}/orders/{verificationContext.ForeignOrder.Id}", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await Assertions.Expect(page)
                .ToHaveURLAsync(new Regex($".*/orders/{verificationContext.ForeignOrder.Id}$"));
            await Assertions.Expect(page.GetByTestId("order-not-found"))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText(verificationContext.ForeignOrder.CustomerName, new PageGetByTextOptions { Exact = true }))
                .ToHaveCountAsync(0);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }
}
