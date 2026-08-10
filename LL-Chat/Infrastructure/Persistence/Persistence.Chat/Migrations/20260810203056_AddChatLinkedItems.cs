using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Chat.Migrations
{
    /// <inheritdoc />
    public partial class AddChatLinkedItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkedItemJson",
                table: "ChatMessages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkedItemJson",
                table: "ChatMessages");
        }
    }
}
