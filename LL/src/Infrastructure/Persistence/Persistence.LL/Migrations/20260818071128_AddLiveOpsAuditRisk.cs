using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddLiveOpsAuditRisk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RiskLevel",
                table: "AdminActions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Normal");

            migrationBuilder.Sql(
                """
                UPDATE "AdminActions"
                SET "RiskLevel" = 'Permanent'
                WHERE "ActionType" = 'AccountBanned'
                  AND ("DetailsJson" ->> 'expiresAt') IS NULL;

                UPDATE "AdminActions"
                SET "RiskLevel" = 'HighValue'
                WHERE "ActionType" = 'CompensationItemsGranted'
                  AND COALESCE(("DetailsJson" ->> 'Quantity')::integer, 0) >= 100;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_AdminActions_RiskLevel_OccurredAt",
                table: "AdminActions",
                columns: new[] { "RiskLevel", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdminActions_RiskLevel_OccurredAt",
                table: "AdminActions");

            migrationBuilder.DropColumn(
                name: "RiskLevel",
                table: "AdminActions");
        }
    }
}
