using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

[Collection(PlaywrightCollection.Name)]
public sealed class OrderProductLinePickerWorkflowTests
{
    private readonly PlaywrightFixture _fixture;
    private static readonly string RunId = Guid.NewGuid().ToString("N")[..8];

    public OrderProductLinePickerWorkflowTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddProduct_PickFillAndSave_PersistsOrderItemAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            var userId = await _fixture.GetSeededUserIdAsync("admin@acmecoffee.com");
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");
            await page.GotoAsync($"{_fixture.BaseUrl}/orders/new", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            // An order must exist before adding products. Pick a customer and save the order first.
            await page.GetByTestId("pick-customer-button").ClickAsync();
            await Assertions.Expect(page.GetByTestId("customer-picker-dialog")).ToBeVisibleAsync();
            var firstCustomerRow = page.Locator("[data-testid='customer-picker-grid'] .mud-table-body tr").First;
            await Assertions.Expect(firstCustomerRow).ToBeVisibleAsync();
            await firstCustomerRow.GetByTestId("customer-picker-dialog-row-use").ClickAsync();
            await Assertions.Expect(page.GetByTestId("customer-picker-dialog")).Not.ToBeVisibleAsync();

            // Save the new order
            await page.GetByTestId("save-button").ClickAsync();
            await page.WaitForURLAsync(url => url.Contains("/orders/") && !url.EndsWith("/new"));

            var orderId = int.Parse(page.Url.Split('/').Last());
            Assert.True(orderId > 0);

            // Now, add a product to the newly created order
            await page.GetByTestId("order-items-shell-add-button").ClickAsync();
            await Assertions.Expect(page.GetByTestId("product-line-picker")).ToBeVisibleAsync();

            // Select the first product from the grid. This populates the target card but keeps the dialog open.
            var firstProductRow = page.Locator("[data-testid='product-line-picker-grid'] .mud-table-body tr").First;
            await Assertions.Expect(firstProductRow).ToBeVisibleAsync();
            var productName = (await firstProductRow.Locator("td").Nth(0).InnerTextAsync()).Trim();
            await firstProductRow.GetByTestId("product-line-picker-row-use").ClickAsync();

            // Dialog stays open (row-use returns null → PickOrAddDialog keeps the dialog open for link data entry)
            await Assertions.Expect(page.GetByTestId("product-line-picker")).ToBeVisibleAsync();

            // Verify product is loaded into the (read-only) target card.
            // In MudBlazor 8.x, data-testid is placed directly on the <input> element; use ToHaveValueAsync.
            await Assertions.Expect(page.GetByTestId("product-line-picker-name")).ToHaveValueAsync(productName);

            // Fill in the link data (quantity and discount)
            await PlaywrightFixture.FillFormFieldAsync(page, "product-line-picker-quantity", "2");
            await PlaywrightFixture.FillFormFieldAsync(page, "product-line-picker-discount", "15.50");

            // Save and Use
            await page.GetByTestId("product-line-picker-primary-button").ClickAsync();
            await Assertions.Expect(page.GetByTestId("product-line-picker")).Not.ToBeVisibleAsync();

            // Verify the new item appears in the order items grid on the parent page.
            // Use `.mud-table-body tr` (data rows only) so unrelated header / filter /
            // expansion-shell chrome rows don't pollute the count.
            var itemsGrid = page.GetByTestId("order-items-grid");
            await Assertions.Expect(itemsGrid.Locator(".mud-table-body tr")).ToHaveCountAsync(1);
            await Assertions.Expect(itemsGrid).ToContainTextAsync(productName);
            await Assertions.Expect(itemsGrid).ToContainTextAsync("2"); // Quantity

            // Session 5: the add flow is now in-memory only. The order's Save
            // button commits the full graph (EF cascade). The grid above
            // reflects the in-memory state; persistence requires a parent save.
            await page.GetByTestId("save-button").ClickAsync();
            await _fixture.WaitForQuietAsync(page);

            // Verify persisted state via API after the parent save
            // Briefly poll — WaitForQuietAsync confirms the client action is done,
            // but a separate API GET round-trip can still race with the PUT's
            // commit visibility under heavy test load.
            OrderSnapshot order = null!;
            for (var i = 0; i < 10; i++)
            {
                order = await _fixture.GetOrderByIdAsync(userId, orderId);
                if (order.OrderItems.Count >= 1) break;
                await Task.Delay(250);
            }
            Assert.NotNull(order);
            var orderItem = Assert.Single(order.OrderItems);
            Assert.Equal(productName, orderItem.ProductName);
            Assert.Equal(2, orderItem.Quantity);
            Assert.Equal(15.50m, orderItem.Discount);
        }
        finally
        { 
            await _fixture.ClosePageAsync(page);
        }
    }
}
