using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddRaidSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "RaidTrophies",
                table: "Entities",
                type: "bigint",
                nullable: true,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "RaidRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RaidBossId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    DefinitionHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DefinitionSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    LeaderCharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SignupClosesAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CommencedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SettledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    WeekKey = table.Column<int>(type: "integer", nullable: false),
                    ReinforcementPenalty = table.Column<decimal>(type: "numeric(8,6)", precision: 8, scale: 6, nullable: true),
                    WardBreak = table.Column<decimal>(type: "numeric(8,6)", precision: 8, scale: 6, nullable: true),
                    BossHealthRemainingPercent = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    Outcome = table.Column<int>(type: "integer", nullable: true),
                    SimulationLeaseOwner = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SimulationLeaseUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SimulationAttempts = table.Column<int>(type: "integer", nullable: false),
                    WarhornRefunded = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RaidLaneResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RaidRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Lane = table.Column<int>(type: "integer", nullable: false),
                    Seed = table.Column<int>(type: "integer", nullable: false),
                    DurationTicks = table.Column<int>(type: "integer", nullable: false),
                    BattleOutcome = table.Column<int>(type: "integer", nullable: false),
                    TotalFriendlyDamage = table.Column<long>(type: "bigint", nullable: false),
                    ObjectiveDamage = table.Column<long>(type: "bigint", nullable: false),
                    ObjectiveBarrierAbsorbed = table.Column<long>(type: "bigint", nullable: false),
                    SurvivingHostileHealthFraction = table.Column<decimal>(type: "numeric(8,6)", precision: 8, scale: 6, nullable: false),
                    DerivedModifier = table.Column<decimal>(type: "numeric(8,6)", precision: 8, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidLaneResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaidLaneResults_RaidRuns_RaidRunId",
                        column: x => x.RaidRunId,
                        principalTable: "RaidRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RaidParticipantResults",
                columns: table => new
                {
                    RaidRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Lane = table.Column<int>(type: "integer", nullable: false),
                    DamageDone = table.Column<long>(type: "bigint", nullable: false),
                    DeathTick = table.Column<int>(type: "integer", nullable: true),
                    ContributionScore = table.Column<decimal>(type: "numeric(12,8)", precision: 12, scale: 8, nullable: false),
                    PayoutMultiplier = table.Column<decimal>(type: "numeric(8,6)", precision: 8, scale: 6, nullable: false),
                    ContributionRank = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidParticipantResults", x => new { x.RaidRunId, x.CharacterId });
                    table.ForeignKey(
                        name: "FK_RaidParticipantResults_RaidRuns_RaidRunId",
                        column: x => x.RaidRunId,
                        principalTable: "RaidRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RaidRewardClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RaidRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    RaidBossId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekKey = table.Column<int>(type: "integer", nullable: false),
                    Trophies = table.Column<int>(type: "integer", nullable: false),
                    PendingItemsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClaimedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    WasReduced = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidRewardClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaidRewardClaims_RaidRuns_RaidRunId",
                        column: x => x.RaidRunId,
                        principalTable: "RaidRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RaidSignups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RaidRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CharacterSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoadoutHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PowerRating = table.Column<int>(type: "integer", nullable: false),
                    Lane = table.Column<int>(type: "integer", nullable: true),
                    WingSlotIndex = table.Column<int>(type: "integer", nullable: true),
                    SignedUpAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SnapshotRefreshedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidSignups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaidSignups_CharacterSnapshots_CharacterSnapshotId",
                        column: x => x.CharacterSnapshotId,
                        principalTable: "CharacterSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RaidSignups_RaidRuns_RaidRunId",
                        column: x => x.RaidRunId,
                        principalTable: "RaidRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RaidLaneResults_RaidRunId_Lane",
                table: "RaidLaneResults",
                columns: new[] { "RaidRunId", "Lane" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RaidRewardClaims_RaidBossId_CharacterId_WeekKey",
                table: "RaidRewardClaims",
                columns: new[] { "RaidBossId", "CharacterId", "WeekKey" },
                unique: true,
                filter: "\"WasReduced\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_RaidRewardClaims_RaidRunId_CharacterId",
                table: "RaidRewardClaims",
                columns: new[] { "RaidRunId", "CharacterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RaidRuns_LeaderCharacterId_Status",
                table: "RaidRuns",
                columns: new[] { "LeaderCharacterId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RaidRuns_RaidBossId_Status_SignupClosesAt",
                table: "RaidRuns",
                columns: new[] { "RaidBossId", "Status", "SignupClosesAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RaidRuns_Status_SimulationLeaseUntil",
                table: "RaidRuns",
                columns: new[] { "Status", "SimulationLeaseUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_RaidSignups_CharacterId",
                table: "RaidSignups",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidSignups_CharacterSnapshotId",
                table: "RaidSignups",
                column: "CharacterSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidSignups_RaidRunId_AccountId",
                table: "RaidSignups",
                columns: new[] { "RaidRunId", "AccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RaidSignups_RaidRunId_CharacterId",
                table: "RaidSignups",
                columns: new[] { "RaidRunId", "CharacterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RaidSignups_RaidRunId_Lane_WingSlotIndex",
                table: "RaidSignups",
                columns: new[] { "RaidRunId", "Lane", "WingSlotIndex" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RaidLaneResults");

            migrationBuilder.DropTable(
                name: "RaidParticipantResults");

            migrationBuilder.DropTable(
                name: "RaidRewardClaims");

            migrationBuilder.DropTable(
                name: "RaidSignups");

            migrationBuilder.DropTable(
                name: "RaidRuns");

            migrationBuilder.DropColumn(
                name: "RaidTrophies",
                table: "Entities");
        }
    }
}
