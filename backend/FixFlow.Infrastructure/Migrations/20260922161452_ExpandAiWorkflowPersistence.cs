using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandAiWorkflowPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "completed_steps_json",
                table: "ai_workflows",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<int>(
                name: "duration_ms",
                table: "ai_workflows",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "outcome_json",
                table: "ai_workflows",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "retry_count",
                table: "ai_workflows",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "ck_ai_workflows_duration",
                table: "ai_workflows",
                sql: "duration_ms >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_ai_workflows_retry",
                table: "ai_workflows",
                sql: "retry_count >= 0");

            migrationBuilder.Sql("""
                UPDATE ai_workflows SET status = 'PLANNING' WHERE status = 'RUNNING';
                UPDATE ai_workflows SET status = 'WAITING_FOR_CUSTOMER_APPROVAL' WHERE status = 'WAITING_APPROVAL';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_ai_workflows_duration",
                table: "ai_workflows");

            migrationBuilder.DropCheckConstraint(
                name: "ck_ai_workflows_retry",
                table: "ai_workflows");

            migrationBuilder.DropColumn(
                name: "completed_steps_json",
                table: "ai_workflows");

            migrationBuilder.DropColumn(
                name: "duration_ms",
                table: "ai_workflows");

            migrationBuilder.DropColumn(
                name: "outcome_json",
                table: "ai_workflows");

            migrationBuilder.DropColumn(
                name: "retry_count",
                table: "ai_workflows");
        }
    }
}
