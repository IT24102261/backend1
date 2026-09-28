using FixFlow.Application.Common;
using FixFlow.Application.DTOs.Technicians;
using FixFlow.Application.Exceptions;
using FixFlow.Application.Interfaces;
using FixFlow.Application.Mapping;
using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Application.Services;

public class TechnicianService(
    IRepository<TechnicianProfile> profiles,
    IRepository<TechnicianCategoryApplication> applications,
    IRepository<TechnicianDocument> documents,
    IRepository<ServiceCategory> categories,
    IRepository<Booking> bookings,
    IRepository<AuditLog> audits,
    IFileStorage files,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : ITechnicianService
{
    public async Task<TechnicianProfileDto> CreateProfileAsync(TechnicianProfileRequest request, CancellationToken cancellationToken = default)
    {
        EnsureTechnician();
        if (await profiles.Query().AnyAsync(x => x.UserId == currentUser.UserId, cancellationToken))
        {
            throw new ConflictException("Technician profile already exists.");
        }

        var profile = Apply(new TechnicianProfile { UserId = currentUser.UserId }, request);
        await profiles.AddAsync(profile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await Map(profile, cancellationToken);
    }

    public async Task<TechnicianProfileDto> GetProfileAsync(CancellationToken cancellationToken = default) =>
        await Map(await RequireProfile(cancellationToken), cancellationToken);

    public async Task<TechnicianProfileDto> UpdateProfileAsync(TechnicianProfileRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await RequireProfile(cancellationToken);
        Apply(profile, request);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await Map(profile, cancellationToken);
    }

    public async Task<TechnicianApplicationDto> ApplyAsync(TechnicianApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await RequireProfile(cancellationToken);
        _ = await categories.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Category not found.");

        var existing = await applications.Query()
            .FirstOrDefaultAsync(x => x.TechnicianId == profile.Id && x.CategoryId == request.CategoryId, cancellationToken);
        if (existing is not null)
        {
            if (existing.Status is ApplicationStatus.Approved)
            {
                throw new ConflictException("This category is already approved.");
            }

            existing.Status = ApplicationStatus.Submitted;
            existing.SubmittedAt = DateTimeOffset.UtcNow;
            existing.DecisionNotes = null;
            existing.DecidedAt = null;
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return MapSync(existing);
        }

        var application = new TechnicianCategoryApplication
        {
            TechnicianId = profile.Id,
            CategoryId = request.CategoryId,
            Status = ApplicationStatus.Submitted
        };
        await applications.AddAsync(application, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapSync(application);
    }

    public async Task<IReadOnlyList<TechnicianApplicationDto>> ListMineAsync(CancellationToken cancellationToken = default)
    {
        var profile = await RequireProfile(cancellationToken);
        var items = await applications.Query()
            .Include(x => x.Category)
            .Include(x => x.Documents)
            .Include(x => x.Technician).ThenInclude(x => x.User)
            .Where(x => x.TechnicianId == profile.Id)
            .OrderByDescending(x => x.SubmittedAt)
            .ToListAsync(cancellationToken);
        return items.Select(MapSync).ToList();
    }

    public async Task<TechnicianApplicationDto> GetMineAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var profile = await RequireProfile(cancellationToken);
        var application = await LoadApplication(id, cancellationToken);
        if (!currentUser.IsAdmin && application.TechnicianId != profile.Id)
        {
            throw new ForbiddenException();
        }

        return MapSync(application);
    }

    public async Task<DocumentDto> AddDocumentAsync(Guid applicationId, string evidenceType, string fileName, string contentType, Stream content, CancellationToken cancellationToken = default)
    {
        var profile = await RequireProfile(cancellationToken);
        var application = await LoadApplication(applicationId, cancellationToken);
        if (application.TechnicianId != profile.Id)
        {
            throw new ForbiddenException();
        }

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        UploadRules.EnsureDocument(contentType, buffer.Length);
        var sha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(buffer.ToArray()));
        buffer.Position = 0;
        var key = await files.SaveAsync($"applications/{applicationId}", fileName, buffer, cancellationToken);
        var document = new TechnicianDocument
        {
            ApplicationId = applicationId,
            EvidenceType = EnumMap.Parse<EvidenceType>(evidenceType),
            StorageKey = key,
            MimeType = contentType,
            Sha256 = sha256
        };
        await documents.AddAsync(document, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDocument(document);
    }

    public async Task<TechnicianPhotoFile> GetDocumentFileAsync(Guid applicationId, Guid documentId, CancellationToken cancellationToken = default)
    {
        var application = await applications.Query()
            .Include(x => x.Documents)
            .Include(x => x.Technician)
            .FirstOrDefaultAsync(x => x.Id == applicationId, cancellationToken)
            ?? throw new NotFoundException("Application not found.");

        if (!currentUser.IsAdmin)
        {
            var profile = await RequireProfile(cancellationToken);
            if (application.TechnicianId != profile.Id)
            {
                throw new ForbiddenException();
            }
        }

        var document = application.Documents.FirstOrDefault(x => x.Id == documentId)
            ?? throw new NotFoundException("Document not found.");
        await using var source = await files.OpenAsync(document.StorageKey, cancellationToken);
        var copy = new MemoryStream();
        await source.CopyToAsync(copy, cancellationToken);
        copy.Position = 0;
        var contentType = string.IsNullOrWhiteSpace(document.MimeType) ? "application/octet-stream" : document.MimeType;
        return new TechnicianPhotoFile(copy, contentType);
    }

    public async Task<PagedResult<TechnicianApplicationDto>> AdminListAsync(PagedQuery query, CancellationToken cancellationToken = default)
    {
        var source = applications.Query()
            .Include(x => x.Category)
            .Include(x => x.Documents)
            .Include(x => x.Technician).ThenInclude(x => x.User)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = EnumMap.Parse<ApplicationStatus>(query.Status);
            source = source.Where(x => x.Status == status);
        }

        var total = await source.CountAsync(cancellationToken);
        var items = await source.OrderByDescending(x => x.SubmittedAt).Skip(query.Skip).Take(query.Take).ToListAsync(cancellationToken);
        return new PagedResult<TechnicianApplicationDto>
        {
            Items = items.Select(MapSync).ToList(),
            Page = query.Page,
            PageSize = query.Take,
            TotalCount = total
        };
    }

    public async Task<TechnicianApplicationDto> AdminGetAsync(Guid id, CancellationToken cancellationToken = default) =>
        MapSync(await LoadApplication(id, cancellationToken));

    public Task<TechnicianApplicationDto> ApproveAsync(Guid id, ApplicationDecisionRequest request, CancellationToken cancellationToken = default) =>
        Decide(id, ApplicationStatus.Approved, request.Notes, cancellationToken);

    public Task<TechnicianApplicationDto> RejectAsync(Guid id, ApplicationDecisionRequest request, CancellationToken cancellationToken = default) =>
        Decide(id, ApplicationStatus.Rejected, request.Notes, cancellationToken);

    public Task<TechnicianApplicationDto> RequestInfoAsync(Guid id, ApplicationDecisionRequest request, CancellationToken cancellationToken = default) =>
        Decide(id, ApplicationStatus.MoreInformationRequired, request.Notes, cancellationToken);

    public Task<TechnicianApplicationDto> RequestReverificationAsync(Guid id, ApplicationDecisionRequest request, CancellationToken cancellationToken = default) =>
        Decide(id, ApplicationStatus.ReverificationRequired, request.Notes, cancellationToken);

    public Task<TechnicianApplicationDto> SuspendApplicationAsync(Guid id, ApplicationDecisionRequest request, CancellationToken cancellationToken = default) =>
        Decide(id, ApplicationStatus.Suspended, request.Notes, cancellationToken);

    public async Task<PublicTechnicianDto> GetPublicAsync(Guid technicianId, CancellationToken cancellationToken = default)
    {
        var profile = await profiles.Query().Include(x => x.User).FirstOrDefaultAsync(x => x.Id == technicianId, cancellationToken)
            ?? throw new NotFoundException("Technician not found.");
        var mapped = await Map(profile, cancellationToken);
        return new PublicTechnicianDto
        {
            Id = mapped.Id,
            DisplayName = mapped.DisplayName,
            ApprovedCategories = mapped.ApprovedCategories,
            CategoryVerified = mapped.ApprovedCategories.Count > 0,
            AverageRating = mapped.AverageRating,
            ReviewCount = mapped.ReviewCount,
            CompletedJobs = mapped.CompletedJobs,
            ServiceSummary = string.IsNullOrWhiteSpace(mapped.ExperienceSummary) ? mapped.Bio : mapped.ExperienceSummary,
            ProfilePhotoUrl = mapped.ProfilePhotoUrl
        };
    }

    public async Task<TechnicianPhotoFile?> GetPhotoAsync(Guid technicianId, CancellationToken cancellationToken = default)
    {
        var profile = await profiles.GetByIdAsync(technicianId, cancellationToken)
            ?? throw new NotFoundException("Technician not found.");
        if (string.IsNullOrWhiteSpace(profile.ProfilePhotoStorageKey))
        {
            return null;
        }

        var stream = await files.OpenAsync(profile.ProfilePhotoStorageKey, cancellationToken);
        return new TechnicianPhotoFile(stream, string.IsNullOrWhiteSpace(profile.ProfilePhotoMimeType) ? "image/jpeg" : profile.ProfilePhotoMimeType);
    }

    public async Task<TechnicianProfileDto> SetProfilePhotoAsync(Guid technicianId, string fileName, string contentType, Stream content, CancellationToken cancellationToken = default)
    {
        var profile = await profiles.Query().Include(x => x.User).FirstOrDefaultAsync(x => x.Id == technicianId, cancellationToken)
            ?? throw new NotFoundException("Technician not found.");

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        UploadRules.EnsureImage(contentType, buffer.Length);
        buffer.Position = 0;
        var key = await files.SaveAsync($"profiles/{profile.Id}", fileName, buffer, cancellationToken);
        profile.ProfilePhotoStorageKey = key;
        profile.ProfilePhotoMimeType = contentType;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await audits.AddAsync(new AuditLog
        {
            ActorId = currentUser.UserId,
            Action = "TECHNICIAN_PHOTO_SET",
            Entity = "TechnicianProfile",
            EntityId = profile.Id,
            Outcome = AuditOutcome.Success
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await Map(profile, cancellationToken);
    }

    public Task<TechnicianProfileDto> SuspendTechnicianAsync(Guid technicianId, ApplicationDecisionRequest request, CancellationToken cancellationToken = default) =>
        SetSuspended(technicianId, true, request.Notes, cancellationToken);

    public Task<TechnicianProfileDto> ReactivateTechnicianAsync(Guid technicianId, ApplicationDecisionRequest request, CancellationToken cancellationToken = default) =>
        SetSuspended(technicianId, false, request.Notes, cancellationToken);

    private async Task<TechnicianProfileDto> SetSuspended(Guid technicianId, bool suspended, string? notes, CancellationToken cancellationToken)
    {
        var profile = await profiles.Query().Include(x => x.User).FirstOrDefaultAsync(x => x.Id == technicianId, cancellationToken)
            ?? throw new NotFoundException("Technician not found.");
        profile.IsSuspended = suspended;
        await audits.AddAsync(new AuditLog
        {
            ActorId = currentUser.UserId,
            Action = suspended ? "TECHNICIAN_SUSPENDED" : "TECHNICIAN_REACTIVATED",
            Entity = "TechnicianProfile",
            EntityId = profile.Id,
            Outcome = AuditOutcome.Success,
            MetadataJson = System.Text.Json.JsonSerializer.Serialize(new { notes })
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await Map(profile, cancellationToken);
    }

    private async Task<TechnicianApplicationDto> Decide(Guid id, ApplicationStatus status, string? notes, CancellationToken cancellationToken)
    {
        var application = await LoadApplication(id, cancellationToken);
        application.Status = status;
        application.DecidedAt = DateTimeOffset.UtcNow;
        application.DecidedBy = currentUser.UserId;
        application.DecisionNotes = notes;
        await audits.AddAsync(new AuditLog
        {
            ActorId = currentUser.UserId,
            Action = $"APPLICATION_{EnumMap.ToApi(status)}",
            Entity = "TechnicianApplication",
            EntityId = application.Id,
            Outcome = AuditOutcome.Success,
            MetadataJson = System.Text.Json.JsonSerializer.Serialize(new { notes })
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapSync(application);
    }

    private async Task<TechnicianProfile> RequireProfile(CancellationToken cancellationToken) =>
        await profiles.Query().Include(x => x.User).FirstOrDefaultAsync(x => x.UserId == currentUser.UserId, cancellationToken)
        ?? throw new NotFoundException("Technician profile not found. Create a profile first.");

    private async Task<TechnicianCategoryApplication> LoadApplication(Guid id, CancellationToken cancellationToken) =>
        await applications.Query()
            .Include(x => x.Category)
            .Include(x => x.Documents)
            .Include(x => x.Technician).ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Application not found.");

    private void EnsureTechnician()
    {
        if (!currentUser.IsTechnician && !currentUser.IsAdmin)
        {
            throw new ForbiddenException("Only technicians can manage this profile.");
        }
    }

    private static TechnicianProfile Apply(TechnicianProfile profile, TechnicianProfileRequest request)
    {
        profile.Bio = request.Bio;
        profile.ServiceArea = request.ServiceArea;
        profile.LatitudeApprox = request.LatitudeApprox;
        profile.LongitudeApprox = request.LongitudeApprox;
        profile.ExperienceSummary = request.ExperienceSummary;
        return profile;
    }

    private async Task<TechnicianProfileDto> Map(TechnicianProfile profile, CancellationToken cancellationToken)
    {
        var approved = await applications.Query()
            .Include(x => x.Category)
            .Where(x => x.TechnicianId == profile.Id && x.Status == ApplicationStatus.Approved)
            .Select(x => x.Category.Name)
            .ToListAsync(cancellationToken);

        var completedJobs = await bookings.Query().CountAsync(
            x => x.TechnicianId == profile.Id && (x.Status == BookingStatus.CustomerConfirmed || x.Status == BookingStatus.Closed),
            cancellationToken);

        return new TechnicianProfileDto
        {
            Id = profile.Id,
            UserId = profile.UserId,
            DisplayName = profile.User?.DisplayName ?? currentUser.Email,
            Bio = profile.Bio,
            ServiceArea = profile.ServiceArea,
            LatitudeApprox = profile.LatitudeApprox,
            LongitudeApprox = profile.LongitudeApprox,
            ExperienceSummary = profile.ExperienceSummary,
            IsSuspended = profile.IsSuspended,
            AverageRating = profile.AverageRating,
            ReviewCount = profile.ReviewCount,
            ApprovedCategories = approved,
            CompletedJobs = completedJobs,
            ProfilePhotoUrl = TechnicianPhotoUrl.For(profile.Id, profile.ProfilePhotoStorageKey)
        };
    }

    private static TechnicianApplicationDto MapSync(TechnicianCategoryApplication application) => new()
    {
        Id = application.Id,
        TechnicianId = application.TechnicianId,
        CategoryId = application.CategoryId,
        CategoryName = application.Category?.Name,
        TechnicianDisplayName = application.Technician?.User?.DisplayName,
        TechnicianEmail = application.Technician?.User?.Email,
        ProfilePhotoUrl = TechnicianPhotoUrl.For(application.TechnicianId, application.Technician?.ProfilePhotoStorageKey),
        Status = EnumMap.ToApi(application.Status),
        SubmittedAt = application.SubmittedAt,
        DecidedAt = application.DecidedAt,
        DecisionNotes = application.DecisionNotes,
        Version = application.Version,
        EvidenceCount = application.Documents.Count,
        Documents = application.Documents
            .OrderBy(x => x.UploadedAt)
            .Select(MapDocument)
            .ToList()
    };

    private static DocumentDto MapDocument(TechnicianDocument document) => new()
    {
        Id = document.Id,
        EvidenceType = EnumMap.ToApi(document.EvidenceType),
        StorageKey = document.StorageKey,
        MimeType = document.MimeType,
        ReviewStatus = EnumMap.ToApi(document.ReviewStatus),
        UploadedAt = document.UploadedAt,
        Url = TechnicianDocumentUrl.For(document.ApplicationId, document.Id),
        Issuer = document.Issuer
    };
}
