using FixFlow.Domain.Constants;
using FixFlow.Domain.Entities;
using FixFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixFlow.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(x => x.Role).HasUpperSnakeConversion().IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(32);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.HasIndex(x => x.Role);
        builder.HasIndex(x => x.IsActive);
        builder.UseXminConcurrency();
        builder.ToTable(table =>
            table.HasCheckConstraint("ck_users_role", "role IN ('CUSTOMER', 'TECHNICIAN', 'ADMIN')"));
    }
}

public class TechnicianProfileConfiguration : IEntityTypeConfiguration<TechnicianProfile>
{
    public void Configure(EntityTypeBuilder<TechnicianProfile> builder)
    {
        builder.ToTable("technician_profiles");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.Property(x => x.Bio).HasMaxLength(2000);
        builder.Property(x => x.Address).HasMaxLength(400);
        builder.Property(x => x.ServiceArea).HasMaxLength(200);
        builder.Property(x => x.ExperienceSummary).HasMaxLength(2000);
        builder.Property(x => x.ProfilePhotoStorageKey).HasMaxLength(512);
        builder.Property(x => x.ProfilePhotoMimeType).HasMaxLength(128);
        builder.Property(x => x.AverageRating).HasPrecision(3, 2);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.HasOne(x => x.User)
            .WithOne(x => x.TechnicianProfile)
            .HasForeignKey<TechnicianProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.UseXminConcurrency();
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_technician_profiles_latitude", "latitude_approx IS NULL OR (latitude_approx >= -90 AND latitude_approx <= 90)");
            table.HasCheckConstraint("ck_technician_profiles_longitude", "longitude_approx IS NULL OR (longitude_approx >= -180 AND longitude_approx <= 180)");
        });
    }
}

public class ServiceCategoryConfiguration : IEntityTypeConfiguration<ServiceCategory>
{
    public void Configure(EntityTypeBuilder<ServiceCategory> builder)
    {
        builder.ToTable("service_categories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.IsActive);
        builder.UseXminConcurrency();

        builder.HasData(
            new ServiceCategory { Id = ServiceCategorySeed.Electrician, Name = "Electrician", Description = "Electrical installation and repair", IsActive = true },
            new ServiceCategory { Id = ServiceCategorySeed.Plumber, Name = "Plumber", Description = "Plumbing installation and repair", IsActive = true },
            new ServiceCategory { Id = ServiceCategorySeed.AcRefrigeration, Name = "AC/Refrigeration", Description = "Air conditioning and refrigeration services", IsActive = true },
            new ServiceCategory { Id = ServiceCategorySeed.Solar, Name = "Solar", Description = "Solar panel installation and maintenance", IsActive = true },
            new ServiceCategory { Id = ServiceCategorySeed.Carpenter, Name = "Carpenter", Description = "Carpentry and woodwork", IsActive = true },
            new ServiceCategory { Id = ServiceCategorySeed.Painter, Name = "Painter", Description = "Interior and exterior painting", IsActive = true },
            new ServiceCategory { Id = ServiceCategorySeed.ApplianceRepair, Name = "Appliance Repair", Description = "Home appliance diagnostics and repair", IsActive = true });
    }
}

public class CategoryVerificationRequirementConfiguration : IEntityTypeConfiguration<CategoryVerificationRequirement>
{
    public void Configure(EntityTypeBuilder<CategoryVerificationRequirement> builder)
    {
        builder.ToTable("category_verification_requirements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EvidenceType).HasUpperSnakeConversion();
        builder.Property(x => x.ValidationMethod).HasUpperSnakeConversion();
        builder.Property(x => x.RuleVersion).HasMaxLength(32).IsRequired();
        builder.HasIndex(x => new { x.CategoryId, x.EvidenceType }).IsUnique();
        builder.HasOne(x => x.Category)
            .WithMany(x => x.VerificationRequirements)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.UseXminConcurrency();
    }
}

public class TechnicianCategoryApplicationConfiguration : IEntityTypeConfiguration<TechnicianCategoryApplication>
{
    public void Configure(EntityTypeBuilder<TechnicianCategoryApplication> builder)
    {
        builder.ToTable("technician_category_applications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasUpperSnakeConversion();
        builder.Property(x => x.DecisionNotes).HasMaxLength(2000);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.SubmittedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DecidedAt).HasColumnType("timestamptz");
        builder.HasIndex(x => new { x.TechnicianId, x.CategoryId }).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasOne(x => x.Technician)
            .WithMany(x => x.Applications)
            .HasForeignKey(x => x.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DecisionMaker)
            .WithMany()
            .HasForeignKey(x => x.DecidedBy)
            .OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table =>
            table.HasCheckConstraint(
                "ck_technician_category_applications_status",
                "status IN ('DRAFT', 'SUBMITTED', 'MORE_INFORMATION_REQUIRED', 'APPROVED', 'REJECTED', 'REVERIFICATION_REQUIRED', 'SUSPENDED')"));
    }
}

public class TechnicianDocumentConfiguration : IEntityTypeConfiguration<TechnicianDocument>
{
    public void Configure(EntityTypeBuilder<TechnicianDocument> builder)
    {
        builder.ToTable("technician_documents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EvidenceType).HasUpperSnakeConversion();
        builder.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
        builder.Property(x => x.MimeType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Sha256).HasColumnName("sha256").HasMaxLength(64).IsRequired();
        builder.Property(x => x.ClaimNumberEncrypted).HasMaxLength(1024);
        builder.Property(x => x.Issuer).HasMaxLength(200);
        builder.Property(x => x.ReviewStatus).HasUpperSnakeConversion();
        builder.Property(x => x.UploadedAt).HasColumnType("timestamptz");
        builder.HasIndex(x => x.Sha256);
        builder.HasOne(x => x.Application)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.UseXminConcurrency();
    }
}

public class VerificationCheckConfiguration : IEntityTypeConfiguration<VerificationCheck>
{
    public void Configure(EntityTypeBuilder<VerificationCheck> builder)
    {
        builder.ToTable("verification_checks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SourceType).HasUpperSnakeConversion();
        builder.Property(x => x.SourceReference).HasMaxLength(256);
        builder.Property(x => x.Outcome).HasUpperSnakeConversion();
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.CheckedAt).HasColumnType("timestamptz");
        builder.HasIndex(x => x.ApplicationId);
        builder.HasOne(x => x.Application)
            .WithMany(x => x.VerificationChecks)
            .HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Evidence)
            .WithMany()
            .HasForeignKey(x => x.EvidenceId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Admin)
            .WithMany()
            .HasForeignKey(x => x.AdminId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.UseXminConcurrency();
    }
}
