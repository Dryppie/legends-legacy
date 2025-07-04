using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Chat.Migrations
{
    /// <inheritdoc />
    public partial class AddChatChannels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Channel",
                table: "ChatMessages",
                newName: "ContextKey");

            migrationBuilder.AddColumn<int>(
                name: "ChannelType",
                table: "ChatMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "TargetUserId",
                table: "ChatMessages",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChannelType",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "TargetUserId",
                table: "ChatMessages");

            migrationBuilder.RenameColumn(
                name: "ContextKey",
                table: "ChatMessages",
                newName: "Channel");
        }
    }
}
