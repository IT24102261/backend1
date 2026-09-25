using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AssignmentGapFill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "budget_amount",
                table: "service_requests",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "excluded_materials",
                table: "quotations",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "included_materials",
                table: "quotations",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "resolution",
                table: "complaints",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_service_requests_budget",
                table: "service_requests",
                sql: "budget_amount IS NULL OR budget_amount > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_service_requests_budget",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "budget_amount",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "excluded_materials",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "included_materials",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "resolution",
                table: "complaints");
        }
    }
}
