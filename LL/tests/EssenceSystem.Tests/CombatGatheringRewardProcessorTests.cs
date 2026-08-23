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
    [Fact]
    public void Matching_tool_bonuses_compound_multiplicatively()
    {
        var tool = new EquippedGatheringTool
        {
            Bonuses =
            [
                new ToolBonusModifier
                {
                    BonusType = ToolBonusType.GatheringYieldPercent,
                    Amount = 10d
                },
                new ToolBonusModifier
                {
                    BonusType = ToolBonusType.GatheringYieldPercent,
                    Amount = 20d
                }
            ]
        };

        Assert.Equal(32d, tool.GetBonus(ToolBonusType.GatheringYieldPercent), 10);
    }

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

    [Fact]
    public async Task General_and_scoped_yield_bonuses_compound_multiplicatively()
    {
        var processor = new CombatGatheringRewardProcessor(
            new RecordingRewardRoller(quantity: 10),
            new StaticItemBaseRepository(),
            new InventoryItemFactory(),
            new FixedRandomSource(0d),
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
                        BonusType = ToolBonusType.GatheringYieldPercent,
                        Amount = 20d
                    },
                    new ToolBonusModifier
                    {
                        BonusType = ToolBonusType.SpecificNodeYieldPercent,
                        Amount = 30d,
                        ScopeId = "ore"
                    }
                ]
            },
            [new CombatGatheringNode("ore", "Ore", GatheringType.Mining, null, 1f, "loot.test")]);

        var reward = Assert.Single(await processor.ProcessAsync(facts, CancellationToken.None));

        Assert.Equal(16, Assert.Single(reward.ItemsGained).Quantity);
    }

    [Fact]
    public async Task Node_yield_multiplier_scales_gathered_quantity()
    {
        var processor = new CombatGatheringRewardProcessor(
            new RecordingRewardRoller(quantity: 15),
            new StaticItemBaseRepository(),
            new InventoryItemFactory(),
            new FixedRandomSource(0d),
            new StaticProfessionService(),
            new NoopLevelingService(),
            new EmptyBonusService());
        var facts = new CombatGatheringRewardFacts(
            Guid.NewGuid(),
            Victories: 1,
            new EquippedGatheringTool
            {
                Name = "Test Pickaxe",
                GatheringType = GatheringType.Mining
            },
            [new CombatGatheringNode(
                "ore",
                "Ore",
                GatheringType.Mining,
                null,
                1f,
                "loot.test",
                YieldMultiplier: 2d / 3d)]);

        var reward = Assert.Single(await processor.ProcessAsync(facts, CancellationToken.None));

        Assert.Equal(10, Assert.Single(reward.ItemsGained).Quantity);
    }

    [Fact]
    public async Task Tool_and_soulstone_rare_bonuses_compound_multiplicatively()
    {
        var rewardRoller = new RecordingRewardRoller();
        var processor = new CombatGatheringRewardProcessor(
            rewardRoller,
            new StaticItemBaseRepository(),
            new InventoryItemFactory(),
            new FixedRandomSource(0d),
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
                        BonusType = ToolBonusType.RareMaterialChancePercent,
                        Amount = 20d
                    }
                ]
            },
            [new CombatGatheringNode("ore", "Ore", GatheringType.Mining, null, 1f, "loot.test")]);
        var bonusFactors = new Dictionary<BonusKind, double>
        {
            [BonusKind.GatheringRareDropChanceRelativeBps] = 3000d
        };

        await processor.ProcessAsync(facts, CancellationToken.None, bonusFactors);

        var rareBonus = Assert.IsType<Dictionary<string, double>>(
            rewardRoller.LastContext!.EntryWeightBonusPercentByTag)["rare"];
        Assert.Equal(56d, rareBonus, 10);
    }

    [Fact]
    public async Task Every_eligible_attempt_awards_fifty_experience_even_when_gathering_fails()
    {
        var professions = new RecordingProfessionService();
        var processor = new CombatGatheringRewardProcessor(
            new StaticRewardRoller(),
            new StaticItemBaseRepository(),
            new InventoryItemFactory(),
            new FixedRandomSource(1d),
            professions,
            new NoopLevelingService(),
            new EmptyBonusService());
        var characterId = Guid.NewGuid();
        var facts = new CombatGatheringRewardFacts(
            characterId,
            Victories: 2,
            new EquippedGatheringTool
            {
                Name = "Test Pickaxe",
                GatheringType = GatheringType.Mining
            },
            [new CombatGatheringNode("ore", "Ore", GatheringType.Mining, null, 0f, "loot.test")]);

        var rewards = await processor.ProcessAsync(facts, CancellationToken.None);

        Assert.Equal(2, rewards.Count);
        Assert.All(rewards, reward =>
        {
            Assert.False(reward.Success);
            Assert.Equal(50, reward.ExperienceGained);
        });
        Assert.Equal(100, professions.Profession.Experience);
        Assert.Equal(1, professions.UpdateCalls);
    }

    [Fact]
    public async Task Multiple_matching_nodes_do_not_multiply_experience_per_encounter()
    {
        var professions = new RecordingProfessionService();
        var processor = new CombatGatheringRewardProcessor(
            new StaticRewardRoller(),
            new StaticItemBaseRepository(),
            new InventoryItemFactory(),
            new FixedRandomSource(1d),
            professions,
            new NoopLevelingService(),
            new EmptyBonusService());
        var facts = new CombatGatheringRewardFacts(
            Guid.NewGuid(),
            Victories: 1,
            new EquippedGatheringTool
            {
                Name = "Test Pickaxe",
                GatheringType = GatheringType.Mining
            },
            [
                new CombatGatheringNode("ore", "Ore", GatheringType.Mining, null, 0f, "loot.ore"),
                new CombatGatheringNode("crystal", "Crystal", GatheringType.Mining, null, 0f, "loot.crystal")
            ]);

        var rewards = await processor.ProcessAsync(facts, CancellationToken.None);

        Assert.Equal(50, rewards.Sum(reward => reward.ExperienceGained));
        Assert.Equal(50, professions.Profession.Experience);
    }

    [Fact]
    public async Task Returned_experience_includes_the_awarded_gathering_experience_bonus()
    {
        var professions = new RecordingProfessionService();
        var processor = new CombatGatheringRewardProcessor(
            new StaticRewardRoller(),
            new StaticItemBaseRepository(),
            new InventoryItemFactory(),
            new FixedRandomSource(1d),
            professions,
            new NoopLevelingService(),
            new EmptyBonusService());
        var facts = new CombatGatheringRewardFacts(
            Guid.NewGuid(),
            Victories: 2,
            new EquippedGatheringTool
            {
                Name = "Test Pickaxe",
                GatheringType = GatheringType.Mining
            },
            [new CombatGatheringNode("ore", "Ore", GatheringType.Mining, null, 0f, "loot.test")]);
        var bonusFactors = new Dictionary<BonusKind, double>
        {
            [BonusKind.GatheringExperienceGainBps] = 2500d
        };

        var rewards = await processor.ProcessAsync(facts, CancellationToken.None, bonusFactors);

        Assert.Equal(125, rewards.Sum(reward => reward.ExperienceGained));
        Assert.Equal(125, professions.Profession.Experience);
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

    private sealed class RecordingRewardRoller(int quantity = 1) : IRewardRoller
    {
        public RewardRollContext? LastContext { get; private set; }

        public RewardRollResult Roll(string rewardTableId, RewardRollContext context)
        {
            LastContext = context;
            return CreateResult();
        }

        public RewardRollResult Roll(RewardTableDefinition table, RewardRollContext context)
        {
            LastContext = context;
            return CreateResult();
        }

        private RewardRollResult CreateResult() => new(
            [new ItemRewardResult("item.test", quantity, "test")],
            0,
            0,
            0,
            []);
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

    private sealed class RecordingProfessionService : IProfessionService
    {
        public Profession Profession { get; } = new() { Level = 1 };
        public int UpdateCalls { get; private set; }

        public Task<Profession> GetOrCreateProfessionAsync(
            Guid characterId,
            ProfessionType professionType,
            CancellationToken cancellationToken)
        {
            Profession.CharacterId = characterId;
            Profession.ProfessionType = professionType;
            return Task.FromResult(Profession);
        }

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
            UpdateCalls++;
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
