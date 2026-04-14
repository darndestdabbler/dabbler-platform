using AcmeCoffeeOnline.Api.Services;
using AcmeCoffeeOnline.Shared.DTOs;
using Dabbler.Security.Authorization;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Mvc;

namespace AcmeCoffeeOnline.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly IPermissionService _permissionService;
    private readonly ICurrentUserService _currentUser;

    public CustomersController(
        ICustomerService customerService,
        IPermissionService permissionService,
        ICurrentUserService currentUser)
    {
        _customerService = customerService;
        _permissionService = permissionService;
        _currentUser = currentUser;
    }

    [HttpGet]
    [DabblerEndpointAuthorize("GetCustomers")]
    public async Task<ActionResult<PagedResponse<CustomerDto>>> GetAllAsync(
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await _customerService.GetAllAsync(request, cancellationToken);
        foreach (var item in result.Items)
            await _permissionService.ApplyFieldPermissionsAsync(item, userId, "Customer");
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [DabblerEndpointAuthorize("GetCustomerById")]
    public async Task<ActionResult<CustomerDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var customer = await _customerService.GetByIdAsync(id, cancellationToken);
        if (customer is null)
            return NotFound();

        await _permissionService.ApplyFieldPermissionsAsync(customer, userId, "Customer");
        return Ok(customer);
    }

    [HttpPost]
    [DabblerEndpointAuthorize("CreateCustomer")]
    public async Task<ActionResult<CustomerDto>> CreateAsync(
        [FromBody] CustomerDto dto,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var created = await _customerService.CreateAsync(dto, cancellationToken);
        await _permissionService.ApplyFieldPermissionsAsync(created, userId, "Customer");
        return CreatedAtAction(nameof(GetByIdAsync), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [DabblerEndpointAuthorize("UpdateCustomer")]
    public async Task<ActionResult<CustomerDto>> UpdateAsync(
        int id,
        [FromBody] CustomerDto dto,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var updated = await _customerService.UpdateAsync(id, dto, cancellationToken);
        if (updated is null)
            return NotFound();

        await _permissionService.ApplyFieldPermissionsAsync(updated, userId, "Customer");
        return Ok(updated);
    }

    [HttpGet("search")]
    [DabblerEndpointAuthorize("SearchCustomers")]
    public async Task<ActionResult<PagedResponse<CustomerDto>>> SearchAsync(
        [FromQuery] string searchTerm,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return BadRequest("Search term is required.");

        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await _customerService.SearchAsync(searchTerm, request, cancellationToken);
        foreach (var item in result.Items)
            await _permissionService.ApplyFieldPermissionsAsync(item, userId, "Customer");
        return Ok(result);
    }

    private bool TryGetUserId(out int userId)
        => int.TryParse(_currentUser.UserId, out userId);
}
