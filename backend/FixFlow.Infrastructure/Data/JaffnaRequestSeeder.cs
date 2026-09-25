using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FixFlow.Infrastructure.Data;

public sealed class JaffnaRequestSeeder(FixFlowDbContext db, ILogger<JaffnaRequestSeeder> logger)
{
    private const string DemoPrefix = "[Demo] Jaffna ";

    public async Task<int> SeedAsync(CancellationToken cancellationToken = default)
    {
        var demos = await db.ServiceRequests
            .Where(x => x.Description.StartsWith(DemoPrefix))
            .ToListAsync(cancellationToken);
        if (demos.Count == 0)
        {
            logger.LogInformation("No demo marketplace requests to retire.");
            return 0;
        }

        var ids = demos.Select(x => x.Id).ToList();
        foreach (var request in demos.Where(x => x.Status != ServiceRequestStatus.Booked && x.Status != ServiceRequestStatus.Completed))
        {
            request.Status = ServiceRequestStatus.Cancelled;
        }

        var invites = await db.RequestInvitations
            .Where(x => ids.Contains(x.RequestId) && x.Status == InvitationStatus.Sent)
            .ToListAsync(cancellationToken);
        foreach (var invitation in invites)
        {
            invitation.Status = InvitationStatus.Declined;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Retired {Requests} demo requests and declined {Invites} leftover invitations so live customer jobs own matching.",
            demos.Count,
            invites.Count);
        return invites.Count;
    }
}
