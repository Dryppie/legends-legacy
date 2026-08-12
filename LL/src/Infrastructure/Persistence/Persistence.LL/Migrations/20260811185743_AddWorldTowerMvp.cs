using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddWorldTowerMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServerUnlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UnlockKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SourceSystem = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourceFloorNumber = table.Column<int>(type: "integer", nullable: true),
                    UnlockedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerUnlocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TowerContributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FloorNumber = table.Column<int>(type: "integer", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    WeekKey = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TowerContributions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TowerEchoClears",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FloorNumber = table.Column<int>(type: "integer", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekKey = table.Column<int>(type: "integer", nullable: false),
                    ClearedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TowerEchoClears", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TowerRallies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FloorNumber = table.Column<int>(type: "integer", nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedByCharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiredSlots = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TowerRallies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TowerAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TowerRallyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FloorNumber = table.Column<int>(type: "integer", nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Succeeded = table.Column<bool>(type: "boolean", nullable: false),
                    AttemptNumberForFloor = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FightDurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    CombatResultJson = table.Column<string>(type: "text", nullable: true),
                    BattleReportJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TowerAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TowerAttempts_TowerRallies_TowerRallyId",
                        column: x => x.TowerRallyId,
                        principalTable: "TowerRallies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TowerRallyParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TowerRallyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: true),
                    GuildName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PowerRating = table.Column<int>(type: "integer", nullable: false),
                    CharacterSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TowerRallyParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TowerRallyParticipants_CharacterSnapshots_CharacterSnapshot~",
                        column: x => x.CharacterSnapshotId,
                        principalTable: "CharacterSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TowerRallyParticipants_TowerRallies_TowerRallyId",
                        column: x => x.TowerRallyId,
                        principalTable: "TowerRallies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TowerFloorProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FloorNumber = table.Column<int>(type: "integer", nullable: false),
                    IsCleared = table.Column<bool>(type: "boolean", nullable: false),
                    ScoutingProgress = table.Column<int>(type: "integer", nullable: false),
                    FirstClearAttemptId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnlockedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClearedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TowerFloorProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TowerFloorProgresses_TowerAttempts_FirstClearAttemptId",
                        column: x => x.FirstClearAttemptId,
                        principalTable: "TowerAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServerUnlocks_ServerId_UnlockKey",
                table: "ServerUnlocks",
                columns: new[] { "ServerId", "UnlockKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TowerAttempts_ServerId_FloorNumber_Mode_Succeeded",
                table: "TowerAttempts",
                columns: new[] { "ServerId", "FloorNumber", "Mode", "Succeeded" });

            migrationBuilder.CreateIndex(
                name: "IX_TowerAttempts_TowerRallyId",
                table: "TowerAttempts",
                column: "TowerRallyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TowerContributions_CharacterId_FloorNumber_WeekKey",
                table: "TowerContributions",
                columns: new[] { "CharacterId", "FloorNumber", "WeekKey" });

            migrationBuilder.CreateIndex(
                name: "IX_TowerContributions_ServerId_FloorNumber_WeekKey",
                table: "TowerContributions",
                columns: new[] { "ServerId", "FloorNumber", "WeekKey" });

            migrationBuilder.CreateIndex(
                name: "IX_TowerEchoClears_ServerId_FloorNumber_CharacterId_WeekKey",
                table: "TowerEchoClears",
                columns: new[] { "ServerId", "FloorNumber", "CharacterId", "WeekKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TowerFloorProgresses_FirstClearAttemptId",
                table: "TowerFloorProgresses",
                column: "FirstClearAttemptId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TowerFloorProgresses_ServerId_FloorNumber",
                table: "TowerFloorProgresses",
                columns: new[] { "ServerId", "FloorNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TowerRallies_ServerId_FloorNumber_Mode_Status",
                table: "TowerRallies",
                columns: new[] { "ServerId", "FloorNumber", "Mode", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TowerRallyParticipants_CharacterId",
                table: "TowerRallyParticipants",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_TowerRallyParticipants_CharacterSnapshotId",
                table: "TowerRallyParticipants",
                column: "CharacterSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_TowerRallyParticipants_TowerRallyId_AccountId",
                table: "TowerRallyParticipants",
                columns: new[] { "TowerRallyId", "AccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TowerRallyParticipants_TowerRallyId_CharacterId",
                table: "TowerRallyParticipants",
                columns: new[] { "TowerRallyId", "CharacterId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServerUnlocks");

            migrationBuilder.DropTable(
                name: "TowerContributions");

            migrationBuilder.DropTable(
                name: "TowerEchoClears");

            migrationBuilder.DropTable(
                name: "TowerFloorProgresses");

            migrationBuilder.DropTable(
                name: "TowerRallyParticipants");

            migrationBuilder.DropTable(
                name: "TowerAttempts");

            migrationBuilder.DropTable(
                name: "TowerRallies");
        }
    }
}
