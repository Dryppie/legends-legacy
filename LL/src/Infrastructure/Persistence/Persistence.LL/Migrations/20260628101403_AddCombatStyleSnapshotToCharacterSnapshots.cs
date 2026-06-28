using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddCombatStyleSnapshotToCharacterSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CharacterSnapshotId",
                table: "DungeonRuns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CombatStyle",
                table: "CharacterSnapshots",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DungeonRuns_CharacterSnapshotId",
                table: "DungeonRuns",
                column: "CharacterSnapshotId");

            migrationBuilder.AddForeignKey(
                name: "FK_DungeonRuns_CharacterSnapshots_CharacterSnapshotId",
                table: "DungeonRuns",
                column: "CharacterSnapshotId",
                principalTable: "CharacterSnapshots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DungeonRuns_CharacterSnapshots_CharacterSnapshotId",
                table: "DungeonRuns");

            migrationBuilder.DropIndex(
                name: "IX_DungeonRuns_CharacterSnapshotId",
                table: "DungeonRuns");

            migrationBuilder.DropColumn(
                name: "CharacterSnapshotId",
                table: "DungeonRuns");

            migrationBuilder.DropColumn(
                name: "CombatStyle",
                table: "CharacterSnapshots");
        }
    }
}
