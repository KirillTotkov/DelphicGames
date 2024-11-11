using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DelphicGames.Data.Migrations
{
    /// <inheritdoc />
    public partial class removeregioncity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_users_cities_city_id",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_users_regions_region_id",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "fk_cameras_cities_city_id",
                table: "cameras");

            migrationBuilder.DropTable(
                name: "cities");

            migrationBuilder.DropTable(
                name: "regions");

            migrationBuilder.DropIndex(
                name: "ix_cameras_city_id",
                table: "cameras");

            migrationBuilder.DropIndex(
                name: "ix_asp_net_users_city_id",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "ix_asp_net_users_region_id",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "city_id",
                table: "cameras");

            migrationBuilder.DropColumn(
                name: "city_id",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "region_id",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "city_id",
                table: "cameras",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "city_id",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "region_id",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "regions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_regions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cities",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    region_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cities", x => x.id);
                    table.ForeignKey(
                        name: "fk_cities_regions_region_id",
                        column: x => x.region_id,
                        principalTable: "regions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cameras_city_id",
                table: "cameras",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_users_city_id",
                table: "AspNetUsers",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_users_region_id",
                table: "AspNetUsers",
                column: "region_id");

            migrationBuilder.CreateIndex(
                name: "ix_cities_region_id",
                table: "cities",
                column: "region_id");

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_users_cities_city_id",
                table: "AspNetUsers",
                column: "city_id",
                principalTable: "cities",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_users_regions_region_id",
                table: "AspNetUsers",
                column: "region_id",
                principalTable: "regions",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_cameras_cities_city_id",
                table: "cameras",
                column: "city_id",
                principalTable: "cities",
                principalColumn: "id");
        }
    }
}
