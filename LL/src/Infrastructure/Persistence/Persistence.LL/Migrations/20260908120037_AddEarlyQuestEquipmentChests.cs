using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddEarlyQuestEquipmentChests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migrations run before JSON seeding, so the new item bases must exist first.
            migrationBuilder.Sql(
                """
                INSERT INTO "ItemBases" ("Id", "Name", "Description", "Stackable", "IsBound", "ItemType", "Rarity")
                VALUES
                    ('item.armor_chest', 'Armor Chest',
                     'Open to receive 1 random Common armor piece at Tier 1, with Standard quality and no reinforcement.',
                     TRUE, TRUE, 2, 0),
                    ('item.jewelry_chest', 'Jewelry Chest',
                     'Open to receive 1 random Common jewelry piece at Tier 1, with Standard quality and no reinforcement.',
                     TRUE, TRUE, 2, 0)
                ON CONFLICT ("Id") DO NOTHING;
                """);

            // Award only the added chests; leave quest status and original rewards alone.
            // The durable ledger receipt prevents replay even after the chest is consumed
            // or this data migration is rolled back and reapplied.
            migrationBuilder.Sql(
                """
                WITH rewards("QuestId", "ItemBaseId") AS (
                    VALUES
                        ('quest.region01.into_lumo_ruins', 'item.armor_chest'),
                        ('quest.shenic.blood_in_the_grove', 'item.jewelry_chest')
                ), eligible AS (
                    SELECT
                        md5('early-quest-equipment-chest-v1:' || progress."CharacterId"::text || ':' || rewards."QuestId")::uuid AS "GrantId",
                        progress."CharacterId", character."UserId", character."Level",
                        item."Id" AS "ItemBaseId", item."Name"
                    FROM "CharacterQuestProgresses" AS progress
                    JOIN rewards ON rewards."QuestId" = lower(progress."QuestId")
                    JOIN "Entities" AS character ON character."Id" = progress."CharacterId"
                    JOIN "Inventories" AS inventory ON inventory."CharacterId" = progress."CharacterId"
                    JOIN "ItemBases" AS item ON item."Id" = rewards."ItemBaseId"
                    WHERE progress."Status" = 3
                      AND NOT EXISTS (
                          SELECT 1 FROM "EconomyLedger" AS ledger
                          WHERE ledger."RecipientCharacterId" = progress."CharacterId"
                            AND ledger."AssetId" = rewards."ItemBaseId"
                            AND ledger."EventType" = 'ItemAcquisition'
                            AND ledger."Source" IN ('quest-reward', 'quest-reward:equipment-chest-backfill')
                      )
                ), grants AS (
                    INSERT INTO "EconomyLedger" (
                        "Id", "EventType", "AssetType", "ReferenceId",
                        "RecipientAccountId", "RecipientCharacterId", "RecipientCharacterLevel",
                        "AssetId", "AssetName", "DestinationItemInstanceId", "Quantity", "Source", "OccurredAt")
                    SELECT "GrantId", 'ItemAcquisition', 'Item', "GrantId",
                        "UserId", "CharacterId", "Level", "ItemBaseId", "Name", "GrantId", 1,
                        'quest-reward:equipment-chest-backfill', CURRENT_TIMESTAMP
                    FROM eligible
                    ON CONFLICT ("Id") DO NOTHING
                    RETURNING "DestinationItemInstanceId", "RecipientCharacterId", "AssetId", "Source", "OccurredAt"
                ), instances AS (
                    INSERT INTO "ItemInstances" ("Id", "ItemBaseId", "ItemType", "AcquisitionSource", "AcquiredAtUtc")
                    SELECT "DestinationItemInstanceId", "AssetId", 4, "Source", "OccurredAt"
                    FROM grants
                    RETURNING "Id"
                )
                INSERT INTO "InventoryItems" ("InventoryId", "ItemInstanceId", "Quantity", "IsFavorite", "SeenAtUtc")
                SELECT grants."RecipientCharacterId", instances."Id", 1, FALSE, NULL
                FROM grants
                JOIN instances ON instances."Id" = grants."DestinationItemInstanceId";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Retain delivered rewards and their receipts: players may already have opened them.
        }
    }
}
