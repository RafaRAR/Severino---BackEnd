using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIseverino.Migrations
{
    /// <inheritdoc />
    public partial class ChatroomConfirmar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ClienteConfirmou",
                table: "ChatRooms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PrestadorConfirmou",
                table: "ChatRooms",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClienteConfirmou",
                table: "ChatRooms");

            migrationBuilder.DropColumn(
                name: "PrestadorConfirmou",
                table: "ChatRooms");
        }
    }
}