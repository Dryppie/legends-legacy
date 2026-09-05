using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class MigrateLegacyBlueprintItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TEMP TABLE "__LegacyBlueprintMap" (
                    "OldId" text PRIMARY KEY,
                    "NewId" text NOT NULL UNIQUE,
                    "DisplayName" text NOT NULL
                ) ON COMMIT DROP;

                INSERT INTO "__LegacyBlueprintMap" ("OldId", "NewId", "DisplayName")
                VALUES
                    ('blueprint_fury', 'item.blueprint_fury', 'Fury'),
                    ('blueprint_arcane', 'item.blueprint_arcane', 'Arcane'),
                    ('blueprint_execution', 'item.blueprint_execution', 'Execution'),
                    ('blueprint_aegis', 'item.blueprint_aegis', 'Aegis'),
                    ('blueprint_warden', 'item.blueprint_warden', 'Warden'),
                    ('blueprint_endurance', 'item.blueprint_endurance', 'Endurance'),
                    ('blueprint_phoenix', 'item.blueprint_phoenix', 'Phoenix'),
                    ('blueprint_spirit', 'item.blueprint_spirit', 'Spirit'),
                    ('blueprint_primal', 'item.blueprint_primal', 'Primal'),
                    ('blueprint_venom_touched_sword', 'item.blueprint_venom', 'Venom'),
                    ('blueprint_hivefang_dagger', 'item.blueprint_hive', 'Hive'),
                    ('blueprint_raidforged', 'item.blueprint_raidforged', 'Raidforged'),
                    ('blueprint_gravebound', 'item.blueprint_gravebound', 'Gravebound');

                -- Migrations run before JSON seeding. Ensure every destination exists so
                -- existing item instances can be repointed on the first upgraded startup.
                INSERT INTO "ItemBases" (
                    "Id", "Name", "Description", "Stackable", "IsBound", "ItemType", "Rarity")
                SELECT
                    map."NewId",
                    'Blueprint: ' || map."DisplayName",
                    'Consumed to apply ' || map."DisplayName" || ' to compatible equipment.',
                    TRUE,
                    FALSE,
                    2,
                    2
                FROM "__LegacyBlueprintMap" AS map
                ON CONFLICT ("Id") DO UPDATE SET
                    "Name" = EXCLUDED."Name",
                    "Stackable" = TRUE,
                    "IsBound" = FALSE,
                    "ItemType" = 2,
                    "Rarity" = 2;

                -- Active buy orders should follow the usable item identity. Completed market
                -- orders and economy ledgers remain untouched as historical records.
                UPDATE "MarketPlaceBuyOrders" AS buy_order
                SET "ItemBaseId" = map."NewId"
                FROM "__LegacyBlueprintMap" AS map
                WHERE buy_order."ItemBaseId" = map."OldId";

                -- This converts inventory and marketplace-listing instances alike because both
                -- reference the shared item instance row.
                UPDATE "ItemInstances" AS instance
                SET "ItemBaseId" = map."NewId"
                FROM "__LegacyBlueprintMap" AS map
                WHERE instance."ItemBaseId" = map."OldId";

                -- A character may own both the former Forge stack and a newly awarded consumable
                -- stack. Consolidate all stacks of each converted blueprint without losing
                -- quantity, favorite state, or unseen state.
                CREATE TEMP TABLE "__BlueprintInventoryCanonical" ON COMMIT DROP AS
                SELECT
                    inventory_item."InventoryId",
                    instance."ItemBaseId",
                    (ARRAY_AGG(inventory_item."ItemInstanceId"
                        ORDER BY inventory_item."ItemInstanceId"))[1] AS "ItemInstanceId",
                    SUM(inventory_item."Quantity") AS "Quantity",
                    BOOL_OR(inventory_item."IsFavorite") AS "IsFavorite",
                    CASE
                        WHEN BOOL_OR(inventory_item."SeenAtUtc" IS NULL) THEN NULL
                        ELSE MIN(inventory_item."SeenAtUtc")
                    END AS "SeenAtUtc"
                FROM "InventoryItems" AS inventory_item
                INNER JOIN "ItemInstances" AS instance
                    ON instance."Id" = inventory_item."ItemInstanceId"
                WHERE instance."ItemBaseId" IN (
                    SELECT "NewId" FROM "__LegacyBlueprintMap")
                GROUP BY inventory_item."InventoryId", instance."ItemBaseId";

                UPDATE "InventoryItems" AS inventory_item
                SET
                    "Quantity" = canonical."Quantity"::integer,
                    "IsFavorite" = canonical."IsFavorite",
                    "SeenAtUtc" = canonical."SeenAtUtc"
                FROM "__BlueprintInventoryCanonical" AS canonical
                WHERE inventory_item."InventoryId" = canonical."InventoryId"
                  AND inventory_item."ItemInstanceId" = canonical."ItemInstanceId";

                DELETE FROM "InventoryItems" AS inventory_item
                USING "ItemInstances" AS instance,
                      "__BlueprintInventoryCanonical" AS canonical
                WHERE instance."Id" = inventory_item."ItemInstanceId"
                  AND canonical."InventoryId" = inventory_item."InventoryId"
                  AND canonical."ItemBaseId" = instance."ItemBaseId"
                  AND canonical."ItemInstanceId" <> inventory_item."ItemInstanceId";

                DROP TABLE "__BlueprintInventoryCanonical";
                DROP TABLE "__LegacyBlueprintMap";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally irreversible: converted stacks may have been merged or consumed.
            // Recreating the former Forge identities would invent ownership history.
        }
    }
}
