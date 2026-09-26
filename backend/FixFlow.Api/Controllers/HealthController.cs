using FixFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Controllers;

[ApiController]
public class HealthController(FixFlowDbContext db) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("/health")]
    [HttpGet("/api/health")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var database = false;
        string? databaseError = null;
        try
        {
            database = await db.Database.CanConnectAsync(cancellationToken);
            if (database)
            {
                await db.ServiceCategories.Select(x => x.Id).Take(1).ToListAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            database = false;
            databaseError = ex.InnerException?.Message ?? ex.Message;
        }

        return Ok(new
        {
            status = database ? "healthy" : "degraded",
            service = "FixFlow.Api",
            database,
            databaseError
        });
    }
}
