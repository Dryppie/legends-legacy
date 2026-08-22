using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddRegionBossSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegionBossEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegionBossDefinitionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RegionId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SignupStartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SignupClosesAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EncounterStartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PlaybackStartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PlaybackEndsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DefinitionHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DefinitionSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    MatchmakingAlgorithmVersion = table.Column<int>(type: "integer", nullable: false),
                    CombatRulesVersion = table.Column<int>(type: "integer", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegionBossEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegionBossRewardGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegionBossEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegionBossRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegionBossDefinitionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    RewardKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    MilestoneLevel = table.Column<int>(type: "integer", nullable: false),
                    RewardSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClaimedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegionBossRewardGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegionBossRewardGrants_RegionBossEvents_RegionBossEventId",
                        column: x => x.RegionBossEventId,
                        principalTable: "RegionBossEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegionBossRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegionBossEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyNumber = table.Column<int>(type: "integer", nullable: false),
                    PartySize = table.Column<int>(type: "integer", nullable: false),
                    MatchmakingBand = table.Column<int>(type: "integer", nullable: false),
                    PartySizeScalingVersion = table.Column<int>(type: "integer", nullable: false),
                    RandomSeed = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PlaybackStartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PlaybackEndsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    HighestLevelDefeated = table.Column<int>(type: "integer", nullable: false),
                    CurrentBossLevel = table.Column<int>(type: "integer", nullable: false),
                    CurrentBossMaxHealth = table.Column<int>(type: "integer", nullable: false),
                    CurrentBossHealthRemaining = table.Column<int>(type: "integer", nullable: false),
                    CurrentBossProgressBasisPoints = table.Column<int>(type: "integer", nullable: false),
                    DurationTicks = table.Column<int>(type: "integer", nullable: false),
                    FuryStacksAtEnd = table.Column<int>(type: "integer", nullable: false),
                    TerminationReason = table.Column<int>(type: "integer", nullable: true),
                    SimulationLeaseOwner = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SimulationLeaseUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SimulationAttempts = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegionBossRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegionBossRuns_RegionBossEvents_RegionBossEventId",
                        column: x => x.RegionBossEventId,
                        principalTable: "RegionBossEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegionBossParticipantResults",
                columns: table => new
                {
                    RegionBossRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    DamageDone = table.Column<int>(type: "integer", nullable: false),
                    DamageTaken = table.Column<int>(type: "integer", nullable: false),
                    HealingDone = table.Column<int>(type: "integer", nullable: false),
                    HealingReceived = table.Column<int>(type: "integer", nullable: false),
                    BarrierGenerated = table.Column<int>(type: "integer", nullable: false),
                    DamagePrevented = table.Column<int>(type: "integer", nullable: false),
                    ThreatGenerated = table.Column<int>(type: "integer", nullable: false),
                    Deaths = table.Column<int>(type: "integer", nullable: false),
                    Revivals = table.Column<int>(type: "integer", nullable: false),
                    DownedTicks = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegionBossParticipantResults", x => new { x.RegionBossRunId, x.CharacterId });
                    table.ForeignKey(
                        name: "FK_RegionBossParticipantResults_RegionBossRuns_RegionBossRunId",
                        column: x => x.RegionBossRunId,
                        principalTable: "RegionBossRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegionBossPlaybacks",
                columns: table => new
                {
                    RegionBossRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    TicksPerSecond = table.Column<int>(type: "integer", nullable: false),
                    TicksPerFrame = table.Column<int>(type: "integer", nullable: false),
                    TotalTicks = table.Column<int>(type: "integer", nullable: false),
                    FrameCount = table.Column<int>(type: "integer", nullable: false),
                    BundleHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BundleLength = table.Column<int>(type: "integer", nullable: false),
                    BundleContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    BundleContentEncoding = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegionBossPlaybacks", x => x.RegionBossRunId);
                    table.ForeignKey(
                        name: "FK_RegionBossPlaybacks_RegionBossRuns_RegionBossRunId",
                        column: x => x.RegionBossRunId,
                        principalTable: "RegionBossRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegionBossSignups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegionBossEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CharacterSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoadoutHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PowerRating = table.Column<int>(type: "integer", nullable: false),
                    PowerRatingAlgorithmVersion = table.Column<int>(type: "integer", nullable: false),
                    BuildFingerprint = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RegionBossRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    PartySlot = table.Column<int>(type: "integer", nullable: true),
                    SignedUpAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SnapshotRefreshedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegionBossSignups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegionBossSignups_CharacterSnapshots_CharacterSnapshotId",
                        column: x => x.CharacterSnapshotId,
                        principalTable: "CharacterSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegionBossSignups_RegionBossEvents_RegionBossEventId",
                        column: x => x.RegionBossEventId,
                        principalTable: "RegionBossEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegionBossSignups_RegionBossRuns_RegionBossRunId",
                        column: x => x.RegionBossRunId,
                        principalTable: "RegionBossRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RegionBossPlaybackArtifacts",
                columns: table => new
                {
                    RegionBossRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    BundleBytes = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegionBossPlaybackArtifacts", x => x.RegionBossRunId);
                    table.ForeignKey(
                        name: "FK_RegionBossPlaybackArtifacts_RegionBossPlaybacks_RegionBossR~",
                        column: x => x.RegionBossRunId,
                        principalTable: "RegionBossPlaybacks",
                        principalColumn: "RegionBossRunId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegionBossEvents_RegionBossDefinitionId_SignupStartsAtUtc",
                table: "RegionBossEvents",
                columns: new[] { "RegionBossDefinitionId", "SignupStartsAtUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegionBossEvents_Status_PlaybackEndsAtUtc",
                table: "RegionBossEvents",
                columns: new[] { "Status", "PlaybackEndsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RegionBossEvents_Status_SignupClosesAtUtc",
                table: "RegionBossEvents",
                columns: new[] { "Status", "SignupClosesAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RegionBossEvents_Status_SignupStartsAtUtc",
                table: "RegionBossEvents",
                columns: new[] { "Status", "SignupStartsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RegionBossRewardGrants_CharacterId_Status",
                table: "RegionBossRewardGrants",
                columns: new[] { "CharacterId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RegionBossRewardGrants_RegionBossEventId_CharacterId_Reward~",
                table: "RegionBossRewardGrants",
                columns: new[] { "RegionBossEventId", "CharacterId", "RewardKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegionBossRuns_RegionBossEventId_HighestLevelDefeated_Curre~",
                table: "RegionBossRuns",
                columns: new[] { "RegionBossEventId", "HighestLevelDefeated", "CurrentBossProgressBasisPoints" });

            migrationBuilder.CreateIndex(
                name: "IX_RegionBossRuns_RegionBossEventId_PartyNumber",
                table: "RegionBossRuns",
                columns: new[] { "RegionBossEventId", "PartyNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegionBossRuns_Status_SimulationLeaseUntil",
                table: "RegionBossRuns",
                columns: new[] { "Status", "SimulationLeaseUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_RegionBossSignups_CharacterId",
                table: "RegionBossSignups",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_RegionBossSignups_CharacterSnapshotId",
                table: "RegionBossSignups",
                column: "CharacterSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_RegionBossSignups_RegionBossEventId_AccountId",
                table: "RegionBossSignups",
                columns: new[] { "RegionBossEventId", "AccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegionBossSignups_RegionBossEventId_CharacterId",
                table: "RegionBossSignups",
                columns: new[] { "RegionBossEventId", "CharacterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegionBossSignups_RegionBossRunId_PartySlot",
                table: "RegionBossSignups",
                columns: new[] { "RegionBossRunId", "PartySlot" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegionBossParticipantResults");

            migrationBuilder.DropTable(
                name: "RegionBossPlaybackArtifacts");

            migrationBuilder.DropTable(
                name: "RegionBossRewardGrants");

            migrationBuilder.DropTable(
                name: "RegionBossSignups");

            migrationBuilder.DropTable(
                name: "RegionBossPlaybacks");

            migrationBuilder.DropTable(
                name: "RegionBossRuns");

            migrationBuilder.DropTable(
                name: "RegionBossEvents");
        }
    }
}
