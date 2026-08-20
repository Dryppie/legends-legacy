using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Chat.Migrations
{
    /// <inheritdoc />
    public partial class AddRaidChatChannels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RaidChatChannels",
                columns: table => new
                {
                    RaidRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    IsOpen = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidChatChannels", x => x.RaidRunId);
                });

            migrationBuilder.CreateTable(
                name: "RaidChatMemberships",
                columns: table => new
                {
                    RaidRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidChatMemberships", x => new { x.RaidRunId, x.CharacterId });
                    table.ForeignKey(
                        name: "FK_RaidChatMemberships_RaidChatChannels_RaidRunId",
                        column: x => x.RaidRunId,
                        principalTable: "RaidChatChannels",
                        principalColumn: "RaidRunId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RaidChatChannels_IsOpen_UpdatedAt",
                table: "RaidChatChannels",
                columns: new[] { "IsOpen", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RaidChatMemberships_CharacterId",
                table: "RaidChatMemberships",
                column: "CharacterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RaidChatMemberships");

            migrationBuilder.DropTable(
                name: "RaidChatChannels");
        }
    }
}
