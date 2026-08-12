using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddWorldTowerCombatPlayback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TowerCombatPlaybacks",
                columns: table => new
                {
                    TowerAttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    TicksPerSecond = table.Column<int>(type: "integer", nullable: false),
                    TicksPerFrame = table.Column<int>(type: "integer", nullable: false),
                    TotalTicks = table.Column<int>(type: "integer", nullable: false),
                    FrameCount = table.Column<int>(type: "integer", nullable: false),
                    TimelineJson = table.Column<string>(type: "jsonb", nullable: false),
                    SimulationCompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PlaybackStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PlaybackEndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastPublishedSequence = table.Column<int>(type: "integer", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TowerCombatPlaybacks", x => x.TowerAttemptId);
                    table.ForeignKey(
                        name: "FK_TowerCombatPlaybacks_TowerAttempts_TowerAttemptId",
                        column: x => x.TowerAttemptId,
                        principalTable: "TowerAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TowerCombatPlaybacks_PlaybackEndsAt_LastPublishedSequence",
                table: "TowerCombatPlaybacks",
                columns: new[] { "PlaybackEndsAt", "LastPublishedSequence" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TowerCombatPlaybacks");
        }
    }
}
