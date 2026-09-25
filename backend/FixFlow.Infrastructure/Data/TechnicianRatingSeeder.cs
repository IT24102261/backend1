using System.Security.Cryptography;
using System.Text;
using FixFlow.Application.Interfaces;
using FixFlow.Domain.Constants;
using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FixFlow.Infrastructure.Data;

public sealed class TechnicianRatingSeeder(FixFlowDbContext db, IPasswordHasher passwordHasher, ILogger<TechnicianRatingSeeder> logger)
{
    public const string CustomerEmail = "ratings.customer@fixflow.local";
    private const string HistoryPrefix = "[History] Completed job for ";

    private static readonly string[] Comments =
    [
        "Arrived on time and completed the work properly.",
        "Professional, tidy, and explained the repair clearly.",
        "Good service and a fair quotation.",
        "Fixed the issue on the first visit.",
        "Polite and careful with the house.",
        "Would hire again for similar work."
    ];

    public async Task<int> SeedAsync(CancellationToken cancellationToken = default)
    {
        var technicians = await db.TechnicianProfiles
            .Include(x => x.User)
            .Include(x => x.Applications)
            .Where(x => x.User.IsActive && !x.IsSuspended)
            .ToListAsync(cancellationToken);
        if (technicians.Count == 0)
        {
            logger.LogInformation("No technicians available for rating seed.");
            return 0;
        }

        var alreadyReviewed = await db.Reviews
            .Where(x => x.Status == ReviewStatus.Published)
            .Select(x => x.TechnicianId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var reviewed = alreadyReviewed.ToHashSet();
        var pending = technicians.Where(x => !reviewed.Contains(x.Id)).ToArray();
        if (pending.Length == 0)
        {
            logger.LogInformation("Technician ratings already seeded for {Total} profiles.", technicians.Count);
            return 0;
        }

        var customer = await EnsureCustomerAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var addedReviews = 0;

        foreach (var technician in pending)
        {
            var seed = StableSeed(technician.Id);
            var reviewCount = 3 + seed % 5;
            var categoryId = technician.Applications
                .Where(x => x.Status == ApplicationStatus.Approved)
                .Select(x => x.CategoryId)
                .FirstOrDefault();
            if (categoryId == Guid.Empty)
            {
                categoryId = ServiceCategorySeed.Electrician;
            }

            var ratings = new List<int>(reviewCount);
            for (var index = 0; index < reviewCount; index++)
            {
                var rating = RatingFor(seed, index);
                ratings.Add(rating);
                var requestId = SeedGuid($"request:{technician.Id:N}:{index}");
                var quoteId = SeedGuid($"quote:{technician.Id:N}:{index}");
                var bookingId = SeedGuid($"booking:{technician.Id:N}:{index}");
                var reviewId = SeedGuid($"review:{technician.Id:N}:{index}");
                var completedAt = now.AddDays(-(8 + index * 3));

                db.ServiceRequests.Add(new ServiceRequest
                {
                    Id = requestId,
                    CustomerId = customer.Id,
                    CategoryId = categoryId,
                    Description = $"{HistoryPrefix}{technician.User.DisplayName}.",
                    ServiceArea = technician.ServiceArea,
                    AddressEncrypted = "Seeded completed address",
                    Status = ServiceRequestStatus.Completed,
                    CreatedAt = completedAt.AddDays(-2),
                    UpdatedAt = completedAt
                });
                db.Quotations.Add(new Quotation
                {
                    Id = quoteId,
                    RequestId = requestId,
                    TechnicianId = technician.Id,
                    LabourAmount = 1500 + index * 250,
                    MaterialsAmount = 800,
                    TravelAmount = 0,
                    TotalAmount = 2300 + index * 250,
                    Currency = "LKR",
                    DurationMinutes = 60,
                    ExpiresAt = completedAt.AddDays(1),
                    Status = QuotationStatus.Accepted,
                    CreatedAt = completedAt.AddDays(-1)
                });
                db.Bookings.Add(new Booking
                {
                    Id = bookingId,
                    RequestId = requestId,
                    QuotationId = quoteId,
                    CustomerId = customer.Id,
                    TechnicianId = technician.Id,
                    Status = BookingStatus.CustomerConfirmed,
                    ApprovedAt = completedAt.AddHours(-6),
                    ConfirmedAt = completedAt.AddHours(-5),
                    AddressReleaseAt = completedAt.AddHours(-5)
                });
                db.Reviews.Add(new Review
                {
                    Id = reviewId,
                    BookingId = bookingId,
                    CustomerId = customer.Id,
                    TechnicianId = technician.Id,
                    Rating = rating,
                    Body = Comments[index % Comments.Length],
                    Status = ReviewStatus.Published,
                    CreatedAt = completedAt
                });
                addedReviews++;
            }

            technician.ReviewCount = ratings.Count;
            technician.AverageRating = Math.Round((decimal)ratings.Average(), 2);
            technician.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Seeded {Reviews} published reviews for {Technicians} technicians.", addedReviews, pending.Length);
        return addedReviews;
    }

    private async Task<User> EnsureCustomerAsync(CancellationToken cancellationToken)
    {
        var existing = await db.Users.FirstOrDefaultAsync(x => x.Email == CustomerEmail, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var now = DateTimeOffset.UtcNow;
        var customer = new User
        {
            Id = Guid.Parse("b7a1c001-0099-4000-8000-990000000001"),
            Email = CustomerEmail,
            PasswordHash = passwordHasher.Hash(JaffnaTechnicianSeeder.Password),
            Role = UserRole.Customer,
            DisplayName = "FixFlow Ratings Customer",
            Phone = "0212200000",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Users.Add(customer);
        await db.SaveChangesAsync(cancellationToken);
        return customer;
    }

    private static int RatingFor(int seed, int index)
    {
        var wheel = (seed + index * 3) % 10;
        return wheel switch
        {
            0 => 3,
            1 or 2 or 3 => 4,
            _ => 5
        };
    }

    private static int StableSeed(Guid id) => Math.Abs(BitConverter.ToInt32(id.ToByteArray(), 0));

    private static Guid SeedGuid(string key)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes($"fixflow-rating:{key}"));
        return new Guid(hash);
    }
}
