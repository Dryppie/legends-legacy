using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Bonuses;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Entities;
using Domain.Models.Essences;
using Domain.Models.Inventories;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Extensions;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Interfaces.Combat.Reward.Dungeon;
using Services.LL.Interfaces.Combat.Reward.Idle;

namespace Services.LL.Combat.Layers.Rewards.Dungeon;

public sealed class DungeonCombatRewardCalculator : IDungeonCombatRewardCalculator
{
    private static readonly EssenceDropRollModifiers BossEssenceDropModifiers = new(
        DropChanceMultiplier: 10,
        PityProgressionMultiplier: 1_000,
        ResonanceCapMultiplier: 10);

    private readonly IBonusService _bonusService;
    private readonly ILootService _lootService;
    private readonly IDungeonRewardBalanceProvider _rewardBalance;
    private readonly IEssenceResonanceService _essenceResonanceService;
    private readonly ICombatGatheringRewardProcessor _gatheringRewardProcessor;

    public DungeonCombatRewardCalculator(
        IBonusService bonusService,
        ILootService lootService,
        IDungeonRewardBalanceProvider rewardBalance,
        IEssenceResonanceService essenceResonanceService,
        ICombatGatheringRewardProcessor gatheringRewardProcessor)
    {
        _bonusService = bonusService;
        _lootService = lootService;
        _rewardBalance = rewardBalance;
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

        var combatExperienceGainBps = factors.Get(BonusKind.CombatExperienceGainBps);
        var baseReward = _rewardBalance.GetEncounterReward(facts.DungeonTier, facts.RoomType);

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
                loot = await _lootService.GenerateIdleCombatLootAsync(
                    encounter.HostileCreatures.Cast<Entity>().ToList(),
                    facts.MonsterLootModifiers.ToDictionary(x => x.Key, x => x.Value),
                    cancellationToken);

                var essenceDrops = await RollEssenceDropsAsync(
                    facts,
                    encounter,
                    factors,
                    cancellationToken);

                if (essenceDrops.Count > 0)
                {
                    loot = loot.Concat(essenceDrops).ToList();
                }

                experience = baseReward.Experience.ApplyPositiveBps(combatExperienceGainBps);
                cinders = baseReward.Cinders;

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

    private async Task<IReadOnlyList<InventoryItem>> RollEssenceDropsAsync(
        DungeonCombatRewardFacts facts,
        DungeonEncounterRewardFacts encounter,
        IReadOnlyDictionary<BonusKind, double> factors,
        CancellationToken cancellationToken)
    {
        var featuredMonsterId = facts.RoomType is RoomType.MiniBoss or RoomType.Boss
            ? facts.FeaturedEssenceMonsterDefinitionId
            : null;
        var featuredCreatures = string.IsNullOrWhiteSpace(featuredMonsterId)
            ? []
            : encounter.HostileCreatures
                .Where(creature => string.Equals(
                    CreatureEssenceSource.GetMonsterDefinitionId(creature),
                    featuredMonsterId,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
        var featuredCreatureIds = featuredCreatures
            .Select(creature => creature.Id)
            .ToHashSet();
        var regularCreatures = encounter.HostileCreatures
            .Where(creature => !featuredCreatureIds.Contains(creature.Id))
            .ToArray();
        var drops = new List<InventoryItem>();

        if (regularCreatures.Length > 0)
        {
            drops.AddRange(await _essenceResonanceService.RollEssenceDropsAsync(
                facts.CharacterId,
                regularCreatures,
                eligible: true,
                cancellationToken,
                factors));
        }

        if (featuredCreatures.Length > 0)
        {
            drops.AddRange(await _essenceResonanceService.RollEssenceDropsAsync(
                facts.CharacterId,
                featuredCreatures,
                eligible: true,
                cancellationToken,
                factors,
                BossEssenceDropModifiers));
        }

        return drops;
    }

}
