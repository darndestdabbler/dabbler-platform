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
            await page.GetByTestId("add-product-button").ClickAsync();
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

            // Verify the new item appears in the order items grid on the parent page
            var itemsGrid = page.GetByTestId("order-items-grid");
            await Assertions.Expect(itemsGrid.Locator("tr")).ToHaveCountAsync(2); // Header + 1 data row
            await Assertions.Expect(itemsGrid).ToContainTextAsync(productName);
            await Assertions.Expect(itemsGrid).ToContainTextAsync("2"); // Quantity

            // Verify persisted state via API
            var order = await _fixture.GetOrderByIdAsync(userId, orderId);
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
