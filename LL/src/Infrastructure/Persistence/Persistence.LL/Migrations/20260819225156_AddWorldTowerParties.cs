using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddWorldTowerParties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PartySlot",
                table: "TowerRallyParticipants",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TowerRallyParticipants_TowerRallyId_PartySlot",
                table: "TowerRallyParticipants",
                columns: new[] { "TowerRallyId", "PartySlot" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_TowerRallyParticipants_PartySlot",
                table: "TowerRallyParticipants",
                sql: "\"PartySlot\" IS NULL OR \"PartySlot\" > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TowerRallyParticipants_TowerRallyId_PartySlot",
                table: "TowerRallyParticipants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TowerRallyParticipants_PartySlot",
                table: "TowerRallyParticipants");

            migrationBuilder.DropColumn(
                name: "PartySlot",
                table: "TowerRallyParticipants");
        }
    }
}
