using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddGuildExpansion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildBuildingUpgrade");

            migrationBuilder.AddColumn<int>(
                name: "GuildLevel",
                table: "Guilds",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<long>(
                name: "GuildXp",
                table: "Guilds",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "GuildFavor",
                table: "Entities",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "GuildHonors",
                table: "Entities",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GuildActivityLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: true),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildActivityLogs_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuildBuildings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    TargetLevel = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletesAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildBuildings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildBuildings_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuildContributionLedgers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    Metric = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    ContextId = table.Column<string>(type: "text", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "text", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildContributionLedgers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildMemberContributionPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodType = table.Column<int>(type: "integer", nullable: false),
                    PeriodKey = table.Column<string>(type: "text", nullable: false),
                    ContributionScore = table.Column<long>(type: "bigint", nullable: false),
                    GuildFavorEarned = table.Column<long>(type: "bigint", nullable: false),
                    GuildXpGenerated = table.Column<long>(type: "bigint", nullable: false),
                    GuildSuppliesGenerated = table.Column<long>(type: "bigint", nullable: false),
                    OrdersCompleted = table.Column<int>(type: "integer", nullable: false),
                    WeeklyMissionContribution = table.Column<long>(type: "bigint", nullable: false),
                    LastContributedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildMemberContributionPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildMissionInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    MissionDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekKey = table.Column<string>(type: "text", nullable: false),
                    TargetAmount = table.Column<long>(type: "bigint", nullable: false),
                    CurrentAmount = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RewardClaimDeadline = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildMissionInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildMissionInstances_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuildMissionOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    MissionDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekKey = table.Column<string>(type: "text", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsSelected = table.Column<bool>(type: "boolean", nullable: false),
                    SelectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SelectedByCharacterId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildMissionOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildMissionOptions_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuildShopPurchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShopItemKey = table.Column<string>(type: "text", nullable: false),
                    StockType = table.Column<int>(type: "integer", nullable: false),
                    PeriodKey = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    PurchasedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildShopPurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildShopPurchases_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonalGuildOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    MissionDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodType = table.Column<int>(type: "integer", nullable: false),
                    PeriodKey = table.Column<string>(type: "text", nullable: false),
                    TargetAmount = table.Column<long>(type: "bigint", nullable: false),
                    CurrentAmount = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RewardClaimedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalGuildOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonalGuildOrders_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuildMissionContributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildMissionInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    ContributionTier = table.Column<int>(type: "integer", nullable: false),
                    LastContributedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RewardClaimedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildMissionContributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildMissionContributions_GuildMissionInstances_GuildMissio~",
                        column: x => x.GuildMissionInstanceId,
                        principalTable: "GuildMissionInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuildActivityLogs_GuildId_CreatedAt",
                table: "GuildActivityLogs",
                columns: new[] { "GuildId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GuildBuildings_GuildId_Type",
                table: "GuildBuildings",
                columns: new[] { "GuildId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildContributionLedgers_GuildId_CharacterId_OccurredAt",
                table: "GuildContributionLedgers",
                columns: new[] { "GuildId", "CharacterId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GuildContributionLedgers_IdempotencyKey",
                table: "GuildContributionLedgers",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildMemberContributionPeriods_GuildId_CharacterId_PeriodTy~",
                table: "GuildMemberContributionPeriods",
                columns: new[] { "GuildId", "CharacterId", "PeriodType", "PeriodKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildMemberContributionPeriods_LastContributedAt",
                table: "GuildMemberContributionPeriods",
                column: "LastContributedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GuildMissionContributions_GuildMissionInstanceId_CharacterId",
                table: "GuildMissionContributions",
                columns: new[] { "GuildMissionInstanceId", "CharacterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildMissionInstances_GuildId_WeekKey",
                table: "GuildMissionInstances",
                columns: new[] { "GuildId", "WeekKey" });

            migrationBuilder.CreateIndex(
                name: "IX_GuildMissionOptions_GuildId_WeekKey",
                table: "GuildMissionOptions",
                columns: new[] { "GuildId", "WeekKey" });

            migrationBuilder.CreateIndex(
                name: "IX_GuildMissionOptions_GuildId_WeekKey_IsSelected",
                table: "GuildMissionOptions",
                columns: new[] { "GuildId", "WeekKey", "IsSelected" });

            migrationBuilder.CreateIndex(
                name: "IX_GuildShopPurchases_GuildId_CharacterId_ShopItemKey_PeriodKey",
                table: "GuildShopPurchases",
                columns: new[] { "GuildId", "CharacterId", "ShopItemKey", "PeriodKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonalGuildOrders_GuildId_CharacterId_PeriodType_PeriodKey",
                table: "PersonalGuildOrders",
                columns: new[] { "GuildId", "CharacterId", "PeriodType", "PeriodKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildActivityLogs");

            migrationBuilder.DropTable(
                name: "GuildBuildings");

            migrationBuilder.DropTable(
                name: "GuildContributionLedgers");

            migrationBuilder.DropTable(
                name: "GuildMemberContributionPeriods");

            migrationBuilder.DropTable(
                name: "GuildMissionContributions");

            migrationBuilder.DropTable(
                name: "GuildMissionOptions");

            migrationBuilder.DropTable(
                name: "GuildShopPurchases");

            migrationBuilder.DropTable(
                name: "PersonalGuildOrders");

            migrationBuilder.DropTable(
                name: "GuildMissionInstances");

            migrationBuilder.DropColumn(
                name: "GuildLevel",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "GuildXp",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "GuildFavor",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "GuildHonors",
                table: "Entities");

            migrationBuilder.CreateTable(
                name: "GuildBuildingUpgrade",
                columns: table => new
                {
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuildingUpgradeDefinitionId = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildBuildingUpgrade", x => new { x.GuildId, x.BuildingUpgradeDefinitionId });
                    table.ForeignKey(
                        name: "FK_GuildBuildingUpgrade_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
