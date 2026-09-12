using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AllowNobleArenaTicketCapacity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ArenaTicketStatus_CurrentTickets_Range",
                table: "ArenaTicketStatus");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ArenaTicketStatus_CurrentTickets_Range",
                table: "ArenaTicketStatus",
                sql: "\"CurrentTickets\" >= 0 AND \"CurrentTickets\" <= 8");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ArenaTicketStatus_CurrentTickets_Range",
                table: "ArenaTicketStatus");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ArenaTicketStatus_CurrentTickets_Range",
                table: "ArenaTicketStatus",
                sql: "\"CurrentTickets\" >= 0 AND \"CurrentTickets\" <= 5");
        }
    }
}
