using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class RenameWarhornToRaidSeal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WarhornRefunded",
                table: "RaidRuns",
                newName: "RaidSealRefunded");

            migrationBuilder.RenameColumn(
                name: "WarhornOwnerCharacterId",
                table: "RaidRuns",
                newName: "RaidSealOwnerCharacterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RaidSealRefunded",
                table: "RaidRuns",
                newName: "WarhornRefunded");

            migrationBuilder.RenameColumn(
                name: "RaidSealOwnerCharacterId",
                table: "RaidRuns",
                newName: "WarhornOwnerCharacterId");
        }
    }
}
