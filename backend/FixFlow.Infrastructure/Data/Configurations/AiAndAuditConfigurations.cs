using FixFlow.Domain.Entities;
using FixFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixFlow.Infrastructure.Data.Configurations;

public class AiWorkflowConfiguration : IEntityTypeConfiguration<AiWorkflow>
{
    public void Configure(EntityTypeBuilder<AiWorkflow> builder)
    {
        builder.ToTable("ai_workflows");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Objective).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PlanJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CurrentStep).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasUpperSnakeConversion();
        builder.Property(x => x.ApprovalStatus).HasUpperSnakeConversion();
        builder.Property(x => x.ErrorCode).HasMaxLength(64);
        builder.Property(x => x.CompletedStepsJson).HasColumnType("jsonb").HasDefaultValue("[]").IsRequired();
        builder.Property(x => x.OutcomeJson).HasColumnType("jsonb");
        builder.Property(x => x.StartedAt).HasColumnType("timestamptz");
        builder.Property(x => x.FinishedAt).HasColumnType("timestamptz");
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_ai_workflows_retry", "retry_count >= 0");
            table.HasCheckConstraint("ck_ai_workflows_duration", "duration_ms >= 0");
        });
        builder.HasIndex(x => x.RequestId);
        builder.HasIndex(x => x.Status);
        builder.HasOne(x => x.Request)
            .WithMany()
            .HasForeignKey(x => x.RequestId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.UseXminConcurrency();
    }
}

public class AiWorkflowStepConfiguration : IEntityTypeConfiguration<AiWorkflowStep>
{
    public void Configure(EntityTypeBuilder<AiWorkflowStep> builder)
    {
        builder.ToTable("ai_workflow_steps");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AgentRole).HasUpperSnakeConversion();
        builder.Property(x => x.InputSummary).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.OutputJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ToolName).HasMaxLength(100);
        builder.Property(x => x.ToolArgsSummary).HasMaxLength(2000);
        builder.Property(x => x.ToolResultSummary).HasMaxLength(2000);
        builder.Property(x => x.ValidationJson).HasColumnType("jsonb");
        builder.Property(x => x.ErrorCode).HasMaxLength(64);
        builder.Property(x => x.Timestamp).HasColumnType("timestamptz");
        builder.HasIndex(x => new { x.WorkflowId, x.Timestamp });
        builder.HasOne(x => x.Workflow)
            .WithMany(x => x.Steps)
            .HasForeignKey(x => x.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.UseXminConcurrency();
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_ai_workflow_steps_duration", "duration_ms >= 0");
            table.HasCheckConstraint("ck_ai_workflow_steps_attempt", "attempt >= 1");
        });
    }
}

public class ApprovalConfiguration : IEntityTypeConfiguration<Approval>
{
    public void Configure(EntityTypeBuilder<Approval> builder)
    {
        builder.ToTable("approvals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Decision).HasUpperSnakeConversion();
        builder.Property(x => x.Reason).HasMaxLength(2000);
        builder.Property(x => x.Timestamp).HasColumnType("timestamptz");
        builder.HasIndex(x => x.WorkflowId);
        builder.HasOne(x => x.Workflow)
            .WithMany()
            .HasForeignKey(x => x.WorkflowId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Booking)
            .WithMany()
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Request)
            .WithMany()
            .HasForeignKey(x => x.RequestId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Actor)
            .WithMany()
            .HasForeignKey(x => x.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.UseXminConcurrency();
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Entity).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Outcome).HasUpperSnakeConversion();
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb");
        builder.Property(x => x.Timestamp).HasColumnType("timestamptz");
        builder.HasIndex(x => new { x.Entity, x.EntityId });
        builder.HasIndex(x => x.Timestamp);
        builder.HasOne(x => x.Actor)
            .WithMany()
            .HasForeignKey(x => x.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.UseXminConcurrency();
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.UseXminConcurrency();
    }
}
