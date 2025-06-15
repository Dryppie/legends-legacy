using Domain.Models.LootTables;
using Domain.Models.Professions;
using Domain.Models.Professions.Gathering.GatheringNodes;

namespace Persistence.LL.Seeds.Seeding;
public static class SeedProfessions
{
    public static async Task SeedProfessionsData(this LLDbContext context)
    {
        // Tier‑1
        await SeedMiningLootTables(context);
        await SeedWoodcuttingLootTables(context);

        // Tier‑2
        await SeedMiningTier2LootTables(context);
        await SeedWoodcuttingTier2LootTables(context);

        // Tier-3
        await SeedMiningTier3LootTables(context);
        await SeedWoodcuttingTier3LootTables(context);
    }

    #region Tier-1
    private static async Task SeedMiningLootTables(LLDbContext context)
    {
        /* ────────────────────────────────
         *  Item IDs (Tier‑1 Mining)
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
                new LootTableItem { Id = Guid.NewGuid(), ItemId = STONE_ID, Weight = 20 },
                new LootTableItem { Id = Guid.NewGuid(), ItemId = FLINT_ID, Weight = 20 }
            ],
            tableWeight: 60); // 12 %

        var miningUncommon = MakeItemTable(
            [
                new LootTableItem { Id = Guid.NewGuid(), ItemId = TINY_GEODE_ID, Weight = 10 },
                new LootTableItem { Id = Guid.NewGuid(), ItemId = JAGGED_OBSIDIAN_ID, Weight = 10 }
            ],
            tableWeight: 20); // 9 %

        var miningRare = MakeItemTable(
            [
                new LootTableItem { Id = Guid.NewGuid(), ItemId = CRYSTALLINE_POWDER_ID, Weight = 5 }
            ],
            tableWeight: 10); // 0.15 %

        //var miningEpic = MakeItemTable(
        //    [
        //    ],
        //    tableWeight: 3); // 0.9 %

        //var miningLegendary = MakeItemTable(
        //    [
        //    ],
        //    tableWeight: 1); // 0.03 %

        var miningRoot = BuildLootTable(miningCommon, miningUncommon, miningRare/*, miningEpic, miningLegendary*/);

        /* ────────────────────────────────
         *  Persist
         * ────────────────────────────────*/
        await context.LootTables.AddRangeAsync(miningRoot, miningCommon, miningUncommon,
                                               miningRare/*, miningEpic, miningLegendary*/);

        var miningNode = new GatheringNode
        {
            Id = "mining_slate_shard",
            Name = "Slate Shard",
            LevelRequirement = 1,
            ProfessionType = ProfessionType.Mining,
            LootTableId = miningRoot.Id
        };

        await context.GatheringNodes.AddAsync(miningNode);
    }

    private static async Task SeedWoodcuttingLootTables(LLDbContext context)
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
                new LootTableItem { Id = Guid.NewGuid(), ItemId = WILLOW_LOG_ID, Weight = 20 },
                new LootTableItem { Id = Guid.NewGuid(), ItemId = STICKY_SAP_ID, Weight = 20 }
            ],
            tableWeight: 60); // 16 %

        var willowUncommon = MakeItemTable(
            [
                new LootTableItem { Id = Guid.NewGuid(), ItemId = FEATHER_NEST_ID, Weight = 10 },
                new LootTableItem { Id = Guid.NewGuid(), ItemId = SILK_VINE_ID, Weight = 10 }
            ],
            tableWeight: 20); // 9 %

        var willowRare = MakeItemTable(
            [
                new LootTableItem { Id = Guid.NewGuid(), ItemId = SHIMMER_LEAF_ID, Weight = 5 }
            ],
            tableWeight: 10); // 0.15 %

        //var willowEpic = MakeItemTable(
        //    [
        //    ],
        //    tableWeight: 3); // 0.9 %

        //var willowLegendary = MakeItemTable(
        //    [
        //    ],
        //    tableWeight: 1); // 0.03 %

        var willowRoot = BuildLootTable(willowCommon, willowUncommon, willowRare/*, willowEpic, willowLegendary*/);

        /* ────────────────────────────────
         *  Persist
         * ────────────────────────────────*/
        await context.LootTables.AddRangeAsync(willowRoot, willowCommon, willowUncommon,
                                               willowRare/*, willowEpic, willowLegendary*/);

        var willowGatheringNode = new GatheringNode
        {
            Id = "woodcutting_young_willow",
            Name = "Young Willow",
            LevelRequirement = 1,
            ProfessionType = ProfessionType.Woodcutting,
            LootTableId = willowRoot.Id
        };

        await context.GatheringNodes.AddRangeAsync(willowGatheringNode);
    }
    #endregion

    #region Tier‑2
    private static async Task SeedMiningTier2LootTables(LLDbContext context)
    {
        /* ────────────────────────────────
         *  Item IDs (Tier‑2 Mining ‑ Copperbloom Vein)
         * ────────────────────────────────*/
        const string COPPER_ORE_ID = "copper_ore";
        const string VEINSTONE_CHIP_ID = "veinstone_chip";
        const string MALACHITE_SHARD_ID = "malachite_shard";
        const string VERDANT_ORE_ID = "verdant_ore";
        const string LIVING_AMBER_ID = "living_amber";

        var copperCommon = MakeItemTable(
        [
            new LootTableItem { Id = Guid.NewGuid(), ItemId = COPPER_ORE_ID,     Weight = 20 },
            new LootTableItem { Id = Guid.NewGuid(), ItemId = VEINSTONE_CHIP_ID, Weight = 20 }
        ], 60);

        var copperUncommon = MakeItemTable(
        [
            new LootTableItem { Id = Guid.NewGuid(), ItemId = MALACHITE_SHARD_ID, Weight = 10 },
            new LootTableItem { Id = Guid.NewGuid(), ItemId = VERDANT_ORE_ID,     Weight = 10 }
        ], 20);

        var copperRare = MakeItemTable(
        [
            new LootTableItem { Id = Guid.NewGuid(), ItemId = LIVING_AMBER_ID, Weight = 5 }
        ], 10);

        var copperRoot = BuildLootTable(copperCommon, copperUncommon, copperRare);

        await context.LootTables.AddRangeAsync(copperRoot, copperCommon, copperUncommon, copperRare);

        await context.GatheringNodes.AddAsync(new GatheringNode
        {
            Id = "mining_copperbloom_vein",
            Name = "Copperbloom Vein",
            LevelRequirement = 25,
            ProfessionType = ProfessionType.Mining,
            LootTableId = copperRoot.Id
        });
    }

    private static async Task SeedWoodcuttingTier2LootTables(LLDbContext context)
    {
        /* ────────────────────────────────
         *  Item IDs (Tier‑2 Woodcutting ‑ Amberleaf Maple)
         * ────────────────────────────────*/
        const string MAPLE_LOG_ID = "maple_log";
        const string AMBER_SYRUP_ID = "amber_syrup";
        const string SWEET_BARK_CHIPS_ID = "sweet_bark_chips";
        const string HONEYCOMB_ID = "honeycomb";
        const string GLOWING_AMBER_ID = "glowing_amber";

        var mapleCommon = MakeItemTable(
        [
            new LootTableItem { Id = Guid.NewGuid(), ItemId = MAPLE_LOG_ID,  Weight = 20 },
            new LootTableItem { Id = Guid.NewGuid(), ItemId = AMBER_SYRUP_ID, Weight = 20 }
        ], 60);

        var mapleUncommon = MakeItemTable(
        [
            new LootTableItem { Id = Guid.NewGuid(), ItemId = SWEET_BARK_CHIPS_ID, Weight = 10 },
            new LootTableItem { Id = Guid.NewGuid(), ItemId = HONEYCOMB_ID, Weight = 10 }
        ], 20);

        var mapleRare = MakeItemTable(
        [
            new LootTableItem { Id = Guid.NewGuid(), ItemId = GLOWING_AMBER_ID, Weight = 5 }
        ], 10);

        var mapleRoot = BuildLootTable(mapleCommon, mapleUncommon, mapleRare);

        await context.LootTables.AddRangeAsync(mapleRoot, mapleCommon, mapleUncommon, mapleRare);

        await context.GatheringNodes.AddAsync(new GatheringNode
        {
            Id = "woodcutting_amberleaf_maple",
            Name = "Amberleaf Maple",
            LevelRequirement = 25,
            ProfessionType = ProfessionType.Woodcutting,
            LootTableId = mapleRoot.Id
        });
    }
    #endregion

    #region Tier-3
    private static async Task SeedMiningTier3LootTables(LLDbContext context)
    {
        /* Tier-3 Mining – Tinspine Vein */
        const string TIN_ORE_ID = "tin_ore";
        const string RIVER_PEARL_ID = "river_pearl";
        const string DULL_QUARTZ_ID = "dull_quartz";
        const string GALVANIC_DUST_ID = "galvanic_dust";
        const string FROSTED_METAL_SHARD_ID = "frosted_metal_shard";

        var tinCommon = MakeItemTable(
        [
            new LootTableItem { Id = Guid.NewGuid(), ItemId = TIN_ORE_ID, Weight = 20 },
            new LootTableItem { Id = Guid.NewGuid(), ItemId = RIVER_PEARL_ID, Weight = 20 }
        ], 60);

        var tinUncommon = MakeItemTable(
        [
            new LootTableItem { Id = Guid.NewGuid(), ItemId = DULL_QUARTZ_ID, Weight = 10 },
            new LootTableItem { Id = Guid.NewGuid(), ItemId = GALVANIC_DUST_ID, Weight = 10 }
        ], 20);

        var tinRare = MakeItemTable(
        [
            new LootTableItem { Id = Guid.NewGuid(), ItemId = FROSTED_METAL_SHARD_ID, Weight = 5 }
        ], 10);

        var tinRoot = BuildLootTable(tinCommon, tinUncommon, tinRare);

        await context.LootTables.AddRangeAsync(tinRoot, tinCommon, tinUncommon, tinRare);

        await context.GatheringNodes.AddAsync(new GatheringNode
        {
            Id = "mining_tinspine_vein",
            Name = "Tinspine Vein",
            LevelRequirement = 50,
            ProfessionType = ProfessionType.Mining,
            LootTableId = tinRoot.Id
        });
    }

    private static async Task SeedWoodcuttingTier3LootTables(LLDbContext context)
    {
        /* Tier-3 Woodcutting – Ember Ash */
        const string ASH_LOG_ID = "ash_log";
        const string CHARCOAL_CHUNK_ID = "charcoal_chunk";
        const string FIRE_BEETLE_CARAPACE_ID = "fire_beetle_carapace";
        const string SCORCHED_RESIN_ID = "scorched_resin";
        const string INFERNO_BARK_ID = "inferno_bark";

        var ashCommon = MakeItemTable(
        [
            new LootTableItem { Id = Guid.NewGuid(), ItemId = ASH_LOG_ID, Weight = 20 },
            new LootTableItem { Id = Guid.NewGuid(), ItemId = CHARCOAL_CHUNK_ID, Weight = 20 }
        ], 60);

        var ashUncommon = MakeItemTable(
        [
            new LootTableItem { Id = Guid.NewGuid(), ItemId = FIRE_BEETLE_CARAPACE_ID, Weight = 10 },
            new LootTableItem { Id = Guid.NewGuid(), ItemId = SCORCHED_RESIN_ID, Weight = 10 }
        ], 20);

        var ashRare = MakeItemTable(
        [
            new LootTableItem { Id = Guid.NewGuid(), ItemId = INFERNO_BARK_ID, Weight = 5 }
        ], 10);

        var ashRoot = BuildLootTable(ashCommon, ashUncommon, ashRare);

        await context.LootTables.AddRangeAsync(ashRoot, ashCommon, ashUncommon, ashRare);

        await context.GatheringNodes.AddAsync(new GatheringNode
        {
            Id = "woodcutting_ember_ash",
            Name = "Ember Ash",
            LevelRequirement = 50,
            ProfessionType = ProfessionType.Woodcutting,
            LootTableId = ashRoot.Id
        });
    }
    #endregion

    #region Tier-4
    #endregion

    #region Tier-5
    #endregion

    #region Tier-6
    #endregion

    #region Tier-7
    #endregion

    #region Tier-8
    #endregion

    #region Tier-9
    #endregion

    #region Tier-10
    #endregion

    #region Tier-11
    #endregion


    #region LootTable Creation Helpers
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
    #endregion
}