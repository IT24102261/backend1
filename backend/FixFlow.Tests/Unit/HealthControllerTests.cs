using FixFlow.Api.Controllers;
using FixFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Tests.Unit;

public class HealthControllerTests
{
    [Fact]
    public async Task Get_ReturnsStatusWhenDatabaseIsUnreachable()
    {
        var options = new DbContextOptionsBuilder<FixFlowDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=1")
            .Options;
        await using var db = new FixFlowDbContext(options);
        var controller = new HealthController(db);

        var result = await controller.Get(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }
}
