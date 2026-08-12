using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations;

[DbContext(typeof(LLDbContext))]
[Migration("20260812210000_NormalizeWorldTowerPowerRatings")]
public partial class NormalizeWorldTowerPowerRatings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE "TowerRallyParticipants" AS participant
            SET "PowerRating" = participant."PowerRating" / 10
            WHERE NOT EXISTS (
                SELECT 1
                FROM "Users" AS account
                WHERE account."Id" = participant."AccountId"
                  AND account."IsGuest"
                  AND account."Username" LIKE 'SeedGuest%'
            );

            UPDATE "TowerRallyApplications"
            SET "PowerRating" = "PowerRating" / 10;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE "TowerRallyParticipants" AS participant
            SET "PowerRating" = participant."PowerRating" * 10
            WHERE NOT EXISTS (
                SELECT 1
                FROM "Users" AS account
                WHERE account."Id" = participant."AccountId"
                  AND account."IsGuest"
                  AND account."Username" LIKE 'SeedGuest%'
            );

            UPDATE "TowerRallyApplications"
            SET "PowerRating" = "PowerRating" * 10;
            """);
    }
}
