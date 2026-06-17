using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Bonuses;
using Domain.Models.Entities;
using Domain.Models.Inventories;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Extensions;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Interfaces.Combat.Reward.Dungeon;
using Services.LL.Interfaces.Combat.Reward.Idle;

namespace Services.LL.Combat.Layers.Rewards.Dungeon;

internal class DungeonCombatRewardCalculator : IDungeonCombatRewardCalculator
{
    private readonly IBonusService _bonusService;
    private readonly ILootService _lootService;
    private readonly ICinderRewardCalculator _cinderRewardCalculator;
    private readonly ISoulstoneRewardCalculator _soulstoneRewardCalculator;
    private readonly IRandomSource _randomSource;
    private readonly IEssenceResonanceService _essenceResonanceService;
    private readonly ICombatGatheringRewardProcessor _gatheringRewardProcessor;

    public DungeonCombatRewardCalculator(
        IBonusService bonusService,
        ILootService lootService,
        ICinderRewardCalculator cinderRewardCalculator,
        ISoulstoneRewardCalculator soulstoneRewardCalculator,
        IRandomSource randomSource,
        IEssenceResonanceService essenceResonanceService,
        ICombatGatheringRewardProcessor gatheringRewardProcessor)
    {
        _bonusService = bonusService;
        _lootService = lootService;
        _cinderRewardCalculator = cinderRewardCalculator;
        _soulstoneRewardCalculator = soulstoneRewardCalculator;
        _randomSource = randomSource;
        _essenceResonanceService = essenceResonanceService;
        _gatheringRewardProcessor = gatheringRewardProcessor;
    }

    public async Task<DungeonCombatCalculatedOutcome> CalculateAsync(
        DungeonCombatRewardFacts facts,
        CancellationToken cancellationToken)
    {
        var factors = await _bonusService.GetAggregatedAsync(
            facts.CharacterId,
            DateTimeOffset.UtcNow,
            cancellationToken);

        var doubleExpChance = factors.Get(BonusKind.CombatDoubleExpChance);

        var encounterOutcomes = new List<DungeonEncounterCalculatedOutcome>(facts.Encounters.Count);
        var totalLoot = new List<InventoryItem>();
        var totalExperience = 0;
        var totalCinders = 0;

        foreach (var encounter in facts.Encounters)
        {
            var experience = 0;
            var cinders = 0;
            IReadOnlyList<InventoryItem> loot = Array.Empty<InventoryItem>();

            if (encounter.IsVictory)
            {
                loot = _lootService.GenerateIdleCombatLootAsync(
                    encounter.HostileCreatures.Cast<Entity>().ToList(),
                    facts.MonsterLootModifiers.ToDictionary(x => x.Key, x => x.Value));

                var essenceDrops = await _essenceResonanceService.RollEssenceDropsAsync(
                    facts.CharacterId,
                    encounter.HostileCreatures,
                    eligible: true,
                    cancellationToken);

                if (essenceDrops.Count > 0)
                {
                    loot = loot.Concat(essenceDrops).ToList();
                }

                experience = encounter.HostileCreatures.Sum(x => x.ExperienceReward);

                if (_randomSource.NextDouble() < (doubleExpChance / 100d))
                {
                    experience *= 2;
                }

                cinders = _cinderRewardCalculator.Calculate(encounter.HostileCreatures);

                totalLoot.AddRange(loot);
                totalExperience += experience;
                totalCinders += cinders;
            }

            encounterOutcomes.Add(new DungeonEncounterCalculatedOutcome(
                EncounterId: encounter.EncounterId,
                ExperienceGained: experience,
                CindersGained: cinders,
                Loot: loot));
        }

        var gatheringRewards = await _gatheringRewardProcessor.ProcessAsync(
            new CombatGatheringRewardFacts(
                facts.CharacterId,
                facts.Encounters.Count(x => x.IsVictory),
                facts.EquippedTool,
                facts.GatheringNodes),
            cancellationToken);

        var gatheringLoot = gatheringRewards
            .SelectMany(x => x.ItemsGained)
            .ToList();

        if (gatheringLoot.Count > 0)
        {
            totalLoot.AddRange(gatheringLoot);
        }

        var totalSoulstones = 5;

        return new DungeonCombatCalculatedOutcome(
            CharacterId: facts.CharacterId,
            TotalExperience: totalExperience,
            TotalCinders: totalCinders,
            TotalSoulstones: totalSoulstones,
            TotalLoot: totalLoot,
            GatheringRewards: gatheringRewards,
            EncounterOutcomes: encounterOutcomes);
    }
}
