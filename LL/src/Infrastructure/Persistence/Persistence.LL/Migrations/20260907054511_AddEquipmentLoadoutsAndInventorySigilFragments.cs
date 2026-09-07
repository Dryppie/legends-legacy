using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentLoadoutsAndInventorySigilFragments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Entities_CharacterCurrencyBalances_NonNegative",
                table: "Entities");

            // Convert every existing balance before removing the character currency column.
            migrationBuilder.Sql("""
                INSERT INTO "ItemBases" ("Id", "Name", "Description", "ItemType", "Rarity", "Stackable", "IsBound")
                VALUES ('sigil_fragment', 'Sigil Fragments', 'Fragments used to assemble dungeon entrance sigils.', 2, 0, TRUE, TRUE)
                ON CONFLICT ("Id") DO NOTHING;

                -- Inventory quantities are int32. Fail transactionally instead of truncating an oversized balance.
                DO $$ BEGIN
                  IF EXISTS (SELECT 1 FROM "Entities" e WHERE e."EntityType" = 1 AND
                    e."SigilFragments" + COALESCE((SELECT SUM(i."Quantity") FROM "InventoryItems" i
                      JOIN "ItemInstances" item ON item."Id" = i."ItemInstanceId"
                      WHERE i."InventoryId" = e."Id" AND item."ItemBaseId" = 'sigil_fragment'), 0) > 2147483647)
                  THEN RAISE EXCEPTION 'Sigil Fragment balance exceeds inventory quantity capacity'; END IF;
                END $$;

                INSERT INTO "Inventories" ("CharacterId")
                SELECT "Id" FROM "Entities" WHERE "EntityType" = 1 AND "SigilFragments" > 0
                ON CONFLICT ("CharacterId") DO NOTHING;

                CREATE TEMP TABLE sigil_fragment_conversion ON COMMIT DROP AS
                SELECT "Id" AS character_id, gen_random_uuid() AS instance_id, "SigilFragments"::integer AS quantity
                FROM "Entities" WHERE "EntityType" = 1 AND "SigilFragments" > 0;

                INSERT INTO "ItemInstances" ("Id", "ItemBaseId", "ItemType", "AcquiredAtUtc", "AcquisitionSource")
                SELECT instance_id, 'sigil_fragment', 4, CURRENT_TIMESTAMP, 'SigilFragmentConversion'
                FROM sigil_fragment_conversion;
                INSERT INTO "InventoryItems" ("InventoryId", "ItemInstanceId", "Quantity", "IsFavorite")
                SELECT character_id, instance_id, quantity, FALSE FROM sigil_fragment_conversion;
                """);

            migrationBuilder.DropColumn(
                name: "SigilFragments",
                table: "Entities");

            migrationBuilder.CreateTable(
                name: "EquipmentLoadouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    AutoUseActivities = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentLoadouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentLoadouts_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentLoadoutSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentLoadoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotType = table.Column<int>(type: "integer", nullable: false),
                    EquipmentInstanceId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentLoadoutSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentLoadoutSlots_EquipmentLoadouts_EquipmentLoadoutId",
                        column: x => x.EquipmentLoadoutId,
                        principalTable: "EquipmentLoadouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EquipmentLoadoutSlots_ItemInstances_EquipmentInstanceId",
                        column: x => x.EquipmentInstanceId,
                        principalTable: "ItemInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Entities_CharacterCurrencyBalances_NonNegative",
                table: "Entities",
                sql: "\"EntityType\" <> 1 OR (\"Cinders\" IS NOT NULL AND \"Cinders\" >= 0 AND \"Soulstones\" IS NOT NULL AND \"Soulstones\" >= 0 AND \"FateEcho\" IS NOT NULL AND \"FateEcho\" >= 0 AND \"GuildFavor\" IS NOT NULL AND \"GuildFavor\" >= 0 AND \"TowerTokens\" IS NOT NULL AND \"TowerTokens\" >= 0 AND \"RaidTrophies\" IS NOT NULL AND \"RaidTrophies\" >= 0)");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentLoadouts_CharacterId_Name",
                table: "EquipmentLoadouts",
                columns: new[] { "CharacterId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentLoadoutSlots_EquipmentInstanceId",
                table: "EquipmentLoadoutSlots",
                column: "EquipmentInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentLoadoutSlots_EquipmentLoadoutId_SlotType",
                table: "EquipmentLoadoutSlots",
                columns: new[] { "EquipmentLoadoutId", "SlotType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EquipmentLoadoutSlots");

            migrationBuilder.DropTable(
                name: "EquipmentLoadouts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Entities_CharacterCurrencyBalances_NonNegative",
                table: "Entities");

            migrationBuilder.AddColumn<long>(
                name: "SigilFragments",
                table: "Entities",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Entities" e SET "SigilFragments" = COALESCE((
                    SELECT SUM(i."Quantity"::bigint) FROM "InventoryItems" i
                    JOIN "ItemInstances" item ON item."Id" = i."ItemInstanceId"
                    WHERE i."InventoryId" = e."Id" AND item."ItemBaseId" = 'sigil_fragment'), 0)
                WHERE e."EntityType" = 1;
                DELETE FROM "InventoryItems" i USING "ItemInstances" item
                WHERE item."Id" = i."ItemInstanceId" AND item."ItemBaseId" = 'sigil_fragment';
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Entities_CharacterCurrencyBalances_NonNegative",
                table: "Entities",
                sql: "\"EntityType\" <> 1 OR (\"Cinders\" IS NOT NULL AND \"Cinders\" >= 0 AND \"Soulstones\" IS NOT NULL AND \"Soulstones\" >= 0 AND \"FateEcho\" IS NOT NULL AND \"FateEcho\" >= 0 AND \"SigilFragments\" IS NOT NULL AND \"SigilFragments\" >= 0 AND \"GuildFavor\" IS NOT NULL AND \"GuildFavor\" >= 0 AND \"TowerTokens\" IS NOT NULL AND \"TowerTokens\" >= 0 AND \"RaidTrophies\" IS NOT NULL AND \"RaidTrophies\" >= 0)");
        }
    }
}
