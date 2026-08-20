using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class RedesignRaidPartyEncounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WardBreak",
                table: "RaidRuns",
                newName: "GuardianBreak");

            migrationBuilder.AddColumn<decimal>(
                name: "SignatureDisruption",
                table: "RaidRuns",
                type: "numeric(8,6)",
                precision: 8,
                scale: 6,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SignatureDisruption",
                table: "RaidRuns");

            migrationBuilder.RenameColumn(
                name: "GuardianBreak",
                table: "RaidRuns",
                newName: "WardBreak");
        }
    }
}
