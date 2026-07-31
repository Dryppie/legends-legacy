using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations;

[DbContext(typeof(LLDbContext))]
[Migration("20260731120000_SimplifyGatheringResources")]
public sealed class SimplifyGatheringResources : Migration
{
    private const string ResourceMapping =
        """
        CASE
            WHEN {0} IN ('copper_ore', 'verdant_ore', 'crystalline_powder', 'soulglass_shard', 'mossy_stone', 'deep_stone', 'bone_fragments', 'grave_bone', 'ancient_bone', 'rough_stone', 'cracked_garnet') THEN 'ore'
            WHEN {0} IN ('bloodwood', 'living_bark', 'woven_fiber', 'silk_thread', 'spectral_thread', 'hive_resin', 'amber_resin', 'living_resin') THEN 'wood'
            WHEN {0} IN ('thick_hide', 'scaled_hide', 'cave_fish', 'murky_fish_oil', 'refined_fish_oil', 'shadow_oil', 'hardened_chitin', 'ant_chitin', 'royal_chitin_fragment') THEN 'rawhide'
            WHEN {0} = 'basic_fishing_rod' THEN 'basic_skinning_knife'
            WHEN {0} = 'fishing_rod' THEN 'skinning_knife'
            WHEN {0} = 'rare_fishing_rod' THEN 'rare_skinning_knife'
            WHEN {0} = 'epic_fishing_rod' THEN 'epic_skinning_knife'
            WHEN {0} = 'unique_fishing_rod' THEN 'unique_skinning_knife'
            WHEN {0} = 'legendary_fishing_rod' THEN 'legendary_skinning_knife'
            WHEN {0} = 'legacy_fishing_rod' THEN 'legacy_skinning_knife'
            ELSE {0}
        END
        """;

    private const string RetiredItemIds =
        """
        'copper_ore', 'verdant_ore', 'crystalline_powder', 'thick_hide', 'scaled_hide',
        'soulglass_shard', 'mossy_stone', 'deep_stone', 'woven_fiber', 'silk_thread',
        'spectral_thread', 'bone_fragments', 'grave_bone', 'ancient_bone', 'rough_stone',
        'cracked_garnet', 'cave_fish', 'murky_fish_oil', 'bloodwood', 'living_bark',
        'refined_fish_oil', 'shadow_oil', 'hardened_chitin', 'amber_resin', 'living_resin',
        'ant_chitin', 'hive_resin', 'royal_chitin_fragment',
        'basic_fishing_rod', 'fishing_rod', 'rare_fishing_rod', 'epic_fishing_rod',
        'unique_fishing_rod', 'legendary_fishing_rod', 'legacy_fishing_rod'
        """;

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        RemapColumn(migrationBuilder, "ItemInstances", "ItemBaseId");
        RemapColumn(migrationBuilder, "MarketPlaceBuyOrders", "ItemBaseId");
        RemapColumn(migrationBuilder, "MarketPlaceOrders", "ItemBaseId");
        RemapColumn(migrationBuilder, "RunRewards", "ItemId");

        migrationBuilder.Sql(
            $"""
            DELETE FROM "ItemBases"
            WHERE "Id" IN ({RetiredItemIds});
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Retired resource stacks and Fishing tools are deliberately folded into
        // their replacements and cannot be split back into their former variants.
    }

    private static void RemapColumn(
        MigrationBuilder migrationBuilder,
        string table,
        string column)
    {
        var quotedColumn = $"\"{column}\"";
        var mapping = string.Format(ResourceMapping, quotedColumn);

        migrationBuilder.Sql(
            $"""
            UPDATE "{table}"
            SET {quotedColumn} = {mapping}
            WHERE {quotedColumn} IN ({RetiredItemIds});
            """);
    }
}
