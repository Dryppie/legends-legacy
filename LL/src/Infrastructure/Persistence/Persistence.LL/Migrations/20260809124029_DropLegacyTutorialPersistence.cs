using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class DropLegacyTutorialPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterTutorialProgresses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterTutorialProgresses",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    TutorialId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletionRewardGranted = table.Column<bool>(type: "boolean", nullable: false),
                    CraftedTierOneEquipmentCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CurrentStep = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EquippedTierOneEquipmentCount = table.Column<int>(type: "integer", nullable: false),
                    EssenceAbsorbedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EssenceEquippedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TrainingCombatWonAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TrainingEssenceRewardGranted = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    WelcomeAcknowledgedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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
    }
}
