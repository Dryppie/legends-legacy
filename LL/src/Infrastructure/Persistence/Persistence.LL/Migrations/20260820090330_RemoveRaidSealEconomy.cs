using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRaidSealEconomy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "RaidRewardClaims"
                SET "PendingItemsJson" = COALESCE(
                    (
                        SELECT jsonb_agg(item)
                        FROM jsonb_array_elements("PendingItemsJson") AS item
                        WHERE item ->> 'itemId' NOT IN (
                            'raid_seal_fragment_hives_abyss',
                            'raid_seal_fragment_sanguine_horror',
                            'raid_seal_hives_abyss',
                            'raid_seal_sanguine_horror')
                    ),
                    '[]'::jsonb)
                WHERE "PendingItemsJson" @> '[{"itemId": "raid_seal_fragment_hives_abyss"}]'::jsonb
                   OR "PendingItemsJson" @> '[{"itemId": "raid_seal_fragment_sanguine_horror"}]'::jsonb
                   OR "PendingItemsJson" @> '[{"itemId": "raid_seal_hives_abyss"}]'::jsonb
                   OR "PendingItemsJson" @> '[{"itemId": "raid_seal_sanguine_horror"}]'::jsonb;

                DELETE FROM "MarketPlaceBuyOrders"
                WHERE "ItemBaseId" IN (
                    'raid_seal_fragment_hives_abyss',
                    'raid_seal_fragment_sanguine_horror',
                    'raid_seal_hives_abyss',
                    'raid_seal_sanguine_horror');

                DELETE FROM "MarketPlaceOrders"
                WHERE "ItemBaseId" IN (
                    'raid_seal_fragment_hives_abyss',
                    'raid_seal_fragment_sanguine_horror',
                    'raid_seal_hives_abyss',
                    'raid_seal_sanguine_horror');

                DELETE FROM "ItemBases"
                WHERE "Id" IN (
                    'raid_seal_fragment_hives_abyss',
                    'raid_seal_fragment_sanguine_horror',
                    'raid_seal_hives_abyss',
                    'raid_seal_sanguine_horror');
                """);

            migrationBuilder.DropColumn(
                name: "RaidSealOwnerCharacterId",
                table: "RaidRuns");

            migrationBuilder.DropColumn(
                name: "RaidSealRefunded",
                table: "RaidRuns");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RaidSealOwnerCharacterId",
                table: "RaidRuns",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "RaidSealRefunded",
                table: "RaidRuns",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
