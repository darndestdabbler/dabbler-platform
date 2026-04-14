using Microsoft.AspNetCore.Mvc;

namespace AcmeCoffeeOnline.Api.Controllers;

[ApiController]
[Route("api/platform")]
public sealed class PlatformController : ControllerBase
{
    [HttpGet("status")]
    public ActionResult<PlatformStatusResponse> GetStatus()
    {
        return Ok(new PlatformStatusResponse(
            Status: "ready",
            Stage: "session-2-foundation",
            UtcTimestamp: DateTimeOffset.UtcNow));
    }
}

public sealed record PlatformStatusResponse(
    string Status,
    string Stage,
    DateTimeOffset UtcTimestamp);
