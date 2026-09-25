using FixFlow.Domain.Constants;
using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;
using FixFlow.Infrastructure.Data;
using FixFlow.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FixFlow.Tests.Integration;

[Collection(nameof(FixFlowApiCollection))]
public class DatabaseConstraintTests(FixFlowApiFixture fixture)
{
    [Fact]
    public async Task ForeignKey_RejectsQuotationWithoutRequest()
    {
        await fixture.ScopeAsync(async services =>
        {
            var db = services.GetRequiredService<FixFlowDbContext>();
            db.Quotations.Add(new Quotation
            {
                RequestId = Guid.NewGuid(),
                TechnicianId = Guid.NewGuid(),
                LabourAmount = 1,
                MaterialsAmount = 0,
                TravelAmount = 0,
                TotalAmount = 1,
                DurationMinutes = 30,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
            });

            var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            Assert.IsType<PostgresException>(ex.InnerException);
        });
    }

    [Fact]
    public async Task Unique_RejectsDuplicateUserEmail()
    {
        var email = $"unique.{Guid.NewGuid():N}@fixflow.test";
        await fixture.ScopeAsync(async services =>
        {
            var db = services.GetRequiredService<FixFlowDbContext>();
            db.Users.Add(new User { Email = email, PasswordHash = "x", DisplayName = "One", Role = UserRole.Customer });
            await db.SaveChangesAsync();
        });

        await fixture.ScopeAsync(async services =>
        {
            var db = services.GetRequiredService<FixFlowDbContext>();
            db.Users.Add(new User { Email = email, PasswordHash = "y", DisplayName = "Two", Role = UserRole.Customer });
            var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            Assert.IsType<PostgresException>(ex.InnerException);
        });
    }

    [Fact]
    public async Task CheckConstraint_RejectsInvalidLatitudeAndRating()
    {
        await fixture.ScopeAsync(async services =>
        {
            var db = services.GetRequiredService<FixFlowDbContext>();
            var customer = new User
            {
                Email = $"ck.{Guid.NewGuid():N}@fixflow.test",
                PasswordHash = "x",
                DisplayName = "Check",
                Role = UserRole.Customer
            };
            db.Users.Add(customer);
            await db.SaveChangesAsync();

            db.ServiceRequests.Add(new ServiceRequest
            {
                CustomerId = customer.Id,
                CategoryId = ServiceCategorySeed.Electrician,
                Description = "Invalid latitude",
                Latitude = 120
            });
            var lat = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            Assert.Contains("ck_service_requests_latitude", lat.InnerException?.Message ?? lat.Message);

            db.ChangeTracker.Clear();
            var technicianUser = new User
            {
                Email = $"ck.tech.{Guid.NewGuid():N}@fixflow.test",
                PasswordHash = "x",
                DisplayName = "Tech",
                Role = UserRole.Technician
            };
            db.Users.Add(technicianUser);
            await db.SaveChangesAsync();
            var profile = new TechnicianProfile { UserId = technicianUser.Id, ServiceArea = "Colombo" };
            db.TechnicianProfiles.Add(profile);
            var request = new ServiceRequest
            {
                CustomerId = customer.Id,
                CategoryId = ServiceCategorySeed.Electrician,
                Description = "Valid request for review check"
            };
            db.ServiceRequests.Add(request);
            var quote = new Quotation
            {
                RequestId = request.Id,
                TechnicianId = profile.Id,
                LabourAmount = 1,
                MaterialsAmount = 0,
                TravelAmount = 0,
                TotalAmount = 1,
                DurationMinutes = 30,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
                Status = QuotationStatus.Accepted
            };
            db.Quotations.Add(quote);
            var booking = new Booking
            {
                RequestId = request.Id,
                QuotationId = quote.Id,
                CustomerId = customer.Id,
                TechnicianId = profile.Id,
                Status = BookingStatus.CustomerConfirmed
            };
            db.Bookings.Add(booking);
            await db.SaveChangesAsync();

            db.Reviews.Add(new Review
            {
                BookingId = booking.Id,
                CustomerId = customer.Id,
                TechnicianId = profile.Id,
                Rating = 0,
                Status = ReviewStatus.Published
            });
            var rating = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            Assert.Contains("ck_reviews_rating", rating.InnerException?.Message ?? rating.Message);
        });
    }

    [Fact]
    public async Task Transaction_RollsBackOnFailure()
    {
        var email = $"tx.{Guid.NewGuid():N}@fixflow.test";
        await fixture.ScopeAsync(async services =>
        {
            var db = services.GetRequiredService<FixFlowDbContext>();
            var uow = services.GetRequiredService<FixFlow.Application.Interfaces.IUnitOfWork>();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                uow.ExecuteInTransactionAsync(async () =>
                {
                    db.Users.Add(new User
                    {
                        Email = email,
                        PasswordHash = "x",
                        DisplayName = "Rollback",
                        Role = UserRole.Customer
                    });
                    await db.SaveChangesAsync();
                    throw new InvalidOperationException("force rollback");
                }));
        });

        await fixture.ScopeAsync(async services =>
        {
            var db = services.GetRequiredService<FixFlowDbContext>();
            Assert.False(await db.Users.AnyAsync(x => x.Email == email));
        });
    }

    [Fact]
    public async Task Migrations_AreAppliedAndHaveNoPending()
    {
        await fixture.ScopeAsync(async services =>
        {
            var db = services.GetRequiredService<FixFlowDbContext>();
            var pending = await db.Database.GetPendingMigrationsAsync();
            var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();

            Assert.Empty(pending);
            Assert.Contains(applied, x => x.Contains("InitialCreate"));
            Assert.Contains(applied, x => x.Contains("AddAuthAndWorkflowSupport"));
            Assert.Contains(applied, x => x.Contains("ExpandAiWorkflowPersistence"));
            Assert.True(await db.ServiceCategories.AnyAsync(x => x.Id == ServiceCategorySeed.Electrician));
        });
    }
}
