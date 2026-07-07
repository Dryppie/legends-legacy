using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddAchievementEventLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AchievementEventLedgers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OutboxMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchievementEventLedgers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AchievementEventLedgers_CharacterId_ProcessedAt",
                table: "AchievementEventLedgers",
                columns: new[] { "CharacterId", "ProcessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AchievementEventLedgers_OutboxMessageId",
                table: "AchievementEventLedgers",
                column: "OutboxMessageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AchievementEventLedgers");
        }
    }
}
