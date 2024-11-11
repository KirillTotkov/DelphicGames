using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DelphicGames.Data.Migrations
{
    /// <inheritdoc />
    public partial class adduserincamera : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "user_id",
                table: "cameras",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_cameras_user_id",
                table: "cameras",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_cameras_users_user_id",
                table: "cameras",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cameras_users_user_id",
                table: "cameras");

            migrationBuilder.DropIndex(
                name: "ix_cameras_user_id",
                table: "cameras");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "cameras");
        }
    }
}
