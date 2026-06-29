using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Chat.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessageTitles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SenderTitleDisplayName",
                table: "ChatMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetCharacterTitleDisplayName",
                table: "ChatMessages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SenderTitleDisplayName",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "TargetCharacterTitleDisplayName",
                table: "ChatMessages");
        }
    }
}
