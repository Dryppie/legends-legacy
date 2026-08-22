using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRegionBossCharacterSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RegionBossSignups_CharacterSnapshots_CharacterSnapshotId",
                table: "RegionBossSignups");

            migrationBuilder.DropIndex(
                name: "IX_RegionBossSignups_CharacterSnapshotId",
                table: "RegionBossSignups");

            migrationBuilder.DropColumn(
                name: "BuildFingerprint",
                table: "RegionBossSignups");

            migrationBuilder.DropColumn(
                name: "CharacterSnapshotId",
                table: "RegionBossSignups");

            migrationBuilder.DropColumn(
                name: "LoadoutHash",
                table: "RegionBossSignups");

            migrationBuilder.DropColumn(
                name: "SnapshotRefreshedAtUtc",
                table: "RegionBossSignups");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BuildFingerprint",
                table: "RegionBossSignups",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "CharacterSnapshotId",
                table: "RegionBossSignups",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "LoadoutHash",
                table: "RegionBossSignups",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SnapshotRefreshedAtUtc",
                table: "RegionBossSignups",
                type: "timestamp with time zone",
                nullable: true);

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
    }
}
