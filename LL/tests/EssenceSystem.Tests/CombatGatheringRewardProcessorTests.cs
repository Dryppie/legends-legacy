using Application.Interfaces.Services.LL.Professions;
using Application.Interfaces.Services.LL.Rewards;
using Domain.Models.Bonuses;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Tools;
using Domain.Models.Professions;
using Domain.Models.Professions.Gathering.GatheringNodes;
using Domain.Models.Rewards;
using Services.LL.Combat.Layers.Rewards.Idle;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Inventories;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward;

namespace EssenceSystem.Tests;

public sealed class CombatGatheringRewardProcessorTests
{
    [Theory]
    [InlineData(0.45d, true)]
    [InlineData(0.55d, false)]
    public async Task Node_success_bonus_is_relative_to_base_chance(
        double successRoll,
        bool expectedSuccess)
    {
        var processor = new CombatGatheringRewardProcessor(
            new StaticRewardRoller(),
            new StaticItemBaseRepository(),
            new InventoryItemFactory(),
            new FixedRandomSource(successRoll),
            new StaticProfessionService(),
            new NoopLevelingService(),
            new EmptyBonusService());
        var facts = new CombatGatheringRewardFacts(
            Guid.NewGuid(),
            Victories: 1,
            new EquippedGatheringTool
            {
                Name = "Test Pickaxe",
                GatheringType = GatheringType.Mining,
                Bonuses =
                [
                    new ToolBonusModifier
                    {
                        BonusType = ToolBonusType.NodeSuccessChancePercent,
                        Amount = 25d
                    }
                ]
            },
            [new CombatGatheringNode("ore", "Ore", GatheringType.Mining, null, 0.4f, "loot.test")]);

        var reward = Assert.Single(await processor.ProcessAsync(facts, CancellationToken.None));

        Assert.Equal(expectedSuccess, reward.Success);
    }

    private sealed class StaticRewardRoller : IRewardRoller
    {
        private static readonly RewardRollResult Result = new(
            [new ItemRewardResult("item.test", 1, "test")],
            0,
            0,
            0,
            []);

        public RewardRollResult Roll(string rewardTableId, RewardRollContext context) => Result;
        public RewardRollResult Roll(RewardTableDefinition table, RewardRollContext context) => Result;
    }

    private sealed class StaticItemBaseRepository : IItemBaseRepository
    {
        private static readonly ItemBase ItemBase = new()
        {
            Id = "item.test",
            Name = "Test Item",
            ItemType = ItemType.Resource,
            Stackable = true
        };

        public Task<IReadOnlyDictionary<string, ItemBase>> GetItemBasesByIdsAsync(
            IReadOnlyCollection<string> itemIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, ItemBase>>(
                new Dictionary<string, ItemBase>(StringComparer.OrdinalIgnoreCase)
                {
                    [ItemBase.Id] = ItemBase
                });

        public Task<IReadOnlyDictionary<string, string>> GetEssenceItemBaseIdsByDefinitionIdAsync(
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<EquipmentBase?> GetCraftableEquipmentBaseAsync(
            string itemBaseId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AddMissingItemBasesAsync(
            IReadOnlyCollection<ItemBase> itemBases,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FixedRandomSource(double value) : IRandomSource
    {
        public double NextDouble() => value;
    }

    private sealed class StaticProfessionService : IProfessionService
    {
        public Task<Profession> GetOrCreateProfessionAsync(
            Guid characterId,
            ProfessionType professionType,
            CancellationToken cancellationToken) =>
            Task.FromResult(new Profession
            {
                CharacterId = characterId,
                ProfessionType = professionType,
                Level = 1
            });

        public Task<int> GetProfessionLevelAsync(
            Guid characterId,
            ProfessionType professionType,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<List<Profession>> GetProfessionsAsync(
            Guid characterId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void UpdateProfessionLevel(List<Profession> professions)
        {
        }
    }

    private sealed class NoopLevelingService : ILevelingService
    {
        public Task UpdateCharacterLevel(Character entity, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task UpdateProfessionLevel(Profession profession, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class EmptyBonusService : IBonusService
    {
        public ValueTask<IReadOnlyDictionary<BonusKind, double>> GetAggregatedAsync(
            Guid characterId,
            DateTimeOffset now,
            CancellationToken ct = default) =>
            ValueTask.FromResult<IReadOnlyDictionary<BonusKind, double>>(
                new Dictionary<BonusKind, double>());
    }
}
