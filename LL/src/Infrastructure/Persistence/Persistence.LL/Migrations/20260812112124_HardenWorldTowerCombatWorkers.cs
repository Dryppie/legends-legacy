using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class HardenWorldTowerCombatWorkers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DispatchLeaseOwner",
                table: "TowerCombatPlaybacks",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DispatchLeaseUntil",
                table: "TowerCombatPlaybacks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SimulationAttempts",
                table: "TowerAttempts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SimulationLeaseOwner",
                table: "TowerAttempts",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SimulationLeaseUntil",
                table: "TowerAttempts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TowerCombatPlaybacks_DispatchLeaseUntil",
                table: "TowerCombatPlaybacks",
                column: "DispatchLeaseUntil");

            migrationBuilder.CreateIndex(
                name: "IX_TowerAttempts_Status_SimulationLeaseUntil",
                table: "TowerAttempts",
                columns: new[] { "Status", "SimulationLeaseUntil" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TowerCombatPlaybacks_DispatchLeaseUntil",
                table: "TowerCombatPlaybacks");

            migrationBuilder.DropIndex(
                name: "IX_TowerAttempts_Status_SimulationLeaseUntil",
                table: "TowerAttempts");

            migrationBuilder.DropColumn(
                name: "DispatchLeaseOwner",
                table: "TowerCombatPlaybacks");

            migrationBuilder.DropColumn(
                name: "DispatchLeaseUntil",
                table: "TowerCombatPlaybacks");

            migrationBuilder.DropColumn(
                name: "SimulationAttempts",
                table: "TowerAttempts");

            migrationBuilder.DropColumn(
                name: "SimulationLeaseOwner",
                table: "TowerAttempts");

            migrationBuilder.DropColumn(
                name: "SimulationLeaseUntil",
                table: "TowerAttempts");
        }
    }
}
