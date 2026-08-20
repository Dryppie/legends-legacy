using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddRaidPlaybackTimeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PlaybackEndsAt",
                table: "RaidRuns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PlaybackStartedAt",
                table: "RaidRuns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RaidRuns_Status_PlaybackEndsAt",
                table: "RaidRuns",
                columns: new[] { "Status", "PlaybackEndsAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RaidRuns_Status_PlaybackEndsAt",
                table: "RaidRuns");

            migrationBuilder.DropColumn(
                name: "PlaybackEndsAt",
                table: "RaidRuns");

            migrationBuilder.DropColumn(
                name: "PlaybackStartedAt",
                table: "RaidRuns");
        }
    }
}
