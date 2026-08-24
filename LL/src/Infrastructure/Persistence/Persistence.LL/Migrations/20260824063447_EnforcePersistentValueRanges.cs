using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class EnforcePersistentValueRanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "Entities"
                        WHERE "EntityType" = 1
                          AND (
                              "Cinders" IS NULL OR "Cinders" < 0 OR
                              "Soulstones" IS NULL OR "Soulstones" < 0 OR
                              "FateEcho" IS NULL OR "FateEcho" < 0 OR
                              "SigilFragments" IS NULL OR "SigilFragments" < 0 OR
                              "GuildFavor" IS NULL OR "GuildFavor" < 0 OR
                              "TowerTokens" IS NULL OR "TowerTokens" < 0 OR
                              "RaidTrophies" IS NULL OR "RaidTrophies" < 0
                          )
                    ) THEN
                        RAISE EXCEPTION 'Cannot enforce non-negative character balances: invalid currency rows must be resolved first.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM "InventoryItems"
                        WHERE "Quantity" <= 0
                    ) THEN
                        RAISE EXCEPTION 'Cannot enforce positive inventory quantities: zero or negative stacks must be resolved first.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM "ArenaTicketStatus"
                        WHERE "CurrentTickets" < 0 OR "CurrentTickets" > 5
                    ) THEN
                        RAISE EXCEPTION 'Cannot enforce the arena ticket range: values outside 0 through 5 must be resolved first.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryItems_Quantity_Positive",
                table: "InventoryItems",
                sql: "\"Quantity\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Entities_CharacterCurrencyBalances_NonNegative",
                table: "Entities",
                sql: "\"EntityType\" <> 1 OR (\"Cinders\" IS NOT NULL AND \"Cinders\" >= 0 AND \"Soulstones\" IS NOT NULL AND \"Soulstones\" >= 0 AND \"FateEcho\" IS NOT NULL AND \"FateEcho\" >= 0 AND \"SigilFragments\" IS NOT NULL AND \"SigilFragments\" >= 0 AND \"GuildFavor\" IS NOT NULL AND \"GuildFavor\" >= 0 AND \"TowerTokens\" IS NOT NULL AND \"TowerTokens\" >= 0 AND \"RaidTrophies\" IS NOT NULL AND \"RaidTrophies\" >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ArenaTicketStatus_CurrentTickets_Range",
                table: "ArenaTicketStatus",
                sql: "\"CurrentTickets\" >= 0 AND \"CurrentTickets\" <= 5");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryItems_Quantity_Positive",
                table: "InventoryItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Entities_CharacterCurrencyBalances_NonNegative",
                table: "Entities");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ArenaTicketStatus_CurrentTickets_Range",
                table: "ArenaTicketStatus");
        }
    }
}
