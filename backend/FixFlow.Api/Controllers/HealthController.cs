using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.Api.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("/health")]
    [HttpGet("/api/health")]
    public IActionResult Get() => Ok(new { status = "healthy", service = "FixFlow.Api" });
}
