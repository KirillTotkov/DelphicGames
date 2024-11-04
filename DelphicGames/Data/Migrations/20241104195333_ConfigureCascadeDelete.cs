using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DelphicGames.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cameras_nominations_nomination_id",
                table: "cameras");

            migrationBuilder.AddForeignKey(
                name: "fk_cameras_nominations_nomination_id",
                table: "cameras",
                column: "nomination_id",
                principalTable: "nominations",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cameras_nominations_nomination_id",
                table: "cameras");

            migrationBuilder.AddForeignKey(
                name: "fk_cameras_nominations_nomination_id",
                table: "cameras",
                column: "nomination_id",
                principalTable: "nominations",
                principalColumn: "id");
        }
    }
}
