using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(LLDbContext))]
    [Migration("20260708203000_DropLegacyUserNormalizedUsername")]
    public partial class DropLegacyUserNormalizedUsername : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "IX_Users_NormalizedUsername";
                ALTER TABLE "Users" DROP COLUMN IF EXISTS "NormalizedUsername";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Users"
                ADD COLUMN IF NOT EXISTS "NormalizedUsername" character varying(80) NOT NULL DEFAULT '';

                UPDATE "Users"
                SET "NormalizedUsername" = upper(btrim("Username"));

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_NormalizedUsername"
                ON "Users" ("NormalizedUsername");
                """);
        }
    }
}
