using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class ScopeOrdinaryEquipmentSelectionsByPool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PoolId",
                table: "ModelEOrdinarySelectionReceipts",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            // These are current ordinary-acquisition receipts, all created before Meran existed.
            migrationBuilder.Sql("""
                UPDATE "ModelEOrdinarySelectionReceipts"
                SET "PoolId" = 'model_e.r1.plain.tier1.v1';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "PoolId",
                table: "ModelEOrdinarySelectionReceipts",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(160)",
                oldMaxLength: 160,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PoolId",
                table: "ModelEOrdinarySelectionReceipts");
        }
    }
}
