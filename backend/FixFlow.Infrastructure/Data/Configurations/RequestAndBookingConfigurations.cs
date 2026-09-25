using FixFlow.Domain.Entities;
using FixFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixFlow.Infrastructure.Data.Configurations;

public class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> builder)
    {
        builder.ToTable("service_requests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.ServiceArea).HasMaxLength(200);
        builder.Property(x => x.BudgetAmount).HasPrecision(12, 2);
        builder.Property(x => x.AddressEncrypted).HasMaxLength(2048);
        builder.Property(x => x.Status).HasUpperSnakeConversion();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.PreferredStart).HasColumnType("timestamptz");
        builder.Property(x => x.PreferredEnd).HasColumnType("timestamptz");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.HasIndex(x => new { x.CustomerId, x.Status });
        builder.HasIndex(x => x.CategoryId);
        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_service_requests_latitude", "latitude IS NULL OR (latitude >= -90 AND latitude <= 90)");
            table.HasCheckConstraint("ck_service_requests_longitude", "longitude IS NULL OR (longitude >= -180 AND longitude <= 180)");
            table.HasCheckConstraint("ck_service_requests_preferred_window", "preferred_start IS NULL OR preferred_end IS NULL OR preferred_end >= preferred_start");
            table.HasCheckConstraint("ck_service_requests_budget", "budget_amount IS NULL OR budget_amount > 0");
        });
    }
}

public class RequestMediaConfiguration : IEntityTypeConfiguration<RequestMedia>
{
    public void Configure(EntityTypeBuilder<RequestMedia> builder)
    {
        builder.ToTable("request_media");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
        builder.Property(x => x.MimeType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.UploadedAt).HasColumnType("timestamptz");
        builder.HasOne(x => x.Request)
            .WithMany(x => x.Media)
            .HasForeignKey(x => x.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.UseXminConcurrency();
    }
}

public class RequestInvitationConfiguration : IEntityTypeConfiguration<RequestInvitation>
{
    public void Configure(EntityTypeBuilder<RequestInvitation> builder)
    {
        builder.ToTable("request_invitations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasUpperSnakeConversion();
        builder.Property(x => x.SentAt).HasColumnType("timestamptz");
        builder.HasIndex(x => new { x.RequestId, x.TechnicianId }).IsUnique();
        builder.HasOne(x => x.Request)
            .WithMany(x => x.Invitations)
            .HasForeignKey(x => x.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Technician)
            .WithMany()
            .HasForeignKey(x => x.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.UseXminConcurrency();
    }
}

public class QuotationConfiguration : IEntityTypeConfiguration<Quotation>
{
    public void Configure(EntityTypeBuilder<Quotation> builder)
    {
        builder.ToTable("quotations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LabourAmount).HasPrecision(12, 2);
        builder.Property(x => x.MaterialsAmount).HasPrecision(12, 2);
        builder.Property(x => x.TravelAmount).HasPrecision(12, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(12, 2);
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Assumptions).HasMaxLength(4000);
        builder.Property(x => x.IncludedMaterials).HasMaxLength(2000);
        builder.Property(x => x.ExcludedMaterials).HasMaxLength(2000);
        builder.Property(x => x.Status).HasUpperSnakeConversion();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.ArrivalStart).HasColumnType("timestamptz");
        builder.Property(x => x.ExpiresAt).HasColumnType("timestamptz");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.HasIndex(x => new { x.RequestId, x.TechnicianId });
        builder.HasIndex(x => x.QuoteGroupId);
        builder.HasOne(x => x.Request)
            .WithMany(x => x.Quotations)
            .HasForeignKey(x => x.RequestId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Technician)
            .WithMany()
            .HasForeignKey(x => x.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Invitation)
            .WithMany()
            .HasForeignKey(x => x.InvitationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_quotations_amounts", "labour_amount >= 0 AND materials_amount >= 0 AND travel_amount >= 0 AND total_amount >= 0");
            table.HasCheckConstraint("ck_quotations_duration", "duration_minutes > 0");
        });
    }
}

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasUpperSnakeConversion();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.ApprovedAt).HasColumnType("timestamptz");
        builder.Property(x => x.ConfirmedAt).HasColumnType("timestamptz");
        builder.Property(x => x.AddressReleaseAt).HasColumnType("timestamptz");
        builder.HasIndex(x => x.RequestId).IsUnique();
        builder.HasIndex(x => x.QuotationId).IsUnique();
        builder.HasIndex(x => new { x.TechnicianId, x.Status });
        builder.HasOne(x => x.Request)
            .WithMany()
            .HasForeignKey(x => x.RequestId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Quotation)
            .WithMany()
            .HasForeignKey(x => x.QuotationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Technician)
            .WithMany()
            .HasForeignKey(x => x.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class BookingStatusHistoryConfiguration : IEntityTypeConfiguration<BookingStatusHistory>
{
    public void Configure(EntityTypeBuilder<BookingStatusHistory> builder)
    {
        builder.ToTable("booking_status_history");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FromStatus).HasUpperSnakeConversion();
        builder.Property(x => x.ToStatus).HasUpperSnakeConversion();
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.Property(x => x.Timestamp).HasColumnType("timestamptz");
        builder.HasIndex(x => new { x.BookingId, x.Timestamp });
        builder.HasOne(x => x.Booking)
            .WithMany(x => x.StatusHistory)
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Actor)
            .WithMany()
            .HasForeignKey(x => x.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.UseXminConcurrency();
    }
}

public class ScopeChangeRequestConfiguration : IEntityTypeConfiguration<ScopeChangeRequest>
{
    public void Configure(EntityTypeBuilder<ScopeChangeRequest> builder)
    {
        builder.ToTable("scope_change_requests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProposedCost).HasPrecision(12, 2);
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.CustomerDecision).HasUpperSnakeConversion();
        builder.Property(x => x.DecisionAt).HasColumnType("timestamptz");
        builder.HasOne(x => x.Booking)
            .WithMany(x => x.ScopeChanges)
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.UseXminConcurrency();
        builder.ToTable(table =>
            table.HasCheckConstraint("ck_scope_change_requests_cost", "proposed_cost >= 0"));
    }
}

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Body).HasMaxLength(2000);
        builder.Property(x => x.Status).HasUpperSnakeConversion();
        builder.Property(x => x.ModerationReason).HasMaxLength(500);
        builder.Property(x => x.AdminReply).HasMaxLength(2000);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.AdminRepliedAt).HasColumnType("timestamptz");
        builder.HasIndex(x => x.BookingId)
            .IsUnique()
            .HasFilter("status IN ('PENDING', 'PUBLISHED')")
            .HasDatabaseName("ux_reviews_one_active_per_booking");
        builder.HasIndex(x => x.TechnicianId);
        builder.HasOne(x => x.Booking)
            .WithMany()
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Technician)
            .WithMany()
            .HasForeignKey(x => x.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.UseXminConcurrency();
        builder.ToTable(table =>
            table.HasCheckConstraint("ck_reviews_rating", "rating >= 1 AND rating <= 5"));
    }
}

public class ComplaintConfiguration : IEntityTypeConfiguration<Complaint>
{
    public void Configure(EntityTypeBuilder<Complaint> builder)
    {
        builder.ToTable("complaints");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Subject).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Resolution).HasMaxLength(2000);
        builder.Property(x => x.AdminReply).HasMaxLength(2000);
        builder.Property(x => x.Status).HasUpperSnakeConversion();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.ResolvedAt).HasColumnType("timestamptz");
        builder.Property(x => x.AdminRepliedAt).HasColumnType("timestamptz");
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
        builder.HasOne(x => x.Booking)
            .WithMany()
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ReportedBy)
            .WithMany()
            .HasForeignKey(x => x.ReportedById)
            .OnDelete(DeleteBehavior.Restrict);
        builder.UseXminConcurrency();
    }
}

public class RequestStatusHistoryConfiguration : IEntityTypeConfiguration<RequestStatusHistory>
{
    public void Configure(EntityTypeBuilder<RequestStatusHistory> builder)
    {
        builder.ToTable("request_status_history");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FromStatus).HasUpperSnakeConversion();
        builder.Property(x => x.ToStatus).HasUpperSnakeConversion();
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.Property(x => x.Timestamp).HasColumnType("timestamptz");
        builder.HasIndex(x => new { x.RequestId, x.Timestamp });
        builder.HasOne(x => x.Request)
            .WithMany(x => x.History)
            .HasForeignKey(x => x.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Actor)
            .WithMany()
            .HasForeignKey(x => x.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.UseXminConcurrency();
    }
}

public class RequestClarificationConfiguration : IEntityTypeConfiguration<RequestClarification>
{
    public void Configure(EntityTypeBuilder<RequestClarification> builder)
    {
        builder.ToTable("request_clarifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Message).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.HasOne(x => x.Request)
            .WithMany(x => x.Clarifications)
            .HasForeignKey(x => x.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Author)
            .WithMany()
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.UseXminConcurrency();
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.Property(x => x.ExpiresAt).HasColumnType("timestamptz");
        builder.Property(x => x.RevokedAt).HasColumnType("timestamptz");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.UseXminConcurrency();
    }
}
