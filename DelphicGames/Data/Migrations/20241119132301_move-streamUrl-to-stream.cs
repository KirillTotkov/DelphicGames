using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DelphicGames.Data.Migrations
{
    /// <inheritdoc />
    public partial class movestreamUrltostream : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "stream_url",
                table: "nominations");

            migrationBuilder.AddColumn<string>(
                name: "stream_url",
                table: "streams",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "stream_url",
                table: "streams");

            migrationBuilder.AddColumn<string>(
                name: "stream_url",
                table: "nominations",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
