using AcmeCoffeeOnline.Api.Services;
using AcmeCoffeeOnline.Shared.DTOs;
using Dabbler.Security.Authorization;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Mvc;

namespace AcmeCoffeeOnline.Api.Controllers;

[ApiController]
[Route("api/orders/{orderId:int}/notes")]
public class OrderNotesController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IPermissionService _permissionService;
    private readonly ICurrentUserService _currentUser;

    public OrderNotesController(
        IOrderService orderService,
        IPermissionService permissionService,
        ICurrentUserService currentUser)
    {
        _orderService = orderService;
        _permissionService = permissionService;
        _currentUser = currentUser;
    }

    [HttpGet]
    [DabblerEndpointAuthorize("GetOrderNotes")]
    public async Task<ActionResult<List<OrderNoteDto>>> GetByOrderIdAsync(
        int orderId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var notes = await _orderService.GetNotesByOrderIdAsync(orderId, cancellationToken);
        foreach (var note in notes)
            await _permissionService.ApplyFieldPermissionsAsync(note, userId, "OrderNote");
        return Ok(notes);
    }

    [HttpPost]
    [DabblerEndpointAuthorize("CreateOrderNote")]
    public async Task<ActionResult<OrderNoteDto>> CreateAsync(
        int orderId,
        [FromBody] OrderNoteDto dto,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var note = await _orderService.AddNoteAsync(orderId, dto, cancellationToken);
        if (note is null)
            return BadRequest("Order not found or not modifiable.");

        await _permissionService.ApplyFieldPermissionsAsync(note, userId, "OrderNote");
        return Created($"api/orders/{orderId}/notes/{note.Id}", note);
    }

    [HttpPut("{noteId:int}")]
    [DabblerEndpointAuthorize("UpdateOrderNote")]
    public async Task<ActionResult<OrderNoteDto>> UpdateAsync(
        int orderId,
        int noteId,
        [FromBody] OrderNoteDto dto,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var updated = await _orderService.UpdateNoteAsync(orderId, noteId, dto, cancellationToken);
        if (updated is null)
            return NotFound();

        await _permissionService.ApplyFieldPermissionsAsync(updated, userId, "OrderNote");
        return Ok(updated);
    }

    [HttpDelete("{noteId:int}")]
    [DabblerEndpointAuthorize("DeleteOrderNote")]
    public async Task<ActionResult> DeleteAsync(
        int orderId,
        int noteId,
        CancellationToken cancellationToken)
    {
        var success = await _orderService.DeleteNoteAsync(orderId, noteId, cancellationToken);
        if (!success)
            return NotFound();

        return NoContent();
    }

    private bool TryGetUserId(out int userId)
        => int.TryParse(_currentUser.UserId, out userId);
}
