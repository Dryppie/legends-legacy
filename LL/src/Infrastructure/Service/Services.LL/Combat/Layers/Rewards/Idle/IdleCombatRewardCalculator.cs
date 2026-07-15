using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Regions;
using Domain.Models.Bonuses;
using Domain.Models.Entities;
using Domain.Models.Inventories;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Extensions;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Interfaces.Combat.Reward.Idle;

namespace Services.LL.Combat.Layers.Rewards.Idle;

public sealed class IdleCombatRewardCalculator : IIdleCombatRewardCalculator
{
    private readonly IBonusService _bonusService;
    private readonly ILootService _lootService;
    private readonly ICinderRewardCalculator _cinderRewardCalculator;
    private readonly ISoulstoneRewardCalculator _soulstoneRewardCalculator;
    private readonly IEssenceResonanceService _essenceResonanceService;
    private readonly IIdleDungeonSigilDropCalculator _sigilDropCalculator;
    private readonly ICombatGatheringRewardProcessor _gatheringRewardProcessor;
    private readonly IAreaExperienceBalanceProvider _areaExperienceBalance;

    public IdleCombatRewardCalculator(
        IBonusService bonusService,
        ILootService lootService,
        ICinderRewardCalculator cinderRewardCalculator,
        ISoulstoneRewardCalculator soulstoneRewardCalculator,
        IEssenceResonanceService essenceResonanceService,
        IIdleDungeonSigilDropCalculator sigilDropCalculator,
        ICombatGatheringRewardProcessor gatheringRewardProcessor,
        IAreaExperienceBalanceProvider areaExperienceBalance)
    {
        _bonusService = bonusService;
        _lootService = lootService;
        _cinderRewardCalculator = cinderRewardCalculator;
        _soulstoneRewardCalculator = soulstoneRewardCalculator;
        _essenceResonanceService = essenceResonanceService;
        _sigilDropCalculator = sigilDropCalculator;
        _gatheringRewardProcessor = gatheringRewardProcessor;
        _areaExperienceBalance = areaExperienceBalance;
    }

    public async Task<IdleCombatCalculatedOutcome> CalculateAsync(
        IdleCombatRewardFacts facts,
        CancellationToken cancellationToken)
    {
        var factors = await _bonusService.GetAggregatedAsync(
            facts.CharacterId,
            facts.RequestedTo,
            cancellationToken);

        var combatExperienceGainBps = factors.Get(BonusKind.CombatExperienceGainBps);
        var defeatExperienceRetentionBps = factors.Get(BonusKind.IdleCombatDefeatExperienceRetentionBps);

        var encounterOutcomes = new List<IdleEncounterCalculatedOutcome>(facts.Encounters.Count);
        var totalLoot = new List<InventoryItem>();
        var totalExperience = 0;
        var totalCinders = 0;
        var sigilEligibleVictories = 0;

        foreach (var encounter in facts.Encounters.OrderBy(x => x.Sequence))
        {
            var creatureCount = encounter.HostileCreatures.Count;
            var areaBaseExperience = _areaExperienceBalance.CalculateEncounterExperience(
                facts.Area.Id,
                creatureCount);
            var bonusAdjustedExperience = areaBaseExperience.ApplyPositiveBps(combatExperienceGainBps);
            var experience = 0;
            var cinders = 0;
            IReadOnlyList<InventoryItem> loot = Array.Empty<InventoryItem>();

            if (encounter.IsVictory)
            {
                loot = await _lootService.GenerateIdleCombatLootAsync(
                    encounter.HostileCreatures.Cast<Entity>().ToList(),
                    [],
                    cancellationToken);

                var essenceDrops = await _essenceResonanceService.RollEssenceDropsAsync(
                    facts.CharacterId,
                    encounter.HostileCreatures,
                    eligible: true,
                    cancellationToken,
                    factors);

                if (essenceDrops.Count > 0)
                {
                    loot = loot.Concat(essenceDrops).ToList();
                }

                sigilEligibleVictories++;

                experience = bonusAdjustedExperience;

                cinders = _cinderRewardCalculator.Calculate(encounter.HostileCreatures);

                totalLoot.AddRange(loot);
                totalCinders += cinders;
            }
            else if (defeatExperienceRetentionBps > 0)
            {
                experience = bonusAdjustedExperience.TakeBpsPortion(defeatExperienceRetentionBps);
            }

            totalExperience += experience;

            encounterOutcomes.Add(new IdleEncounterCalculatedOutcome(
                EncounterId: encounter.EncounterId,
                Sequence: encounter.Sequence,
                CreatureCount: creatureCount,
                AreaBaseExperience: areaBaseExperience,
                BonusAdjustedExperience: bonusAdjustedExperience,
                ExperienceGained: experience,
                CindersGained: cinders,
                Loot: loot));
        }

        var sigilDrops = await _sigilDropCalculator.RollAsync(
            facts.CharacterId,
            facts.Area,
            sigilEligibleVictories,
            cancellationToken,
            factors);

        if (sigilDrops.Count > 0)
        {
            totalLoot.AddRange(sigilDrops);
        }

        var gatheringRewards = await _gatheringRewardProcessor.ProcessAsync(
            new CombatGatheringRewardFacts(
                facts.CharacterId,
                facts.Encounters.Count(x => x.IsVictory),
                facts.EquippedTool,
                facts.Area.GatheringNodes
                    .Select(node => new CombatGatheringNode(
                        node.Id,
                        node.Name,
                        node.Type,
                        node.LevelRequirement,
                        node.ProcChance,
                        node.RewardTableId))
                    .ToArray()),
            cancellationToken,
            factors);

        var gatheringLoot = gatheringRewards
            .SelectMany(x => x.ItemsGained)
            .ToList();

        if (gatheringLoot.Count > 0)
        {
            totalLoot.AddRange(gatheringLoot);
        }

        var totalSoulstones = _soulstoneRewardCalculator.Calculate(
            durationInSeconds: (int)Math.Abs(facts.ProcessedDuration.TotalSeconds),
            dropRatePercent: 0,
            doubleDropChancePercent: 0);

        return new IdleCombatCalculatedOutcome(
            CharacterId: facts.CharacterId,
            From: facts.From,
            ProcessedUntil: facts.ProcessedUntil,
            TotalExperience: totalExperience,
            TotalCinders: totalCinders,
            TotalSoulstones: totalSoulstones,
            TotalLoot: totalLoot,
            GatheringRewards: gatheringRewards,
            EncounterOutcomes: encounterOutcomes);
    }

}
