using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddLeanTelemetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountActivityDays",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityDateUtc = table.Column<DateOnly>(type: "date", nullable: false),
                    FirstSeenAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountActivityDays", x => new { x.AccountId, x.ActivityDateUtc });
                    table.ForeignKey(
                        name: "FK_AccountActivityDays_Users_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyTelemetryReports",
                columns: table => new
                {
                    ReportDateUtc = table.Column<DateOnly>(type: "date", nullable: false),
                    GeneratedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyTelemetryReports", x => x.ReportDateUtc);
                });

            migrationBuilder.CreateTable(
                name: "DungeonAttemptHistories",
                columns: table => new
                {
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    DungeonDefinitionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinishedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Outcome = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DungeonAttemptHistories", x => x.RunId);
                    table.ForeignKey(
                        name: "FK_DungeonAttemptHistories_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedUtc",
                table: "Users",
                column: "CreatedUtc")
                .Annotation("Npgsql:CreatedConcurrently", true);

            migrationBuilder.CreateIndex(
                name: "IX_TowerAttempts_CompletedAt",
                table: "TowerAttempts",
                column: "CompletedAt")
                .Annotation("Npgsql:CreatedConcurrently", true);

            migrationBuilder.CreateIndex(
                name: "IX_TowerAttempts_StartedAt",
                table: "TowerAttempts",
                column: "StartedAt")
                .Annotation("Npgsql:CreatedConcurrently", true);

            migrationBuilder.CreateIndex(
                name: "IX_RegionBossRuns_ResolvedAtUtc",
                table: "RegionBossRuns",
                column: "ResolvedAtUtc")
                .Annotation("Npgsql:CreatedConcurrently", true);

            migrationBuilder.CreateIndex(
                name: "IX_RegionBossRuns_StartedAtUtc",
                table: "RegionBossRuns",
                column: "StartedAtUtc")
                .Annotation("Npgsql:CreatedConcurrently", true);

            migrationBuilder.CreateIndex(
                name: "IX_RaidRuns_CommencedAt",
                table: "RaidRuns",
                column: "CommencedAt")
                .Annotation("Npgsql:CreatedConcurrently", true);

            migrationBuilder.CreateIndex(
                name: "IX_RaidRuns_ResolvedAt",
                table: "RaidRuns",
                column: "ResolvedAt")
                .Annotation("Npgsql:CreatedConcurrently", true);

            migrationBuilder.CreateIndex(
                name: "IX_ColosseumMatches_PlayedAt",
                table: "ColosseumMatches",
                column: "PlayedAt")
                .Annotation("Npgsql:CreatedConcurrently", true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountActivityDays_ActivityDateUtc",
                table: "AccountActivityDays",
                column: "ActivityDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DungeonAttemptHistories_CharacterId",
                table: "DungeonAttemptHistories",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_DungeonAttemptHistories_FinishedAtUtc",
                table: "DungeonAttemptHistories",
                column: "FinishedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DungeonAttemptHistories_StartedAtUtc",
                table: "DungeonAttemptHistories",
                column: "StartedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountActivityDays");

            migrationBuilder.DropTable(
                name: "DailyTelemetryReports");

            migrationBuilder.DropTable(
                name: "DungeonAttemptHistories");

            migrationBuilder.DropIndex(
                name: "IX_Users_CreatedUtc",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_TowerAttempts_CompletedAt",
                table: "TowerAttempts");

            migrationBuilder.DropIndex(
                name: "IX_TowerAttempts_StartedAt",
                table: "TowerAttempts");

            migrationBuilder.DropIndex(
                name: "IX_RegionBossRuns_ResolvedAtUtc",
                table: "RegionBossRuns");

            migrationBuilder.DropIndex(
                name: "IX_RegionBossRuns_StartedAtUtc",
                table: "RegionBossRuns");

            migrationBuilder.DropIndex(
                name: "IX_RaidRuns_CommencedAt",
                table: "RaidRuns");

            migrationBuilder.DropIndex(
                name: "IX_RaidRuns_ResolvedAt",
                table: "RaidRuns");

            migrationBuilder.DropIndex(
                name: "IX_ColosseumMatches_PlayedAt",
                table: "ColosseumMatches");
        }
    }
}
