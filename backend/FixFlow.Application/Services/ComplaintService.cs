using FixFlow.Application.Common;
using FixFlow.Application.DTOs.Admin;
using FixFlow.Application.DTOs.Complaints;
using FixFlow.Application.Exceptions;
using FixFlow.Application.Interfaces;
using FixFlow.Application.Mapping;
using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Application.Services;

public class ComplaintService(
    IRepository<Complaint> complaints,
    IRepository<Booking> bookings,
    INotificationService notifications,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IComplaintService
{
    public async Task<ComplaintDto> CreateAsync(ComplaintWriteRequest request, CancellationToken cancellationToken = default)
    {
        var booking = await bookings.Query()
            .Include(x => x.Customer)
            .Include(x => x.Technician).ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == request.BookingId, cancellationToken)
            ?? throw new NotFoundException("Booking not found.");
        if (!currentUser.IsAdmin && booking.CustomerId != currentUser.UserId)
        {
            throw new ForbiddenException("You can only complain about your own booking.");
        }

        var complaint = new Complaint
        {
            BookingId = request.BookingId,
            ReportedById = currentUser.UserId,
            Subject = request.Subject.Trim(),
            Description = request.Description.Trim()
        };
        await complaints.AddAsync(complaint, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var bookingLabel = booking.Id.ToString("N")[..8];
        await notifications.NotifyAsync(
            booking.Technician.UserId,
            "New customer complaint",
            $"{booking.Customer.DisplayName} opened a complaint on booking {bookingLabel}: {complaint.Subject}. {complaint.Description}",
            cancellationToken);
        return Map(complaint);
    }

    public async Task<PagedResult<ComplaintDto>> ListAsync(PagedQuery query, CancellationToken cancellationToken = default)
    {
        var source = WithPeople(complaints.Query());
        if (currentUser.IsAdmin)
        {
            return await Page(source, query, cancellationToken);
        }

        if (currentUser.IsTechnician)
        {
            source = source.Where(x => x.Booking != null && x.Booking.Technician.UserId == currentUser.UserId);
            return await Page(source, query, cancellationToken);
        }

        source = source.Where(x => x.ReportedById == currentUser.UserId);
        return await Page(source, query, cancellationToken);
    }

    public Task<PagedResult<ComplaintDto>> AdminListAsync(PagedQuery query, CancellationToken cancellationToken = default) =>
        Page(WithPeople(complaints.Query()), query, cancellationToken);

    public async Task<ComplaintDto> UpdateStatusAsync(Guid id, ComplaintStatusRequest request, CancellationToken cancellationToken = default)
    {
        var complaint = await complaints.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Complaint not found.");
        complaint.Status = EnumMap.Parse<ComplaintStatus>(request.Status);
        if (!string.IsNullOrWhiteSpace(request.Resolution))
        {
            complaint.Resolution = request.Resolution.Trim();
        }

        complaint.ResolvedAt = complaint.Status is ComplaintStatus.Resolved or ComplaintStatus.Dismissed
            ? DateTimeOffset.UtcNow
            : null;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(await LoadComplaint(id, cancellationToken));
    }

    public async Task<ComplaintDto> ReplyAsync(Guid id, AdminReplyRequest request, CancellationToken cancellationToken = default)
    {
        var complaint = await complaints.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Complaint not found.");
        complaint.AdminReply = request.Reply.Trim();
        complaint.AdminRepliedAt = DateTimeOffset.UtcNow;
        if (complaint.Status == ComplaintStatus.Open)
        {
            complaint.Status = ComplaintStatus.InReview;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await notifications.NotifyAsync(
            complaint.ReportedById,
            "Admin replied to your complaint",
            request.Reply.Trim(),
            cancellationToken);
        return Map(await LoadComplaint(id, cancellationToken));
    }

    private async Task<Complaint> LoadComplaint(Guid id, CancellationToken cancellationToken) =>
        await WithPeople(complaints.Query()).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Complaint not found.");

    private static IQueryable<Complaint> WithPeople(IQueryable<Complaint> source) =>
        source
            .Include(x => x.ReportedBy)
            .Include(x => x.Booking).ThenInclude(x => x!.Technician).ThenInclude(x => x.User);

    private static async Task<PagedResult<ComplaintDto>> Page(IQueryable<Complaint> source, PagedQuery query, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = EnumMap.Parse<ComplaintStatus>(query.Status);
            source = source.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            source = source.Where(x => x.Subject.Contains(query.Search) || x.Description.Contains(query.Search));
        }

        var total = await source.CountAsync(cancellationToken);
        var items = await source.OrderByDescending(x => x.CreatedAt).Skip(query.Skip).Take(query.Take).ToListAsync(cancellationToken);
        return new PagedResult<ComplaintDto>
        {
            Items = items.Select(Map).ToList(),
            Page = query.Page,
            PageSize = query.Take,
            TotalCount = total
        };
    }

    private static ComplaintDto Map(Complaint complaint) => new()
    {
        Id = complaint.Id,
        BookingId = complaint.BookingId,
        ReportedById = complaint.ReportedById,
        TechnicianDisplayName = complaint.Booking?.Technician?.User?.DisplayName,
        CustomerDisplayName = complaint.ReportedBy?.DisplayName ?? complaint.Booking?.Customer?.DisplayName,
        Subject = complaint.Subject,
        Description = complaint.Description,
        Resolution = complaint.Resolution,
        AdminReply = complaint.AdminReply,
        AdminRepliedAt = complaint.AdminRepliedAt,
        Status = EnumMap.ToApi(complaint.Status),
        CreatedAt = complaint.CreatedAt,
        ResolvedAt = complaint.ResolvedAt
    };
}
