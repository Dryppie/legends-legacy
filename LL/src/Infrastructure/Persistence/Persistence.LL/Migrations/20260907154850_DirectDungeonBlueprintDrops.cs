using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class DirectDungeonBlueprintDrops : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TEMP TABLE "__DungeonBlueprintChoices" (
                    "OldId" text PRIMARY KEY, "BlueprintIds" text[] NOT NULL
                ) ON COMMIT DROP;
                INSERT INTO "__DungeonBlueprintChoices" VALUES
                    ('item.blueprint_choice.goblin_mines', ARRAY['item.blueprint_fury', 'item.blueprint_phoenix']),
                    ('item.blueprint_choice.forgotten_catacombs', ARRAY['item.blueprint_arcane', 'item.blueprint_endurance']),
                    ('item.blueprint_choice.tangled_cave', ARRAY['item.blueprint_execution']),
                    ('item.blueprint_choice.great_tree', ARRAY['item.blueprint_spirit']);

                CREATE TEMP TABLE "__RetiredBlueprintItems" ("Id" text PRIMARY KEY) ON COMMIT DROP;
                INSERT INTO "__RetiredBlueprintItems" VALUES
                    ('blueprint_gravebound'), ('item.blueprint_gravebound'),
                    ('blueprint_raidforged'), ('item.blueprint_raidforged');

                -- Migrations precede content seeding, including on a fresh database.
                INSERT INTO "ItemBases" ("Id", "Name", "Description", "Stackable", "IsBound", "ItemType", "Rarity")
                SELECT 'item.blueprint_' || name, 'Blueprint: ' || initcap(name),
                    'Consumed to apply ' || initcap(name) || ' to compatible equipment.', TRUE, FALSE, 2, 2
                FROM unnest(ARRAY['fury', 'phoenix', 'arcane', 'endurance', 'execution', 'spirit']) AS name
                ON CONFLICT ("Id") DO NOTHING;

                -- Unfilled buy orders requested items which no longer exist. Return their escrow
                -- instead of changing which item the buyer is purchasing.
                UPDATE "Entities" AS buyer
                SET "Cinders" = buyer."Cinders" + refunds.amount
                FROM (
                    SELECT "BuyerId", SUM("UnitPrice"::numeric * "Quantity") AS amount
                    FROM "MarketPlaceBuyOrders"
                    WHERE "ItemBaseId" IN (SELECT "OldId" FROM "__DungeonBlueprintChoices")
                       OR "ItemBaseId" IN (SELECT "Id" FROM "__RetiredBlueprintItems")
                    GROUP BY "BuyerId"
                ) AS refunds
                WHERE buyer."Id" = refunds."BuyerId";
                DELETE FROM "MarketPlaceBuyOrders"
                WHERE "ItemBaseId" IN (SELECT "OldId" FROM "__DungeonBlueprintChoices")
                   OR "ItemBaseId" IN (SELECT "Id" FROM "__RetiredBlueprintItems");

                -- Each existing choice stack becomes a direct blueprint stack, retaining quantity,
                -- favorite state and ownership. Two-blueprint families choose deterministically
                -- from the instance ID. This also converts listed stacks without losing them.
                UPDATE "ItemInstances" AS instance
                SET "ItemBaseId" = choices."BlueprintIds"[
                    1 + get_byte(decode(md5(instance."Id"::text), 'hex'), 0) % cardinality(choices."BlueprintIds")]
                FROM "__DungeonBlueprintChoices" AS choices
                WHERE instance."ItemBaseId" = choices."OldId";

                UPDATE "RunRewards" AS reward
                SET "ItemId" = choices."BlueprintIds"[
                    1 + get_byte(decode(md5(reward."Id"::text), 'hex'), 0) % cardinality(choices."BlueprintIds")]
                FROM "__DungeonBlueprintChoices" AS choices
                WHERE reward."ItemId" = choices."OldId";
                UPDATE "RunRewards" AS reward
                SET "Name" = item."Name", "ItemType" = 2
                FROM "ItemBases" AS item
                WHERE item."Id" = reward."ItemId"
                  AND reward."Source" = 'equipment-blueprint';

                -- Merge converted stacks with already-owned blueprints. The integer cast fails
                -- transactionally if a combined quantity would overflow; it never truncates it.
                CREATE TEMP TABLE "__DirectBlueprintInventory" ON COMMIT DROP AS
                SELECT inventory."InventoryId", instance."ItemBaseId",
                    (ARRAY_AGG(inventory."ItemInstanceId" ORDER BY inventory."ItemInstanceId"))[1] AS "ItemInstanceId",
                    SUM(inventory."Quantity")::integer AS "Quantity",
                    BOOL_OR(inventory."IsFavorite") AS "IsFavorite",
                    CASE WHEN BOOL_OR(inventory."SeenAtUtc" IS NULL) THEN NULL
                        ELSE MIN(inventory."SeenAtUtc") END AS "SeenAtUtc"
                FROM "InventoryItems" AS inventory
                JOIN "ItemInstances" AS instance ON instance."Id" = inventory."ItemInstanceId"
                WHERE instance."ItemBaseId" IN (SELECT unnest("BlueprintIds") FROM "__DungeonBlueprintChoices")
                GROUP BY inventory."InventoryId", instance."ItemBaseId";
                UPDATE "InventoryItems" AS inventory
                SET "Quantity" = canonical."Quantity", "IsFavorite" = canonical."IsFavorite",
                    "SeenAtUtc" = canonical."SeenAtUtc"
                FROM "__DirectBlueprintInventory" AS canonical
                WHERE inventory."InventoryId" = canonical."InventoryId"
                  AND inventory."ItemInstanceId" = canonical."ItemInstanceId";
                DELETE FROM "InventoryItems" AS inventory
                USING "ItemInstances" AS instance, "__DirectBlueprintInventory" AS canonical
                WHERE inventory."ItemInstanceId" = instance."Id"
                  AND inventory."InventoryId" = canonical."InventoryId"
                  AND instance."ItemBaseId" = canonical."ItemBaseId"
                  AND inventory."ItemInstanceId" <> canonical."ItemInstanceId";

                -- Inventory and sell listings cascade from their retired item instances.
                DELETE FROM "ItemInstances" WHERE "ItemBaseId" IN (SELECT "Id" FROM "__RetiredBlueprintItems");
                DELETE FROM "RunRewards" WHERE "ItemId" IN (SELECT "Id" FROM "__RetiredBlueprintItems");

                -- Preserve completed trades and economy history. Definitions referenced by those
                -- trades remain only as bound historical records, unavailable in the Bazaar.
                UPDATE "ItemBases" SET "IsBound" = TRUE
                WHERE "Id" IN (SELECT "Id" FROM "__RetiredBlueprintItems")
                   OR "Id" IN (SELECT "OldId" FROM "__DungeonBlueprintChoices");
                DELETE FROM "ItemBases" AS item
                WHERE (item."Id" IN (SELECT "Id" FROM "__RetiredBlueprintItems")
                    OR item."Id" IN (SELECT "OldId" FROM "__DungeonBlueprintChoices"))
                  AND NOT EXISTS (SELECT 1 FROM "MarketPlaceOrders" AS trade WHERE trade."ItemBaseId" = item."Id");

                DROP TABLE "__DirectBlueprintInventory";
                DROP TABLE "__RetiredBlueprintItems";
                DROP TABLE "__DungeonBlueprintChoices";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Content retirement, consumed blueprints and refunded orders cannot be reconstructed.
        }
    }
}
