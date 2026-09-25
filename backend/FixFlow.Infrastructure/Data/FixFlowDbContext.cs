using FixFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Infrastructure.Data;

public class FixFlowDbContext(DbContextOptions<FixFlowDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<TechnicianProfile> TechnicianProfiles => Set<TechnicianProfile>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<CategoryVerificationRequirement> CategoryVerificationRequirements => Set<CategoryVerificationRequirement>();
    public DbSet<TechnicianCategoryApplication> TechnicianCategoryApplications => Set<TechnicianCategoryApplication>();
    public DbSet<TechnicianDocument> TechnicianDocuments => Set<TechnicianDocument>();
    public DbSet<VerificationCheck> VerificationChecks => Set<VerificationCheck>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<RequestMedia> RequestMedia => Set<RequestMedia>();
    public DbSet<RequestInvitation> RequestInvitations => Set<RequestInvitation>();
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingStatusHistory> BookingStatusHistory => Set<BookingStatusHistory>();
    public DbSet<ScopeChangeRequest> ScopeChangeRequests => Set<ScopeChangeRequest>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<AiWorkflow> AiWorkflows => Set<AiWorkflow>();
    public DbSet<AiWorkflowStep> AiWorkflowSteps => Set<AiWorkflowStep>();
    public DbSet<Approval> Approvals => Set<Approval>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<RequestStatusHistory> RequestStatusHistory => Set<RequestStatusHistory>();
    public DbSet<RequestClarification> RequestClarifications => Set<RequestClarification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FixFlowDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditAndVersioning();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditAndVersioning();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditAndVersioning()
    {
        var utcNow = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            if (entry.Metadata.FindProperty("UpdatedAt") is not null)
            {
                entry.Property("UpdatedAt").CurrentValue = utcNow;
            }

            if (entry.State == EntityState.Added && entry.Metadata.FindProperty("CreatedAt") is not null)
            {
                entry.Property("CreatedAt").CurrentValue = utcNow;
            }

            if (entry.State == EntityState.Modified && entry.Metadata.FindProperty("Version") is not null)
            {
                var version = entry.Property("Version");
                var original = Convert.ToInt32(version.OriginalValue ?? version.CurrentValue ?? 1);
                version.OriginalValue = original;
                version.CurrentValue = original + 1;
            }
        }
    }
}
