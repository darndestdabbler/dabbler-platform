using System.Text.RegularExpressions;
using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

/// <summary>
/// Session 5 (composable-crud-helpers): Order → OrderItems nested 1:N flow.
/// Add / Edit / Delete on the nested grid all mutate the parent's in-memory
/// OrderItems collection. The order's DirtyAwareActionRow Save is the sole
/// server commit; OrderService.UpdateAsync merges the full graph via EF cascade.
/// </summary>
[Collection(PlaywrightCollection.Name)]
public sealed class OrderNestedItemsTests
{
    private readonly PlaywrightFixture _fixture;

    public OrderNestedItemsTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task NestedShell_AddItem_PersistsOnlyAfterParentSaveAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            var userId = await _fixture.GetSeededUserIdAsync("admin@acmecoffee.com");
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");

            var targetOrder = await PickModifiableOrderAsync(userId);
            await GoToOrderDetailAsync(page, targetOrder.Id);

            var preCount = targetOrder.OrderItems.Count;

            // Open the picker dialog, pick first product, fill link data, Save and Use.
            await page.GetByTestId("add-product-button").ClickAsync();
            await Assertions.Expect(page.GetByTestId("product-line-picker")).ToBeVisibleAsync();
            var firstProductRow = page.Locator("[data-testid='product-line-picker-grid'] .mud-table-body tr").First;
            await Assertions.Expect(firstProductRow).ToBeVisibleAsync();
            await firstProductRow.GetByTestId("product-line-picker-row-use").ClickAsync();
            await PlaywrightFixture.FillFormFieldAsync(page, "product-line-picker-quantity", "3");
            await PlaywrightFixture.FillFormFieldAsync(page, "product-line-picker-discount", "5");
            await page.GetByTestId("product-line-picker-primary-button").ClickAsync();
            await Assertions.Expect(page.GetByTestId("product-line-picker")).Not.ToBeVisibleAsync();

            // The grid shows the new line, BUT the DB still has the original set
            // because the add is in-memory. The parent's Save is the commit.
            var orderBeforeParentSave = await _fixture.GetOrderByIdAsync(userId, targetOrder.Id);
            Assert.Equal(preCount, orderBeforeParentSave.OrderItems.Count);

            // Save the parent order — this is what cascades the new item to the DB.
            await Assertions.Expect(page.GetByTestId("save-button")).ToBeEnabledAsync();
            await page.GetByTestId("save-button").ClickAsync();
            await _fixture.WaitForQuietAsync(page);

            var orderAfterParentSave = await FetchOrderUntilAsync(userId, targetOrder.Id,
                o => o.OrderItems.Count == preCount + 1);
            Assert.Equal(preCount + 1, orderAfterParentSave.OrderItems.Count);

            // Cleanup: remove the new item we just added so the test is idempotent.
            var newItem = orderAfterParentSave.OrderItems
                .OrderByDescending(i => i.Id)
                .First();
            await _fixture.RemoveOrderItemAsync(userId, targetOrder.Id, newItem.Id);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task NestedShell_DeleteItem_PersistsOnlyAfterParentSaveAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            var userId = await _fixture.GetSeededUserIdAsync("admin@acmecoffee.com");
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");

            // We need an order whose modifiable status AND has at least one item we can
            // remove without leaving the order empty (we restore it at the end).
            var targetOrder = await PickModifiableOrderAsync(userId, minItems: 2);
            await GoToOrderDetailAsync(page, targetOrder.Id);

            var preCount = targetOrder.OrderItems.Count;
            var itemToRestore = targetOrder.OrderItems.First();

            // Delete the first row from the grid (in-memory only).
            var firstDelete = page.GetByTestId("delete-item-button").First;
            await Assertions.Expect(firstDelete).ToBeVisibleAsync();
            await firstDelete.ClickAsync();
            await _fixture.WaitForQuietAsync(page);

            // DB still has the original count — delete is in-memory until parent save.
            var orderBeforeParentSave = await _fixture.GetOrderByIdAsync(userId, targetOrder.Id);
            Assert.Equal(preCount, orderBeforeParentSave.OrderItems.Count);

            await Assertions.Expect(page.GetByTestId("save-button")).ToBeEnabledAsync();
            await page.GetByTestId("save-button").ClickAsync();
            await _fixture.WaitForQuietAsync(page);

            var orderAfterParentSave = await FetchOrderUntilAsync(userId, targetOrder.Id,
                o => o.OrderItems.Count == preCount - 1);
            Assert.Equal(preCount - 1, orderAfterParentSave.OrderItems.Count);

            // Restore: re-add the deleted item so the seed stays clean.
            await _fixture.AddOrderItemAsync(
                userId,
                targetOrder.Id,
                productId: itemToRestore.ProductId,
                quantity: itemToRestore.Quantity,
                unitPrice: itemToRestore.UnitPrice,
                discount: itemToRestore.Discount);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    [Fact]
    public async Task NestedShell_EditItem_PersistsOnlyAfterParentSaveAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            var userId = await _fixture.GetSeededUserIdAsync("admin@acmecoffee.com");
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");

            var targetOrder = await PickModifiableOrderAsync(userId);
            await GoToOrderDetailAsync(page, targetOrder.Id);

            var itemToEdit = targetOrder.OrderItems.First();
            var newQuantity = itemToEdit.Quantity + 7;

            // Click the row's edit button — the per-row form below the grid appears.
            await page.GetByTestId("edit-item-button").First.ClickAsync();
            await Assertions.Expect(page.GetByTestId("edit-item-quantity")).ToBeVisibleAsync();

            // Mutate Quantity in the row form, click Save line (in-memory only).
            await PlaywrightFixture.FillFormFieldAsync(page, "edit-item-quantity", newQuantity.ToString());
            await Assertions.Expect(page.GetByTestId("save-item-button")).ToBeEnabledAsync();
            await page.GetByTestId("save-item-button").ClickAsync();
            await _fixture.WaitForQuietAsync(page);

            // DB still has the original quantity until parent save commits.
            var orderBeforeParentSave = await _fixture.GetOrderByIdAsync(userId, targetOrder.Id);
            var preItem = orderBeforeParentSave.OrderItems.First(i => i.Id == itemToEdit.Id);
            Assert.Equal(itemToEdit.Quantity, preItem.Quantity);

            // Parent save commits the row-form edit via merge.
            await Assertions.Expect(page.GetByTestId("save-button")).ToBeEnabledAsync();
            await page.GetByTestId("save-button").ClickAsync();
            await _fixture.WaitForQuietAsync(page);

            var orderAfterParentSave = await FetchOrderUntilAsync(userId, targetOrder.Id,
                o => o.OrderItems.Any(i => i.Id == itemToEdit.Id && i.Quantity == newQuantity));
            var persisted = orderAfterParentSave.OrderItems.First(i => i.Id == itemToEdit.Id);
            Assert.Equal(newQuantity, persisted.Quantity);

            // Restore: roll the quantity back so subsequent runs see the seed shape.
            await _fixture.RemoveOrderItemAsync(userId, targetOrder.Id, itemToEdit.Id);
            await _fixture.AddOrderItemAsync(
                userId,
                targetOrder.Id,
                productId: itemToEdit.ProductId,
                quantity: itemToEdit.Quantity,
                unitPrice: itemToEdit.UnitPrice,
                discount: itemToEdit.Discount);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    private async Task<OrderSnapshot> PickModifiableOrderAsync(string userId, int minItems = 1)
    {
        // Status 0 = Pending, 1 = Confirmed — both modifiable per OrderService.IsModifiable.
        // The paged list endpoint doesn't hydrate OrderItems, so we have to fetch full
        // detail for each modifiable candidate until we find one that matches minItems.
        var orders = await _fixture.GetOrdersAsync(userId, pageSize: 100);
        foreach (var summary in orders.Where(o => o.Status == 0 || o.Status == 1))
        {
            var detail = await _fixture.GetOrderByIdAsync(userId, summary.Id);
            if (detail.OrderItems.Count >= minItems)
                return detail;
        }

        throw new Xunit.Sdk.XunitException(
            $"No seeded order found in Pending/Confirmed status with >= {minItems} item(s).");
    }

    /// <summary>
    /// Poll the read API until the predicate matches. WaitForQuietAsync confirms
    /// the client-side action completed (HTTP PUT response received), but the
    /// subsequent read GET can still race with commit visibility under load.
    /// Polls up to 10 times at 250ms intervals (~2.5s total).
    /// </summary>
    private async Task<OrderSnapshot> FetchOrderUntilAsync(string userId, int orderId, Func<OrderSnapshot, bool> predicate)
    {
        OrderSnapshot? snapshot = null;
        for (var i = 0; i < 10; i++)
        {
            snapshot = await _fixture.GetOrderByIdAsync(userId, orderId);
            if (predicate(snapshot)) return snapshot;
            await Task.Delay(250);
        }
        return snapshot!;
    }

    private async Task GoToOrderDetailAsync(IPage page, int orderId)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/orders/{orderId}", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await Assertions.Expect(page).ToHaveURLAsync(new Regex($".*/orders/{orderId}$"));
        await _fixture.WaitForQuietAsync(page);
    }
}
