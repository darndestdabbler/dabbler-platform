using AcmeCoffeeOnline.Api.Services;
using AcmeCoffeeOnline.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcmeCoffeeOnline.Api.Controllers;

[ApiController]
[Route("api/session")]
[Authorize]
public sealed class SessionController : ControllerBase
{
    private readonly IOrganizationOptionsService _organizationOptionsService;

    public SessionController(IOrganizationOptionsService organizationOptionsService)
    {
        _organizationOptionsService = organizationOptionsService;
    }

    [HttpGet("organization-options")]
    public async Task<ActionResult<IReadOnlyList<OrganizationOptionDto>>> GetOrganizationOptionsAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var options = await _organizationOptionsService.GetOptionsAsync(cancellationToken);
            return Ok(options);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }
}
