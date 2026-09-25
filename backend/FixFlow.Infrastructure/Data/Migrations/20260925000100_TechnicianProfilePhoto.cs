using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using FixFlow.Infrastructure.Data;

#nullable disable

namespace FixFlow.Infrastructure.Data.Migrations
{
    [DbContext(typeof(FixFlowDbContext))]
    [Migration("20260925000100_TechnicianProfilePhoto")]
    public partial class TechnicianProfilePhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "profile_photo_mime_type",
                table: "technician_profiles",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "profile_photo_storage_key",
                table: "technician_profiles",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "profile_photo_mime_type",
                table: "technician_profiles");

            migrationBuilder.DropColumn(
                name: "profile_photo_storage_key",
                table: "technician_profiles");
        }
    }
}
