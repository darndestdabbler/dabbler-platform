using AcmeCoffeeOnline.Api.Data;
using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Shared.Enums;
using AcmeCoffeeOnline.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AcmeCoffeeOnline.Api.Services;

public class OrderService : IOrderService
{
    private readonly AcmeCoffeeDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public OrderService(AcmeCoffeeDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResponse<OrderDto>> GetAllAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        ClampPaging(request);
        var query = BuildOrderQuery();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(o =>
                (o.Notes != null && o.Notes.Contains(term)) ||
                o.Customer.FirstName.Contains(term) ||
                o.Customer.LastName.Contains(term));
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => MapToDto(o))
            .ToListAsync(cancellationToken);

        return new PagedResponse<OrderDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<OrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await BuildOrderQuery()
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.OrderNotes)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        return order is null ? null : MapToDto(order);
    }

    public async Task<PagedResponse<OrderDto>> GetByCustomerIdAsync(int customerId, PagedRequest request, CancellationToken cancellationToken = default)
    {
        ClampPaging(request);
        var query = BuildOrderQuery().Where(o => o.CustomerId == customerId);

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => MapToDto(o))
            .ToListAsync(cancellationToken);

        return new PagedResponse<OrderDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<OrderDto> CreateAsync(OrderDto dto, CancellationToken cancellationToken = default)
    {
        var order = new Order
        {
            CustomerId = dto.CustomerId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            Total = 0m,
            Notes = dto.Notes,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            ModifiedBy = dto.ModifiedBy
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        // Reload with Customer navigation for CustomerName mapping
        var created = await BuildOrderQuery()
            .FirstAsync(o => o.Id == order.Id, cancellationToken);

        return MapToDto(created);
    }

    public async Task<StatusTransitionResult> UpdateStatusAsync(int id, OrderStatus newStatus, CancellationToken cancellationToken = default)
    {
        var order = await FindOrderWithRowFilterAsync(id, cancellationToken);
        if (order is null)
            return StatusTransitionResult.NotFound;

        if (!IsValidTransition(order.Status, newStatus))
            return StatusTransitionResult.InvalidTransition(order.Status, newStatus);

        order.Status = newStatus;
        order.ModifiedDate = DateTime.UtcNow;

        if (newStatus == OrderStatus.Shipped)
            order.ShipDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return StatusTransitionResult.Success;
    }

    public async Task<OrderItemDto?> AddItemAsync(int orderId, OrderItemDto dto, CancellationToken cancellationToken = default)
    {
        var order = await FindOrderWithRowFilterAsync(orderId, cancellationToken);
        if (order is null || !IsModifiable(order.Status))
            return null;

        var product = await _db.Products.FindAsync(new object[] { dto.ProductId }, cancellationToken);
        if (product is null)
            return null;

        var item = new OrderItem
        {
            OrderId = orderId,
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice > 0 ? dto.UnitPrice : product.UnitPrice,
            Discount = dto.Discount
        };

        _db.OrderItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        // Recalculate order total from persisted data
        order.Total = await _db.OrderItems
            .Where(oi => oi.OrderId == orderId)
            .SumAsync(oi => oi.Quantity * oi.UnitPrice * (1 - oi.Discount), cancellationToken);
        order.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return new OrderItemDto
        {
            Id = item.Id,
            OrderId = item.OrderId,
            ProductId = item.ProductId,
            ProductName = product.Name,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            Discount = item.Discount
        };
    }

    public async Task<bool> RemoveItemAsync(int orderId, int itemId, CancellationToken cancellationToken = default)
    {
        var order = await FindOrderWithRowFilterAsync(orderId, cancellationToken);
        if (order is null || !IsModifiable(order.Status))
            return false;

        var item = await _db.OrderItems
            .FirstOrDefaultAsync(oi => oi.Id == itemId && oi.OrderId == orderId, cancellationToken);

        if (item is null)
            return false;

        _db.OrderItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);

        // Recalculate order total from persisted data
        order.Total = await _db.OrderItems
            .Where(oi => oi.OrderId == orderId)
            .SumAsync(oi => oi.Quantity * oi.UnitPrice * (1 - oi.Discount), cancellationToken);
        order.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<OrderNoteDto?> AddNoteAsync(int orderId, OrderNoteDto dto, CancellationToken cancellationToken = default)
    {
        var order = await FindOrderWithRowFilterAsync(orderId, cancellationToken);
        if (order is null || !IsModifiable(order.Status))
            return null;

        var note = new OrderNote
        {
            OrderId = orderId,
            NoteText = dto.NoteText,
            NoteDate = DateTime.UtcNow,
            Author = dto.Author
        };

        _db.OrderNotes.Add(note);
        await _db.SaveChangesAsync(cancellationToken);

        return new OrderNoteDto
        {
            Id = note.Id,
            OrderId = note.OrderId,
            NoteText = note.NoteText,
            NoteDate = note.NoteDate,
            Author = note.Author
        };
    }

    public async Task<List<OrderNoteDto>> GetNotesByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await FindOrderWithRowFilterAsync(orderId, cancellationToken);
        if (order is null)
            return new();

        return await _db.OrderNotes
            .AsNoTracking()
            .Where(n => n.OrderId == orderId)
            .OrderByDescending(n => n.NoteDate)
            .Select(n => new OrderNoteDto
            {
                Id = n.Id,
                OrderId = n.OrderId,
                NoteText = n.NoteText,
                NoteDate = n.NoteDate,
                Author = n.Author
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderNoteDto?> UpdateNoteAsync(int orderId, int noteId, OrderNoteDto dto, CancellationToken cancellationToken = default)
    {
        var order = await FindOrderWithRowFilterAsync(orderId, cancellationToken);
        if (order is null)
            return null;

        var note = await _db.OrderNotes
            .FirstOrDefaultAsync(n => n.Id == noteId && n.OrderId == orderId, cancellationToken);

        if (note is null)
            return null;

        note.NoteText = dto.NoteText;
        note.Author = dto.Author;

        await _db.SaveChangesAsync(cancellationToken);

        return new OrderNoteDto
        {
            Id = note.Id,
            OrderId = note.OrderId,
            NoteText = note.NoteText,
            NoteDate = note.NoteDate,
            Author = note.Author
        };
    }

    public async Task<bool> DeleteNoteAsync(int orderId, int noteId, CancellationToken cancellationToken = default)
    {
        var order = await FindOrderWithRowFilterAsync(orderId, cancellationToken);
        if (order is null)
            return false;

        var note = await _db.OrderNotes
            .FirstOrDefaultAsync(n => n.Id == noteId && n.OrderId == orderId, cancellationToken);

        if (note is null)
            return false;

        _db.OrderNotes.Remove(note);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<Order?> FindOrderWithRowFilterAsync(int orderId, CancellationToken cancellationToken)
    {
        var query = _db.Orders.AsQueryable();
        query = ApplyRowFilter(query);
        return await query.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }

    private static bool IsModifiable(OrderStatus status) =>
        status is OrderStatus.Pending or OrderStatus.Confirmed;

    private static void ClampPaging(PagedRequest request)
    {
        if (request.Page < 1) request.Page = 1;
        request.PageSize = Math.Clamp(request.PageSize, 1, 100);
    }

    private IQueryable<Order> BuildOrderQuery()
    {
        var query = _db.Orders
            .AsNoTracking()
            .Include(o => o.Customer);

        return ApplyRowFilter(query);
    }

    private IQueryable<Order> ApplyRowFilter(IQueryable<Order> query)
    {
        if (_currentUser.Role == Shared.Enums.UserRole.Customer && _currentUser.CustomerId.HasValue)
        {
            query = query.Where(o => o.CustomerId == _currentUser.CustomerId.Value);
        }

        return query;
    }

    private static bool IsValidTransition(OrderStatus current, OrderStatus next)
    {
        return (current, next) switch
        {
            (OrderStatus.Pending, OrderStatus.Confirmed) => true,
            (OrderStatus.Confirmed, OrderStatus.Shipped) => true,
            (OrderStatus.Shipped, OrderStatus.Delivered) => true,
            // Can cancel from any non-Delivered state
            (OrderStatus.Pending, OrderStatus.Cancelled) => true,
            (OrderStatus.Confirmed, OrderStatus.Cancelled) => true,
            (OrderStatus.Shipped, OrderStatus.Cancelled) => true,
            _ => false
        };
    }

    private static IQueryable<Order> ApplySorting(IQueryable<Order> query, string? sortBy, bool descending)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "orderdate" => descending ? query.OrderByDescending(o => o.OrderDate) : query.OrderBy(o => o.OrderDate),
            "total" => descending ? query.OrderByDescending(o => o.Total) : query.OrderBy(o => o.Total),
            "status" => descending ? query.OrderByDescending(o => o.Status) : query.OrderBy(o => o.Status),
            "customername" => descending
                ? query.OrderByDescending(o => o.Customer.LastName).ThenByDescending(o => o.Customer.FirstName)
                : query.OrderBy(o => o.Customer.LastName).ThenBy(o => o.Customer.FirstName),
            "shipdate" => descending ? query.OrderByDescending(o => o.ShipDate) : query.OrderBy(o => o.ShipDate),
            "createddate" => descending ? query.OrderByDescending(o => o.CreatedDate) : query.OrderBy(o => o.CreatedDate),
            "modifieddate" => descending ? query.OrderByDescending(o => o.ModifiedDate) : query.OrderBy(o => o.ModifiedDate),
            _ => query.OrderByDescending(o => o.OrderDate)
        };
    }

    private static OrderDto MapToDto(Order o) => new()
    {
        Id = o.Id,
        CustomerId = o.CustomerId,
        CustomerName = o.Customer is not null
            ? $"{o.Customer.FirstName} {o.Customer.LastName}"
            : string.Empty,
        OrderDate = o.OrderDate,
        ShipDate = o.ShipDate,
        Status = o.Status,
        Total = o.Total,
        Notes = o.Notes,
        CreatedDate = o.CreatedDate,
        ModifiedDate = o.ModifiedDate,
        ModifiedBy = o.ModifiedBy,
        OrderItems = o.OrderItems?.Select(oi => new OrderItemDto
        {
            Id = oi.Id,
            OrderId = oi.OrderId,
            ProductId = oi.ProductId,
            ProductName = oi.Product?.Name ?? string.Empty,
            Quantity = oi.Quantity,
            UnitPrice = oi.UnitPrice,
            Discount = oi.Discount
        }).ToList() ?? new(),
        OrderNotes = o.OrderNotes?.Select(n => new OrderNoteDto
        {
            Id = n.Id,
            OrderId = n.OrderId,
            NoteText = n.NoteText,
            NoteDate = n.NoteDate,
            Author = n.Author
        }).ToList() ?? new()
    };
}
