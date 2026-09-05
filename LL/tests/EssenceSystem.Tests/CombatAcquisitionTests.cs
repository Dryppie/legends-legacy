using Domain.Models.Combat;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Regions.Areas;
using Application.Interfaces.Services.LL.Items;
using Microsoft.Extensions.Options;
using Services.LL.Combat.Layers.Rewards.Idle;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Items;

namespace EssenceSystem.Tests;

public sealed class CombatAcquisitionTests
{
    private static readonly DateTimeOffset Epoch = new(2026, 9, 2, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Regional_content_exposes_all_seven_rarities_and_the_selected_drop_rates()
    {
        var catalog = Catalog();

        Assert.Equal(2, catalog.Pools.Count);
        Assert.Equal(31, catalog.DropDefinitions(EquipmentRarity.Common).Count);
        Assert.All(Enum.GetValues<EquipmentRarity>(), rarity =>
            Assert.True(catalog.DropDefinitions(rarity).Count >= 31));
        Assert.Equal(catalog.Equipment.Evaluator.Definitions.Count,
            Enum.GetValues<EquipmentRarity>().Sum(rarity => catalog.DropDefinitions(rarity).Count));
        Assert.All(catalog.Pools, rules =>
        {
            Assert.Equal(0.0003, rules.AreaEquipment.DropChance);
            Assert.Equal(0, rules.AreaEquipment.Rank);
            Assert.Equal(0.2, rules.DungeonEquipment.DropChance);
            Assert.Equal(1, rules.DungeonEquipment.Rank);
            Assert.Equal(1d / 4320d, rules.SigilDropChance, 15);
            Assert.Equal(1d, rules.AreaEquipment.Rarities.Entries().Sum(x => x.Weight), 12);
            Assert.Equal(1d, rules.DungeonEquipment.Rarities.Entries().Sum(x => x.Weight), 12);
            Assert.Equal(new[] { 0d, 0.35d, 0.45d, 0.16d, 0.04d },
                rules.AreaEquipment.Qualities.Entries().Select(x => x.Weight));
            Assert.Equal(new[] { 0d, 0.35d, 0.45d, 0.16d, 0.04d },
                rules.DungeonEquipment.Qualities.Entries().Select(x => x.Weight));
        });
    }

    [Fact]
    public async Task Every_victory_can_drop_any_legacy_item_at_the_regions_tier_and_area_rank()
    {
        var fixture = Fixture.Create(areaChance: 1, areaRarity: EquipmentRarity.Legacy,
            areaQuality: ItemQuality.Masterpiece);
        var result = await fixture.Processor.ProcessAsync(fixture.Facts("region_02_area_04", 64), CancellationToken.None);

        Assert.Equal(64, result.Equipment.Count);
        Assert.All(result.Equipment, item =>
        {
            var equipment = Assert.IsType<EquipmentInstance>(item.ItemInstance).ProgressionData!;
            Assert.Equal(EquipmentRarity.Legacy, equipment.Rarity);
            Assert.Equal(ItemQuality.Masterpiece, equipment.Quality);
            Assert.InRange(equipment.AttributeRollMultiplier, 0.95d, 1.05d);
            Assert.Equal(ItemQuality.Masterpiece, Assert.IsType<EquipmentInstance>(item.ItemInstance).Quality);
            Assert.Equal(2, equipment.State.Tier);
            Assert.Equal(0, equipment.State.Rank);
            Assert.Equal(EquipmentAwardKind.RandomDiscovery, equipment.State.Provenance.Kind);
            Assert.Equal(EquipmentOwnershipKind.UnboundPersonal, equipment.State.Ownership.Kind);
            Assert.Null(equipment.State.ActiveStyleId);
        });
        Assert.Equal(64, fixture.Entitlements.Awards.Count);
    }

    [Fact]
    public async Task Area_equipment_and_sigils_are_batch_independent()
    {
        var characterId = Guid.NewGuid();
        var full = Fixture.Create(characterId, areaChance: 1, sigilChance: 1);
        var split = Fixture.Create(characterId, areaChance: 1, sigilChance: 1);

        var one = await full.Processor.ProcessAsync(full.Facts("region_01_area_01", 120), CancellationToken.None);
        var first = await split.Processor.ProcessAsync(split.Facts("region_01_area_01", 61), CancellationToken.None);
        var second = await split.Processor.ProcessAsync(split.Facts("region_01_area_01", 59, 61), CancellationToken.None);

        Assert.Equal(one.Equipment.Select(ItemJson), first.Equipment.Concat(second.Equipment).Select(ItemJson));
        Assert.Equal(
            one.Sigils.GroupBy(x => x.ItemInstance.ItemBaseId).ToDictionary(x => x.Key, x => x.Sum(y => y.Quantity)),
            first.Sigils.Concat(second.Sigils).GroupBy(x => x.ItemInstance.ItemBaseId)
                .ToDictionary(x => x.Key, x => x.Sum(y => y.Quantity)));
    }

    [Theory]
    [InlineData("region_01_area_01", "sigil_goblin_mines", "sigil_forgotten_catacombs", 1)]
    [InlineData("region_02_area_01", "sigil_tangled_cave", "sigil_great_tree", 2)]
    public async Task Every_supported_area_uses_its_regions_equipment_tier_and_sigil_pool(
        string areaId, string firstSigil, string secondSigil, int tier)
    {
        var fixture = Fixture.Create(areaChance: 1, sigilChance: 1);
        var result = await fixture.Processor.ProcessAsync(fixture.Facts(areaId, 80), CancellationToken.None);

        Assert.All(result.Equipment, item => Assert.Equal(tier,
            Assert.IsType<EquipmentInstance>(item.ItemInstance).ProgressionData!.State.Tier));
        Assert.Equal(80, result.Sigils.Sum(x => x.Quantity));
        Assert.All(result.Sigils, item => Assert.Contains(item.ItemInstance.ItemBaseId, new[] { firstSigil, secondSigil }));
    }

    [Theory]
    [InlineData("training", true, false)]
    [InlineData("region_03_area_01", true, true)]
    [InlineData("region_01_area_01", false, true)]
    public async Task Unsupported_areas_losses_and_disabled_drops_award_nothing(
        string areaId, bool enabled, bool victory)
    {
        var fixture = Fixture.Create(areaChance: 1, sigilChance: 1, enabled: enabled);
        var result = await fixture.Processor.ProcessAsync(
            fixture.Facts(areaId, 10, victory: victory), CancellationToken.None);

        Assert.Empty(result.Equipment);
        Assert.Empty(result.Sigils);
        Assert.Empty(fixture.Entitlements.Awards);
    }

    private static string ItemJson(InventoryItem item) =>
        Assert.IsType<EquipmentInstance>(item.ItemInstance).ProgressionData!.Serialize();

    private static CombatAcquisitionCatalog Catalog()
    {
        var root = ContentRoot();
        var equipment = JsonStarterEquipmentCatalog.Load(Path.Combine(root, "equipment-starters.v1.json"));
        return JsonStarterEquipmentCatalog.LoadOrdinary(equipment, Path.Combine(root, "equipment-ordinary.v1.json"));
    }

    private static string ContentRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "LL/src/API/API.LL/Data/equipment");
            if (Directory.Exists(path)) return path;
        }
        throw new DirectoryNotFoundException();
    }

    private sealed class Fixture
    {
        private Fixture(Guid characterId, CombatAcquisitionCatalog catalog, bool enabled)
        {
            CharacterId = characterId;
            var itemBases = new Dictionary<string, ItemBase>(StringComparer.Ordinal);
            foreach (var option in catalog.Equipment.Options)
            {
                var evaluated = catalog.Equipment.Evaluator.Evaluate(option.DefinitionId, 1, 0, null);
                itemBases.TryAdd(evaluated.Archetype.ItemBaseId, new EquipmentBase
                {
                    Id = evaluated.Archetype.ItemBaseId,
                    Name = option.Name,
                    EquipmentType = option.EquipmentType
                });
            }
            foreach (var sigil in catalog.Pools.SelectMany(x => x.Sigils))
                itemBases.TryAdd(sigil.ItemBaseId, new ItemBase
                {
                    Id = sigil.ItemBaseId,
                    Name = sigil.FamilyId,
                    ItemType = ItemType.Resource,
                    IsBound = true,
                    Stackable = true
                });

            Entitlements = new EntitlementRepository();
            Processor = new CombatAcquisitionRewardProcessor(catalog, new ItemBases(itemBases),
                Options.Create(new EquipmentProgressionOptions { OrdinaryAcquisitionEnabled = enabled }), Entitlements);
        }

        public Guid CharacterId { get; }
        public CombatAcquisitionRewardProcessor Processor { get; }
        public EntitlementRepository Entitlements { get; }

        public static Fixture Create(Guid? characterId = null, double areaChance = 0.0003,
            EquipmentRarity? areaRarity = null, double sigilChance = 1d / 4320d, bool enabled = true,
            ItemQuality? areaQuality = null)
        {
            var source = Catalog();
            var pools = source.Pools.Select(rules => rules with
            {
                AreaEquipment = rules.AreaEquipment with
                {
                    DropChance = areaChance,
                    Rarities = areaRarity.HasValue ? Only(areaRarity.Value) : rules.AreaEquipment.Rarities,
                    Qualities = areaQuality.HasValue ? Only(areaQuality.Value) : rules.AreaEquipment.Qualities
                },
                SigilDropChance = sigilChance
            });
            return new Fixture(characterId ?? Guid.NewGuid(), new CombatAcquisitionCatalog(source.Equipment, pools), enabled);
        }

        public IdleCombatRewardFacts Facts(string areaId, int count, int start = 0, bool victory = true) => new(
            CharacterId,
            Epoch.AddSeconds(start * 10),
            Epoch.AddSeconds((start + count) * 10),
            Epoch.AddSeconds((start + count) * 10),
            TimeSpan.FromSeconds(count * 10),
            new Area { Id = areaId, Name = areaId },
            [CharacterId],
            Enumerable.Range(start, count).Select((value, index) => new IdleEncounterRewardFacts(
                Guid.NewGuid(), index + 1, Epoch.AddSeconds(value * 10),
                victory ? BattleOutcome.Victory : BattleOutcome.Defeat, [], [], null!)).ToArray())
        { ScheduleGeneration = 1 };
    }

    private static EquipmentRarityWeights Only(EquipmentRarity rarity) => new(
        rarity == EquipmentRarity.Common ? 1 : 0,
        rarity == EquipmentRarity.Uncommon ? 1 : 0,
        rarity == EquipmentRarity.Rare ? 1 : 0,
        rarity == EquipmentRarity.Epic ? 1 : 0,
        rarity == EquipmentRarity.Unique ? 1 : 0,
        rarity == EquipmentRarity.Legendary ? 1 : 0,
        rarity == EquipmentRarity.Legacy ? 1 : 0);

    private static EquipmentQualityWeights Only(ItemQuality quality) => new(
        quality == ItemQuality.Crude ? 1 : 0,
        quality == ItemQuality.Standard ? 1 : 0,
        quality == ItemQuality.Fine ? 1 : 0,
        quality == ItemQuality.Exceptional ? 1 : 0,
        quality == ItemQuality.Masterpiece ? 1 : 0);

    private sealed class ItemBases(IReadOnlyDictionary<string, ItemBase> items) : IItemBaseRepository
    {
        public Task<IReadOnlyDictionary<string, ItemBase>> GetItemBasesByIdsAsync(
            IReadOnlyCollection<string> ids, CancellationToken ct) =>
            Task.FromResult<IReadOnlyDictionary<string, ItemBase>>(ids.ToDictionary(id => id, id => items[id]));
        public Task<IReadOnlyDictionary<string, string>> GetEssenceItemBaseIdsByDefinitionIdAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>());
        public Task AddMissingItemBasesAsync(IReadOnlyCollection<ItemBase> itemBases, CancellationToken ct) => Task.CompletedTask;
    }

    public sealed class EntitlementRepository : IPlainEquipmentRepository
    {
        public List<EquipmentData> Awards { get; } = [];
        public Task<IReadOnlyList<PlainEquipmentEntitlement>> GetAsync(Guid characterId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<PlainEquipmentEntitlement>>([]);
        public Task RecordAwardAsync(Guid characterId, EquipmentData award, CancellationToken ct)
        {
            Awards.Add(award);
            return Task.CompletedTask;
        }
    }
}
