using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Models.GatheringNodes;
using Domain.Models.LootTables;

namespace Persistence.LL.Seeds.Seeding;
public static class SeedProfessions
{
    public static async Task SeedProfessionsData(this LLDbContext context)
    {
        await SeedMiningLootTables(context);
        await SeedWoodcuttingLootTables(context);
    }

    public static async Task SeedMiningLootTables(LLDbContext context)
    {
        /* ────────────────────────────────
         *  Existing ItemBase IDs
         * ────────────────────────────────*/
        const string STONE_ID = "stone";
        const string FLINT_ID = "flint";
        const string TINY_GEODE_ID = "tiny_geode";
        const string JAGGED_OBSIDIAN_ID = "jagged_obsidian";
        const string CRYSTALLINE_POWDER_ID = "crystalline_powder";

        /* ────────────────────────────────
         *  Mining tiers & weights
         * ────────────────────────────────*/
        var miningCommon = MakeItemTable(
            [
                new LootTableItem { Id = Guid.NewGuid(), ItemId = STONE_ID, Weight = 20 }
            ],
            tableWeight: 80); // 16 %

        var miningUncommon = MakeItemTable(
            [
                new LootTableItem { Id = Guid.NewGuid(), ItemId = FLINT_ID, Weight = 30 }
            ],
            tableWeight: 30); // 9 %

        var miningRare = MakeItemTable(
            [
                new LootTableItem { Id = Guid.NewGuid(), ItemId = TINY_GEODE_ID, Weight = 1 }
            ],
            tableWeight: 15); // 0.15 %

        var miningEpic = MakeItemTable(
            [
                new LootTableItem { Id = Guid.NewGuid(), ItemId = JAGGED_OBSIDIAN_ID, Weight = 30 }
            ],
            tableWeight: 3); // 0.9 %

        var miningLegendary = MakeItemTable(
            [
                new LootTableItem { Id = Guid.NewGuid(), ItemId = CRYSTALLINE_POWDER_ID, Weight = 1 }
            ],
            tableWeight: 1); // 0.03 %

        var miningRoot = BuildLootTable(
            miningCommon, miningUncommon, miningRare, miningEpic, miningLegendary);

        /* ────────────────────────────────
         *  Persist
         * ────────────────────────────────*/
        await context.LootTables.AddRangeAsync(
            miningRoot, miningCommon, miningUncommon,
            miningRare, miningEpic, miningLegendary);

        var miningNode = new GatheringNode
        {
            Id = "mining_slate_shard",
            Name = "Slate Shard",
            GatheringType = GatheringType.Mining,
            LootTableId = miningRoot.Id
        };

        await context.GatheringNodes.AddAsync(miningNode);
    }

    public static async Task SeedWoodcuttingLootTables(LLDbContext context)
    {
        /* ────────────────────────────────
         *  Existing ItemBase IDs
         * ────────────────────────────────*/
        const string WILLOW_LOG_ID = "willow_log";
        const string STICKY_SAP_ID = "sticky_sap";
        const string FEATHER_NEST_ID = "feather_lined_nest";
        const string SILK_VINE_ID = "silk_vine";
        const string SHIMMER_LEAF_ID = "shimmering_leaf";

        /* ────────────────────────────────
         *  Woodcutting tiers & weights
         * ────────────────────────────────*/
        var willowCommon = MakeItemTable(
            [
            new LootTableItem { Id = Guid.NewGuid(), ItemId = WILLOW_LOG_ID, Weight = 20 }
            ],
            tableWeight: 80); // 16 %

        var willowUncommon = MakeItemTable(
            [
                new LootTableItem { Id = Guid.NewGuid(), ItemId = STICKY_SAP_ID, Weight = 30 }
            ],
            tableWeight: 30); // 9 %

        var willowRare = MakeItemTable(
            [
                new LootTableItem { Id = Guid.NewGuid(), ItemId = FEATHER_NEST_ID, Weight = 1 }
            ],
            tableWeight: 15); // 0.15 %

        var willowEpic = MakeItemTable(
            [
                new LootTableItem { Id = Guid.NewGuid(), ItemId = SILK_VINE_ID, Weight = 30 }
            ],
            tableWeight: 3); // 0.9 %

        var willowLegendary = MakeItemTable(
            [
                new LootTableItem { Id = Guid.NewGuid(), ItemId = SHIMMER_LEAF_ID, Weight = 1 }
            ],
            tableWeight: 1); // 0.03 %

        var willowRoot = BuildLootTable(
            willowCommon, willowUncommon, willowRare, willowEpic, willowLegendary);

        /* ────────────────────────────────
         *  Persist
         * ────────────────────────────────*/
        await context.LootTables.AddRangeAsync(willowRoot, willowCommon, willowUncommon,
                                               willowRare, willowEpic, willowLegendary);

        var willowGatheringNode = new GatheringNode
        {
            Id = "woodcutting_young_willow",
            Name = "Young Willow",
            GatheringType = GatheringType.Woodcutting,
            LootTableId = willowRoot.Id
        };

        await context.GatheringNodes.AddRangeAsync(willowGatheringNode);
    }

    private static LootTable BuildLootTable(params LootTable[] subtables) =>
            new() { Id = Guid.NewGuid(), Entries = subtables };

    /// <summary>
    /// Creates a <see cref="LootTable"/> composed of the supplied <paramref name="items"/> and assigns the
    /// weight that this table should have relative to its sibling tables.
    /// </summary>
    /// <param name="items">One or more <see cref="LootTableItem"/> instances to place in the table.</param>
    /// <param name="tableWeight">The weight for this table relative to its siblings.</param>
    private static LootTable MakeItemTable(IEnumerable<LootTableItem> items, int tableWeight) =>
        new()
        {
            Id = Guid.NewGuid(),
            Weight = tableWeight,
            Entries = [.. items]
        };
}