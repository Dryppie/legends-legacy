using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class GrantAlphaSignetToExistingCharacters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Startup migrates before JSON seeding. Persist the catalog entry and canonical
            // ownership together; an inventory-only gift cannot be redeemed or traded.
            migrationBuilder.Sql("""
                INSERT INTO "ItemBases" ("Id", "Name", "Description", "ItemType", "Rarity", "Stackable", "IsBound")
                VALUES ('signet', 'Signet', 'Redeem to gain one calendar month of Nobility. Unredeemed Signets can be traded on the market.', 4, 2, TRUE, FALSE)
                ON CONFLICT ("Id") DO UPDATE SET
                    "Name" = EXCLUDED."Name", "Description" = EXCLUDED."Description",
                    "ItemType" = EXCLUDED."ItemType", "Rarity" = EXCLUDED."Rarity",
                    "Stackable" = EXCLUDED."Stackable", "IsBound" = EXCLUDED."IsBound";

                CREATE TEMP TABLE alpha_signet_recipients ON COMMIT DROP AS
                SELECT c."Id" AS character_id, c."UserId" AS account_id,
                    md5('nobility-alpha-launch:issuance:' || c."Id"::text)::uuid AS issuance_id,
                    md5('nobility-alpha-launch:unit:' || c."Id"::text)::uuid AS unit_id
                FROM "Entities" c JOIN "Users" u ON u."Id" = c."UserId"
                WHERE c."EntityType" = 1;

                INSERT INTO "SignetIssuances"
                    ("Id", "AccountId", "CharacterId", "ActorSubject", "Origin", "Reason", "Quantity", "IssuedAt", "Refunded")
                SELECT issuance_id, account_id, character_id, 'migration:alpha-signet', 0,
                    'One-time alpha Signet for every existing character.', 1, CURRENT_TIMESTAMP, FALSE
                FROM alpha_signet_recipients
                ON CONFLICT ("Id") DO NOTHING;

                INSERT INTO "SignetUnits"
                    ("Id", "IssuanceId", "Ordinal", "OwnerCharacterId", "State", "Version", "IssuedAt")
                SELECT r.unit_id, r.issuance_id, 0, r.character_id, 0,
                    md5('nobility-alpha-launch:version:' || r.character_id::text)::uuid, i."IssuedAt"
                FROM alpha_signet_recipients r JOIN "SignetIssuances" i ON i."Id" = r.issuance_id
                ON CONFLICT ("Id") DO NOTHING;

                INSERT INTO "SignetMovements"
                    ("Id", "UnitId", "OperationId", "Kind", "ToCharacterId", "OccurredAt")
                SELECT md5('nobility-alpha-launch:movement:' || r.character_id::text)::uuid,
                    r.unit_id, r.issuance_id, 0, r.character_id, i."IssuedAt"
                FROM alpha_signet_recipients r JOIN "SignetIssuances" i ON i."Id" = r.issuance_id
                ON CONFLICT ("Id") DO NOTHING;

                INSERT INTO "Inventories" ("CharacterId")
                SELECT character_id FROM alpha_signet_recipients
                ON CONFLICT ("CharacterId") DO NOTHING;

                -- Match the runtime inventory projection, including any earlier manual grants.
                -- Stable identities keep replay from granting twice or reviving spent units.
                CREATE TEMP TABLE alpha_signet_stacks ON COMMIT DROP AS
                SELECT r.character_id,
                    (SELECT count(*)::integer FROM "SignetUnits" u
                        WHERE u."OwnerCharacterId" = r.character_id AND u."State" = 0) AS quantity,
                    COALESCE((SELECT ii."ItemInstanceId" FROM "InventoryItems" ii
                        JOIN "ItemInstances" inst ON inst."Id" = ii."ItemInstanceId"
                        WHERE ii."InventoryId" = r.character_id AND inst."ItemBaseId" = 'signet'
                        ORDER BY ii."ItemInstanceId" LIMIT 1),
                        md5('nobility-alpha-launch:inventory:' || r.character_id::text)::uuid) AS instance_id
                FROM alpha_signet_recipients r;

                INSERT INTO "ItemInstances" ("Id", "ItemBaseId", "ItemType", "AcquiredAtUtc", "AcquisitionSource")
                SELECT instance_id, 'signet', 4, CURRENT_TIMESTAMP, 'NobilityAlphaMigration'
                FROM alpha_signet_stacks WHERE quantity > 0
                ON CONFLICT ("Id") DO NOTHING;

                INSERT INTO "InventoryItems" ("InventoryId", "ItemInstanceId", "Quantity", "IsFavorite")
                SELECT character_id, instance_id, quantity, FALSE
                FROM alpha_signet_stacks WHERE quantity > 0
                ON CONFLICT ("InventoryId", "ItemInstanceId") DO UPDATE SET "Quantity" = EXCLUDED."Quantity";

                DELETE FROM "InventoryItems" ii USING "ItemInstances" inst, alpha_signet_stacks s
                WHERE ii."InventoryId" = s.character_id AND inst."Id" = ii."ItemInstanceId"
                    AND inst."ItemBaseId" = 'signet'
                    AND (s.quantity = 0 OR ii."ItemInstanceId" <> s.instance_id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "Alpha Signets may already have been traded or redeemed. Preserve ownership and membership history; use a forward migration instead of rolling back this grant.");
        }
    }
}
