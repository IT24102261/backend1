using FixFlow.Application.Common;
using FixFlow.Application.DTOs.Reporting;
using FixFlow.Application.Interfaces;
using FixFlow.Application.Mapping;
using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;
using FixFlow.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Application.Services;

public class ReportingService(
    IRepository<ServiceRequest> requests,
    IRepository<Booking> bookings,
    IRepository<TechnicianProfile> technicians,
    IRepository<TechnicianCategoryApplication> applications,
    IRepository<Complaint> complaints,
    IRepository<AiWorkflow> workflows,
    IRepository<User> users,
    IRepository<AuditLog> audits) : IReportingService
{
    public async Task<DashboardDto> DashboardAsync(CancellationToken cancellationToken = default)
    {
        var technicianCount = await technicians.Query().CountAsync(cancellationToken);
        var pending = await applications.Query().CountAsync(x => x.Status == ApplicationStatus.Submitted, cancellationToken);
        var workflowsTotal = await workflows.Query().CountAsync(cancellationToken);
        var workflowsCompleted = await workflows.Query().CountAsync(x => x.Status == AiWorkflowStatus.Completed, cancellationToken);
        var ratings = await technicians.Query().Where(x => x.ReviewCount > 0).Select(x => x.AverageRating).ToListAsync(cancellationToken);
        return new DashboardDto
        {
            TotalCustomers = await users.Query().CountAsync(x => x.Role == UserRole.Customer, cancellationToken),
            TotalTechnicians = technicianCount,
            Technicians = technicianCount,
            PendingVerification = pending,
            PendingApplications = pending,
            ApprovedTechnicians = await applications.Query().Where(x => x.Status == ApplicationStatus.Approved).Select(x => x.TechnicianId).Distinct().CountAsync(cancellationToken),
            RejectedApplications = await applications.Query().CountAsync(x => x.Status == ApplicationStatus.Rejected, cancellationToken),
            TotalRequests = await requests.Query().CountAsync(cancellationToken),
            OpenRequests = await requests.Query().CountAsync(x =>
                x.Status != ServiceRequestStatus.Completed &&
                x.Status != ServiceRequestStatus.Cancelled &&
                x.Status != ServiceRequestStatus.Failed, cancellationToken),
            TotalBookings = await bookings.Query().CountAsync(cancellationToken),
            ActiveBookings = await bookings.Query().CountAsync(x => BookingStateMachine.Active.Contains(x.Status), cancellationToken),
            CompletedJobs = await bookings.Query().CountAsync(x => x.Status == BookingStatus.CustomerConfirmed || x.Status == BookingStatus.Closed, cancellationToken),
            CancelledJobs = await bookings.Query().CountAsync(x => x.Status == BookingStatus.Cancelled, cancellationToken),
            OpenComplaints = await complaints.Query().CountAsync(x => x.Status == ComplaintStatus.Open || x.Status == ComplaintStatus.InReview, cancellationToken),
            AiWorkflows = workflowsTotal,
            AverageRating = ratings.Count == 0 ? 0 : Math.Round(ratings.Average(), 2),
            AiWorkflowSuccessRate = workflowsTotal == 0 ? 0 : Math.Round(100.0 * workflowsCompleted / workflowsTotal, 1)
        };
    }

    public async Task<PagedResult<RequestReportDto>> RequestsAsync(PagedQuery query, CancellationToken cancellationToken = default)
    {
        var source = requests.Query().Include(x => x.Category).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(x => x.Status == EnumMap.Parse<ServiceRequestStatus>(query.Status));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            source = source.Where(x => x.Description.Contains(query.Search));
        }

        var total = await source.CountAsync(cancellationToken);
        var rows = await source.OrderByDescending(x => x.CreatedAt).Skip(query.Skip).Take(query.Take).ToListAsync(cancellationToken);
        var items = rows.Select(x => new RequestReportDto
        {
            Id = x.Id,
            Status = EnumMap.ToApi(x.Status),
            CategoryName = x.Category?.Name,
            CreatedAt = x.CreatedAt
        }).ToList();

        return Page(items, query, total);
    }

    public async Task<PagedResult<BookingReportDto>> BookingsAsync(PagedQuery query, CancellationToken cancellationToken = default)
    {
        var source = bookings.Query().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(x => x.Status == EnumMap.Parse<BookingStatus>(query.Status));
        }

        var total = await source.CountAsync(cancellationToken);
        var rows = await source.OrderByDescending(x => x.ConfirmedAt).Skip(query.Skip).Take(query.Take).ToListAsync(cancellationToken);
        var items = rows.Select(x => new BookingReportDto
        {
            Id = x.Id,
            Status = EnumMap.ToApi(x.Status),
            TechnicianId = x.TechnicianId,
            ConfirmedAt = x.ConfirmedAt
        }).ToList();
        return Page(items, query, total);
    }

    public async Task<PagedResult<TechnicianReportDto>> TechniciansAsync(PagedQuery query, CancellationToken cancellationToken = default)
    {
        var source = technicians.Query().Include(x => x.User).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            source = source.Where(x => x.User.DisplayName.Contains(query.Search) || x.User.Email.Contains(query.Search));
        }

        var total = await source.CountAsync(cancellationToken);
        var rows = await source.OrderByDescending(x => x.AverageRating).Skip(query.Skip).Take(query.Take).ToListAsync(cancellationToken);
        var items = rows.Select(x => new TechnicianReportDto
        {
            Id = x.Id,
            DisplayName = x.User.DisplayName,
            AverageRating = x.AverageRating,
            ReviewCount = x.ReviewCount,
            IsSuspended = x.IsSuspended,
            ProfilePhotoUrl = TechnicianPhotoUrl.For(x.Id, x.ProfilePhotoStorageKey)
        }).ToList();
        return Page(items, query, total);
    }

    public async Task<PagedResult<AgentReportDto>> AgentsAsync(PagedQuery query, CancellationToken cancellationToken = default)
    {
        var source = workflows.Query().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(x => x.Status == EnumMap.Parse<AiWorkflowStatus>(query.Status));
        }

        var total = await source.CountAsync(cancellationToken);
        var rows = await source.OrderByDescending(x => x.StartedAt).Skip(query.Skip).Take(query.Take).ToListAsync(cancellationToken);
        var items = rows.Select(x => new AgentReportDto
        {
            Id = x.Id,
            RequestId = x.RequestId,
            Status = EnumMap.ToApi(x.Status),
            ApprovalStatus = EnumMap.ToApi(x.ApprovalStatus),
            StartedAt = x.StartedAt
        }).ToList();
        return Page(items, query, total);
    }

    public async Task<PagedResult<AuditLogDto>> AuditAsync(PagedQuery query, CancellationToken cancellationToken = default)
    {
        var source = audits.Query().Include(x => x.Actor).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            source = source.Where(x =>
                x.Action.Contains(query.Search) ||
                x.Entity.Contains(query.Search) ||
                (x.Actor != null && x.Actor.Email.Contains(query.Search)));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(x => x.Outcome == EnumMap.Parse<AuditOutcome>(query.Status));
        }

        var total = await source.CountAsync(cancellationToken);
        var rows = await source.OrderByDescending(x => x.Timestamp).Skip(query.Skip).Take(query.Take).ToListAsync(cancellationToken);
        var items = rows.Select(x => new AuditLogDto
        {
            Id = x.Id,
            ActorId = x.ActorId,
            Actor = x.Actor?.Email,
            Action = x.Action,
            Entity = x.Entity,
            EntityId = x.EntityId,
            Outcome = EnumMap.ToApi(x.Outcome),
            Timestamp = x.Timestamp,
            MetadataSummary = string.IsNullOrWhiteSpace(x.MetadataJson) ? null : x.MetadataJson[..Math.Min(x.MetadataJson.Length, 240)]
        }).ToList();
        return Page(items, query, total);
    }

    private static PagedResult<T> Page<T>(IReadOnlyList<T> items, PagedQuery query, int total) => new()
    {
        Items = items,
        Page = query.Page,
        PageSize = query.Take,
        TotalCount = total
    };
}
