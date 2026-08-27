using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class UnifyCombatSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CharacterSnapshotId",
                table: "RegionBossSignups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "CharacterSnapshots",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_RegionBossSignups_CharacterSnapshotId",
                table: "RegionBossSignups",
                column: "CharacterSnapshotId");

            migrationBuilder.AddForeignKey(
                name: "FK_RegionBossSignups_CharacterSnapshots_CharacterSnapshotId",
                table: "RegionBossSignups",
                column: "CharacterSnapshotId",
                principalTable: "CharacterSnapshots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RegionBossSignups_CharacterSnapshots_CharacterSnapshotId",
                table: "RegionBossSignups");

            migrationBuilder.DropIndex(
                name: "IX_RegionBossSignups_CharacterSnapshotId",
                table: "RegionBossSignups");

            migrationBuilder.DropColumn(
                name: "CharacterSnapshotId",
                table: "RegionBossSignups");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "CharacterSnapshots");
        }
    }
}
