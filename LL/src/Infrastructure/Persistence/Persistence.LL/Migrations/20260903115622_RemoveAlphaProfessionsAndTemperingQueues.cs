using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAlphaProfessionsAndTemperingQueues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AreaGatheringNode");

            migrationBuilder.DropTable(
                name: "CharacterRecipeMasteries");

            migrationBuilder.DropTable(
                name: "CharacterRecipeUnlocks");

            migrationBuilder.DropTable(
                name: "CraftingQueueItems");

            migrationBuilder.DropTable(
                name: "Professions");

            migrationBuilder.DropColumn(
                name: "DeliveredTemperedScrap",
                table: "TournamentRewardGrants");

            migrationBuilder.DropColumn(
                name: "ReturnToCombatAreaId",
                table: "CharacterActions");

            migrationBuilder.RenameColumn(
                name: "CatalystSelectionCaches",
                table: "TournamentRewardGrants",
                newName: "TemperedScrap");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TemperedScrap",
                table: "TournamentRewardGrants",
                newName: "CatalystSelectionCaches");

            migrationBuilder.AddColumn<int>(
                name: "DeliveredTemperedScrap",
                table: "TournamentRewardGrants",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnToCombatAreaId",
                table: "CharacterActions",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AreaGatheringNode",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AreaId = table.Column<string>(type: "text", nullable: false),
                    LevelRequirement = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ProcChance = table.Column<float>(type: "real", nullable: false),
                    RewardTableId = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    YieldBonusPercent = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AreaGatheringNode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AreaGatheringNode_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterRecipeMasteries",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Experience = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterRecipeMasteries", x => new { x.CharacterId, x.RecipeId });
                });

            migrationBuilder.CreateTable(
                name: "CharacterRecipeUnlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlueprintId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UnlockedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterRecipeUnlocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CraftingQueueItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CraftType = table.Column<int>(type: "integer", nullable: false),
                    CraftingActionDetailsId = table.Column<Guid>(type: "uuid", nullable: true),
                    PausedForCharacterId = table.Column<Guid>(type: "uuid", nullable: true),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    RemoveAfterNextRarityUpgrade = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CraftingQueueItems", x => x.Id);
                    table.CheckConstraint("CK_CraftingQueueItems_ActiveOrPaused", "(\"CraftingActionDetailsId\" IS NOT NULL AND \"PausedForCharacterId\" IS NULL) OR (\"CraftingActionDetailsId\" IS NULL AND \"PausedForCharacterId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_CraftingQueueItems_ActionDetails_CraftingActionDetailsId",
                        column: x => x.CraftingActionDetailsId,
                        principalTable: "ActionDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CraftingQueueItems_Entities_PausedForCharacterId",
                        column: x => x.PausedForCharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CraftingQueueItems_ItemInstances_EquipmentInstanceId",
                        column: x => x.EquipmentInstanceId,
                        principalTable: "ItemInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Professions",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfessionType = table.Column<int>(type: "integer", nullable: false),
                    Experience = table.Column<float>(type: "real", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Professions", x => new { x.CharacterId, x.ProfessionType });
                    table.ForeignKey(
                        name: "FK_Professions_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AreaGatheringNode_AreaId",
                table: "AreaGatheringNode",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRecipeUnlocks_CharacterId_RecipeId_BlueprintId",
                table: "CharacterRecipeUnlocks",
                columns: new[] { "CharacterId", "RecipeId", "BlueprintId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CraftingQueueItems_CraftingActionDetailsId_Position",
                table: "CraftingQueueItems",
                columns: new[] { "CraftingActionDetailsId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_CraftingQueueItems_EquipmentInstanceId",
                table: "CraftingQueueItems",
                column: "EquipmentInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_CraftingQueueItems_PausedForCharacterId_Position",
                table: "CraftingQueueItems",
                columns: new[] { "PausedForCharacterId", "Position" });
        }
    }
}
