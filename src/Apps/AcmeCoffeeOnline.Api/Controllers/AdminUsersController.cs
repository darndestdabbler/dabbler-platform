using AcmeCoffeeOnline.Api.Services;
using AcmeCoffeeOnline.Shared.DTOs;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcmeCoffeeOnline.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IAdminUserScopeService _adminUserScopeService;
    private readonly IBackofficeUserCreationService _userCreationService;

    public AdminUsersController(
        IAdminUserScopeService adminUserScopeService,
        IBackofficeUserCreationService userCreationService)
    {
        _adminUserScopeService = adminUserScopeService;
        _userCreationService = userCreationService;
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

    [HttpPost]
    public async Task<ActionResult<BackofficeUserCreationResponseDto>> CreateBackofficeUserAsync(
        [FromBody] CreateBackofficeUserDto dto,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.DisplayName)
            || string.IsNullOrWhiteSpace(dto.Email)
            || string.IsNullOrWhiteSpace(dto.InitialRoleName)
            || dto.InitialOrganizationId <= 0)
        {
            return BadRequest(new { error = "DisplayName, Email, InitialRoleName, and InitialOrganizationId are required." });
        }

        // Derive the magic-link base URL from the server's own request context rather than
        // accepting it from the client, to prevent a compromised admin from supplying a
        // phishing URL in the onboarding email.
        var baseVerifyUrl = $"{Request.Scheme}://{Request.Host}/login";

        var request = new CreateBackofficeUserRequest(
            dto.DisplayName,
            dto.Email,
            dto.InitialRoleName,
            dto.InitialOrganizationId);

        try
        {
            var result = await _userCreationService.CreateAsync(
                request, User, baseVerifyUrl, cancellationToken);

            // Sanitize MagicLinkError before returning: full exception messages may
            // contain internal infrastructure details (SMTP host, connection strings).
            string? safeError = result.MagicLinkSent ? null : "Failed to send verification email. An admin can re-send manually.";

            var response = new BackofficeUserCreationResponseDto(
                UserId: result.User.UserId,
                DisplayName: result.User.DisplayName,
                Email: result.User.Email,
                IsActive: result.User.IsActive,
                MagicLinkSent: result.MagicLinkSent,
                MagicLinkError: safeError);

            return CreatedAtAction(
                actionName: nameof(GetScopeAsync),
                routeValues: null,
                value: response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (EmailAlreadyExistsException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
