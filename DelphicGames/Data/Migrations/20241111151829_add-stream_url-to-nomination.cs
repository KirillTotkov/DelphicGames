using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DelphicGames.Data.Migrations
{
    /// <inheritdoc />
    public partial class addstream_urltonomination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "stream_url",
                table: "nominations",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "stream_url",
                table: "nominations");
        }
    }
}
