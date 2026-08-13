using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeWorldTowerCombatProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TowerCombatPlaybacks_PlaybackEndsAt_LastPublishedSequence",
                table: "TowerCombatPlaybacks");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextFrameDueAt",
                table: "TowerCombatPlaybacks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.Sql(
                """
                UPDATE "TowerCombatPlaybacks" AS playback
                SET "NextFrameDueAt" = CASE
                    WHEN attempt."Status" = 4 THEN playback."PlaybackStartedAt"
                    ELSE TIMESTAMPTZ '9999-12-31 23:59:59.999999+00'
                END
                FROM "TowerAttempts" AS attempt
                WHERE attempt."Id" = playback."TowerAttemptId";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_TowerCombatPlaybacks_NextFrameDueAt_LastPublishedSequence",
                table: "TowerCombatPlaybacks",
                columns: new[] { "NextFrameDueAt", "LastPublishedSequence" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TowerCombatPlaybacks_NextFrameDueAt_LastPublishedSequence",
                table: "TowerCombatPlaybacks");

            migrationBuilder.DropColumn(
                name: "NextFrameDueAt",
                table: "TowerCombatPlaybacks");

            migrationBuilder.CreateIndex(
                name: "IX_TowerCombatPlaybacks_PlaybackEndsAt_LastPublishedSequence",
                table: "TowerCombatPlaybacks",
                columns: new[] { "PlaybackEndsAt", "LastPublishedSequence" });
        }
    }
}
