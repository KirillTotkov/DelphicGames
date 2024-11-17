using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DelphicGames.Data.Migrations
{
    /// <inheritdoc />
    public partial class addpkstreams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cameras_nominations_nomination_id",
                table: "cameras");

            migrationBuilder.DropTable(
                name: "nomination_platforms");

            migrationBuilder.DropTable(
                name: "platforms");

            migrationBuilder.CreateTable(
                name: "streams",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nomination_id = table.Column<int>(type: "integer", nullable: false),
                    platform_name = table.Column<string>(type: "text", nullable: false),
                    platform_url = table.Column<string>(type: "text", nullable: false),
                    token = table.Column<string>(type: "text", nullable: true),
                    day = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_streams", x => x.id);
                    table.ForeignKey(
                        name: "fk_streams_nominations_nomination_id",
                        column: x => x.nomination_id,
                        principalTable: "nominations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_streams_nomination_id",
                table: "streams",
                column: "nomination_id");

            migrationBuilder.AddForeignKey(
                name: "fk_cameras_nominations_nomination_id",
                table: "cameras",
                column: "nomination_id",
                principalTable: "nominations",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cameras_nominations_nomination_id",
                table: "cameras");

            migrationBuilder.DropTable(
                name: "streams");

            migrationBuilder.CreateTable(
                name: "platforms",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platforms", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "nomination_platforms",
                columns: table => new
                {
                    nomination_id = table.Column<int>(type: "integer", nullable: false),
                    platform_id = table.Column<int>(type: "integer", nullable: false),
                    day = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    token = table.Column<string>(type: "text", nullable: true)
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

            migrationBuilder.AddForeignKey(
                name: "fk_cameras_nominations_nomination_id",
                table: "cameras",
                column: "nomination_id",
                principalTable: "nominations",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
