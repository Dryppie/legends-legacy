using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddEventQuestPersonalMilestones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventQuestMilestoneClaims",
                columns: table => new
                {
                    EventQuestId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    MilestoneKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ClaimedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventQuestMilestoneClaims", x => new { x.EventQuestId, x.CharacterId, x.MilestoneKey });
                    table.ForeignKey(
                        name: "FK_EventQuestMilestoneClaims_EventQuestInstances_EventQuestId",
                        column: x => x.EventQuestId,
                        principalTable: "EventQuestInstances",
                        principalColumn: "EventQuestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventQuestMilestoneClaims_EventQuestId_CharacterId",
                table: "EventQuestMilestoneClaims",
                columns: new[] { "EventQuestId", "CharacterId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventQuestMilestoneClaims");
        }
    }
}
