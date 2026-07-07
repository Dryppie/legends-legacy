using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterTutorialProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterTutorialProgresses",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    TutorialId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CurrentStep = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CraftedTierOneEquipmentCount = table.Column<int>(type: "integer", nullable: false),
                    EquippedTierOneEquipmentCount = table.Column<int>(type: "integer", nullable: false),
                    TrainingEssenceRewardGranted = table.Column<bool>(type: "boolean", nullable: false),
                    CompletionRewardGranted = table.Column<bool>(type: "boolean", nullable: false),
                    TrainingCombatWonAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EssenceAbsorbedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EssenceEquippedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterTutorialProgresses", x => new { x.CharacterId, x.TutorialId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterTutorialProgresses_CurrentStep",
                table: "CharacterTutorialProgresses",
                column: "CurrentStep");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterTutorialProgresses");
        }
    }
}
