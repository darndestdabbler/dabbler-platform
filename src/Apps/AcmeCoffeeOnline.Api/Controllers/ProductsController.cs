using AcmeCoffeeOnline.Api.Services;
using AcmeCoffeeOnline.Shared.DTOs;
using Dabbler.Security.Authorization;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Mvc;

namespace AcmeCoffeeOnline.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IPermissionService _permissionService;
    private readonly ICurrentUserService _currentUser;

    public ProductsController(
        IProductService productService,
        IPermissionService permissionService,
        ICurrentUserService currentUser)
    {
        _productService = productService;
        _permissionService = permissionService;
        _currentUser = currentUser;
    }

    [HttpGet]
    [DabblerEndpointAuthorize("GetProducts")]
    public async Task<ActionResult<PagedResponse<ProductDto>>> GetAllAsync(
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await _productService.GetAllAsync(request, cancellationToken);
        foreach (var item in result.Items)
            await _permissionService.ApplyFieldPermissionsAsync(item, userId, "Product");
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [DabblerEndpointAuthorize("GetProductById")]
    public async Task<ActionResult<ProductDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var product = await _productService.GetByIdAsync(id, cancellationToken);
        if (product is null)
            return NotFound();

        await _permissionService.ApplyFieldPermissionsAsync(product, userId, "Product");
        return Ok(product);
    }

    [HttpPost]
    [DabblerEndpointAuthorize("CreateProduct")]
    public async Task<ActionResult<ProductDto>> CreateAsync(
        [FromBody] ProductDto dto,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var created = await _productService.CreateAsync(dto, cancellationToken);
        await _permissionService.ApplyFieldPermissionsAsync(created, userId, "Product");
        return CreatedAtAction(nameof(GetByIdAsync), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [DabblerEndpointAuthorize("UpdateProduct")]
    public async Task<ActionResult<ProductDto>> UpdateAsync(
        int id,
        [FromBody] ProductDto dto,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var updated = await _productService.UpdateAsync(id, dto, cancellationToken);
        if (updated is null)
            return NotFound();

        await _permissionService.ApplyFieldPermissionsAsync(updated, userId, "Product");
        return Ok(updated);
    }

    [HttpPatch("{id:int}/availability")]
    [DabblerEndpointAuthorize("ToggleProductAvailability")]
    public async Task<ActionResult> ToggleAvailabilityAsync(int id, CancellationToken cancellationToken)
    {
        var success = await _productService.ToggleAvailabilityAsync(id, cancellationToken);
        if (!success)
            return NotFound();

        return NoContent();
    }

    private bool TryGetUserId(out int userId)
        => int.TryParse(_currentUser.UserId, out userId);
}
