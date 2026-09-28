using FixFlow.Application.Common;
using FixFlow.Application.DTOs.Requests;
using FixFlow.Application.Exceptions;
using FixFlow.Application.Interfaces;
using FixFlow.Application.Mapping;
using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;
using FixFlow.Domain.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FixFlow.Application.Services;

public class RequestService(
    IRepository<ServiceRequest> requests,
    IRepository<RequestMedia> media,
    IRepository<RequestClarification> clarifications,
    IRepository<RequestStatusHistory> history,
    IRepository<TechnicianProfile> technicians,
    IRepository<RequestInvitation> invitations,
    IRepository<Quotation> quotations,
    IAgentOrchestrator orchestrator,
    IMapService maps,
    IFileStorage files,
    INotificationService notifications,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<RequestService> logger) : IRequestService
{
    public async Task<RequestDto> CreateAsync(RequestWriteRequest request, CancellationToken cancellationToken = default)
    {
        EnsureCustomer();
        var latitude = request.Latitude;
        var longitude = request.Longitude;
        if ((latitude is null || longitude is null) && !string.IsNullOrWhiteSpace(request.Address))
        {
            var geocoded = await maps.GeocodeAddressAsync(request.Address, cancellationToken);
            if (geocoded.Found)
            {
                latitude ??= geocoded.Latitude;
                longitude ??= geocoded.Longitude;
            }
            else
            {
                logger.LogInformation("Geocode skipped for request create: {Code}. Manual service area will be used.", geocoded.ErrorCode);
            }
        }

        var entity = new ServiceRequest
        {
            CustomerId = currentUser.UserId,
            CategoryId = request.CategoryId,
            Description = request.Description.Trim(),
            PreferredStart = request.PreferredStart?.ToUniversalTime(),
            PreferredEnd = request.PreferredEnd?.ToUniversalTime(),
            BudgetAmount = request.BudgetAmount,
            ServiceArea = request.ServiceArea,
            AddressEncrypted = request.Address,
            Latitude = latitude,
            Longitude = longitude,
            Status = ServiceRequestStatus.Draft
        };
        await requests.AddAsync(entity, cancellationToken);
        await AddHistory(entity, ServiceRequestStatus.Draft, ServiceRequestStatus.Draft, "Created", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity, includeAddress: true);
    }

    public async Task<PagedResult<RequestDto>> ListAsync(PagedQuery query, CancellationToken cancellationToken = default)
    {
        var source = requests.Query().Include(x => x.Category).AsQueryable();
        if (currentUser.IsCustomer)
        {
            source = source.Where(x => x.CustomerId == currentUser.UserId);
        }
        else if (currentUser.IsTechnician)
        {
            var profile = await RequireTechnician(cancellationToken);
            source = source.Where(x => x.Invitations.Any(i => i.TechnicianId == profile.Id));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = EnumMap.Parse<ServiceRequestStatus>(query.Status);
            source = source.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            source = source.Where(x => x.Description.Contains(query.Search) || (x.ServiceArea != null && x.ServiceArea.Contains(query.Search)));
        }

        source = query.Descending
            ? source.OrderByDescending(x => x.CreatedAt)
            : source.OrderBy(x => x.CreatedAt);

        var total = await source.CountAsync(cancellationToken);
        var items = await source.Skip(query.Skip).Take(query.Take).ToListAsync(cancellationToken);
        return new PagedResult<RequestDto>
        {
            Items = items.Select(x => Map(x, includeAddress: CanSeeAddress(x))).ToList(),
            Page = query.Page,
            PageSize = query.Take,
            TotalCount = total
        };
    }

    public async Task<RequestDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var request = await Load(id, cancellationToken);
        EnsureCanView(request);
        return Map(request, includeAddress: CanSeeAddress(request));
    }

    public async Task<RequestDto> UpdateAsync(Guid id, RequestWriteRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await Load(id, cancellationToken);
        EnsureOwner(entity);
        if (!RequestStateMachine.IsEditable(entity.Status))
        {
            throw new ConflictException("Only draft or clarification-required requests can be updated.");
        }

        entity.CategoryId = request.CategoryId;
        entity.Description = request.Description.Trim();
        entity.PreferredStart = request.PreferredStart?.ToUniversalTime();
        entity.PreferredEnd = request.PreferredEnd?.ToUniversalTime();
        entity.BudgetAmount = request.BudgetAmount;
        entity.ServiceArea = request.ServiceArea;
        entity.AddressEncrypted = request.Address;
        entity.Latitude = request.Latitude;
        entity.Longitude = request.Longitude;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity, includeAddress: true);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await Load(id, cancellationToken);
        EnsureOwner(entity);
        if (entity.Status != ServiceRequestStatus.Draft)
        {
            throw new ConflictException("Only draft requests can be deleted.");
        }

        requests.Remove(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<RequestDto> SubmitAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await Load(id, cancellationToken);
        EnsureOwner(entity);

        if (entity.Status is ServiceRequestStatus.Booked or ServiceRequestStatus.Completed or ServiceRequestStatus.Cancelled)
        {
            return Map(entity, includeAddress: true);
        }

        if (string.IsNullOrWhiteSpace(entity.ServiceArea))
        {
            throw new ValidationFailedException("Set a service area before submitting so matching can invite technicians.");
        }

        if (entity.Status == ServiceRequestStatus.Draft)
        {
            await ChangeStatusAsync(entity, ServiceRequestStatus.Submitted, "Submitted by customer", cancellationToken);
        }

        if (entity.Status == ServiceRequestStatus.Submitted)
        {
            await ChangeStatusAsync(entity, ServiceRequestStatus.Analyzing, "Intake analysis started", cancellationToken);
        }

        try
        {
            await orchestrator.StartRequestWorkflowAsync(entity.Id, cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.LogWarning(exception, "Workflow persist collided for request {RequestId}; returning current request state.", entity.Id);
        }

        entity = await Load(entity.Id, cancellationToken);
        logger.LogInformation("Request {RequestId} submitted by {UserId}", entity.Id, currentUser.UserId);
        return Map(entity, includeAddress: true);
    }

    public async Task<RequestDto> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await Load(id, cancellationToken);
        EnsureOwner(entity);
        if (entity.Status == ServiceRequestStatus.Cancelled)
        {
            return Map(entity, includeAddress: true);
        }

        if (!RequestStateMachine.CanCustomerCancel(entity.Status))
        {
            throw new ConflictException("This request cannot be cancelled after it is booked.");
        }

        var service = entity.Category?.Name;
        var openInvitations = await invitations.Query()
            .Include(x => x.Technician)
            .Where(x => x.RequestId == id)
            .ToListAsync(cancellationToken);
        var openQuotes = await quotations.Query()
            .Include(x => x.Technician)
            .Where(x => x.RequestId == id)
            .ToListAsync(cancellationToken);

        foreach (var invitation in openInvitations)
        {
            if (invitation.Status is InvitationStatus.Sent or InvitationStatus.Accepted)
            {
                invitation.Status = InvitationStatus.Expired;
            }
        }

        foreach (var quote in openQuotes)
        {
            if (quote.Status is QuotationStatus.Draft or QuotationStatus.Sent or QuotationStatus.Accepted)
            {
                quote.Status = QuotationStatus.Expired;
            }
        }

        await ChangeStatusAsync(entity, ServiceRequestStatus.Cancelled, "Cancelled by the customer", cancellationToken);

        var recipients = openInvitations.Select(x => x.Technician.UserId)
            .Concat(openQuotes.Select(x => x.Technician.UserId))
            .Distinct()
            .ToList();
        var message = service is null
            ? "This request was cancelled by the customer. Invitations and quotations for this job are no longer open."
            : $"This {service} request was cancelled by the customer. Invitations and quotations for this job are no longer open.";
        foreach (var userId in recipients)
        {
            await notifications.NotifyAsync(userId, "Request cancelled by the customer", message, cancellationToken);
        }

        logger.LogInformation("Request {RequestId} cancelled by {UserId}", entity.Id, currentUser.UserId);
        return Map(entity, includeAddress: true);
    }

    public async Task<MediaDto> AddMediaAsync(Guid id, string fileName, string contentType, Stream content, CancellationToken cancellationToken = default)
    {
        var entity = await Load(id, cancellationToken);
        EnsureOwner(entity);
        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        UploadRules.EnsureImage(contentType, buffer.Length);
        buffer.Position = 0;
        var key = await files.SaveAsync($"requests/{id}", fileName, buffer, cancellationToken);
        var item = new RequestMedia
        {
            RequestId = id,
            StorageKey = key,
            MimeType = contentType
        };
        await media.AddAsync(item, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new MediaDto { Id = item.Id, StorageKey = item.StorageKey, MimeType = item.MimeType, UploadedAt = item.UploadedAt };
    }

    public async Task AddClarificationAsync(Guid id, ClarificationRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await Load(id, cancellationToken);
        EnsureCanView(entity);
        await clarifications.AddAsync(new RequestClarification
        {
            RequestId = id,
            AuthorId = currentUser.UserId,
            Message = request.Message.Trim()
        }, cancellationToken);

        if (entity.Status == ServiceRequestStatus.ClarificationRequired && entity.CustomerId == currentUser.UserId)
        {
            await ChangeStatusAsync(entity, ServiceRequestStatus.Analyzing, "Customer provided clarification", cancellationToken);
            await orchestrator.ResumeAfterClarificationAsync(entity.Id, cancellationToken);
            return;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RequestHistoryDto>> HistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await Load(id, cancellationToken);
        EnsureCanView(entity);
        var items = await history.Query()
            .Where(x => x.RequestId == id)
            .OrderBy(x => x.Timestamp)
            .ToListAsync(cancellationToken);
        return items.Select(x => new RequestHistoryDto
        {
            Id = x.Id,
            FromStatus = EnumMap.ToApi(x.FromStatus),
            ToStatus = EnumMap.ToApi(x.ToStatus),
            Timestamp = x.Timestamp,
            Note = x.Note
        }).ToList();
    }

    private async Task<ServiceRequest> Load(Guid id, CancellationToken cancellationToken) =>
        await requests.Query().Include(x => x.Category).Include(x => x.Invitations).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Request not found.");

    private async Task ChangeStatusAsync(ServiceRequest request, ServiceRequestStatus to, string note, CancellationToken cancellationToken)
    {
        if (request.Status == to)
        {
            return;
        }

        if (!RequestStateMachine.CanTransition(request.Status, to))
        {
            throw new ConflictException($"Cannot move request from {EnumMap.ToApi(request.Status)} to {EnumMap.ToApi(to)}.");
        }

        var from = request.Status;
        request.Status = to;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await AddHistory(request, from, to, note, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private Task AddHistory(ServiceRequest request, ServiceRequestStatus from, ServiceRequestStatus to, string note, CancellationToken cancellationToken) =>
        history.AddAsync(new RequestStatusHistory
        {
            RequestId = request.Id,
            ActorId = currentUser.UserId,
            FromStatus = from,
            ToStatus = to,
            Note = note
        }, cancellationToken);

    private void EnsureCustomer()
    {
        if (!currentUser.IsCustomer && !currentUser.IsAdmin)
        {
            throw new ForbiddenException("Only customers can create requests.");
        }
    }

    private void EnsureOwner(ServiceRequest request)
    {
        if (!currentUser.IsAdmin && request.CustomerId != currentUser.UserId)
        {
            throw new ForbiddenException("Only the owning customer may update this request.");
        }
    }

    private void EnsureCanView(ServiceRequest request)
    {
        if (currentUser.IsAdmin || request.CustomerId == currentUser.UserId)
        {
            return;
        }

        if (currentUser.IsTechnician && request.Invitations.Any())
        {
            return;
        }

        throw new ForbiddenException();
    }

    private bool CanSeeAddress(ServiceRequest request) =>
        currentUser.IsAdmin || request.CustomerId == currentUser.UserId;

    private async Task<TechnicianProfile> RequireTechnician(CancellationToken cancellationToken) =>
        await technicians.FirstAsync(x => x.UserId == currentUser.UserId, cancellationToken)
        ?? throw new ForbiddenException("Technician profile is required.");

    private static RequestDto Map(ServiceRequest request, bool includeAddress) => new()
    {
        Id = request.Id,
        CustomerId = request.CustomerId,
        CategoryId = request.CategoryId,
        CategoryName = request.Category?.Name,
        Description = request.Description,
        PreferredStart = request.PreferredStart,
        PreferredEnd = request.PreferredEnd,
        BudgetAmount = request.BudgetAmount,
        ServiceArea = request.ServiceArea,
        Address = includeAddress ? request.AddressEncrypted : null,
        Latitude = includeAddress ? request.Latitude : null,
        Longitude = includeAddress ? request.Longitude : null,
        Status = EnumMap.ToApi(request.Status),
        Version = request.Version,
        CreatedAt = request.CreatedAt,
        UpdatedAt = request.UpdatedAt
    };
}
