using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DelphicGames.Data.Migrations
{
    /// <inheritdoc />
    public partial class addunuquecameravalues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_cameras_name",
                table: "cameras",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cameras_url",
                table: "cameras",
                column: "url",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_cameras_name",
                table: "cameras");

            migrationBuilder.DropIndex(
                name: "ix_cameras_url",
                table: "cameras");
        }
    }
}
