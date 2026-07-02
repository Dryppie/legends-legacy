using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddEssencePotential : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NativeRegion",
                table: "PlayerEssences",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "PotentialTier",
                table: "PlayerEssences",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(
                """
                UPDATE "PlayerEssences"
                SET "PotentialTier" = LEAST(10, GREATEST(1, CEILING("Level" / 10.0)::integer))
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NativeRegion",
                table: "PlayerEssences");

            migrationBuilder.DropColumn(
                name: "PotentialTier",
                table: "PlayerEssences");
        }
    }
}
