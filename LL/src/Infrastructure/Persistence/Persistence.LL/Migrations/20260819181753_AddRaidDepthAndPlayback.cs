using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddRaidDepthAndPlayback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PlaybackId",
                table: "RaidLaneResults",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RaidPlaybacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RaidRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Lane = table.Column<int>(type: "integer", nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    TicksPerSecond = table.Column<int>(type: "integer", nullable: false),
                    TicksPerFrame = table.Column<int>(type: "integer", nullable: false),
                    TotalTicks = table.Column<int>(type: "integer", nullable: false),
                    FrameCount = table.Column<int>(type: "integer", nullable: false),
                    BundleHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BundleLength = table.Column<int>(type: "integer", nullable: false),
                    BundleContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    BundleContentEncoding = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidPlaybacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaidPlaybacks_RaidRuns_RaidRunId",
                        column: x => x.RaidRunId,
                        principalTable: "RaidRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RaidPowerRecommendationCacheEntries",
                columns: table => new
                {
                    RaidBossId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    DefinitionHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RaidRulesVersion = table.Column<int>(type: "integer", nullable: false),
                    PowerRatingAlgorithmVersion = table.Column<int>(type: "integer", nullable: false),
                    CombatRulesVersion = table.Column<int>(type: "integer", nullable: false),
                    EquipmentBalanceVersion = table.Column<int>(type: "integer", nullable: false),
                    SeedSetVersion = table.Column<int>(type: "integer", nullable: false),
                    RecommendationJson = table.Column<string>(type: "jsonb", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidPowerRecommendationCacheEntries", x => new { x.RaidBossId, x.Tier });
                });

            migrationBuilder.CreateTable(
                name: "RaidTrophyPurchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    RaidBossId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    VendorItemId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    TrophiesSpent = table.Column<int>(type: "integer", nullable: false),
                    WeekKey = table.Column<int>(type: "integer", nullable: false),
                    PurchasedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidTrophyPurchases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RaidPlaybackArtifacts",
                columns: table => new
                {
                    RaidPlaybackId = table.Column<Guid>(type: "uuid", nullable: false),
                    BundleBytes = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidPlaybackArtifacts", x => x.RaidPlaybackId);
                    table.ForeignKey(
                        name: "FK_RaidPlaybackArtifacts_RaidPlaybacks_RaidPlaybackId",
                        column: x => x.RaidPlaybackId,
                        principalTable: "RaidPlaybacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RaidLaneResults_PlaybackId",
                table: "RaidLaneResults",
                column: "PlaybackId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RaidPlaybacks_RaidRunId_Lane",
                table: "RaidPlaybacks",
                columns: new[] { "RaidRunId", "Lane" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RaidTrophyPurchases_CharacterId_VendorItemId",
                table: "RaidTrophyPurchases",
                columns: new[] { "CharacterId", "VendorItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_RaidTrophyPurchases_CharacterId_VendorItemId_WeekKey",
                table: "RaidTrophyPurchases",
                columns: new[] { "CharacterId", "VendorItemId", "WeekKey" });

            migrationBuilder.AddForeignKey(
                name: "FK_RaidLaneResults_RaidPlaybacks_PlaybackId",
                table: "RaidLaneResults",
                column: "PlaybackId",
                principalTable: "RaidPlaybacks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RaidLaneResults_RaidPlaybacks_PlaybackId",
                table: "RaidLaneResults");

            migrationBuilder.DropTable(
                name: "RaidPlaybackArtifacts");

            migrationBuilder.DropTable(
                name: "RaidPowerRecommendationCacheEntries");

            migrationBuilder.DropTable(
                name: "RaidTrophyPurchases");

            migrationBuilder.DropTable(
                name: "RaidPlaybacks");

            migrationBuilder.DropIndex(
                name: "IX_RaidLaneResults_PlaybackId",
                table: "RaidLaneResults");

            migrationBuilder.DropColumn(
                name: "PlaybackId",
                table: "RaidLaneResults");
        }
    }
}
