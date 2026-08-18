using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Bonuses;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Regions.Areas;
using Services.LL.Combat.Layers.Rewards.Idle;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Inventories;

namespace EssenceSystem.Tests;

public sealed class IdleDungeonSigilDropCalculatorTests
{
    [Fact]
    public async Task RollAsync_uses_the_injected_clock_for_bonus_lookup()
    {
        var fixedNow = DateTimeOffset.Parse("2026-08-18T12:00:00Z");
        var bonusService = new CapturingBonusService();
        var calculator = new IdleDungeonSigilDropCalculator(
            new StaticDungeonDefinitions(
            [
                new DungeonDefinition
                {
                    Id = "test_dungeon",
                    Region = 1,
                    SigilItemId = "item.sigil.test"
                }
            ]),
            new StaticItemBaseRepository([]),
            new QueueRandomSource(0.5),
            new InventoryItemFactory(),
            bonusService,
            new API.LL.Benchmarking.FixedTimeProvider(fixedNow));

        await calculator.RollAsync(
            Guid.NewGuid(),
            new Area { Id = "region_01_area_01" },
            eligibleVictories: 1,
            CancellationToken.None);

        Assert.Equal(fixedNow, bonusService.RequestedAt);
    }

    [Fact]
    public async Task RollAsync_applies_sigil_trace_bonus_as_relative_drop_rate()
    {
        var characterId = Guid.NewGuid();
        var area = new Area { Id = "region_01_area_01" };

        var baseCalculator = CreateCalculator(0);
        var boostedCalculator = CreateCalculator(10000);

        var baseDrops = await baseCalculator.RollAsync(characterId, area, eligibleVictories: 43200, CancellationToken.None);
        var boostedDrops = await boostedCalculator.RollAsync(characterId, area, eligibleVictories: 43200, CancellationToken.None);

        Assert.True(boostedDrops.Sum(x => x.Quantity) > baseDrops.Sum(x => x.Quantity));
    }

    [Fact]
    public async Task RollAsync_only_uses_sigils_from_the_areas_region()
    {
        var regionOneSigil = new ItemBase
        {
            Id = "item.sigil.region-one",
            Name = "Region One Sigil",
            ItemType = ItemType.Resource,
            Stackable = true
        };
        var regionTwoSigil = new ItemBase
        {
            Id = "item.sigil.region-two",
            Name = "Region Two Sigil",
            ItemType = ItemType.Resource,
            Stackable = true
        };
        var calculator = new IdleDungeonSigilDropCalculator(
            new StaticDungeonDefinitions(
            [
                new DungeonDefinition { Id = "region_one_dungeon", Region = 1, SigilItemId = regionOneSigil.Id },
                new DungeonDefinition { Id = "region_two_dungeon", Region = 2, SigilItemId = regionTwoSigil.Id }
            ]),
            new StaticItemBaseRepository([regionOneSigil, regionTwoSigil]),
            new QueueRandomSource(Enumerable.Repeat(0.5, 200).ToArray()),
            new InventoryItemFactory(),
            new StaticBonusService(new Dictionary<BonusKind, double>()));

        var drops = await calculator.RollAsync(
            Guid.NewGuid(),
            new Area { Id = "region_02_area_01" },
            eligibleVictories: 43200,
            CancellationToken.None);

        Assert.NotEmpty(drops);
        Assert.All(drops, drop => Assert.Equal(regionTwoSigil.Id, drop.ItemInstance.ItemBaseId));
    }

    private static IdleDungeonSigilDropCalculator CreateCalculator(double sigilTraceBonusBps)
    {
        return new IdleDungeonSigilDropCalculator(
            new StaticDungeonDefinitions(
            [
                new DungeonDefinition
                {
                    Id = "test_dungeon",
                    RequiredAreaId = "region_01_area_02",
                    SigilItemId = "item.sigil.test"
                }
            ]),
            new StaticItemBaseRepository(
            [
                new ItemBase
                {
                    Id = "item.sigil.test",
                    Name = "Test Sigil",
                    ItemType = ItemType.Resource,
                    Stackable = true
                }
            ]),
            new QueueRandomSource(Enumerable.Repeat(0.5, 200).ToArray()),
            new InventoryItemFactory(),
            new StaticBonusService(new Dictionary<BonusKind, double>
            {
                [BonusKind.DungeonSigilDropRateRelativeBps] = sigilTraceBonusBps
            }));
    }

    private sealed class StaticDungeonDefinitions(IReadOnlyList<DungeonDefinition> definitions) : IDungeonDefinitions
    {
        public DungeonDefinition GetByKey(string key) =>
            definitions.First(x => x.Id.Equals(key, StringComparison.OrdinalIgnoreCase));

        public IReadOnlyList<DungeonDefinition> GetAll() => definitions;
    }

    private sealed class StaticItemBaseRepository(IReadOnlyList<ItemBase> itemBases) : IItemBaseRepository
    {
        private readonly Dictionary<string, ItemBase> _itemBases = itemBases
            .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        public Task<IReadOnlyDictionary<string, ItemBase>> GetItemBasesByIdsAsync(
            IReadOnlyCollection<string> itemIds,
            CancellationToken cancellationToken)
        {
            var result = _itemBases.Values
                .Where(x => itemIds.Contains(x.Id, StringComparer.OrdinalIgnoreCase))
                .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

            return Task.FromResult<IReadOnlyDictionary<string, ItemBase>>(result);
        }

        public Task<IReadOnlyDictionary<string, string>> GetEssenceItemBaseIdsByDefinitionIdAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        public Task<EquipmentBase?> GetCraftableEquipmentBaseAsync(string itemBaseId, CancellationToken cancellationToken) =>
            Task.FromResult<EquipmentBase?>(null);

        public Task AddMissingItemBasesAsync(IReadOnlyCollection<ItemBase> itemBases, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class QueueRandomSource(params double[] values) : IRandomSource
    {
        private readonly Queue<double> _values = new(values);

        public double NextDouble() => _values.Count == 0 ? 0.5 : _values.Dequeue();
    }

    private sealed class StaticBonusService(IReadOnlyDictionary<BonusKind, double> bonuses) : IBonusService
    {
        public ValueTask<IReadOnlyDictionary<BonusKind, double>> GetAggregatedAsync(
            Guid characterId,
            DateTimeOffset now,
            CancellationToken ct = default) =>
            ValueTask.FromResult(bonuses);
    }

    private sealed class CapturingBonusService : IBonusService
    {
        public DateTimeOffset? RequestedAt { get; private set; }

        public ValueTask<IReadOnlyDictionary<BonusKind, double>> GetAggregatedAsync(
            Guid characterId,
            DateTimeOffset now,
            CancellationToken ct = default)
        {
            RequestedAt = now;
            return ValueTask.FromResult<IReadOnlyDictionary<BonusKind, double>>(
                new Dictionary<BonusKind, double>());
        }
    }
}
