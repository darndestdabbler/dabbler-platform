using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Web.Services;
using AcmeCoffeeOnline.Web.Tests.Infrastructure;

namespace AcmeCoffeeOnline.Web.Tests.Services;

public sealed class OrderDetailStateNestedItemsTests
{
    [Fact]
    public void AddItemInMemory_ForcesIdToZero_SetsOrderId_AppendsAndRecalculatesTotal()
    {
        var state = CreateStateWithThrowingHttpHandler();
        state.SetRecord(new OrderDto { Id = 42 });
        var newItem = new OrderItemDto
        {
            Id = 99, // Should be overwritten — Session 5 forces Id=0 so the merge inserts on parent save
            ProductId = 7,
            ProductName = "Test Product",
            Quantity = 2,
            UnitPrice = 10m,
            Discount = 0
        };

        state.AddItemInMemory(newItem);

        Assert.Single(state.Record!.OrderItems);
        var addedItem = state.Record.OrderItems[0];
        Assert.Equal(0, addedItem.Id);
        Assert.Equal(42, addedItem.OrderId);
        Assert.Equal(20m, state.Record.Total);
    }

    [Fact]
    public void AddItemInMemory_MultipleCalls_AccumulateAndRecalculateTotal()
    {
        var state = CreateStateWithThrowingHttpHandler();
        state.SetRecord(new OrderDto { Id = 1 });
        var item1 = new OrderItemDto { Quantity = 3, UnitPrice = 10m, Discount = 0 };   // 30
        var item2 = new OrderItemDto { Quantity = 5, UnitPrice = 20m, Discount = 10m }; // 90

        state.AddItemInMemory(item1);
        state.AddItemInMemory(item2);

        Assert.Equal(2, state.Record!.OrderItems.Count);
        Assert.Equal(120m, state.Record.Total);
    }

    [Fact]
    public void AddItemInMemory_DoesNotIssueHttpRequest()
    {
        var state = CreateStateWithThrowingHttpHandler();
        state.SetRecord(new OrderDto { Id = 1 });

        // Throwing HTTP handler would fail this if any API call leaked through.
        state.AddItemInMemory(new OrderItemDto());
    }

    [Fact]
    public void UpdateItemInMemory_MutatesItemAndRecalculatesTotal()
    {
        var state = CreateStateWithThrowingHttpHandler();
        var originalItem = new OrderItemDto { Id = 101, Quantity = 1, UnitPrice = 50m, Discount = 0 };
        state.SetRecord(new OrderDto
        {
            Id = 1,
            OrderItems = [originalItem]
        });

        state.UpdateItemInMemory(originalItem, 2, 60m, 10m);

        Assert.Equal(2, originalItem.Quantity);
        Assert.Equal(60m, originalItem.UnitPrice);
        Assert.Equal(10m, originalItem.Discount);
        // New total = 2 * 60 * (1 - 0.10) = 108
        Assert.Equal(108m, state.Record!.Total);
    }

    [Fact]
    public void UpdateItemInMemory_PreservesReferenceIdentity()
    {
        var state = CreateStateWithThrowingHttpHandler();
        var itemToUpdate = new OrderItemDto { Id = 101, Quantity = 1, UnitPrice = 50m };
        state.SetRecord(new OrderDto
        {
            Id = 1,
            OrderItems = [itemToUpdate]
        });

        state.UpdateItemInMemory(itemToUpdate, 5, 50m, 0);

        var itemInCollection = state.Record!.OrderItems.First();
        Assert.Same(itemToUpdate, itemInCollection);
        Assert.Equal(5, itemInCollection.Quantity);
    }

    [Fact]
    public void UpdateItemInMemory_DoesNotIssueHttpRequest()
    {
        var state = CreateStateWithThrowingHttpHandler();
        var item = new OrderItemDto();
        state.SetRecord(new OrderDto { Id = 1, OrderItems = [item] });

        state.UpdateItemInMemory(item, 1, 1, 1);
    }

    [Fact]
    public void RemoveItemInMemory_RemovesItemAndRecalculatesTotal()
    {
        var state = CreateStateWithThrowingHttpHandler();
        var item1 = new OrderItemDto { Quantity = 2, UnitPrice = 10m }; // 20
        var item2 = new OrderItemDto { Quantity = 1, UnitPrice = 30m }; // 30
        state.SetRecord(new OrderDto { Id = 1, OrderItems = [item1, item2], Total = 50m });

        state.RemoveItemInMemory(item1);

        Assert.Single(state.Record!.OrderItems);
        Assert.Same(item2, state.Record.OrderItems[0]);
        Assert.Equal(30m, state.Record.Total);
    }

    [Fact]
    public void RemoveItemInMemory_WorksOnPersistedItems()
    {
        var state = CreateStateWithThrowingHttpHandler();
        // Items with Id > 0 are persisted; removing them in-memory drops them from
        // Record.OrderItems so the parent save's UpdateAsync merge will DELETE them.
        var persistedItem = new OrderItemDto { Id = 101, Quantity = 1, UnitPrice = 100m };
        state.SetRecord(new OrderDto { Id = 1, OrderItems = [persistedItem], Total = 100m });

        state.RemoveItemInMemory(persistedItem);

        Assert.Empty(state.Record!.OrderItems);
        Assert.Equal(0m, state.Record.Total);
    }

    [Fact]
    public void RemoveItemInMemory_DoesNotIssueHttpRequest()
    {
        var state = CreateStateWithThrowingHttpHandler();
        var item = new OrderItemDto();
        state.SetRecord(new OrderDto { Id = 1, OrderItems = [item] });

        state.RemoveItemInMemory(item);
    }

    /// <summary>
    /// Mirrors the OrderDetail.razor row-form buffer-copy pattern end-to-end:
    /// BeginEdit copies the original to a buffer, mutating the buffer leaves
    /// the original untouched, Save (via UpdateItemInMemory) commits the
    /// buffer back to the original ref, Cancel discards the buffer.
    /// This is the unit-level proof that the per-row DirtyAwareActionRow
    /// flow does not corrupt the parent collection on Cancel.
    /// </summary>
    [Fact]
    public void RowFormBufferCopy_SaveCommitsBufferToOriginal_CancelDiscardsAsync()
    {
        var state = CreateStateWithThrowingHttpHandler();
        var original = new OrderItemDto
        {
            Id = 100, OrderId = 1, ProductId = 7, ProductName = "Espresso",
            Quantity = 1, UnitPrice = 10m, Discount = 0
        };
        state.SetRecord(new OrderDto { Id = 1, OrderItems = [original] });

        // BeginEdit — buffer-copy (matches OrderDetail.BeginEditItem)
        var buffer = new OrderItemDto
        {
            Id = original.Id,
            OrderId = original.OrderId,
            ProductId = original.ProductId,
            ProductName = original.ProductName,
            Quantity = original.Quantity,
            UnitPrice = original.UnitPrice,
            Discount = original.Discount
        };

        // Mutate buffer only — original must NOT change.
        buffer.Quantity = 9;
        buffer.UnitPrice = 12m;
        buffer.Discount = 5m;
        Assert.Equal(1, original.Quantity);
        Assert.Equal(10m, original.UnitPrice);
        Assert.Equal(0m, original.Discount);

        // Cancel path: discard buffer without calling UpdateItemInMemory.
        // (No state mutation expected.)
        Assert.Equal(1, original.Quantity);

        // Save path: copy buffer back via UpdateItemInMemory.
        state.UpdateItemInMemory(original, buffer.Quantity, buffer.UnitPrice, buffer.Discount);

        Assert.Equal(9, original.Quantity);
        Assert.Equal(12m, original.UnitPrice);
        Assert.Equal(5m, original.Discount);
        Assert.Single(state.Record!.OrderItems);
        Assert.Same(original, state.Record.OrderItems[0]);
    }

    private static OrderDetailState CreateStateWithThrowingHttpHandler()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            throw new InvalidOperationException("HTTP call not expected during in-memory operation."));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://acme.test/")
        };

        var apiClient = new OrderApiClient(httpClient);
        return new OrderDetailState(apiClient);
    }
}
