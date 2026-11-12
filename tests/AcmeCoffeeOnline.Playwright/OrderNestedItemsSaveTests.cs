using System.Text.RegularExpressions;
using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

/// <summary>
/// Session 5 (composable-crud-helpers): nested 1:N Orders → OrderItems edit
/// flow. Each test exercises an in-memory mutation (add via dialog, edit via
/// the row form's DirtyAwareActionRow, delete) and then clicks the parent
/// order's Save button. The order's UpdateAsync merges the full OrderItems
/// graph via EF cascade. Persistence is verified against the API after the
/// parent save.
/// </summary>
[Collection(PlaywrightCollection.Name)]
public sealed class OrderNestedItemsSaveTests
{
    private readonly PlaywrightFixture _fixture;
    private static readonly string RunId = Guid.NewGuid().ToString("N")[..8];

    public OrderNestedItemsSaveTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Edit an existing order line via the new shell-hosted row form. The
    /// per-line Save button (DirtyAwareActionRow inside the shell) only
    /// mutates the parent's in-memory OrderItems; the order's Save commits
    /// the merged graph.
    /// </summary>
    [Fact]
    public async Task EditItem_ViaRowForm_PersistsAfterParentSaveAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            var userId = await _fixture.GetSeededUserIdAsync("admin@acmecoffee.com");
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");

            // Find a modifiable order (Pending or Confirmed) with at least one item.
            var orderId = await FindModifiableOrderWithItemsAsync(userId);

            await page.GotoAsync($"{_fixture.BaseUrl}/orders/{orderId}",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            // Capture the original quantity of the first line item.
            var original = await _fixture.GetOrderByIdAsync(userId, orderId);
            Assert.NotEmpty(original.OrderItems);
            var firstItem = original.OrderItems[0];
            var newQuantity = firstItem.Quantity + 7; // deterministic delta

            // Click the row-level Edit (pencil) icon on the first row.
            var firstEditButton = page.GetByTestId("edit-item-button").First;
            await firstEditButton.ClickAsync();
            await _fixture.WaitForQuietAsync(page);

            // Row form should now be visible with the line's data.
            await Assertions.Expect(page.GetByTestId("edit-item-quantity")).ToBeVisibleAsync();

            // Update the quantity via the row form.
            await PlaywrightFixture.FillFormFieldAsync(
                page, "edit-item-quantity", newQuantity.ToString());

            // The row form's Save (DirtyAwareActionRow inside the shell)
            // becomes enabled after the change.
            await Assertions.Expect(page.GetByTestId("save-item-button")).ToBeEnabledAsync();
            await page.GetByTestId("save-item-button").ClickAsync();
            await _fixture.WaitForQuietAsync(page);

            // Edit form is dismissed; the parent's Save (data-testid=save-button)
            // is now the dirty-aware action — click it to commit the full graph.
            await SaveParentAsync(page);

            // Verify persistence — the merged update should land on the same Id.
            var persisted = await _fixture.GetOrderByIdAsync(userId, orderId);
            var persistedItem = persisted.OrderItems.First(i => i.Id == firstItem.Id);
            Assert.Equal(newQuantity, persistedItem.Quantity);

            // Restore original quantity so subsequent test runs are not affected.
            await page.GetByTestId("edit-item-button").First.ClickAsync();
            await _fixture.WaitForQuietAsync(page);
            await PlaywrightFixture.FillFormFieldAsync(
                page, "edit-item-quantity", firstItem.Quantity.ToString());
            await page.GetByTestId("save-item-button").ClickAsync();
            await _fixture.WaitForQuietAsync(page);
            await SaveParentAsync(page);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    /// <summary>
    /// Delete an existing order line via the row's delete icon. The deletion
    /// is in-memory only — the parent's Save commits the merged graph.
    /// </summary>
    [Fact]
    public async Task DeleteItem_ViaRowIcon_PersistsAfterParentSaveAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            var userId = await _fixture.GetSeededUserIdAsync("admin@acmecoffee.com");
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");

            // We need a modifiable order with >= 2 items so we can delete one
            // and the merge still has at least one item (the "skip-if-empty"
            // preservation heuristic in OrderService.UpdateAsync means we
            // never deliberately drive the dto.OrderItems list to zero).
            var orderId = await FindModifiableOrderWithMinItemsAsync(userId, minimumItems: 2);

            await page.GotoAsync($"{_fixture.BaseUrl}/orders/{orderId}",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            var original = await _fixture.GetOrderByIdAsync(userId, orderId);
            var initialCount = original.OrderItems.Count;
            var itemToDelete = original.OrderItems[0];

            // Click the row-level delete (trash) icon. Multiple rows have the
            // testid; .First() targets the first row.
            await page.GetByTestId("delete-item-button").First.ClickAsync();
            await _fixture.WaitForQuietAsync(page);

            // The grid should reflect the in-memory deletion (one fewer row).
            // Parent Save is now dirty.
            await SaveParentAsync(page);

            // Verify persistence — the deleted item should no longer be in the DB.
            var persisted = await _fixture.GetOrderByIdAsync(userId, orderId);
            Assert.Equal(initialCount - 1, persisted.OrderItems.Count);
            Assert.DoesNotContain(persisted.OrderItems, i => i.Id == itemToDelete.Id);

            // Restore: re-add the deleted item via API so subsequent test runs
            // start from the same baseline.
            await _fixture.AddOrderItemAsync(
                userId, orderId,
                productId: itemToDelete.ProductId,
                quantity: itemToDelete.Quantity,
                unitPrice: itemToDelete.UnitPrice,
                discount: itemToDelete.Discount);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    /// <summary>
    /// Cover the canonical Session 5 add-then-save flow. The
    /// ProductLinePickerDialog hands the new OrderItemDto to
    /// <c>OrderDetailState.AddItemInMemory</c>; persistence requires the
    /// parent order's Save. (The existing
    /// <c>OrderProductLinePickerWorkflowTests.AddProduct_PickFillAndSave_PersistsOrderItemAsync</c>
    /// also covers this end-to-end on a new order; this test exercises the
    /// "+Add" flow on an existing order with row counts before/after.)
    /// </summary>
    [Fact]
    public async Task AddItem_ViaPickerDialog_PersistsAfterParentSaveAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            var userId = await _fixture.GetSeededUserIdAsync("admin@acmecoffee.com");
            await _fixture.SeededSignInAsync(page, "admin@acmecoffee.com");

            var orderId = await FindModifiableOrderWithItemsAsync(userId);

            await page.GotoAsync($"{_fixture.BaseUrl}/orders/{orderId}",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            var original = await _fixture.GetOrderByIdAsync(userId, orderId);
            var initialCount = original.OrderItems.Count;
            var existingProductIds = original.OrderItems.Select(i => i.ProductId).ToHashSet();

            // Open the product line picker dialog
            await page.GetByTestId("add-product-button").ClickAsync();
            await Assertions.Expect(page.GetByTestId("product-line-picker")).ToBeVisibleAsync();

            // Pick a product that's NOT already on the order so we don't have
            // ambiguity between "new" and "modified existing line".
            var rows = page.Locator("[data-testid='product-line-picker-grid'] .mud-table-body tr");
            await Assertions.Expect(rows.First).ToBeVisibleAsync();

            // The product picker grid doesn't expose IDs directly; we accept
            // the first row's product. The merge will still work — either
            // the row matches an existing product (then the merge produces a
            // duplicate OrderItem with Id=0 → INSERT, not UPDATE) or it's a
            // new product (then INSERT).
            await rows.First.GetByTestId("product-line-picker-row-use").ClickAsync();
            await Assertions.Expect(page.GetByTestId("product-line-picker")).ToBeVisibleAsync();

            await PlaywrightFixture.FillFormFieldAsync(page, "product-line-picker-quantity", "3");
            await PlaywrightFixture.FillFormFieldAsync(page, "product-line-picker-discount", "5");
            await page.GetByTestId("product-line-picker-primary-button").ClickAsync();
            await Assertions.Expect(page.GetByTestId("product-line-picker")).Not.ToBeVisibleAsync();
            await _fixture.WaitForQuietAsync(page);

            // Save the parent order to commit the in-memory add.
            await SaveParentAsync(page);

            // Persisted order should now have one more item.
            var persisted = await _fixture.GetOrderByIdAsync(userId, orderId);
            Assert.Equal(initialCount + 1, persisted.OrderItems.Count);

            // Restore baseline: find the newly added item (not present before
            // by Id) and DELETE via the legacy endpoint.
            var newItem = persisted.OrderItems
                .Where(i => !original.OrderItems.Any(o => o.Id == i.Id))
                .OrderByDescending(i => i.Id)
                .First();
            await _fixture.RemoveOrderItemAsync(userId, orderId, newItem.Id);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // ── Helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Clicks the order's Save and waits for it to land: the button only
    /// renders disabled again once the PUT has returned and the edit session
    /// is clean, so the API read that follows sees the saved graph.
    /// </summary>
    private static async Task SaveParentAsync(IPage page)
    {
        await PlaywrightFixture.ClickSaveAsync(page);
        await PlaywrightFixture.AssertSaveDisabledAsync(page);
    }

    private async Task<int> FindModifiableOrderWithItemsAsync(string userId)
        => await FindModifiableOrderWithMinItemsAsync(userId, minimumItems: 1);

    private async Task<int> FindModifiableOrderWithMinItemsAsync(string userId, int minimumItems)
    {
        // GetOrdersAsync returns the page's items but without OrderItems
        // populated (the paged list endpoint maps to summary DTOs). Resolve
        // candidate orders then fetch each by Id until one matches.
        var orders = await _fixture.GetOrdersAsync(userId, pageSize: 50);
        foreach (var summary in orders)
        {
            // 0=Pending, 1=Confirmed — match OrderService.IsModifiable.
            if (summary.Status != 0 && summary.Status != 1)
                continue;

            var full = await _fixture.GetOrderByIdAsync(userId, summary.Id);
            if (full.OrderItems.Count >= minimumItems)
                return summary.Id;
        }
        throw new InvalidOperationException(
            $"No modifiable seeded order with >= {minimumItems} items found.");
    }
}
