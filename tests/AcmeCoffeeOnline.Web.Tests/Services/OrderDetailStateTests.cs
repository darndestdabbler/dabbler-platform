using System.Net;
using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Shared.Enums;
using AcmeCoffeeOnline.Web.Services;
using AcmeCoffeeOnline.Web.Tests.Infrastructure;

namespace AcmeCoffeeOnline.Web.Tests.Services;

public sealed class OrderDetailStateTests
{
    public static TheoryData<OrderStatus, OrderStatus[]> TransitionCases => new()
    {
        { OrderStatus.Pending, [OrderStatus.Confirmed, OrderStatus.Cancelled] },
        { OrderStatus.Confirmed, [OrderStatus.Shipped, OrderStatus.Cancelled] },
        { OrderStatus.Shipped, [OrderStatus.Delivered, OrderStatus.Cancelled] },
        { OrderStatus.Delivered, [] },
        { OrderStatus.Cancelled, [] }
    };

    [Theory]
    [MemberData(nameof(TransitionCases))]
    public void GetAvailableTransitions_ReturnsExpectedTransitions(OrderStatus current, OrderStatus[] expected)
    {
        var transitions = OrderDetailState.GetAvailableTransitions(current);

        Assert.Equal(expected, transitions);
    }

    [Fact]
    public void CalculateLineTotal_AppliesQuantityAndDiscount()
    {
        var item = new OrderItemDto
        {
            Quantity = 3,
            UnitPrice = 12.50m,
            Discount = 20m
        };

        var lineTotal = OrderDetailState.CalculateLineTotal(item);

        Assert.Equal(30m, lineTotal);
    }

    [Fact]
    public async Task InitializeAsync_ForNewOrder_SetsPendingDefaults()
    {
        var state = new OrderDetailState(CreateClient((_, _) =>
            Task.FromResult(StubHttpMessageHandler.Empty(HttpStatusCode.OK))));
        var before = DateTime.UtcNow.AddSeconds(-1);

        await state.InitializeAsync(isNew: true, primaryKey: null);

        var after = DateTime.UtcNow.AddSeconds(1);
        Assert.False(state.IsLoading);
        Assert.NotNull(state.Record);
        Assert.Equal(OrderStatus.Pending, state.Record!.Status);
        Assert.InRange(state.Record.OrderDate, before, after);
        Assert.InRange(state.Record.CreatedDate, before, after);
        Assert.InRange(state.Record.ModifiedDate, before, after);
        Assert.Empty(state.Notes);
    }

    [Fact]
    public async Task InitializeAsync_WithoutPrimaryKey_ClearsRecord()
    {
        var state = new OrderDetailState(CreateClient((_, _) =>
            Task.FromResult(StubHttpMessageHandler.Empty(HttpStatusCode.OK))));

        await state.InitializeAsync(isNew: false, primaryKey: null);

        Assert.False(state.IsLoading);
        Assert.Null(state.Record);
        Assert.Empty(state.Notes);
    }

    [Fact]
    public async Task InitializeAsync_ForExistingOrder_LoadsRecordAndNotes()
    {
        var record = new OrderDto
        {
            Id = 42,
            CustomerId = 7,
            CustomerName = "Retail 7",
            Status = OrderStatus.Confirmed
        };
        var notes = new List<OrderNoteDto>
        {
            new() { Id = 5, OrderId = 42, Author = "Casey", NoteText = "Packed" }
        };

        var state = new OrderDetailState(CreateClient((request, _) =>
        {
            return Task.FromResult(request.RequestUri?.PathAndQuery switch
            {
                "/api/orders/42" => StubHttpMessageHandler.Json(HttpStatusCode.OK, record),
                "/api/orders/42/notes" => StubHttpMessageHandler.Json(HttpStatusCode.OK, notes),
                _ => StubHttpMessageHandler.Empty(HttpStatusCode.NotFound)
            });
        }));

        await state.InitializeAsync(isNew: false, primaryKey: 42);

        Assert.False(state.IsLoading);
        Assert.NotNull(state.Record);
        Assert.Equal(42, state.Record!.Id);
        Assert.Single(state.Notes);
        Assert.Equal("Packed", state.Notes[0].NoteText);
    }

    [Fact]
    public async Task SaveAsync_RequiresNewRecordAndSuccessfulCreate()
    {
        var created = new OrderDto
        {
            Id = 88,
            Status = OrderStatus.Pending
        };
        var state = new OrderDetailState(CreateClient((request, _) =>
        {
            return Task.FromResult(request.RequestUri?.PathAndQuery == "/api/orders"
                ? StubHttpMessageHandler.Json(HttpStatusCode.Created, created)
                : StubHttpMessageHandler.Empty(HttpStatusCode.NotFound));
        }));

        Assert.False(await state.SaveAsync(isNew: true));

        state.SetRecord(new OrderDto { CustomerId = 5, Status = OrderStatus.Pending });
        Assert.False(await state.SaveAsync(isNew: false));

        Assert.True(await state.SaveAsync(isNew: true));
        Assert.Equal(88, state.Record!.Id);
    }

    [Fact]
    public async Task AddAndRemoveItemAsync_UpdateOrderTotal()
    {
        var handler = new StubHttpMessageHandler((request, _) =>
        {
            return Task.FromResult(request.Method.Method switch
            {
                "POST" when request.RequestUri?.PathAndQuery == "/api/orders/15/items"
                    => StubHttpMessageHandler.Json(HttpStatusCode.Created, new OrderItemDto
                    {
                        Id = 100,
                        OrderId = 15,
                        ProductId = 3,
                        ProductName = "Breakfast Blend",
                        Quantity = 2,
                        UnitPrice = 9.5m,
                        Discount = 10m
                    }),
                "DELETE" when request.RequestUri?.PathAndQuery == "/api/orders/15/items/100"
                    => StubHttpMessageHandler.Empty(HttpStatusCode.NoContent),
                _ => StubHttpMessageHandler.Empty(HttpStatusCode.NotFound)
            });
        });
        var state = new OrderDetailState(CreateClient(handler));
        state.SetRecord(new OrderDto { Id = 15 });

        await state.AddItemAsync(
            new ProductDto { Id = 3, Name = "Breakfast Blend", UnitPrice = 9.5m },
            quantity: 2,
            discount: 10m);

        Assert.False(state.IsAddingItem);
        Assert.Single(state.Record!.OrderItems);
        Assert.Equal(17.10m, state.Record.Total);

        await state.RemoveItemAsync(state.Record.OrderItems[0]);

        Assert.Empty(state.Record.OrderItems);
        Assert.Equal(0m, state.Record.Total);
    }

    [Fact]
    public async Task UpdateStatusAsync_ClearsBusyFlagWhenClientFails()
    {
        var state = new OrderDetailState(CreateClient((request, _) =>
        {
            return Task.FromResult(request.RequestUri?.PathAndQuery == "/api/orders/15/status"
                ? StubHttpMessageHandler.Empty(HttpStatusCode.InternalServerError)
                : StubHttpMessageHandler.Empty(HttpStatusCode.NotFound));
        }));
        state.SetRecord(new OrderDto { Id = 15, Status = OrderStatus.Pending });

        await Assert.ThrowsAsync<HttpRequestException>(() => state.UpdateStatusAsync(OrderStatus.Confirmed));

        Assert.False(state.IsStatusUpdating);
        Assert.Equal(OrderStatus.Pending, state.Record!.Status);
    }

    [Fact]
    public async Task DeleteAndNoteOperations_UpdateStateFromApiResults()
    {
        var refreshedNotes = new List<OrderNoteDto>
        {
            new() { Id = 8, OrderId = 15, Author = "Dana", NoteText = "Reopened note list" }
        };

        var state = new OrderDetailState(CreateClient((request, _) =>
        {
            return Task.FromResult((request.Method.Method, request.RequestUri?.PathAndQuery) switch
            {
                ("PATCH", "/api/orders/15/status") => StubHttpMessageHandler.Empty(HttpStatusCode.NoContent),
                ("DELETE", "/api/orders/15/notes/4") => StubHttpMessageHandler.Empty(HttpStatusCode.NoContent),
                ("GET", "/api/orders/15/notes") => StubHttpMessageHandler.Json(HttpStatusCode.OK, refreshedNotes),
                _ => StubHttpMessageHandler.Empty(HttpStatusCode.NotFound)
            });
        }));
        state.SetRecord(new OrderDto { Id = 15, Status = OrderStatus.Confirmed });
        state.Notes.Add(new OrderNoteDto { Id = 4, OrderId = 15, Author = "Alex", NoteText = "Original" });

        await state.DeleteAsync();
        await state.DeleteNoteAsync(state.Notes[0]);
        await state.RefreshNotesAsync();

        Assert.Equal(OrderStatus.Cancelled, state.Record!.Status);
        Assert.Single(state.Notes);
        Assert.Equal("Reopened note list", state.Notes[0].NoteText);
    }

    private static OrderApiClient CreateClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("https://acme.test/")
        };

        return new OrderApiClient(httpClient);
    }

    private static OrderApiClient CreateClient(StubHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://acme.test/")
        };

        return new OrderApiClient(httpClient);
    }
}
