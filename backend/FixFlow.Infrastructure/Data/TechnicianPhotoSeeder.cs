using FixFlow.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FixFlow.Infrastructure.Data;

public sealed class TechnicianPhotoSeeder(FixFlowDbContext db, IFileStorage files, ILogger<TechnicianPhotoSeeder> logger)
{
    public async Task<int> SeedAsync(CancellationToken cancellationToken = default)
    {
        var missing = await db.TechnicianProfiles
            .Include(x => x.User)
            .Where(x => x.ProfilePhotoStorageKey == null)
            .ToListAsync(cancellationToken);
        if (missing.Count == 0)
        {
            return 0;
        }

        foreach (var profile in missing)
        {
            var png = PortraitPng.ForName(profile.User.DisplayName);
            await using var stream = new MemoryStream(png);
            var key = await files.SaveAsync($"profiles/{profile.Id}", "portrait.png", stream, cancellationToken);
            profile.ProfilePhotoStorageKey = key;
            profile.ProfilePhotoMimeType = "image/png";
            profile.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Assigned profile photos to {Count} technicians without a picture.", missing.Count);
        return missing.Count;
    }
}
