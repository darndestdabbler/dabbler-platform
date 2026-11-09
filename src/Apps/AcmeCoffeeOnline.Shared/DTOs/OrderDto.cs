using AcmeCoffeeOnline.Shared.Enums;

namespace AcmeCoffeeOnline.Shared.DTOs;

public class OrderDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? ShipDate { get; set; }
    public OrderStatus Status { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public List<OrderItemDto> OrderItems { get; set; } = new();
    public List<OrderNoteDto> OrderNotes { get; set; } = new();
}
