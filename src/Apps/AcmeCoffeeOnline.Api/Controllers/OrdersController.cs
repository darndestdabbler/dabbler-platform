using AcmeCoffeeOnline.Api.Services;
using AcmeCoffeeOnline.Shared.DTOs;
using Dabbler.Security.Authorization;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Mvc;

namespace AcmeCoffeeOnline.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IPermissionService _permissionService;
    private readonly ICurrentUserService _currentUser;

    public OrdersController(
        IOrderService orderService,
        IPermissionService permissionService,
        ICurrentUserService currentUser)
    {
        _orderService = orderService;
        _permissionService = permissionService;
        _currentUser = currentUser;
    }

    [HttpGet]
    [DabblerEndpointAuthorize("GetOrders")]
    public async Task<ActionResult<PagedResponse<OrderDto>>> GetAllAsync(
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await _orderService.GetAllAsync(request, cancellationToken);
        foreach (var item in result.Items)
            await _permissionService.ApplyFieldPermissionsAsync(item, userId, "Order");
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [DabblerEndpointAuthorize("GetOrderById")]
    public async Task<ActionResult<OrderDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var order = await _orderService.GetByIdAsync(id, cancellationToken);
        if (order is null)
            return NotFound();

        await _permissionService.ApplyFieldPermissionsAsync(order, userId, "Order");
        return Ok(order);
    }

    [HttpGet("by-customer/{customerId:int}")]
    [DabblerEndpointAuthorize("GetOrdersByCustomer")]
    public async Task<ActionResult<PagedResponse<OrderDto>>> GetByCustomerIdAsync(
        int customerId,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await _orderService.GetByCustomerIdAsync(customerId, request, cancellationToken);
        foreach (var item in result.Items)
            await _permissionService.ApplyFieldPermissionsAsync(item, userId, "Order");
        return Ok(result);
    }

    [HttpPost]
    [DabblerEndpointAuthorize("CreateOrder")]
    public async Task<ActionResult<OrderDto>> CreateAsync(
        [FromBody] OrderDto dto,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var created = await _orderService.CreateAsync(dto, cancellationToken);
        await _permissionService.ApplyFieldPermissionsAsync(created, userId, "Order");
        return CreatedAtAction(nameof(GetByIdAsync), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [DabblerEndpointAuthorize("UpdateOrder")]
    public async Task<ActionResult<OrderDto>> UpdateAsync(
        int id,
        [FromBody] OrderDto dto,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var updated = await _orderService.UpdateAsync(id, dto, cancellationToken);
        if (updated is null)
            return NotFound();

        await _permissionService.ApplyFieldPermissionsAsync(updated, userId, "Order");
        return Ok(updated);
    }

    [HttpPatch("{id:int}/status")]
    [DabblerEndpointAuthorize("UpdateOrderStatus")]
    public async Task<ActionResult> UpdateStatusAsync(
        int id,
        [FromBody] OrderStatusUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.UpdateStatusAsync(id, request.NewStatus, cancellationToken);

        if (!result.Succeeded)
        {
            if (!result.OrderFound)
                return NotFound();

            return BadRequest(result.ErrorMessage);
        }

        return NoContent();
    }

    [HttpPost("{orderId:int}/items")]
    [DabblerEndpointAuthorize("AddOrderItem")]
    public async Task<ActionResult<OrderItemDto>> AddItemAsync(
        int orderId,
        [FromBody] OrderItemDto dto,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var item = await _orderService.AddItemAsync(orderId, dto, cancellationToken);
        if (item is null)
            return BadRequest("Order not found, not modifiable, or product not found.");

        await _permissionService.ApplyFieldPermissionsAsync(item, userId, "OrderItem");
        return Created($"api/orders/{orderId}/items/{item.Id}", item);
    }

    [HttpDelete("{orderId:int}/items/{itemId:int}")]
    [DabblerEndpointAuthorize("RemoveOrderItem")]
    public async Task<ActionResult> RemoveItemAsync(
        int orderId,
        int itemId,
        CancellationToken cancellationToken)
    {
        var success = await _orderService.RemoveItemAsync(orderId, itemId, cancellationToken);
        if (!success)
            return NotFound();

        return NoContent();
    }

    private bool TryGetUserId(out int userId)
        => int.TryParse(_currentUser.UserId, out userId);
}
