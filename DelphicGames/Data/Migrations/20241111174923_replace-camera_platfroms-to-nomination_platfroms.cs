using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DelphicGames.Data.Migrations
{
    /// <inheritdoc />
    public partial class replacecamera_platfromstonomination_platfroms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "camera_platforms");

            migrationBuilder.CreateTable(
                name: "nomination_platforms",
                columns: table => new
                {
                    nomination_id = table.Column<int>(type: "integer", nullable: false),
                    platform_id = table.Column<int>(type: "integer", nullable: false),
                    token = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nomination_platforms", x => new { x.nomination_id, x.platform_id });
                    table.ForeignKey(
                        name: "fk_nomination_platforms_nominations_nomination_id",
                        column: x => x.nomination_id,
                        principalTable: "nominations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_nomination_platforms_platforms_platform_id",
                        column: x => x.platform_id,
                        principalTable: "platforms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_nomination_platforms_platform_id",
                table: "nomination_platforms",
                column: "platform_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "nomination_platforms");

            migrationBuilder.CreateTable(
                name: "camera_platforms",
                columns: table => new
                {
                    camera_id = table.Column<int>(type: "integer", nullable: false),
                    platform_id = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    token = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_camera_platforms", x => new { x.camera_id, x.platform_id });
                    table.ForeignKey(
                        name: "fk_camera_platforms_cameras_camera_id",
                        column: x => x.camera_id,
                        principalTable: "cameras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_camera_platforms_platforms_platform_id",
                        column: x => x.platform_id,
                        principalTable: "platforms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_camera_platforms_platform_id",
                table: "camera_platforms",
                column: "platform_id");
        }
    }
}
