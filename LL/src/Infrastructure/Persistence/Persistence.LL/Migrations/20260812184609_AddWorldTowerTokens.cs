using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddWorldTowerTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "TowerTokens",
                table: "Entities",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Entities"
                SET "TowerTokens" = 0
                WHERE "EntityType" = 1 AND "TowerTokens" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TowerTokens",
                table: "Entities");
        }
    }
}
