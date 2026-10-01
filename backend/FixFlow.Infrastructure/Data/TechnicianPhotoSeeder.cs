using FixFlow.Application.Exceptions;
using FixFlow.Application.Interfaces;
using FixFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FixFlow.Infrastructure.Data;

public sealed class TechnicianPhotoSeeder(FixFlowDbContext db, IFileStorage files, ILogger<TechnicianPhotoSeeder> logger)
{
    public async Task<int> SeedAsync(CancellationToken cancellationToken = default)
    {
        var copied = await CopyExistingFilesAsync(cancellationToken);
        var created = await AssignMissingPortraitsAsync(cancellationToken);
        if (copied + created > 0)
        {
            logger.LogInformation("Saved {Copied} existing technician photos and assigned {Created} new portraits in the database.", copied, created);
        }

        return copied + created;
    }

    private async Task<int> CopyExistingFilesAsync(CancellationToken cancellationToken)
    {
        var pending = await db.TechnicianProfiles
            .Include(x => x.User)
            .Where(x => x.ProfilePhotoStorageKey != null)
            .Where(x => !db.TechnicianProfileImages.Any(image => image.TechnicianId == x.Id))
            .ToListAsync(cancellationToken);
        if (pending.Count == 0)
        {
            return 0;
        }

        var saved = 0;
        foreach (var profile in pending)
        {
            var bytes = await ReadStoredFileAsync(profile.ProfilePhotoStorageKey!, cancellationToken);
            var mime = string.IsNullOrWhiteSpace(profile.ProfilePhotoMimeType) ? "image/jpeg" : profile.ProfilePhotoMimeType;
            if (bytes is null && profile.ProfilePhotoStorageKey!.EndsWith("_portrait.png", StringComparison.OrdinalIgnoreCase))
            {
                bytes = PortraitPng.ForName(profile.User.DisplayName);
                mime = "image/png";
                profile.ProfilePhotoMimeType = mime;
            }

            if (bytes is null || bytes.Length == 0)
            {
                continue;
            }

            db.TechnicianProfileImages.Add(new TechnicianProfileImage
            {
                TechnicianId = profile.Id,
                Content = bytes,
                MimeType = mime
            });
            saved++;
        }

        if (saved > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return saved;
    }

    private async Task<int> AssignMissingPortraitsAsync(CancellationToken cancellationToken)
    {
        var missing = await db.TechnicianProfiles
            .Include(x => x.User)
            .Where(x => x.ProfilePhotoStorageKey == null)
            .Where(x => !db.TechnicianProfileImages.Any(image => image.TechnicianId == x.Id))
            .ToListAsync(cancellationToken);
        if (missing.Count == 0)
        {
            return 0;
        }

        foreach (var profile in missing)
        {
            var png = PortraitPng.ForName(profile.User.DisplayName);
            profile.ProfilePhotoStorageKey = $"{Guid.NewGuid():N}_portrait.png";
            profile.ProfilePhotoMimeType = "image/png";
            profile.UpdatedAt = DateTimeOffset.UtcNow;
            db.TechnicianProfileImages.Add(new TechnicianProfileImage
            {
                TechnicianId = profile.Id,
                Content = png,
                MimeType = "image/png"
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return missing.Count;
    }

    private async Task<byte[]?> ReadStoredFileAsync(string storageKey, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await files.OpenAsync(storageKey, cancellationToken);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            return buffer.Length == 0 ? null : buffer.ToArray();
        }
        catch (NotFoundException)
        {
            return null;
        }
    }
}
