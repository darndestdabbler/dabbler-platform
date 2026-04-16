using AcmeCoffeeOnline.Api.Services;
using AcmeCoffeeOnline.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcmeCoffeeOnline.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IAdminUserScopeService _adminUserScopeService;

    public AdminUsersController(IAdminUserScopeService adminUserScopeService)
    {
        _adminUserScopeService = adminUserScopeService;
    }

    [HttpGet("scope")]
    public async Task<ActionResult<AdminUserScopeDto>> GetScopeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var scope = await _adminUserScopeService.GetScopeAsync(cancellationToken);
            return Ok(scope);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("memberships/{organizationMembershipId:int}/grant-user-admin")]
    public async Task<ActionResult<AdminUserScopeDto>> GrantScopedUserAdminAsync(
        int organizationMembershipId,
        CancellationToken cancellationToken)
    {
        try
        {
            var scope = await _adminUserScopeService.GrantScopedUserAdminAsync(organizationMembershipId, cancellationToken);
            return Ok(scope);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("memberships/{organizationMembershipId:int}/grant-user-admin")]
    public async Task<ActionResult<AdminUserScopeDto>> RevokeScopedUserAdminAsync(
        int organizationMembershipId,
        CancellationToken cancellationToken)
    {
        try
        {
            var scope = await _adminUserScopeService.RevokeScopedUserAdminAsync(organizationMembershipId, cancellationToken);
            return Ok(scope);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
