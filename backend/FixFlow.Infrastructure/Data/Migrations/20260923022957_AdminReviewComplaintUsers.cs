using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdminReviewComplaintUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "admin_replied_at",
                table: "reviews",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "admin_reply",
                table: "reviews",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "admin_replied_at",
                table: "complaints",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "admin_reply",
                table: "complaints",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "admin_replied_at",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "admin_reply",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "admin_replied_at",
                table: "complaints");

            migrationBuilder.DropColumn(
                name: "admin_reply",
                table: "complaints");
        }
    }
}
