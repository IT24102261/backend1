using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using FixFlow.Infrastructure.Data;

#nullable disable

namespace FixFlow.Infrastructure.Data.Migrations
{
    [DbContext(typeof(FixFlowDbContext))]
    [Migration("20261001120000_PersistTechnicianProfilePhotos")]
    public partial class PersistTechnicianProfilePhotos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "technician_profile_images",
                columns: table => new
                {
                    technician_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<byte[]>(type: "bytea", nullable: false),
                    mime_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_technician_profile_images", x => x.technician_id);
                    table.ForeignKey(
                        name: "fk_technician_profile_images_technician_profiles_technician_id",
                        column: x => x.technician_id,
                        principalTable: "technician_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "technician_profile_images");
        }
    }
}
