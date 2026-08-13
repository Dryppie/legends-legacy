using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeTournamentGroundsPlayback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PlaybackEndsAtUtc",
                table: "TournamentMatches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PlaybackStartedAtUtc",
                table: "TournamentMatches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ScheduledAtUtc",
                table: "TournamentMatches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CombatResultJson",
                table: "TournamentCombatReplays",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AddColumn<string>(
                name: "BundleContentEncoding",
                table: "TournamentCombatReplays",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BundleContentType",
                table: "TournamentCombatReplays",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BundleHash",
                table: "TournamentCombatReplays",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BundleLength",
                table: "TournamentCombatReplays",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FrameCount",
                table: "TournamentCombatReplays",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SchemaVersion",
                table: "TournamentCombatReplays",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TicksPerFrame",
                table: "TournamentCombatReplays",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TicksPerSecond",
                table: "TournamentCombatReplays",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TournamentCombatReplayArtifacts",
                columns: table => new
                {
                    TournamentCombatReplayId = table.Column<Guid>(type: "uuid", nullable: false),
                    BundleBytes = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentCombatReplayArtifacts", x => x.TournamentCombatReplayId);
                    table.ForeignKey(
                        name: "FK_TournamentCombatReplayArtifacts_TournamentCombatReplays_Tou~",
                        column: x => x.TournamentCombatReplayId,
                        principalTable: "TournamentCombatReplays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_Status_PlaybackEndsAtUtc",
                table: "TournamentMatches",
                columns: new[] { "Status", "PlaybackEndsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_TournamentId_ScheduledAtUtc",
                table: "TournamentMatches",
                columns: new[] { "TournamentId", "ScheduledAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TournamentCombatReplayArtifacts");

            migrationBuilder.DropIndex(
                name: "IX_TournamentMatches_Status_PlaybackEndsAtUtc",
                table: "TournamentMatches");

            migrationBuilder.DropIndex(
                name: "IX_TournamentMatches_TournamentId_ScheduledAtUtc",
                table: "TournamentMatches");

            migrationBuilder.DropColumn(
                name: "PlaybackEndsAtUtc",
                table: "TournamentMatches");

            migrationBuilder.DropColumn(
                name: "PlaybackStartedAtUtc",
                table: "TournamentMatches");

            migrationBuilder.DropColumn(
                name: "ScheduledAtUtc",
                table: "TournamentMatches");

            migrationBuilder.DropColumn(
                name: "BundleContentEncoding",
                table: "TournamentCombatReplays");

            migrationBuilder.DropColumn(
                name: "BundleContentType",
                table: "TournamentCombatReplays");

            migrationBuilder.DropColumn(
                name: "BundleHash",
                table: "TournamentCombatReplays");

            migrationBuilder.DropColumn(
                name: "BundleLength",
                table: "TournamentCombatReplays");

            migrationBuilder.DropColumn(
                name: "FrameCount",
                table: "TournamentCombatReplays");

            migrationBuilder.DropColumn(
                name: "SchemaVersion",
                table: "TournamentCombatReplays");

            migrationBuilder.DropColumn(
                name: "TicksPerFrame",
                table: "TournamentCombatReplays");

            migrationBuilder.DropColumn(
                name: "TicksPerSecond",
                table: "TournamentCombatReplays");

            migrationBuilder.AlterColumn<string>(
                name: "CombatResultJson",
                table: "TournamentCombatReplays",
                type: "jsonb",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);
        }
    }
}
