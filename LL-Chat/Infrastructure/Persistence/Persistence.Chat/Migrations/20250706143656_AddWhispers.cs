using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Chat.Migrations
{
    /// <inheritdoc />
    public partial class AddWhispers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TargetUserId",
                table: "ChatMessages",
                newName: "TargetCharacterId");

            migrationBuilder.AddColumn<string>(
                name: "TargetCharacterName",
                table: "ChatMessages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetCharacterName",
                table: "ChatMessages");

            migrationBuilder.RenameColumn(
                name: "TargetCharacterId",
                table: "ChatMessages",
                newName: "TargetUserId");
        }
    }
}
