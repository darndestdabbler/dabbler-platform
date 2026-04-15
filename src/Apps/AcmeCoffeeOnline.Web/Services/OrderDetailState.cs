using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Shared.Enums;

namespace AcmeCoffeeOnline.Web.Services;

public sealed class OrderDetailState
{
    private readonly OrderApiClient _orderClient;

    public OrderDetailState(OrderApiClient orderClient)
    {
        _orderClient = orderClient;
    }

    public OrderDto? Record { get; private set; }

    public List<OrderNoteDto> Notes { get; private set; } = [];

    public bool IsLoading { get; private set; } = true;

    public bool IsStatusUpdating { get; private set; }

    public bool IsAddingItem { get; private set; }

    public async Task InitializeAsync(bool isNew, object? primaryKey, CancellationToken cancellationToken = default)
    {
        IsLoading = true;

        if (isNew)
        {
            Record = new OrderDto
            {
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };
            Notes = [];
            IsLoading = false;
            return;
        }

        if (primaryKey is null)
        {
            Record = null;
            Notes = [];
            IsLoading = false;
            return;
        }

        var id = Convert.ToInt32(primaryKey);
        Record = await _orderClient.GetByIdAsync(id, cancellationToken);
        Notes = Record is not null
            ? await _orderClient.GetNotesByOrderIdAsync(Record.Id, cancellationToken)
            : [];
        IsLoading = false;
    }

    public void SetRecord(OrderDto record)
    {
        Record = record;
    }

    public async Task<bool> SaveAsync(bool isNew, CancellationToken cancellationToken = default)
    {
        if (!isNew || Record is null)
        {
            return false;
        }

        var created = await _orderClient.CreateAsync(Record, cancellationToken);
        if (created is null)
        {
            return false;
        }

        Record = created;
        return true;
    }

    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        if (Record is null)
        {
            return;
        }

        await _orderClient.UpdateStatusAsync(Record.Id, OrderStatus.Cancelled, cancellationToken);
        Record.Status = OrderStatus.Cancelled;
    }

    public async Task UpdateStatusAsync(OrderStatus newStatus, CancellationToken cancellationToken = default)
    {
        if (Record is null)
        {
            return;
        }

        IsStatusUpdating = true;
        try
        {
            await _orderClient.UpdateStatusAsync(Record.Id, newStatus, cancellationToken);
            Record.Status = newStatus;
        }
        finally
        {
            IsStatusUpdating = false;
        }
    }

    public async Task AddItemAsync(
        ProductDto? selectedProduct,
        int quantity,
        decimal discount,
        CancellationToken cancellationToken = default)
    {
        if (Record is null || selectedProduct is null)
        {
            return;
        }

        IsAddingItem = true;
        try
        {
            var newItem = new OrderItemDto
            {
                OrderId = Record.Id,
                ProductId = selectedProduct.Id,
                ProductName = selectedProduct.Name,
                Quantity = quantity,
                UnitPrice = selectedProduct.UnitPrice,
                Discount = discount
            };

            var created = await _orderClient.AddItemAsync(Record.Id, newItem, cancellationToken);
            if (created is not null)
            {
                Record.OrderItems.Add(created);
                RecalculateTotal();
            }
        }
        finally
        {
            IsAddingItem = false;
        }
    }

    public async Task RemoveItemAsync(OrderItemDto item, CancellationToken cancellationToken = default)
    {
        if (Record is null)
        {
            return;
        }

        await _orderClient.RemoveItemAsync(Record.Id, item.Id, cancellationToken);
        Record.OrderItems.Remove(item);
        RecalculateTotal();
    }

    public async Task DeleteNoteAsync(OrderNoteDto note, CancellationToken cancellationToken = default)
    {
        if (Record is null)
        {
            return;
        }

        await _orderClient.DeleteNoteAsync(Record.Id, note.Id, cancellationToken);
        Notes.Remove(note);
    }

    public async Task RefreshNotesAsync(CancellationToken cancellationToken = default)
    {
        if (Record is null)
        {
            return;
        }

        Notes = await _orderClient.GetNotesByOrderIdAsync(Record.Id, cancellationToken);
    }

    public static IReadOnlyList<OrderStatus> GetAvailableTransitions(OrderStatus current) => current switch
    {
        OrderStatus.Pending => [OrderStatus.Confirmed, OrderStatus.Cancelled],
        OrderStatus.Confirmed => [OrderStatus.Shipped, OrderStatus.Cancelled],
        OrderStatus.Shipped => [OrderStatus.Delivered, OrderStatus.Cancelled],
        _ => []
    };

    public static decimal CalculateLineTotal(OrderItemDto item)
        => item.Quantity * item.UnitPrice * (1 - item.Discount / 100m);

    private void RecalculateTotal()
    {
        if (Record is null)
        {
            return;
        }

        Record.Total = Record.OrderItems.Sum(CalculateLineTotal);
    }
}
