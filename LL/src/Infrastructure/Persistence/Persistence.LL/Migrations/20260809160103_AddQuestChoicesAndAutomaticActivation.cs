using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestChoicesAndAutomaticActivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedOptionKey",
                table: "CharacterQuestProgresses",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "CharacterQuestProgresses"
                SET "Status" = 2,
                    "AcceptedAt" = COALESCE("AcceptedAt", "UpdatedAt"),
                    "RowVersion" = "RowVersion" + 1
                WHERE "Status" = 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedOptionKey",
                table: "CharacterQuestProgresses");
        }
    }
}
