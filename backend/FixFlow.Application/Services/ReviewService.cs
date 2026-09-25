using FixFlow.Application.Common;
using FixFlow.Application.DTOs.Admin;
using FixFlow.Application.DTOs.Reviews;
using FixFlow.Application.Exceptions;
using FixFlow.Application.Interfaces;
using FixFlow.Application.Mapping;
using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Application.Services;

public class ReviewService(
    IRepository<Review> reviews,
    IRepository<Booking> bookings,
    IRepository<TechnicianProfile> profiles,
    INotificationService notifications,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IReviewService
{
    public async Task<ReviewDto> CreateAsync(Guid bookingId, ReviewWriteRequest request, CancellationToken cancellationToken = default)
    {
        var booking = await bookings.Query()
            .Include(x => x.Customer)
            .Include(x => x.Technician).ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == bookingId, cancellationToken)
            ?? throw new NotFoundException("Booking not found.");

        if (booking.CustomerId != currentUser.UserId)
        {
            throw new ForbiddenException("Only the booking customer can leave a review.");
        }

        if (booking.Status != BookingStatus.CustomerConfirmed && booking.Status != BookingStatus.Closed)
        {
            throw new ConflictException("Review is allowed only after the customer confirms the work.");
        }

        if (booking.Technician.UserId == currentUser.UserId)
        {
            throw new ForbiddenException("A technician cannot review themselves.");
        }

        var hasActive = await reviews.Query().AnyAsync(
            x => x.BookingId == bookingId && (x.Status == ReviewStatus.Pending || x.Status == ReviewStatus.Published),
            cancellationToken);
        if (hasActive)
        {
            throw new ConflictException("This booking already has an active review.");
        }

        var review = new Review
        {
            BookingId = bookingId,
            CustomerId = currentUser.UserId,
            TechnicianId = booking.TechnicianId,
            Rating = request.Rating,
            Body = request.Body,
            Status = ReviewStatus.Published
        };
        review.Customer = booking.Customer;
        review.Technician = booking.Technician;
        await reviews.AddAsync(review, cancellationToken);
        await RecalculateRating(booking.TechnicianId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var bookingLabel = booking.Id.ToString("N")[..8];
        var customerName = booking.Customer.DisplayName;
        var comment = string.IsNullOrWhiteSpace(request.Body) ? "No written comment." : request.Body.Trim();
        await notifications.NotifyAsync(
            booking.Technician.UserId,
            "New customer review",
            $"{customerName} left a {request.Rating}/5 review on booking {bookingLabel}. {comment}",
            cancellationToken);
        return Map(review);
    }

    public async Task<IReadOnlyList<ReviewDto>> ListMineAsync(CancellationToken cancellationToken = default)
    {
        var items = await WithPeople(reviews.Query())
            .Where(x => x.CustomerId == currentUser.UserId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<ReviewDto>> ListForTechnicianAsync(Guid technicianId, CancellationToken cancellationToken = default)
    {
        var items = await WithPeople(reviews.Query())
            .Where(x => x.TechnicianId == technicianId && x.Status == ReviewStatus.Published)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<PagedResult<ReviewDto>> AdminListAsync(PagedQuery query, CancellationToken cancellationToken = default)
    {
        var source = WithPeople(reviews.Query());
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = EnumMap.Parse<ReviewStatus>(query.Status);
            source = source.Where(x => x.Status == status);
        }

        var total = await source.CountAsync(cancellationToken);
        var items = await source.OrderByDescending(x => x.CreatedAt).Skip(query.Skip).Take(query.Take).ToListAsync(cancellationToken);
        return new PagedResult<ReviewDto>
        {
            Items = items.Select(Map).ToList(),
            Page = query.Page,
            PageSize = query.Take,
            TotalCount = total
        };
    }

    public async Task<ReviewDto> ModerateAsync(Guid id, ModerateReviewRequest request, CancellationToken cancellationToken = default)
    {
        var review = await reviews.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Review not found.");
        review.Status = EnumMap.Parse<ReviewStatus>(request.Status);
        review.ModerationReason = request.Reason;
        await RecalculateRating(review.TechnicianId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(review);
    }

    public async Task<ReviewDto> UpdateAsync(Guid id, AdminReviewUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var review = await reviews.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Review not found.");
        review.Rating = request.Rating;
        review.Body = string.IsNullOrWhiteSpace(request.Body) ? null : request.Body.Trim();
        await RecalculateRating(review.TechnicianId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(review);
    }

    public async Task<ReviewDto> ReplyAsync(Guid id, AdminReplyRequest request, CancellationToken cancellationToken = default)
    {
        var review = await reviews.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Review not found.");
        review.AdminReply = request.Reply.Trim();
        review.AdminRepliedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await notifications.NotifyAsync(
            review.CustomerId,
            "Admin replied to your review",
            request.Reply.Trim(),
            cancellationToken);
        return Map(await LoadReview(id, cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var review = await reviews.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Review not found.");
        var technicianId = review.TechnicianId;
        reviews.Remove(review);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await RecalculateRating(technicianId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task RecalculateRating(Guid technicianId, CancellationToken cancellationToken)
    {
        var profile = await profiles.GetByIdAsync(technicianId, cancellationToken)
            ?? throw new NotFoundException("Technician not found.");
        var valid = await reviews.Query()
            .Where(x => x.TechnicianId == technicianId && x.Status == ReviewStatus.Published)
            .ToListAsync(cancellationToken);
        profile.ReviewCount = valid.Count;
        profile.AverageRating = valid.Count == 0 ? 0 : Math.Round((decimal)valid.Average(x => x.Rating), 2);
    }

    private async Task<Review> LoadReview(Guid id, CancellationToken cancellationToken) =>
        await WithPeople(reviews.Query()).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Review not found.");

    private static IQueryable<Review> WithPeople(IQueryable<Review> source) =>
        source
            .Include(x => x.Customer)
            .Include(x => x.Technician).ThenInclude(x => x.User);

    private static ReviewDto Map(Review review) => new()
    {
        Id = review.Id,
        BookingId = review.BookingId,
        CustomerId = review.CustomerId,
        TechnicianId = review.TechnicianId,
        CustomerDisplayName = review.Customer?.DisplayName,
        TechnicianDisplayName = review.Technician?.User?.DisplayName,
        Rating = review.Rating,
        Body = review.Body,
        Status = EnumMap.ToApi(review.Status),
        CreatedAt = review.CreatedAt,
        ModerationReason = review.ModerationReason,
        AdminReply = review.AdminReply,
        AdminRepliedAt = review.AdminRepliedAt
    };
}
