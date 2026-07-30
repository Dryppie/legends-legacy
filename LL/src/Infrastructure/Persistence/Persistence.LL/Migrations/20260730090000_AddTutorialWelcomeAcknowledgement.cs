using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations;

[DbContext(typeof(LLDbContext))]
[Migration("20260730090000_AddTutorialWelcomeAcknowledgement")]
public sealed class AddTutorialWelcomeAcknowledgement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "WelcomeAcknowledgedAt",
            table: "CharacterTutorialProgresses",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE "CharacterTutorialProgresses"
            SET "WelcomeAcknowledgedAt" = "UpdatedAt"
            WHERE "WelcomeAcknowledgedAt" IS NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "WelcomeAcknowledgedAt",
            table: "CharacterTutorialProgresses");
    }
}
