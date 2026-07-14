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
}
