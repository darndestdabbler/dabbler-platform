using AcmeCoffeeOnline.Shared.Enums;

namespace AcmeCoffeeOnline.Api.Services;

public class StatusTransitionResult
{
    public bool Succeeded { get; init; }
    public bool OrderFound { get; init; } = true;
    public string? ErrorMessage { get; init; }

    public static StatusTransitionResult Success => new() { Succeeded = true };

    public static StatusTransitionResult NotFound => new()
    {
        Succeeded = false,
        OrderFound = false,
        ErrorMessage = "Order not found."
    };

    public static StatusTransitionResult InvalidTransition(OrderStatus from, OrderStatus to) => new()
    {
        Succeeded = false,
        ErrorMessage = $"Cannot transition from {from} to {to}."
    };
}
