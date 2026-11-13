using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Shared.Enums;

namespace AcmeCoffeeOnline.Api.Services;

public interface IOrderService
{
    Task<PagedResponse<OrderDto>> GetAllAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<OrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResponse<OrderDto>> GetByCustomerIdAsync(int customerId, PagedRequest request, CancellationToken cancellationToken = default);
    Task<OrderDto> CreateAsync(OrderDto dto, CancellationToken cancellationToken = default);
    Task<OrderDto?> UpdateAsync(int id, OrderDto dto, CancellationToken cancellationToken = default);
    Task<StatusTransitionResult> UpdateStatusAsync(int id, OrderStatus newStatus, CancellationToken cancellationToken = default);
    Task<OrderItemDto?> AddItemAsync(int orderId, OrderItemDto dto, CancellationToken cancellationToken = default);
    Task<bool> RemoveItemAsync(int orderId, int itemId, CancellationToken cancellationToken = default);
    Task<List<OrderNoteDto>> GetNotesByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);
    Task<OrderNoteDto?> AddNoteAsync(int orderId, OrderNoteDto dto, CancellationToken cancellationToken = default);
    Task<OrderNoteDto?> UpdateNoteAsync(int orderId, int noteId, OrderNoteDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteNoteAsync(int orderId, int noteId, CancellationToken cancellationToken = default);
}
