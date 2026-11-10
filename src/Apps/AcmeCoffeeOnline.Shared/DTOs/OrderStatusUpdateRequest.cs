using AcmeCoffeeOnline.Shared.Enums;

namespace AcmeCoffeeOnline.Shared.DTOs;

public class OrderStatusUpdateRequest
{
    public OrderStatus NewStatus { get; set; }
}
